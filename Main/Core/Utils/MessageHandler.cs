using System;
using Main.Core.Abstractions;
using Newtonsoft.Json;
using Main.Core.Services; // 新增 CommandResultSync
using SharedLibrary;

namespace Main.Core.Utils
{
    internal class MessageHandler
    {
        ILogger<MessageHandler> _logger;

        public MessageHandler(ILogger<MessageHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 命令完成回调函数
        /// </summary>
        public void OnCommandCompleted(object sender, CommandCompletedEventArgs e)
        {
            // Build a JSON payload containing the event data (including Metadata)
            var payload = new
            {
                commandId = e.CommandId,
                success = e.Success,
                completedTime = e.CompletedTime,
                message = e.Message,
                metadata = e.Metadata
            };

            string fullMessage;
            try
            {
                fullMessage = JsonConvert.SerializeObject(payload);
            }
            catch
            {
                // Fallback to a minimal JSON-like string if serialization fails
                fullMessage = "{\"commandId\":\"" + e.CommandId + "\",\"success\":" + (e.Success ? "true" : "false") + "}";
            }

            _logger.LogInfo(fullMessage);

            // 构造返回 AI 的结果并设置到同步上下文
            var resultObject = new
            {
                success = e.Success,
                commandId = e.CommandId,
                message = e.Message,
                completedTime = e.CompletedTime
            };
            CommandResultSync.SetResult(resultObject);
        }
    }
}
