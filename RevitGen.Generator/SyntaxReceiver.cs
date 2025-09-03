// RevitGen.Generator/SyntaxReceiver.cs

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Collections.Generic;

namespace RevitGen.Generator
{
    /// <summary>
    /// 在语法树中查找所有带有属性的、定义为 partial 的类，作为代码生成的候选对象。
    /// </summary>
    internal class SyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // 检查节点是否是一个类声明，并且它带有属性
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax &&
                classDeclarationSyntax.AttributeLists.Count > 0)
            {
                // 检查类是否被声明为 partial
                foreach (var modifier in classDeclarationSyntax.Modifiers)
                {
                    if (modifier.ValueText == "partial")
                    {
                        CandidateClasses.Add(classDeclarationSyntax);
                        break;
                    }
                }
            }
            //if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax &&
            //   classDeclarationSyntax.AttributeLists.Count > 0)
            //{
            //    CandidateClasses.Add(classDeclarationSyntax);
            //}
        }
    }
}