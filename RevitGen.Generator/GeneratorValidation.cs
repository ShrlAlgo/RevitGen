using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RevitGen.Generator
{
    /// <summary>
    /// 提供各生成模块共用的类型和方法校验。
    /// </summary>
    internal static class GeneratorValidation
    {
        /// <summary>
        /// 校验类型及其外层类型均为非泛型 partial 类型。
        /// </summary>
        public static bool ValidatePartialType(SourceProductionContext context, INamedTypeSymbol symbol)
        {
            for (var current = symbol; current != null; current = current.ContainingType)
            {
                if (current.TypeKind != TypeKind.Class)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        GeneratorDiagnostics.InvalidConfiguration,
                        SymbolUtilities.GetLocation(current),
                        $"类型 '{current.ToDisplayString()}' 必须是 class"));
                    return false;
                }
                if (current.IsGenericType)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        GeneratorDiagnostics.GenericTypeNotSupported,
                        SymbolUtilities.GetLocation(symbol),
                        symbol.ToDisplayString()));
                    return false;
                }

                var isPartial = current.DeclaringSyntaxReferences
                    .Select(reference => reference.GetSyntax(context.CancellationToken))
                    .OfType<TypeDeclarationSyntax>()
                    .Any(type => type.Modifiers.Any(SyntaxKind.PartialKeyword));

                if (!isPartial)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        GeneratorDiagnostics.TypeMustBePartial,
                        SymbolUtilities.GetLocation(current),
                        current.ToDisplayString()));
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 校验需要由 Revit 反射创建的命令类型为 public。
        /// </summary>
        public static bool ValidatePublicCommand(SourceProductionContext context, INamedTypeSymbol symbol)
        {
            if (symbol.DeclaredAccessibility == Accessibility.Public) return true;
            context.ReportDiagnostic(Diagnostic.Create(
                GeneratorDiagnostics.InvalidConfiguration,
                SymbolUtilities.GetLocation(symbol),
                $"Revit 命令类型 '{symbol.ToDisplayString()}' 必须是 public"));
            return false;
        }

        /// <summary>
        /// 校验一个特性只标记唯一实例 void 方法，并按需允许 UIApplication 参数。
        /// </summary>
        public static bool ValidateSingleHandler(
            SourceProductionContext context,
            INamedTypeSymbol symbol,
            string handlerAttribute,
            string handlerDisplayName,
            bool allowApplicationParameter)
        {
            var methods = symbol.GetMembers().OfType<IMethodSymbol>()
                .Where(method => SymbolUtilities.HasAttribute(method, handlerAttribute))
                .ToList();

            var valid = methods.Count == 1 &&
                        !methods[0].IsStatic &&
                        methods[0].ReturnsVoid &&
                        (methods[0].Parameters.Length == 0 ||
                         allowApplicationParameter &&
                         methods[0].Parameters.Length == 1 &&
                         methods[0].Parameters[0].Type.ToDisplayString() == "Autodesk.Revit.UI.UIApplication");

            if (valid) return true;

            context.ReportDiagnostic(Diagnostic.Create(
                GeneratorDiagnostics.InvalidHandler,
                SymbolUtilities.GetLocation(symbol),
                symbol.ToDisplayString(),
                handlerDisplayName));
            return false;
        }

        /// <summary>
        /// 校验可选的 CommandAvailability 方法签名。
        /// </summary>
        public static bool ValidateAvailability(SourceProductionContext context, INamedTypeSymbol symbol)
        {
            var methods = symbol.GetMembers().OfType<IMethodSymbol>()
                .Where(method => SymbolUtilities.HasAttribute(
                    method, "RevitGen.Attributes.CommandAvailabilityAttribute"))
                .ToList();
            if (methods.Count == 0) return true;

            var valid = methods.Count == 1 && !methods[0].IsStatic &&
                        methods[0].ReturnType.SpecialType == SpecialType.System_Boolean &&
                        (methods[0].Parameters.Length == 0 ||
                         methods[0].Parameters.Length == 1 &&
                         methods[0].Parameters[0].Type.ToDisplayString() == "Autodesk.Revit.UI.UIApplication");
            if (valid) return true;

            context.ReportDiagnostic(Diagnostic.Create(
                GeneratorDiagnostics.InvalidHandler,
                SymbolUtilities.GetLocation(symbol),
                symbol.ToDisplayString(),
                "CommandAvailability"));
            return false;
        }
    }
}
