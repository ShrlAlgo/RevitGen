using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RevitGen.Generator
{
    /// <summary>
    /// RevitGen 2.0 增量源生成器入口。
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class RevitIncrementalGenerator : IIncrementalGenerator
    {
        internal const string RevitCommandAttribute = "RevitGen.Attributes.RevitCommandAttribute";
        internal const string ExternalEventAttribute = "RevitGen.Attributes.RevitExternalEventAttribute";
        internal const string UpdaterAttribute = "RevitGen.Attributes.RevitUpdaterAttribute";
        internal const string EventContainerAttribute = "RevitGen.Attributes.RevitEventContainerAttribute";
        internal const string DockablePaneAttribute = "RevitGen.Attributes.RevitDockablePaneAttribute";
        internal const string SchemaAttribute = "RevitGen.Attributes.RevitSchemaAttribute";
        internal const string SharedParameterAttribute = "RevitGen.Attributes.RevitSharedParameterAttribute";

        /// <summary>
        /// 注册各功能模块的语法发现、校验和源码输出管线。
        /// </summary>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var commands = CreateTypeProvider(context, RevitCommandAttribute);
            var externalEvents = CreateTypeProvider(context, ExternalEventAttribute);
            var updaters = CreateTypeProvider(context, UpdaterAttribute);
            var eventContainers = CreateTypeProvider(context, EventContainerAttribute);
            var dockablePanes = CreateTypeProvider(context, DockablePaneAttribute);
            var schemas = CreateTypeProvider(context, SchemaAttribute);
            var sharedParameterOwners = context.SyntaxProvider.ForAttributeWithMetadataName(
                    SharedParameterAttribute,
                    static (_, _) => true,
                    static (attributeContext, _) => attributeContext.TargetSymbol.ContainingType)
                .Where(static symbol => symbol != null);

            RegisterCommandOutput(context, commands);
            RegisterFeatureOutput(context, externalEvents, FeatureKind.ExternalEvent);
            RegisterFeatureOutput(context, updaters, FeatureKind.Updater);
            RegisterFeatureOutput(context, eventContainers, FeatureKind.EventContainer);
            RegisterFeatureOutput(context, dockablePanes, FeatureKind.DockablePane);
            RegisterFeatureOutput(context, schemas, FeatureKind.Schema);
            context.RegisterSourceOutput(sharedParameterOwners.Collect(),
                static (productionContext, owners) => RegisterSharedParameterOutputs(productionContext, owners));

            var allAttributedTypes = context.SyntaxProvider.CreateSyntaxProvider(
                    static (node, _) => node is TypeDeclarationSyntax type && type.AttributeLists.Count > 0,
                    static (syntaxContext, cancellationToken) => syntaxContext.SemanticModel.GetDeclaredSymbol(
                        (TypeDeclarationSyntax)syntaxContext.Node, cancellationToken) as INamedTypeSymbol)
                .Where(static symbol => symbol != null);

            context.RegisterSourceOutput(allAttributedTypes.Collect(),
                static (productionContext, symbols) => GenerateApplication(productionContext, symbols));
        }

        /// <summary>
        /// 创建按特性元数据名筛选类型的增量 Provider。
        /// </summary>
        private static IncrementalValuesProvider<INamedTypeSymbol> CreateTypeProvider(
            IncrementalGeneratorInitializationContext context,
            string attributeMetadataName)
        {
            return context.SyntaxProvider.ForAttributeWithMetadataName(
                    attributeMetadataName,
                    static (node, _) => node is TypeDeclarationSyntax,
                    static (attributeContext, _) => attributeContext.TargetSymbol as INamedTypeSymbol)
                .Where(static symbol => symbol != null);
        }

        /// <summary>
        /// 注册 Revit 命令的独立源码输出。
        /// </summary>
        private static void RegisterCommandOutput(
            IncrementalGeneratorInitializationContext context,
            IncrementalValuesProvider<INamedTypeSymbol> commands)
        {
            context.RegisterSourceOutput(commands, static (productionContext, symbol) =>
            {
                if (!GeneratorValidation.ValidatePartialType(productionContext, symbol)) return;
                if (!GeneratorValidation.ValidatePublicCommand(productionContext, symbol)) return;
                if (!GeneratorValidation.ValidateSingleHandler(
                        productionContext,
                        symbol,
                        "RevitGen.Attributes.CommandHandlerAttribute",
                        "CommandHandler",
                        false)) return;
                if (!GeneratorValidation.ValidateAvailability(productionContext, symbol)) return;

                var source = SourceGenerationHelper.GenerateCommandPartialClass(symbol);
                productionContext.AddSource(
                    SymbolUtilities.GetHintName(symbol, "Command"),
                    SourceText.From(source, Encoding.UTF8));
            });
        }

        /// <summary>
        /// 注册独立功能模块的源码输出。
        /// </summary>
        private static void RegisterFeatureOutput(
            IncrementalGeneratorInitializationContext context,
            IncrementalValuesProvider<INamedTypeSymbol> types,
            FeatureKind kind)
        {
            context.RegisterSourceOutput(types, (productionContext, symbol) =>
            {
                if (!GeneratorValidation.ValidatePartialType(productionContext, symbol)) return;

                string source;
                if (!FeatureGenerationHelper.TryGenerate(productionContext, symbol, kind, out source)) return;

                productionContext.AddSource(
                    SymbolUtilities.GetHintName(symbol, kind.ToString()),
                    SourceText.From(source, Encoding.UTF8));
            });
        }

        /// <summary>
        /// 汇总所有注册型功能并生成唯一 Application 与 Bootstrap。
        /// </summary>
        private static void GenerateApplication(
            SourceProductionContext context,
            ImmutableArray<INamedTypeSymbol> symbols)
        {
            var distinctSymbols = symbols
                .Where(static symbol => symbol != null)
                .GroupBy(static symbol => symbol.ToDisplayString(), StringComparer.Ordinal)
                .Select(static group => group.First())
                .ToList();

            var recognized = distinctSymbols.Where(IsRegistrationType).ToList();
            if (recognized.Count == 0) return;
            if (!ValidateRibbonGroups(context, recognized)) return;

            var source = ApplicationGenerationHelper.Generate(recognized);
            context.AddSource("RevitGenApplication.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        /// <summary>
        /// 判断类型是否参与 RevitGen 启动注册。
        /// </summary>
        private static bool IsRegistrationType(INamedTypeSymbol symbol)
        {
            return symbol.GetAttributes().Any(attribute =>
            {
                var name = attribute.AttributeClass?.ToDisplayString();
                return name == RevitCommandAttribute ||
                       name == UpdaterAttribute ||
                       name == EventContainerAttribute ||
                       name == DockablePaneAttribute;
            });
        }

        /// <summary>
        /// 校验按钮组配置的一致性和 Revit 对堆叠数量的限制。
        /// </summary>
        private static bool ValidateRibbonGroups(
            SourceProductionContext context,
            IEnumerable<INamedTypeSymbol> symbols)
        {
            var valid = true;
            var commands = symbols
                .Where(symbol => SymbolUtilities.HasAttribute(symbol, RevitCommandAttribute))
                .Select(symbol => new
                {
                    Symbol = symbol,
                    Attribute = SymbolUtilities.GetAttribute(symbol, RevitCommandAttribute)
                })
                .ToList();

            foreach (var command in commands)
            {
                var groupName = SymbolUtilities.GetNamedArgument(command.Attribute, "GroupName", string.Empty);
                var groupType = SymbolUtilities.GetNamedArgument(command.Attribute, "GroupType", 0);
                if (string.IsNullOrEmpty(groupName) == (groupType != 0))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        GeneratorDiagnostics.InvalidConfiguration,
                        SymbolUtilities.GetLocation(command.Symbol),
                        $"命令 '{command.Symbol.Name}' 的 GroupName 与 GroupType 必须同时配置"));
                    valid = false;
                }
            }

            var groups = commands
                .Select(command => new
                {
                    command.Symbol,
                    Tab = SymbolUtilities.GetNamedArgument(command.Attribute, "TabName", "RevitGen"),
                    Panel = SymbolUtilities.GetNamedArgument(command.Attribute, "PanelName", "Commands"),
                    Name = SymbolUtilities.GetNamedArgument(command.Attribute, "GroupName", string.Empty),
                    Type = SymbolUtilities.GetNamedArgument(command.Attribute, "GroupType", 0)
                })
                .Where(command => !string.IsNullOrEmpty(command.Name))
                .GroupBy(command => command.Tab + "\u001f" + command.Panel + "\u001f" + command.Name, StringComparer.Ordinal);

            foreach (var group in groups)
            {
                if (group.Select(command => command.Type).Distinct().Count() != 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        GeneratorDiagnostics.InvalidConfiguration,
                        SymbolUtilities.GetLocation(group.First().Symbol),
                        $"按钮组 '{group.First().Name}' 配置了不同的 GroupType"));
                    valid = false;
                }
                else if (group.First().Type == 3 && (group.Count() < 2 || group.Count() > 3))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        GeneratorDiagnostics.InvalidConfiguration,
                        SymbolUtilities.GetLocation(group.First().Symbol),
                        $"Stacked 按钮组 '{group.First().Name}' 只能包含 2 到 3 个命令"));
                    valid = false;
                }
            }

            return valid;
        }

        /// <summary>
        /// 按拥有类型合并共享参数成员，避免同一类型重复 AddSource。
        /// </summary>
        private static void RegisterSharedParameterOutputs(
            SourceProductionContext context,
            ImmutableArray<INamedTypeSymbol> owners)
        {
            foreach (var symbol in owners
                         .GroupBy(static owner => owner.ToDisplayString(), StringComparer.Ordinal)
                         .Select(static group => group.First()))
            {
                if (!GeneratorValidation.ValidatePartialType(context, symbol)) continue;
                string source;
                if (!FeatureGenerationHelper.TryGenerate(context, symbol, FeatureKind.SharedParameters, out source)) continue;
                context.AddSource(
                    SymbolUtilities.GetHintName(symbol, "SharedParameters"),
                    SourceText.From(source, Encoding.UTF8));
            }
        }
    }

    internal enum FeatureKind
    {
        ExternalEvent,
        Updater,
        EventContainer,
        DockablePane,
        Schema,
        SharedParameters
    }
}
