- 右键RevitGen项目，打包，生成nuget，可以修改版本。
- 在插件项目去引用nuget包，本地路径需要修改nuget.config中的相对路径。
- 使用partial修饰你的命令类
- 使用RevitCommand生成命令和面板
- CommandHandler来定义运行的逻辑方法
- 如果是路径的图标的话，需要属性窗口中把生成方式改成嵌入式资源
- 命令自带UIApplication,UIDocument,Document,ActiveView等属性
- 自带事务处理
- 命令的特性会生成一个RevitGenApplication的类，注册命令，生成面板。
```
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using RevitGen.Attributes;

using RevitGenTest.Properties;

namespace RevitGenTest
{
    [RevitCommand("我的第一个命令", ToolTip = "这是一个自动生成的酷炫命令！", PanelName = "核心功能", Icon = nameof(Resources.CodeList_32px))]
    public partial class RevitAddinOne
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
                this.ErrorMessage = "项目中没有找到任何墙。";
                this.Result = Result.Failed;
                return; // 提前返回
            }

            TaskDialog.Show("成功", $"找到了 {walls.Count} 面墙。");
        }
    }
    [RevitCommand("我的第二个命令", ToolTip = "这是自动生成的酷炫命令！", PanelName = "核心功能", Icon = "Resources/CodeList_32px.png")]
    public partial class RevitAddinTwo
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
                this.ErrorMessage = "项目中没有找到任何墙。";
                this.Result = Result.Failed;
                return; // 提前返回
            }

            TaskDialog.Show("成功", $"找到了 {walls.Count} 面墙。");
        }
    }
}
```
