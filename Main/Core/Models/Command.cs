using Autodesk.Revit.UI;
using Main.Core.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Main.Core.Models
{
    public class Command : AddinItem
    {
        public Command() : base(AddinType.Command) { }

        public static object Execute(JObject commandParams)
        {
            var app = RevitService.UIApp;
            var cmdId = ExecuteButtonService.ExeBtnCmdId;

            //检查此时 Revit 是否处于可操作状态
            if (!CanExecuteCommand(app))
            {
                return "Revit is not ready.";
            }

            // 将参数写入共享存储
            CommandStorageService.SetCommandParams(commandParams);

            // 准备同步等待上下文，设置超时时长
            // 由于命令自身也带有超时机制，这里设置的超时阈值设置得更长
            CommandResultSync.Prepare();
            TimeSpan timeout = TimeSpan.FromSeconds(7200);

            // 投递命令
            app.PostCommand(cmdId);

            NudgeRevitUi("Nudge:afterPostCommand");
            
            // 阻塞等待结果
            var result = CommandResultSync.Wait(timeout);
            CommandResultSync.Clear();
            return result;
        }

        public static string NudgeRevitUi(string stage)
        {
            var sb = new StringBuilder();
            sb.AppendLine("==================================================");
            sb.AppendLine($"stage={stage}");
            sb.AppendLine($"time={DateTime.Now:O}");
            try
            {
                var hwnd = Process.GetCurrentProcess().MainWindowHandle;
                sb.AppendLine($"mainWindowHandle=0x{hwnd.ToInt64():X}");

                if (hwnd == IntPtr.Zero)
                {
                    sb.AppendLine("postMessage.skipped=true");
                    return sb.ToString();
                }

                bool okNull = PostMessage(hwnd, WM_NULL, IntPtr.Zero, IntPtr.Zero);
                sb.AppendLine($"postMessage.WM_NULL={okNull} lastError={Marshal.GetLastWin32Error()}");

                // Post a harmless mouse-move message to mimic the user moving the cursor on Revit.
                // lParam packs x/y; 0,0 is sufficient for a nudge.
                bool okMove = PostMessage(hwnd, WM_MOUSEMOVE, IntPtr.Zero, IntPtr.Zero);
                sb.AppendLine($"postMessage.WM_MOUSEMOVE={okMove} lastError={Marshal.GetLastWin32Error()}");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"nudge.error={ex.GetType().Name}:{ex.Message}");
            }

            return sb.ToString();
        }

        private const uint WM_NULL = 0x0000;
        private const uint WM_MOUSEMOVE = 0x0200;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static bool CanExecuteCommand(UIApplication uiapp)
        {
            // Document.IsReadOnly / IsModifiable 并不能反映 UI 是否可 PostCommand。
            // 使用官方的 CanPostCommand 判断当前 UI 状态是否允许投递命令。
            if (uiapp == null)
            {
                return false;
            }

            if (uiapp.ActiveUIDocument?.Document == null)
            {
                return false;
            }

            var cmdId = ExecuteButtonService.ExeBtnCmdId;
            if (cmdId == null)
            {
                return false;
            }

            return uiapp.CanPostCommand(cmdId);
        }
    }
}
