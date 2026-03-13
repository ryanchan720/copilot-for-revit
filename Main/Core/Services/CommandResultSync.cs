using System;
using System.Threading;

namespace Main.Core.Services
{
    /// <summary>
    /// 用于同步等待 Revit 命令完成事件并获取结果的共享上下文。
    /// </summary>
    public static class CommandResultSync
    {
        private static readonly object _lock = new object();
        private static ManualResetEventSlim _waitHandle;
        private static object _result;

        /// <summary>
        /// 准备一次新的等待上下文（不可重入）。
        /// </summary>
        public static void Prepare()
        {
            lock (_lock)
            {
                _waitHandle?.Dispose();
                _waitHandle = new ManualResetEventSlim(false);
                _result = null;
            }
        }

        /// <summary>
        /// 设置命令完成结果并唤醒等待线程。
        /// </summary>
        public static void SetResult(object result)
        {
            lock (_lock)
            {
                if (_result == null)
                {
                    _result = result;
                }
                _waitHandle?.Set();
            }
        }

        /// <summary>
        /// 阻塞等待结果。
        /// </summary>
        public static object Wait(TimeSpan timeout)
        {
            ManualResetEventSlim wh;
            lock (_lock)
            {
                wh = _waitHandle;
            }
            if (wh == null)
            {
                return new { success = false, message = "未准备等待上下文" };
            }
            if (!wh.Wait(timeout))
            {
                return new { success = false, message = "等待命令完成事件超时" };
            }
            lock (_lock)
            {
                return _result ?? new { success = false, message = "命令完成但未返回结果" };
            }
        }

        /// <summary>
        /// 清理上下文。
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _waitHandle?.Dispose();
                _waitHandle = null;
                _result = null;
            }
        }
    }
}
