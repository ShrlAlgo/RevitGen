using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RevitGen.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class RevitCommandGenerator : ISourceGenerator
    {
        private const string RevitCommandAttributeFullName = "RevitGen.Attributes.RevitCommandAttribute";
        private const string CommandHandlerAttributeFullName = "RevitGen.Attributes.CommandHandlerAttribute";

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var log = new StringBuilder();
            log.AppendLine("// RevitGen Log:");
            log.AppendLine($"// Compilation assembly: {context.Compilation.AssemblyName}");

            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            {
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

            var commandClasses = new List<INamedTypeSymbol>();
            foreach (var candidateClass in receiver.CandidateClasses)
            {
                log.AppendLine($"// -> Processing candidate: {candidateClass.Identifier.ValueText}");
                var model = context.Compilation.GetSemanticModel(candidateClass.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(candidateClass) as INamedTypeSymbol;

                if (classSymbol == null)
                {
                    log.AppendLine($"//    -> SKIPPED: Could not get class symbol.");
                    continue;
                }

                // ★★ 3. 检查是否应用了正确的 [RevitCommand] 特性 ★★
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
                foreach (var classSymbol in commandClasses)
                {
                    var partialClassSource = SourceGenerationHelper.GenerateCommandPartialClass(classSymbol);
                    AddSource(context, $"{classSymbol.Name}.g.cs", partialClassSource);
                }

                var appSource = SourceGenerationHelper.GenerateApplicationClass(commandClasses);
                AddSource(context, "RevitGenApplication.g.cs", appSource);
            }

            AddSource(context, "RevitGen_Debug_Log.g.cs", log.ToString());
        }

        private void AddSource(GeneratorExecutionContext context, string hintName, string source)
        {
            context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
        }
    }
}