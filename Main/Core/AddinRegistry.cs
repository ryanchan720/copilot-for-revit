using Main.Core.Abstractions;
using Main.Core.Models;
using Main.Core.Services.Mcp;
using Main.Core.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Main
{
    public class AddinRegistry
    {
        private static readonly object _registryFileLock = new object();
        private readonly bool _marketEnabled;

        private readonly ILogger<AddinRegistry> _logger;
        private readonly FolderMonitor.ProductionFolderMonitor _monitor;
        private readonly string _rootDirectory;
        private readonly int? _currentRevitVersion;
        private readonly string _currentRevitLanguage;

        private readonly ConcurrentDictionary<string, Dictionary<string, McpCommandInfo>> _readmeCache =
            new ConcurrentDictionary<string, Dictionary<string, McpCommandInfo>>();

        // RegisteredAddins 用于管理本机注册
        public ConcurrentBag<IAddin> RegisteredAddins { get; } = new ConcurrentBag<IAddin>();

        // MarketManifest 用于管理共享目录中的插件清单
        public string MarketManifestDirectory { get; }
        public string MarketManifestPath { get; }
        public MarketAddinManifest MarketManifest { get; }

        public string AddRegistryPath => Path.Combine(_rootDirectory, "User", "AddinRegistry.json");

        public AddinRegistry(FolderMonitor.ProductionFolderMonitor monitor, ILogger<AddinRegistry> logger)
            : this(monitor, logger, Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), null, null)
        {
        }

        public AddinRegistry(FolderMonitor.ProductionFolderMonitor monitor, ILogger<AddinRegistry> logger, string rootDirectory)
            : this(monitor, logger, rootDirectory, null, null)
        {
        }

        public AddinRegistry(FolderMonitor.ProductionFolderMonitor monitor, ILogger<AddinRegistry> logger, string rootDirectory, int? currentRevitVersion, string currentRevitLanguage)
        {
            if (monitor == null) throw new ArgumentNullException(nameof(monitor));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrEmpty(rootDirectory)) throw new ArgumentException("Root directory is required", nameof(rootDirectory));

            _logger = logger;
            _monitor = monitor;
            _rootDirectory = rootDirectory;
            _currentRevitVersion = currentRevitVersion;
            _currentRevitLanguage = currentRevitLanguage;

            // 默认 MarketManifest 配置基于共享目录
            MarketManifestDirectory = @"TARGET_FOLDER\RevitCopilot\SharedAddins"; // hardcode flag
            MarketManifestPath = Path.Combine(MarketManifestDirectory, "MarketAddinManifest.json");
            try
            {
                MarketManifest = MarketAddinManifest.LoadFromFile(MarketManifestPath);
                _marketEnabled = true;
                _logger.LogInfo("共享市场清单加载成功");
            }
            catch (Exception ex)
            {
                MarketManifest = new MarketAddinManifest();
                _marketEnabled = false;
                _logger.LogWarning($"共享市场不可用，已降级为本地模式: {ex.Message}");
            }

            _monitor.FolderCreated += async (s, e) => await NewAddinHandlerAsync(s, e);
            _logger.LogInfo("AddinRegistry 初始化完成");
        }

        // 递归复制目录到目标位置
        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var targetFilePath = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFilePath, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var targetSubDir = Path.Combine(targetDir, dirName);
                CopyDirectory(dir, targetSubDir);
            }
        }

        private static async Task WaitForDirectoryReadyAsync(string directoryPath, int maxRetries, int delayMs)
        {
            for (var i = 0; i < maxRetries; i++)
            {
                try
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        throw new DirectoryNotFoundException(directoryPath);
                    }

                    foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
                    {
                        using (File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                        }
                    }

                    return;
                }
                catch (IOException)
                {
                    await Task.Delay(delayMs);
                }
                catch (UnauthorizedAccessException)
                {
                    await Task.Delay(delayMs);
                }
            }
        }

        private static async Task<string> SafeMoveFolderWithSuffixAsync(string sourcePath, ILogger<AddinRegistry> logger)
        {
            const int maxRetries = 5;
            const int delayMs = 1000;

            var dirInfo = new DirectoryInfo(sourcePath);
            var originalFolderName = dirInfo.Name;

            await WaitForDirectoryReadyAsync(sourcePath, maxRetries, delayMs);

            var uniqueSuffix = "-(" + Guid.NewGuid().ToString("N").Substring(0, 6) + ")";
            var newFolderName = originalFolderName + uniqueSuffix;
            var parentDir = Path.GetDirectoryName(sourcePath);
            var newFolderPath = Path.Combine(parentDir, newFolderName);

            for (var i = 0; i < maxRetries; i++)
            {
                try
                {
                    Directory.Move(sourcePath, newFolderPath);
                    return newFolderPath;
                }
                catch (IOException ex)
                {
                    logger.LogWarning($"尝试移动插件文件夹失败 (重试 {i + 1}/{maxRetries}): {ex.Message}");
                    await Task.Delay(delayMs);
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.LogWarning($"尝试移动插件文件夹被拒绝 (重试 {i + 1}/{maxRetries}): {ex.Message}");
                    await Task.Delay(delayMs);
                }
            }

            throw new IOException(string.Format("在重试 {0} 次后仍无法移动插件文件夹: {1}", maxRetries, sourcePath));
        }

        public bool TryGetCommand(string commandName, out Command command)
        {
            command = null;
            foreach (var addin in RegisteredAddins)
            {
                command = addin.ItemList.OfType<Command>()
                    .FirstOrDefault(c => c.Name == commandName && IsCommandCompatible(c));
                if (command != null)
                {
                    _logger.LogInfo($"找到命令: {command.Name} ({command.FullClassName})");
                    return true;
                }
            }

            _logger.LogWarning($"未找到命令: {commandName}");
            return false;
        }

        /// <summary>
        /// 根据命令名称从共享目录中查找对应插件并下载
        /// </summary>
        public bool TryGetCommandFromMarket(string commandName, out Command command)
        {
            command = null;

            if (!_marketEnabled)
            {
                _logger.LogWarning("共享市场未启用，跳过市场命令查找");
                return false;
            }

            foreach (var addin in MarketManifest)
            {
                var addinCommands = addin.Value.Keys;
                var marketCommand = addinCommands.FirstOrDefault(c => c == commandName);
                if (marketCommand != null)
                {
                    _logger.LogInfo($"在共享目录中插件命令: {commandName}，尝试下载");
                    var downloadSuccess = DownloadAddin(addin.Key);
                    if (!downloadSuccess)
                    {
                        _logger.LogWarning($"下载失败: {addin.Key}");
                        return false;
                    }

                    _logger.LogInfo($"已下载: {addin.Key}");

                    // 下载后，系统将自动通过文件监视器注册插件。
                    // 由于注册是异步的，需要等待一段时间以确保插件已注册，然后再获取命令。
                    Thread.Sleep(4000);

                    if (TryGetCommand(commandName, out command))
                    {
                        return true;
                    }

                    _logger.LogWarning($"下载成功但未能在本地找到命令: {commandName}");
                    return false;
                }
            }

            _logger.LogWarning($"在共享目录中未找到命令: {commandName}");
            return false;
        }

        private bool DownloadAddin(string addinKey)
        {
            if (!_marketEnabled)
            {
                _logger.LogWarning("共享市场未启用，跳过插件下载");
                return false;
            }

            // 在共享目录中查找对应名称的文件夹
            var addinFolderPath = Path.Combine(MarketManifestDirectory, addinKey);
            if (!Directory.Exists(addinFolderPath))
            {
                _logger.LogWarning($"共享目录中未找到插件文件夹: {addinKey}");
                return false;
            }

            var targetPath = Path.Combine(_rootDirectory, "Addins", addinKey);
            CopyDirectory(addinFolderPath, targetPath);
            return true;
        }

        private async Task NewAddinHandlerAsync(object sender, FolderMonitor.FolderCreatedEventArgs e)
        {
            // 等待 2 秒，确保文件写入完成
            await Task.Delay(2000);

            var folderPath = e.FilePath;
            var folderName = new DirectoryInfo(folderPath).Name;

            // 为避免名称冲突，需要添加防撞码后缀

            // 记录文件夹原始名称
            var originalFolderName = new DirectoryInfo(e.FilePath).Name;

            // 检查是否已包含防撞码后缀，格式为 "-(xxxxxx)"，其中 x 为十六进制字符
            const string suffixFormat = "-(xxxxxx)";
            var suffixLength = suffixFormat.Length;

            var hasSuffix = false;
            if (originalFolderName.Length > suffixLength)
            {
                var suffix = originalFolderName.Substring(originalFolderName.Length - suffixLength, suffixLength);

                if (suffix.StartsWith("-(") && suffix.EndsWith(")"))
                {
                    var hexPart = suffix.Substring(2, 6);
                    hasSuffix = hexPart.All(c =>
                        (c >= '0' && c <= '9') ||
                        (c >= 'a' && c <= 'f') ||
                        (c >= 'A' && c <= 'F'));
                }
            }

            if (!hasSuffix)
            {
                var newFolderPath = await SafeMoveFolderWithSuffixAsync(e.FilePath, _logger);
                folderPath = newFolderPath;
                folderName = new DirectoryInfo(newFolderPath).Name;
                _logger.LogInfo($"插件已添加防撞码: {folderName}");
            }
            else
            {
                _logger.LogInfo($"插件已有防撞码: {originalFolderName}");
            }

            // 在文件夹中搜索同名 DLL 文件（去除后缀）和 README 文件
            var dllFiles = Directory.EnumerateFiles(folderPath, "*.dll").ToArray();
            var readmeFile = Directory.EnumerateFiles(folderPath, "README.md").FirstOrDefault();

            Dictionary<string, McpCommandInfo> readmeInfo = null;
            if (!string.IsNullOrEmpty(readmeFile))
            {
                readmeInfo = GetCachedReadme(folderPath);
                if (readmeInfo.Count > 0 && !HasAnyCompatibleCommand(readmeInfo))
                {
                    _logger.LogInfo($"插件已跳过（与当前 Revit 版本/语言不兼容）: {folderName}");
                    return;
                }
            }

            // 没有 DLL 文件则跳过
            if (dllFiles.Length == 0)
            {
                _logger.LogWarning("插件文件夹中未发现 DLL 文件");
                return;
            }

            #region 本地注册插件
            IAddin addin = null;
            foreach (var dllFile in dllFiles)
            {
                // 如果 dll 文件名与文件夹（后缀不计）不同名，则跳过
                var folderBaseName = folderName;
                var lastHyphenIndex = folderName.LastIndexOf('-');
                if (lastHyphenIndex > 0)
                {
                    // 只去掉最后一个连字符后的防撞码，保留前面的连字符
                    folderBaseName = folderName.Substring(0, lastHyphenIndex);
                }

                if (Path.GetFileNameWithoutExtension(dllFile) != folderBaseName)
                {
                    continue;
                }

                addin = AddinScanner.ScanAddin(dllFile);
                if (addin != null)
                {
                    FilterIncompatibleCommands(addin, readmeInfo);
                    if (addin.ItemList == null || addin.ItemList.Count == 0)
                    {
                        _logger.LogInfo($"插件中无兼容命令，已跳过注册: {folderName}");
                        continue;
                    }

                    RegisteredAddins.Add(addin);
                    //SaveRegistry(AddRegistryPath); // 暂不保存到文件，存在权限问题
                    _logger.LogInfo($"已在本地注册插件: {addin.Name}");
                }
                else
                {
                    _logger.LogWarning("DLL 文件中未找到有效的命令");
                    return;
                }
            }
            #endregion
        }

        public void SaveRegistry(string filePath)
        {
            var json = JsonConvert.SerializeObject(RegisteredAddins, Formatting.Indented,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempFilePath = filePath + ".tmp";

            lock (_registryFileLock)
            {
                File.WriteAllText(tempFilePath, json, Encoding.UTF8);

                const int maxRetries = 5;
                const int retryDelayMs = 500;
                Exception lastException = null;

                for (var i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                        File.Move(tempFilePath, filePath);
                        lastException = null;
                        break;
                    }
                    catch (IOException ex)
                    {
                        lastException = ex;
                        Thread.Sleep(retryDelayMs);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        lastException = ex;
                        Thread.Sleep(retryDelayMs);
                    }
                }

                if (lastException != null)
                {
                    throw lastException;
                }
            }

            _logger.LogInfo("AddinRegistry 文件已更新");
        }

        public void LoadRegistry(string filePath)
        {
            lock (_registryFileLock)
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"AddinRegistry 文件不存在: {filePath}");
                    return;
                }

                var json = File.ReadAllText(filePath);

                try
                {
                    var addins = JsonConvert.DeserializeObject<List<IAddin>>(json,
                        new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.Auto
                        });
                    // Workaround for clearing a ConcurrentBag since it is read-only
                    while (!RegisteredAddins.IsEmpty)
                    {
                        RegisteredAddins.TryTake(out _);
                    }
                    foreach (var addin in addins)
                    {
                        RegisteredAddins.Add(addin);
                    }
                    _logger.LogInfo($"已从注册表文件加载插件，共计: {RegisteredAddins.Count} 个");
                }
                catch (Exception)
                {
                    _logger.LogWarning($"插件注册表文件为空或无法读取: {filePath}");
                }
            }
        }

        private Dictionary<string, McpCommandInfo> GetCachedReadme(string folder)
        {
            return _readmeCache.GetOrAdd(folder, f => ReadmeParser.Parse(Path.Combine(f, "README.md")));
        }

        private bool IsCommandCompatible(Command command)
        {
            if (command == null || string.IsNullOrEmpty(command.AssemblyPath)) return true;

            var folder = Path.GetDirectoryName(command.AssemblyPath);
            if (string.IsNullOrEmpty(folder)) return true;

            var parsed = GetCachedReadme(folder);
            if (parsed == null || parsed.Count == 0) return true;

            McpCommandInfo info;
            if (!parsed.TryGetValue(GetSimpleClassName(command.FullClassName), out info)) return true;

            return IsInfoCompatible(info);
        }

        private bool HasAnyCompatibleCommand(Dictionary<string, McpCommandInfo> readmeInfo)
        {
            foreach (var pair in readmeInfo)
            {
                var info = pair.Value;
                if (IsInfoCompatible(info)) return true;
            }
            return false;
        }

        private void FilterIncompatibleCommands(IAddin addin, Dictionary<string, McpCommandInfo> readmeInfo)
        {
            if (addin == null || addin.ItemList == null || addin.ItemList.Count == 0) return;
            if (readmeInfo == null || readmeInfo.Count == 0) return;

            addin.ItemList = addin.ItemList
                .Where(node =>
                {
                    var command = node as Command;
                    if (command == null) return true;

                    McpCommandInfo info;
                    if (!readmeInfo.TryGetValue(GetSimpleClassName(command.FullClassName), out info)) return true;
                    return IsInfoCompatible(info);
                })
                .ToList();
        }

        private bool IsInfoCompatible(McpCommandInfo info)
        {
            if (info == null) return true;

            var versionOk = !_currentRevitVersion.HasValue || info.IsCompatibleWith(_currentRevitVersion.Value);
            var languageOk = string.IsNullOrWhiteSpace(_currentRevitLanguage) || info.IsLanguageCompatibleWith(_currentRevitLanguage);
            return versionOk && languageOk;
        }

        private static string GetSimpleClassName(string fullClassName)
        {
            if (string.IsNullOrEmpty(fullClassName)) return string.Empty;
            var lastDot = fullClassName.LastIndexOf('.');
            return lastDot >= 0 ? fullClassName.Substring(lastDot + 1) : fullClassName;
        }
    }
}