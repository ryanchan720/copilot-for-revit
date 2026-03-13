using Main.Core.Abstractions;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Main.Core.Services
{
    // 启动与关闭阶段的更新自检与调度
    internal class UpdateService
    {
        private static ILogger<UpdateService> _logger;
        private static bool updateFlag = false;
        private const string UpdateInfoFileName = "update-info.json";
        private const string StagingZipName = "Release.zip"; // 预置的新版本整包压缩文件
        private const string ReplaceScriptName = "update-replace.bat"; // 关闭后执行的替换脚本（负责自解压并覆盖）

        public static void Initialize(ILogger<UpdateService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // 在 ExternalApplication 启动时调用，检查是否存在更高版本并提醒用户
        public static void TryCheckForUpdates()
        {
            if (_logger == null) throw new InvalidOperationException("UpdateService is not initialized. Call UpdateService.Initialize(logger) first.");
            try
            {
                _logger.LogInfo("检查程序更新...");
                var currentVersion = GetCurrentVersion();
                var updateInfoPath = GetUpdateInfoPath();
                if (!File.Exists(updateInfoPath))
                {
                    _logger.LogWarning("更新信息文件不存在，跳过更新检查");
                    return;
                }

                var json = JObject.Parse(File.ReadAllText(updateInfoPath));
                var latestVersionStr = (string)json["version"];
                var packageUrl = (string)json["url"]; // 指向网络盘或HTTP的 Release.zip
                if (string.IsNullOrWhiteSpace(latestVersionStr)) return;

                Version latestVersion;
                if (!Version.TryParse(latestVersionStr, out latestVersion)) return;

                if (latestVersion > currentVersion)
                {
                    _logger.LogInfo($"检测到新版本：{latestVersion}，当前版本：{currentVersion}");
                    // 下载/复制 Release.zip 到预置位置
                    var dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                    var updateDir = Path.Combine(dllDir, "Configs", "Updates");
                    var stagingDir = Path.Combine(updateDir, "Staging");
                    Directory.CreateDirectory(stagingDir);
                    var stagingZipPath = Path.Combine(stagingDir, StagingZipName);

                    if (!string.IsNullOrWhiteSpace(packageUrl))
                    {
                        var isFilePath = packageUrl.StartsWith("\\\\") || packageUrl.StartsWith("file:", StringComparison.OrdinalIgnoreCase) || Path.IsPathRooted(packageUrl);
                        if (isFilePath)
                        {
                            // 处理共享路径或本地路径：直接复制
                            string sourcePath = packageUrl;
                            if (packageUrl.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    var uri = new Uri(packageUrl);
                                    sourcePath = uri.LocalPath;
                                }
                                catch { /* 保留原始字符串作为回退 */ }
                            }

                            if (!File.Exists(sourcePath))
                            {
                                _logger.LogError($"源文件不存在：{sourcePath}");
                                return;
                            }
                            File.Copy(sourcePath, stagingZipPath, true);
                            updateFlag = true;
                        }
                        else
                        {
                            using (var webClient = new System.Net.WebClient())
                            {
                                webClient.DownloadFile(packageUrl, stagingZipPath);
                                updateFlag = true;
                            }
                        }
                    }

                    _logger.LogInfo("更新包已下载完成，重启 Revit 后将自动更新");
                }
                else
                {
                    _logger.LogInfo("未发现新版本");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"更新检查失败: {ex.Message}");
            }
        }

        // 在 ExternalApplication 关闭时调用，若存在已下载的新版本，则调度离线替换脚本
        public static void TrySchedulePostShutdownUpdate()
        {
            if (updateFlag == false) return;
            _logger.LogInfo("发起自动更新...");
            try
            {
                var dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                var configsDir = Path.Combine(dllDir, "Configs");
                var updateDir = Path.Combine(configsDir, "Updates");
                var stagingZip = Path.Combine(updateDir, "Staging", StagingZipName);
                var targetDir = dllDir; // 解压覆盖目标为主程序目录
                var scriptPath = Path.Combine(updateDir, ReplaceScriptName);

                if (!File.Exists(stagingZip))
                {
                    _logger.LogWarning("无待替换的更新包，自动更新取消");
                    return; // 无待替换的新版本
                }
                if (!File.Exists(scriptPath))
                {
                    _logger.LogWarning("更新替换脚本未找到，自动更新取消");
                    return; // 脚本未部署
                }

                // 直接启动批处理脚本，并设置工作目录为脚本所在目录，以避免路径/相对引用问题
                var psi = new ProcessStartInfo
                {
                    FileName = scriptPath,
                    Arguments = $"\"{stagingZip}\" \"{targetDir}\"",
                    WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? string.Empty,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                };
                Process.Start(psi);
                _logger.LogInfo("更新脚本已发起执行");
            }
            catch (Exception ex)
            {
                _logger.LogError($"更新脚本发起失败: {ex.Message}");
            }
        }

        private static Version GetCurrentVersion()
        {
            // 优先使用 AssemblyFileVersion（用于更新依据）
            var fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            Version fileVer;
            if (Version.TryParse(fvi.FileVersion, out fileVer))
            {
                return fileVer;
            }

            // 回退到 AssemblyVersion
            var asmVer = Assembly.GetExecutingAssembly().GetName().Version;
            return asmVer ?? new Version(0, 0, 0, 0);
        }

        private static string GetUpdateInfoPath()
        {
            // update-info.json 位于共享网盘中，便于集中更新管理
            var networkConfigsDir = Path.Combine(@"TARGET_FOLDER\RevitCopilot\Updates"); // hardcode flag
            return Path.Combine(networkConfigsDir, UpdateInfoFileName);
        }
    }
}
