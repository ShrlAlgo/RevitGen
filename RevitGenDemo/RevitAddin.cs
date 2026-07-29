using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using RevitGen.Attributes;

using RevitGenDemo.Properties;

namespace RevitGenDemo
{
    [RevitCommand(
        "我的第一个命令",
        ToolTip = "这是一个自动生成的酷炫命令！",
        PanelName = "核心功能",
        SmallIcon = nameof(Resources.CodeList_16px),
        LargeIcon = nameof(Resources.CodeList_32px),
        GroupName = "基础命令",
        GroupType = RibbonGroupType.Stacked,
        Order = 1)]
    public partial class RevitAddin
    {
        [CommandHandler]
        private void Run()
        {
            var walls = new FilteredElementCollector(Document)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .ToElements();

            if (walls.Count == 0)
            {
                // 如果出现问题，只需设置属性即可
                this.ErrorMessage = "未找到墙";
                this.Result = Result.Failed;
                return; // 提前返回
            }

            TaskDialog.Show("成功", $"成功找到 {walls.Count} 堵墙");
        }

        /// <summary>
        /// 仅在存在活动文档时启用命令。
        /// </summary>
        [CommandAvailability]
        private bool CanExecute(UIApplication application)
        {
            return application.ActiveUIDocument != null;
        }
    }
    [RevitCommand(
        "我的第二个命令",
        ToolTip = "这是一个自动生成的酷炫命令！",
        PanelName = "核心功能",
        Icon = "Resources/CodeList_32px.png",
        GroupName = "基础命令",
        GroupType = RibbonGroupType.Stacked,
        Order = 2)]
    public partial class RevitAddinSample
    {
        [CommandHandler]
        private void Run()
        {
            var walls = new FilteredElementCollector(Document)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .ToElements();

            if (walls.Count == 0)
            {
                // 如果出现问题，只需设置属性即可
                this.ErrorMessage = "未找到墙";
                this.Result = Result.Failed;
                return; // 提前返回
            }

            TaskDialog.Show("成功", $"成功找到 {walls.Count} 堵墙");
        }
    }
}
