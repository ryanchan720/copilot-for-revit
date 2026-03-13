using Autodesk.Revit.UI;

namespace Main.Core.Services
{
    public static class ExecuteButtonService
    {
        public static RevitCommandId ExeBtnCmdId;
        public static PushButton ExeBtn;
        public static void Initialize(PushButton executeButton)
        {
            ExeBtnCmdId = RevitCommandId.LookupCommandId("CustomCtrl_%CustomCtrl_%Revit Copilot%Revit Copilot%Execute");
            ExeBtn = executeButton;
        }
    }
}
