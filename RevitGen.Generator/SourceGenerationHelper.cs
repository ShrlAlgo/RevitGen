using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;

namespace RevitGen.Generator
{
    /// <summary>
    /// A static helper class containing all logic for generating C# source code.
    /// Templates are defined in <see cref="CodeTemplates"/> and filled in here.
    /// </summary>
    internal static class SourceGenerationHelper
    {
        private const string RevitCommandAttributeFullName = "RevitGen.Attributes.RevitCommandAttribute";
        private const string CommandHandlerAttributeFullName = "RevitGen.Attributes.CommandHandlerAttribute";

        /// <summary>
        /// Generates the second partial-class half for a Revit command.
        /// The generated half implements <c>IExternalCommand</c> and exposes context properties.
        /// </summary>
        /// <param name="classSymbol">The named type symbol for the command class.</param>
        /// <returns>The generated C# source text, or a comment-only error string on failure.</returns>
        public static string GenerateCommandPartialClass(INamedTypeSymbol classSymbol)
        {
            if (classSymbol == null)
                return "// ERROR: classSymbol must not be null.";

            var ns = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;

            var commandHandlerMethod = classSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.GetAttributes().Any(a =>
                    a.AttributeClass?.ToDisplayString() == CommandHandlerAttributeFullName));

            if (commandHandlerMethod == null || commandHandlerMethod.Parameters.Any() || !commandHandlerMethod.ReturnsVoid)
            {
                return $"// ERROR ({className}): A parameterless void method marked with [CommandHandler] is required.";
            }

            var attributeData = classSymbol.GetAttributes()
                .First(ad => ad.AttributeClass?.ToDisplayString() == RevitCommandAttributeFullName);
            var usingTransaction = GetAttributeProperty(attributeData, "UsingTransaction", true);

            var executeBody = usingTransaction
                ? string.Format(CodeTemplates.TransactionExecuteBody, className, commandHandlerMethod.Name)
                : string.Format(CodeTemplates.DirectExecuteBody, commandHandlerMethod.Name);

            return string.Format(CodeTemplates.CommandPartialClass, ns, className, executeBody);
        }

        /// <summary>
        /// Generates the <c>RevitGenApplication</c> class that registers Revit ribbon tabs,
        /// panels, and push-buttons for every command decorated with <c>[RevitCommand]</c>.
        /// </summary>
        /// <param name="commandClasses">The command classes to register.</param>
        /// <returns>The generated C# source text.</returns>
        public static string GenerateApplicationClass(IEnumerable<INamedTypeSymbol> commandClasses)
        {
            var source = new StringBuilder();

            // Static header (helper methods + OnStartup opening)
            source.Append(CodeTemplates.ApplicationClassHeader);

            var commandsWithData = commandClasses.Select(c => new
            {
                Symbol = c,
                Attribute = c.GetAttributes().First(ad =>
                    ad.AttributeClass?.ToDisplayString() == RevitCommandAttributeFullName)
            }).ToList();

            var groupedByTab = commandsWithData.GroupBy(data =>
                GetAttributeProperty<string>(data.Attribute, "TabName", null));

            foreach (var tabGroup in groupedByTab)
            {
                var tabName = tabGroup.Key;

                if (!string.IsNullOrEmpty(tabName))
                {
                    source.AppendLine(string.Format(CodeTemplates.CreateRibbonTab, tabName));
                }

                var groupedByPanel = tabGroup.GroupBy(data =>
                    GetAttributeProperty(data.Attribute, "PanelName", "Commands"));

                foreach (var panelGroup in groupedByPanel)
                {
                    var panelName = panelGroup.Key;
                    var panelVar = $"panel_{SanitizeIdentifier(tabName ?? "AddIns")}_{SanitizeIdentifier(panelName)}";

                    if (!string.IsNullOrEmpty(tabName))
                    {
                        source.AppendLine(string.Format(
                            CodeTemplates.CreateRibbonPanelWithTab, tabName, panelName, panelVar));
                    }
                    else
                    {
                        source.AppendLine(string.Format(
                            CodeTemplates.CreateRibbonPanelDefault, panelName, panelVar));
                    }

                    foreach (var commandData in panelGroup)
                    {
                        var commandSymbol = commandData.Symbol;
                        var attr = commandData.Attribute;
                        var buttonText = (string)attr.ConstructorArguments.First().Value;
                        var fullClassName = commandSymbol.ToDisplayString();
                        var iconName = GetAttributeProperty(attr, "Icon", string.Empty);
                        var tooltip = GetAttributeProperty(attr, "ToolTip", string.Empty);

                        source.AppendLine(string.Format(
                            CodeTemplates.PushButtonData, commandSymbol.Name, buttonText, fullClassName));

                        if (!string.IsNullOrEmpty(tooltip))
                        {
                            source.AppendLine(string.Format(
                                CodeTemplates.ButtonToolTip, commandSymbol.Name, tooltip));
                        }

                        if (!string.IsNullOrEmpty(iconName))
                        {
                            source.AppendLine(string.Format(
                                CodeTemplates.ButtonIcon, commandSymbol.Name, iconName));
                        }

                        source.AppendLine(string.Format(
                            CodeTemplates.AddButtonToPanel, panelVar, commandSymbol.Name));
                    }
                }
            }

            // Static footer (closing braces + OnShutdown)
            source.Append(CodeTemplates.ApplicationClassFooter);

            return source.ToString();
        }

        /// <summary>
        /// Safely reads a named property from an <see cref="AttributeData"/> instance,
        /// returning <paramref name="defaultValue"/> if the property is not present.
        /// </summary>
        private static T GetAttributeProperty<T>(AttributeData attributeData, string propertyName, T defaultValue)
        {
            var namedArgument = attributeData.NamedArguments.FirstOrDefault(kvp => kvp.Key == propertyName);
            if (namedArgument.Value.Value == null)
            {
                return defaultValue;
            }
            return (T)namedArgument.Value.Value;
        }

        /// <summary>
        /// Sanitises a string so that it can be used as a C# identifier (variable name).
        /// </summary>
        private static string SanitizeIdentifier(string name)
        {
            return Regex.Replace(name, @"[^\w]", "_");
        }
    }
}