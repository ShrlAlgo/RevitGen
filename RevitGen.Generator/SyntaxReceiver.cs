// RevitGen.Generator/SyntaxReceiver.cs

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Collections.Generic;

namespace RevitGen.Generator
{
    /// <summary>
    /// Walks the syntax trees looking for partial class declarations that carry at least
    /// one attribute.  These are recorded as candidates for the source generator to
    /// inspect during the semantic phase.
    /// </summary>
    internal class SyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode == null)
                return;

            // Only consider class declarations that carry at least one attribute list.
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax &&
                classDeclarationSyntax.AttributeLists.Count > 0)
            {
                // Only partial classes are eligible for code generation.
                foreach (var modifier in classDeclarationSyntax.Modifiers)
                {
                    if (modifier.ValueText == "partial")
                    {
                        CandidateClasses.Add(classDeclarationSyntax);
                        break;
                    }
                }
            }
        }
    }
}