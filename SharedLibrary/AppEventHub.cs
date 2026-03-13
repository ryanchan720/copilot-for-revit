using System;

namespace SharedLibrary
{
    /// <summary>
    /// 应用程序事件中心 - 作为 Application 和 Command 之间的通信桥梁
    /// </summary>
    public static class AppEventHub
    {
        /// <summary>
        /// 命令完成时触发的事件
        /// </summary>
        public static event EventHandler<CommandCompletedEventArgs> CommandCompleted;

        /// <summary>
        /// 触发命令完成事件
        /// </summary>
        /// <param name="commandId">命令标识符</param>
        /// <param name="result">执行结果</param>
        /// <param name="message">附加消息（可选）</param>
        public static void RaiseCommandCompleted(string commandId, bool result, string message = "")
        {
            CommandCompleted?.Invoke(null, new CommandCompletedEventArgs
            {
                CommandId = commandId,
                Success = result,
                Message = message,
                CompletedTime = DateTime.Now
            });
        }

        /// <summary>
        /// 触发命令完成事件（包含执行信息和扩展元数据）
        /// </summary>
        /// <param name="commandId">命令标识符</param>
        /// <param name="result">执行结果</param>
        /// <param name="message">附加消息（可选）</param>
        /// <param name="metadata">扩展元数据（可选）</param>
        public static void RaiseCommandCompleted(string commandId, bool result, string message = "", System.Collections.Generic.IDictionary<string, object> metadata = null)
        {
            CommandCompleted?.Invoke(null, new CommandCompletedEventArgs
            {
                CommandId = commandId,
                Success = result,
                Message = message,
                Metadata = metadata,
                CompletedTime = DateTime.Now
            });
        }
    }

    /// <summary>
    /// 命令完成事件参数
    /// </summary>
    public class CommandCompletedEventArgs : EventArgs
    {
        public string CommandId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }

        /// <summary>
        /// 扩展元数据，用于未来向后兼容扩展
        /// </summary>
        public System.Collections.Generic.IDictionary<string, object> Metadata { get; set; }

        public DateTime CompletedTime { get; set; }
    }
}
