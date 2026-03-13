using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using Main.Core.Abstractions;
using Main.Core.Services;
using Main.Core.Services.Mcp;
using Main.Core.Utils;
using SharedLibrary;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Main.Core
{
    public class Initializer
    {
        private readonly ILogger<Initializer> _logger;
        private AddinRegistry _addinRegistry;
        private const string TargetAssemblyName = "SharedLibrary";

        public Initializer(ILogger<Initializer> logger)
        {
            _logger = logger;
        }

        internal void Initialize(UIControlledApplication uiCtrlApp, PushButton executeButton)
        {
            _logger.LogInfo("-------------------- Revit Start --------------------");
            _logger.LogInfo("初始化核心模块...");

            // 订阅 ApplicationInitialized 事件，在 Revit Application 就绪后再初始化依赖 UIApplication 的服务
            // 这是 Autodesk 推荐的获取 UIApplication 的方式，替代私有字段反射，兼容各版本 Revit
            uiCtrlApp.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;

            // 初始化更新服务，暂时屏蔽
            //UpdateService.Initialize(new FileLogger<UpdateService>());
            //try { UpdateService.TryCheckForUpdates(); } catch { }

            // 初始化并启动插件监视器
            var monitorLogger = new FileLogger<FolderMonitor.ProductionFolderMonitor>();
            var addinFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Addins");
            if (!Directory.Exists(addinFolder))
            {
                Directory.CreateDirectory(addinFolder);
            }
            var monitor = new FolderMonitor.ProductionFolderMonitor(monitorLogger, addinFolder);

            // 初始化 AddinRegistry
            var addinRegistryLogger = new FileLogger<AddinRegistry>();
            int revitVersion;
            int? currentRevitVersion = int.TryParse(uiCtrlApp.ControlledApplication.VersionNumber, out revitVersion)
                ? (int?)revitVersion
                : null;
            var currentRevitLanguage = uiCtrlApp.ControlledApplication.Language.ToString();
            _addinRegistry = new AddinRegistry(
                monitor,
                addinRegistryLogger,
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                currentRevitVersion,
                currentRevitLanguage);

            // 注册到全局服务上下文
            AppServices.AddinRegistry = _addinRegistry;

            // AddinRegistry 初始化后再启动监视器
            monitor.Start();

            // 初始化 ExecuteButtonService
            _logger.LogInfo("初始化 ExecuteButton Service...");
            ExecuteButtonService.Initialize(executeButton);

            // 注册命令完成事件
            var MessageHandlerLogger = new FileLogger<MessageHandler>();
            var messageHandler = new MessageHandler(MessageHandlerLogger);
            AppEventHub.CommandCompleted += messageHandler.OnCommandCompleted;

            // 注册 AssemblyResolve 事件处理程序
            _logger.LogInfo("注册 SharedLibrary 重定向方法");
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            // 初始化并启动 SocketService，监听来自外部应用的请求
            var socketLogger = new FileLogger<SocketService>();
            SocketService.Initialize(socketLogger);
            SocketService.Instance.Start();

            // 初始化并启动 McpService，提供 MCP 协议支持
            var mcpLogger = new FileLogger<McpService>();
            McpService.Initialize(mcpLogger);
            McpService.Instance.Start();

            // 初始化完成
            _logger.LogInfo("核心模块初始化完成");
        }


        private void OnApplicationInitialized(object sender, Autodesk.Revit.DB.Events.ApplicationInitializedEventArgs e)
        {
            // 此时 Revit Application 已完全就绪，可安全构造 UIApplication
            var uiapp = new UIApplication(sender as Application);

            _logger.LogInfo("初始化 Revit Service...");
            RevitService.Initialize(uiapp);
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // 获取请求的程序集名称对象
            var requestedName = new AssemblyName(args.Name);

            // 核心判断：只拦截针对 "SharedLibrary" 的请求
            if (requestedName.Name.Equals(TargetAssemblyName, StringComparison.OrdinalIgnoreCase))
            {
                // A. 偷梁换柱策略：检查内存里是否已经有了
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name.Equals(TargetAssemblyName, StringComparison.OrdinalIgnoreCase));

                if (loadedAssembly != null)
                {
                    // 如果内存里的版本 >= 别人请求的版本，直接返回内存里的（实现重定向）
                    if (loadedAssembly.GetName().Version >= requestedName.Version)
                    {
                        return loadedAssembly;
                    }
                }

                // B. 兜底策略：如果内存里没有，试图从【本插件的安装目录】加载你的新版 DLL
                string myAssemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string targetPath = Path.Combine(myAssemblyFolder, $"{TargetAssemblyName}.dll");

                if (File.Exists(targetPath))
                {
                    // 加载并返回你的新版
                    return Assembly.LoadFrom(targetPath);
                }
            }

            // 对其他不相关的 DLL 请求，返回 null (交给系统默认处理)
            return null;
        }
    }
}
