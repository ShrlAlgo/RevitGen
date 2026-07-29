using System.Windows.Controls;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Events;

using RevitGen.Attributes;

namespace RevitGenDemo
{
    /// <summary>
    /// 演示无模式窗口使用的 ExternalEvent Handler。
    /// </summary>
    [RevitExternalEvent(Name = "刷新当前模型")]
    public partial class RefreshModelExternalEvent
    {
        /// <summary>
        /// 执行需要进入 Revit API 上下文的刷新逻辑。
        /// </summary>
        [ExternalEventHandler]
        private void ExecuteRefresh()
        {
        }
    }

    /// <summary>
    /// 演示监听墙体几何变化的 Updater。
    /// </summary>
    [RevitUpdater("墙体几何更新", "DC7C2E9A-F242-4DFD-BB0C-52B7E5B0B940")]
    [UpdaterTrigger((int)BuiltInCategory.OST_Walls, RevitChangeType.Geometry)]
    public partial class WallGeometryUpdater
    {
        /// <summary>
        /// 处理墙体几何变化。
        /// </summary>
        [UpdaterHandler]
        private void ExecuteUpdater(UpdaterData data)
        {
        }
    }

    /// <summary>
    /// 演示自动订阅和取消订阅 Revit UI 事件。
    /// </summary>
    [RevitEventContainer]
    public partial class RevitUiEvents
    {
        /// <summary>
        /// 处理 Revit 空闲事件。
        /// </summary>
        [RevitEvent(RevitEventKind.Idling)]
        private void OnIdling(object sender, IdlingEventArgs args)
        {
        }
    }

    /// <summary>
    /// 演示自动注册的可停靠面板。
    /// </summary>
    [RevitDockablePane("A19756A4-2A3A-4488-A3AF-AE15D8698795", "RevitGen 示例")]
    public partial class RevitGenDockablePane : UserControl
    {
    }

    /// <summary>
    /// 演示 Extensible Storage Schema 模型。
    /// </summary>
    [RevitSchema("31578522-277A-4F10-BAD7-43C68B36AF50", "RevitGenDemoData", VendorId = "SZMD")]
    public partial class RevitGenStorageData
    {
        [RevitSchemaField(Documentation = "示例构件编号")]
        public string ComponentCode { get; set; }

        [RevitSchemaField]
        public int Version { get; set; }

        [RevitSchemaField]
        public int[] RelatedElementIds { get; set; } = new int[0];
    }

    /// <summary>
    /// 演示共享参数描述和批量绑定辅助方法。
    /// </summary>
    public partial class RevitGenSharedParameters
    {
        [RevitSharedParameter(
            "6DB4557F-69B7-4665-86A7-C821158AC763",
            "构件编码",
            Categories = new[] { (int)BuiltInCategory.OST_Walls })]
        public static readonly string ComponentCode = "构件编码";
    }
}
