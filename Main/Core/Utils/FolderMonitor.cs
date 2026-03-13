using Main.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;

namespace Main.Core.Utils
{
    public class FolderMonitor
    {
        public class FolderCreatedEventArgs : EventArgs
        {
            public string FilePath { get; }
            public DateTime CreatedTime { get; }

            public FolderCreatedEventArgs(string filePath)
            {
                FilePath = filePath;
                CreatedTime = DateTime.Now;
            }
        }

        public class ProductionFolderMonitor : IDisposable
        {
            private static readonly List<ProductionFolderMonitor> _instances = new List<ProductionFolderMonitor>();
            private readonly FileSystemWatcher _watcher;

            public event EventHandler<FolderCreatedEventArgs> FolderCreated;

            private readonly ILogger<ProductionFolderMonitor> _logger;
            private readonly string _monitorFolderPath;

            public ProductionFolderMonitor(ILogger<ProductionFolderMonitor> logger, string monitorFolderPath)
            {
                _logger = logger;
                _monitorFolderPath = monitorFolderPath;

                _watcher = new FileSystemWatcher
                {
                    Path = _monitorFolderPath,
                    Filter = "",
                    NotifyFilter = NotifyFilters.DirectoryName,
                    IncludeSubdirectories = false
                };

                _watcher.Created += OnFolderChanged;
                //_watcher.Renamed += OnFolderChanged;
                _watcher.Error += OnError;

                lock (_instances)
                {
                    _instances.Add(this);
                }
            }

            public void ScanExistingFiles()
            {
                _logger.LogInfo($"扫描已有文件: {_monitorFolderPath}");
                try
                {
                    var folders = Directory.GetDirectories(_monitorFolderPath);
                    foreach (var folder in folders)
                    {
                        _logger.LogInfo($"发现已有文件夹: {Path.GetFileName(folder)}");
                        FolderCreated?.Invoke(this, new FolderCreatedEventArgs(folder));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"扫描已有文件时发生错误: {ex}");
                }
            }

            public void Start()
            {
                ScanExistingFiles();
                _watcher.EnableRaisingEvents = true;
                _logger.LogInfo($"文件夹监视已启动: {_watcher.Path}");
            }

            public void Stop()
            {
                _watcher.EnableRaisingEvents = false;
                _logger.LogInfo("文件夹监视已停止");
            }

            private void OnFolderChanged(object sender, FileSystemEventArgs e)
            {
                _logger.LogInfo($"检测到文件夹变更: {e.Name}");
                FolderCreated?.Invoke(this, new FolderCreatedEventArgs(e.FullPath));
            }

            private void OnError(object sender, ErrorEventArgs e)
            {
                _logger.LogError($"文件夹监视发生错误：{e.GetException()}");
            }

            public void Dispose()
            {
                _logger.LogInfo("文件夹监视器正在释放资源...");
                _watcher?.Dispose();
                lock (_instances)
                {
                    _instances.Remove(this);
                }
                _logger.LogInfo("文件夹监视器已释放");
            }

            public static void DisposeAll()
            {
                lock (_instances)
                {
                    foreach (var instance in _instances.ToArray())
                    {
                        instance.Stop();
                        instance.Dispose();
                    }
                    _instances.Clear();
                }
            }
        }
    }
}
