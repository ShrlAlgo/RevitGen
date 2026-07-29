using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RevitGen.Generator
{
    // 保留旧入口供 1.x 兼容测试使用；2.x 实际入口为 RevitIncrementalGenerator。
    public class RevitCommandGenerator : ISourceGenerator
    {
        private const string RevitCommandAttributeFullName = "RevitGen.Attributes.RevitCommandAttribute";
        private const string CommandHandlerAttributeFullName = "RevitGen.Attributes.CommandHandlerAttribute";

#pragma warning disable RS2008 // Enable release tracking for analyzer rules
        private static readonly DiagnosticDescriptor NoHandlerMethodRule = new DiagnosticDescriptor(
            id: "REVITGEN001",
            title: "Missing CommandHandler method",
            messageFormat: "Class '{0}' must have a parameterless void method marked with [CommandHandler]",
            category: "RevitGen",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
#pragma warning restore RS2008

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.CancellationToken.IsCancellationRequested) return;

            var log = new StringBuilder();
            log.AppendLine("// RevitGen Log:");
            log.AppendLine($"// Compilation assembly: {context.Compilation.AssemblyName}");

            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            {
                log.AppendLine("// WARNING: SyntaxReceiver is null or of unexpected type. Aborting.");
                AddSource(context, "RevitGen_Debug_Log.g.cs", log.ToString());
                return;
            }

            log.AppendLine($"// Candidate classes found by SyntaxReceiver: {receiver.CandidateClasses.Count}");
            if (receiver.CandidateClasses.Count == 0)
            {
                return;
            }

            var attributeSymbol = context.Compilation.GetTypeByMetadataName(RevitCommandAttributeFullName);
            if (attributeSymbol == null)
            {
                log.AppendLine($"// ERROR: Could not find attribute symbol: {RevitCommandAttributeFullName}");
                AddSource(context, "RevitGen_Debug_Log.g.cs", log.ToString());
                return;
            }
            log.AppendLine($"// Successfully found attribute symbol: {attributeSymbol.Name}");

            var handlerAttributeSymbol = context.Compilation.GetTypeByMetadataName(CommandHandlerAttributeFullName);
            if (handlerAttributeSymbol == null)
            {
                log.AppendLine($"// WARNING: Could not find attribute symbol: {CommandHandlerAttributeFullName}. Handler methods will not be validated at the semantic level.");
            }
            else
            {
                log.AppendLine($"// Successfully found attribute symbol: {handlerAttributeSymbol.Name}");
            }

            var commandClasses = new List<INamedTypeSymbol>();
            foreach (var candidateClass in receiver.CandidateClasses)
            {
                if (context.CancellationToken.IsCancellationRequested) return;

                log.AppendLine($"// -> Processing candidate: {candidateClass.Identifier.ValueText}");
                var model = context.Compilation.GetSemanticModel(candidateClass.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(candidateClass) as INamedTypeSymbol;

                if (classSymbol == null)
                {
                    log.AppendLine($"//    -> SKIPPED: Could not get class symbol.");
                    continue;
                }

                bool hasAttribute = classSymbol.GetAttributes().Any(ad =>
                    ad.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) ?? false);

                if (hasAttribute)
                {
                    log.AppendLine($"//    -> SUCCESS: Found [RevitCommand] attribute. Adding to list.");
                    commandClasses.Add(classSymbol);
                }
                else
                {
                    log.AppendLine($"//    -> SKIPPED: Did not find [RevitCommand] attribute.");
                }
            }

            log.AppendLine($"// Total command classes to generate: {commandClasses.Count}");

            if (commandClasses.Any())
            {
                var validCommandClasses = new List<INamedTypeSymbol>();
                foreach (var classSymbol in commandClasses)
                {
                    if (context.CancellationToken.IsCancellationRequested) return;

                    var partialClassSource = SourceGenerationHelper.GenerateCommandPartialClass(classSymbol);
                    if (partialClassSource.StartsWith("// ERROR"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            NoHandlerMethodRule, classSymbol.Locations.FirstOrDefault(), classSymbol.Name));
                        log.AppendLine($"//    -> SKIPPED: {classSymbol.Name} has no valid [CommandHandler] method.");
                        continue;
                    }

                    validCommandClasses.Add(classSymbol);
                    AddSource(context, $"{classSymbol.Name}.g.cs", partialClassSource);
                    log.AppendLine($"// Generated: {classSymbol.Name}.g.cs");
                }

                if (validCommandClasses.Any())
                {
                    var appSource = SourceGenerationHelper.GenerateApplicationClass(validCommandClasses);
                    AddSource(context, "RevitGenApplication.g.cs", appSource);
                    log.AppendLine("// Generated: RevitGenApplication.g.cs");
                }
            }

            AddSource(context, "RevitGen_Debug_Log.g.cs", log.ToString());
        }

        private void AddSource(GeneratorExecutionContext context, string hintName, string source)
        {
            context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
        }
    }
}
