using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Main.Core.Views;
using System;

namespace Main.CommandSet.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class StartCopilotCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            message = string.Empty;

            try
            {
                // 创建并显示窗口
                var window = new WebWindow(WebWindow.RunType.Prod);
                window.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // 提供更详细的错误信息
                message = $"无法创建网页窗口: {ex.Message}\n\n{ex.StackTrace}";

                // 在 Revit 中显示错误对话框
                TaskDialog.Show("网页浏览器错误",
                    $"无法打开网页浏览器:\n\n{ex.Message}\n\n" +
                    $"详细堆栈信息:\n{ex.StackTrace}",
                    TaskDialogCommonButtons.Ok);

                return Result.Failed;
            }
        }
    }
}
