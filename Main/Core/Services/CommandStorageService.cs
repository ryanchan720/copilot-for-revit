using Newtonsoft.Json.Linq;

namespace Main.Core.Services
{
    /// <summary>
    /// A storage for command parameters shared across threads.
    /// Socket 接收线程写入，Revit 主线程读取并注入后清理。
    /// </summary>
    public static class CommandStorageService
    {
        private static readonly object _lock = new object();
        private static JObject _commandParams; // 从 ThreadLocal 修改为全局共享

        /// <summary>
        /// Sets the parameters for the next command execution (thread-safe).
        /// </summary>
        public static void SetCommandParams(JObject commandParams)
        {
            lock (_lock)
            {
                _commandParams = commandParams;
            }
        }

        /// <summary>
        /// Gets current stored parameters (thread-safe).
        /// </summary>
        public static JObject GetCommandParams()
        {
            lock (_lock)
            {
                return _commandParams;
            }
        }

        /// <summary>
        /// Clears stored parameters (thread-safe).
        /// </summary>
        public static void ClearCommandParams()
        {
            lock (_lock)
            {
                _commandParams = null;
            }
        }
    }
}
