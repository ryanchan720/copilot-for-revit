using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;

namespace Main.Core.Services
{
    // Revit 服务静态类，提供对 Revit 应用程序和文档的全局访问
    public static class RevitService
    {
        public static UIApplication UIApp { get; private set; }
        public static Application App => UIApp?.Application;
        public static UIDocument UIDoc => UIApp?.ActiveUIDocument;
        public static Document Doc => UIDoc?.Document;

        // 初始化方法
        public static void Initialize(UIApplication uiapp)
        {
            UIApp = uiapp;
        }

        // 清理方法
        public static void Cleanup()
        {
            UIApp = null;
        }
    }

}