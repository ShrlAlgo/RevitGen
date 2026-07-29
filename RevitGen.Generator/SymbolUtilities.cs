using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace RevitGen.Generator
{
    /// <summary>
    /// 提供 Symbol 名称、特性和生成类型外壳的通用处理。
    /// </summary>
    internal static class SymbolUtilities
    {
        /// <summary>
        /// 判断 Symbol 是否包含指定完整名称的特性。
        /// </summary>
        public static bool HasAttribute(ISymbol symbol, string attributeName)
        {
            return symbol.GetAttributes().Any(attribute =>
                attribute.AttributeClass?.ToDisplayString() == attributeName);
        }

        /// <summary>
        /// 获取指定完整名称的首个特性。
        /// </summary>
        public static AttributeData GetAttribute(ISymbol symbol, string attributeName)
        {
            return symbol.GetAttributes().First(attribute =>
                attribute.AttributeClass?.ToDisplayString() == attributeName);
        }

        /// <summary>
        /// 获取适合报告诊断的源码位置。
        /// </summary>
        public static Location GetLocation(ISymbol symbol)
        {
            return symbol.Locations.FirstOrDefault(location => location.IsInSource) ?? Location.None;
        }

        /// <summary>
        /// 生成不会因同名类型冲突的 HintName。
        /// </summary>
        public static string GetHintName(INamedTypeSymbol symbol, string suffix)
        {
            var value = GetCodeTypeName(symbol).Replace("global::", string.Empty);
            var sanitized = new string(value.Select(character =>
                char.IsLetterOrDigit(character) || character == '_' ? character : '_').ToArray());
            return sanitized + "." + suffix + ".g.cs";
        }

        /// <summary>
        /// 获取在生成 C# 中引用类型的完全限定名称。
        /// </summary>
        public static string GetCodeTypeName(INamedTypeSymbol symbol)
        {
            return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        /// <summary>
        /// 获取供 Revit 反射实例化使用的 CLR 类型名称。
        /// </summary>
        public static string GetRuntimeTypeName(INamedTypeSymbol symbol)
        {
            var typeNames = new Stack<string>();
            for (var current = symbol; current != null; current = current.ContainingType)
            {
                typeNames.Push(current.MetadataName);
            }

            var namespaceName = symbol.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : symbol.ContainingNamespace.ToDisplayString() + ".";
            return namespaceName + string.Join("+", typeNames);
        }

        /// <summary>
        /// 创建命名空间和 partial 外层类型，返回需要关闭的大括号数量。
        /// </summary>
        public static int AppendPartialTypeStart(
            StringBuilder source,
            INamedTypeSymbol symbol,
            string interfaces,
            string targetAttribute = null)
        {
            var closeCount = 0;
            if (!symbol.ContainingNamespace.IsGlobalNamespace)
            {
                source.Append("namespace ").Append(symbol.ContainingNamespace.ToDisplayString()).AppendLine();
                source.AppendLine("{");
                closeCount++;
            }

            var types = new Stack<INamedTypeSymbol>();
            for (var current = symbol; current != null; current = current.ContainingType) types.Push(current);

            while (types.Count > 0)
            {
                var current = types.Pop();
                if (SymbolEqualityComparer.Default.Equals(current, symbol) && !string.IsNullOrEmpty(targetAttribute))
                {
                    source.Append(' ', closeCount * 4).AppendLine(targetAttribute);
                }
                source.Append(' ', closeCount * 4)
                    .Append(GetAccessibility(current.DeclaredAccessibility))
                    .Append(" partial class ")
                    .Append(current.Name);
                if (SymbolEqualityComparer.Default.Equals(current, symbol) && !string.IsNullOrEmpty(interfaces))
                {
                    source.Append(" : ").Append(interfaces);
                }
                source.AppendLine();
                source.Append(' ', closeCount * 4).AppendLine("{");
                closeCount++;
            }

            return closeCount;
        }

        /// <summary>
        /// 关闭由 AppendPartialTypeStart 创建的类型和命名空间。
        /// </summary>
        public static void AppendPartialTypeEnd(StringBuilder source, int closeCount)
        {
            for (var index = closeCount - 1; index >= 0; index--)
            {
                source.Append(' ', index * 4).AppendLine("}");
            }
        }

        /// <summary>
        /// 读取特性命名参数，不存在时返回默认值。
        /// </summary>
        public static T GetNamedArgument<T>(AttributeData attribute, string name, T defaultValue)
        {
            var argument = attribute.NamedArguments.FirstOrDefault(item => item.Key == name);
            return argument.Value.Value == null ? defaultValue : (T)argument.Value.Value;
        }

        /// <summary>
        /// 将文本转换为安全的 C# 字符串字面量内容。
        /// </summary>
        public static string EscapeString(string value)
        {
            if (value == null) return string.Empty;
            return value.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        /// <summary>
        /// 将 Roslyn 可访问性转换为生成代码修饰符。
        /// </summary>
        private static string GetAccessibility(Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.Public: return "public";
                case Accessibility.Private: return "private";
                case Accessibility.Protected: return "protected";
                case Accessibility.Internal: return "internal";
                case Accessibility.ProtectedAndInternal: return "private protected";
                case Accessibility.ProtectedOrInternal: return "protected internal";
                default: return "internal";
            }
        }
    }
}
