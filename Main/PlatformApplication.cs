using Autodesk.Revit.UI;
using Main.Core;
using Main.Core.Abstractions;
using Main.Core.Services;
using Main.Core.Services.Mcp;
using Main.Core.Utils;
using Main.Resources;
using System;

namespace Main
{
    internal class PlatformApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication uiCtrlApp)
        {
            // 界面初始化
            string ribbonTabName = "Revit Copilot";
            string ribbonPanelName = "Revit Copilot";
            string startButtonName = "Start";
            string executeButtonName = "Execute";

            uiCtrlApp.CreateRibbonTab(ribbonTabName);
            var ribbonPanel = uiCtrlApp.CreateRibbonPanel(ribbonTabName, ribbonPanelName);
            
            // 暂时屏蔽对话窗口
            //PushButtonData startButtonData = new PushButtonData(startButtonName, startButtonName, typeof(PlatformApplication).Assembly.Location, "Main.CommandSet.Commands.StartCopilotCommand")
            //{
            //    ToolTip = "启动 Revit Copilot",
            //    LargeImage = Resource.star.CreateBitmapSource()
            //};
            //ribbonPanel.AddItem(startButtonData);

#if DEBUG
            PushButtonData startDebugButtoneData = new PushButtonData("StartDebug", "StartDebug", typeof(PlatformApplication).Assembly.Location, "Main.CommandSet.Commands.StartCopilotDebugCommand")
            {
                ToolTip = "启动 Revit Copilot（调试模式）",
                LargeImage = Resource.star.CreateBitmapSource()
            };
            ribbonPanel.AddItem(startDebugButtoneData);
#endif

            PushButtonData executeButtonData = new PushButtonData(executeButtonName, executeButtonName, typeof(PlatformApplication).Assembly.Location, "Main.CommandSet.Commands.ExecuteCommand")
            {
                ToolTip = "自动触发，点击无效",
                LargeImage = Resource.run.CreateBitmapSource()
            };
            var executeButton = (PushButton)ribbonPanel.AddItem(executeButtonData);


            // 核心服务初始化
            ILogger<Initializer> initializerLogger = new FileLogger<Initializer>();
            var initializer = new Initializer(initializerLogger);
            try { initializer.Initialize(uiCtrlApp, executeButton); }
            catch (Exception ex) { initializerLogger.LogError($"启动失败: {ex.Message}"); }

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            try { SocketService.Instance.Stop(); } catch { }
            try { McpService.Instance.Stop(); } catch { }
            RevitService.Cleanup();
            FolderMonitor.ProductionFolderMonitor.DisposeAll();

            // 调度关闭后的自更新替换
            try { UpdateService.TrySchedulePostShutdownUpdate(); } catch { }

            return Result.Succeeded;
        }
    }
}
