using System;

namespace RevitGen.Attributes
{
    /// <summary>
    /// 将一个类标记为Revit外部命令，并自动为其生成UI按钮和必要的接口实现。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RevitCommandAttribute : Attribute
    {
        /// <summary>
        /// 按钮上显示的文本。
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// 按钮所在的Ribbon Tab的名称。
        /// </summary>
        public string TabName { get; set; } = "RevitGen";

        /// <summary>
        /// 按钮所在的Ribbon Panel的名称。
        /// </summary>
        public string PanelName { get; set; } = "Commands";
        /// <summary>
        /// 图标
        /// </summary>
        public string Icon { get; set; } = "";
        /// <summary>
        /// 鼠标悬停在按钮上时显示的工具提示。
        /// </summary>
        public string ToolTip { get; set; } = "";

        /// <summary>
        /// 按钮在面板或按钮组中的排序值。
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 16 像素小图标资源路径或资源名称。
        /// </summary>
        public string SmallIcon { get; set; } = "";

        /// <summary>
        /// 32 像素大图标资源路径或资源名称；为空时回退到 Icon。
        /// </summary>
        public string LargeIcon { get; set; } = "";

        /// <summary>
        /// 按钮的长说明文本。
        /// </summary>
        public string LongDescription { get; set; } = "";

        /// <summary>
        /// 按钮的在线帮助地址。
        /// </summary>
        public string HelpUrl { get; set; } = "";

        /// <summary>
        /// 是否在按钮前添加分隔线。
        /// </summary>
        public bool AddSeparatorBefore { get; set; }

        /// <summary>
        /// 按钮组名称；为空时生成普通按钮。
        /// </summary>
        public string GroupName { get; set; } = "";

        /// <summary>
        /// 按钮组类型。
        /// </summary>
        public RibbonGroupType GroupType { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="text">按钮上显示的文本。</param>
        public RevitCommandAttribute(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentNullException(nameof(text), "Command button text cannot be empty.");
            }
            Text = text;
        }
        /// <summary>
        /// 是否自动使用 Revit 事务执行命令。
        /// </summary>
        public bool UsingTransaction { get; set; } = true;

    }

    /// <summary>
    /// Ribbon 按钮组类型。
    /// </summary>
    public enum RibbonGroupType
    {
        None,
        Pulldown,
        SplitButton,
        Stacked
    }
}
