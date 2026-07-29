using Autodesk.Revit.UI;

using RevitGen.Attributes;

namespace RevitGenDemo2025
{
    /// <summary>
    /// 验证 Revit 2025 和 .NET 8 的最小命令样例。
    /// </summary>
    [RevitCommand(
        "Revit 2025 命令",
        PanelName = "兼容验证",
        ToolTip = "验证 RevitGen 2.0 在 .NET 8 下生成命令",
        Order = 10,
        UsingTransaction = false)]
    public partial class Revit2025Command
    {
        /// <summary>
        /// 显示兼容性验证结果。
        /// </summary>
        [CommandHandler]
        private void ExecuteCommand()
        {
            TaskDialog.Show("RevitGen", "Revit 2025 / .NET 8 生成成功。");
        }
    }
}
