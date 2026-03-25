using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using RevitGen.Attributes;

using RevitGenTest.Properties;

namespace RevitGenTest
{
    [RevitCommand("我的第一个命令", ToolTip = "这是一个自动生成的酷炫命令！", PanelName = "核心功能", Icon = nameof(Resources.CodeList_32px))]
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
    }
    [RevitCommand("我的第一个命令", ToolTip = "这是一个自动生成的酷炫命令！", PanelName = "核心功能", Icon = "Resources/CodeList_32px.png")]
    public partial class RevitAddinX
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