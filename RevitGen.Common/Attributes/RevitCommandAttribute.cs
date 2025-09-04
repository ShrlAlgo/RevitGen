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
        public bool UsingTransaction { get; set; }

    }
}