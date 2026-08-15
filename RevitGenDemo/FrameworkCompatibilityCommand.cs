using Autodesk.Revit.UI;

using RevitGen.Attributes;

namespace RevitGenDemo
{
    /// <summary>
    /// 验证当前目标框架下的命令生成和执行。
    /// </summary>
    [RevitCommand(
        "框架兼容命令",
        PanelName = "兼容验证",
        ToolTip = "验证当前目标框架下的 RevitGen 命令",
        Order = 10,
        UsingTransaction = false)]
    public partial class FrameworkCompatibilityCommand
    {
        [CommandHandler]
        private void ExecuteCommand()
        {
#if NETFRAMEWORK
            const string target = ".NET Framework 4.7.2 / Revit 2020";
#else
            const string target = ".NET 8 / Revit 2025";
#endif
            TaskDialog.Show("RevitGen", $"{target} 生成成功。");
        }
    }
}
