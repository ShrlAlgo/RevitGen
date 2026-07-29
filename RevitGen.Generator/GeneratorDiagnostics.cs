using Microsoft.CodeAnalysis;

namespace RevitGen.Generator
{
    /// <summary>
    /// 集中定义 RevitGen 编译期诊断。
    /// </summary>
    internal static class GeneratorDiagnostics
    {
#pragma warning disable RS2008
        public static readonly DiagnosticDescriptor TypeMustBePartial = Create(
            "REVITGEN101", "类型必须声明为 partial", "类型 '{0}' 必须声明为 partial");

        public static readonly DiagnosticDescriptor InvalidHandler = Create(
            "REVITGEN102", "处理方法无效", "类型 '{0}' 必须且只能包含一个签名正确的 [{1}] 方法");

        public static readonly DiagnosticDescriptor GenericTypeNotSupported = Create(
            "REVITGEN103", "不支持泛型类型", "类型 '{0}' 或其外层类型不能是泛型类型");

        public static readonly DiagnosticDescriptor InvalidConfiguration = Create(
            "REVITGEN104", "配置无效", "{0}");

        public static readonly DiagnosticDescriptor NoUpdaterTrigger = new DiagnosticDescriptor(
            "REVITGEN105", "Updater 缺少触发器", "Updater '{0}' 未配置 [UpdaterTrigger]，注册后不会收到回调",
            "RevitGen", DiagnosticSeverity.Warning, true);
#pragma warning restore RS2008

        /// <summary>
        /// 创建默认 Error 级别诊断。
        /// </summary>
        private static DiagnosticDescriptor Create(string id, string title, string message)
        {
            return new DiagnosticDescriptor(id, title, message, "RevitGen", DiagnosticSeverity.Error, true);
        }
    }
}
