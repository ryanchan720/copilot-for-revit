using Main.Core.Abstractions;
using System;
using System.IO;
using System.Reflection;

namespace Main.Core.Utils
{
    // 泛型 FileLogger<T>，自动带上类名
    public class FileLogger<T> : ILogger<T>
    {
        public enum LogLevel
        {
            DBG,
            INF,
            WAR,
            ERR
        }

        private readonly string _logDir;
        private LogLevel _currentLogLevel = LogLevel.INF;
        private readonly string _typeName;

        public FileLogger()
        {
            // 获取当前执行 dll 的路径
            string dllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _logDir = Path.Combine(dllPath, "User", "logs");
            _typeName = typeof(T).Name;
        }

        private void Log(LogLevel level, string message)
        {
            if (level < _currentLogLevel)
                return;

            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] [{_typeName}] {message}";

            System.Diagnostics.Debug.WriteLine(logEntry);
            try
            {
                Directory.CreateDirectory(_logDir);
                string logFilePath = Path.Combine(_logDir, $"{DateTime.Now:yyyyMMdd}.txt");
                File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            }
            catch { }
        }

        public void LogDebug(string message) => Log(LogLevel.DBG, message);
        public void LogInfo(string message) => Log(LogLevel.INF, message);
        public void LogWarning(string message) => Log(LogLevel.WAR, message);
        public void LogError(string message) => Log(LogLevel.ERR, message);
    }
}
