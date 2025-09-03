using System;

namespace RevitGen.Attributes
{
    /// <summary>
    /// 标记一个方法作为 RevitGen 命令的执行逻辑入口点。
    /// ★★ 这个方法必须是无参数的，并且返回 void。★★
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class CommandHandlerAttribute : Attribute
    {
    }
}