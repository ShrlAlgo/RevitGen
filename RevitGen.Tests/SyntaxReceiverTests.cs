using Microsoft.CodeAnalysis.CSharp;
using RevitGen.Generator;
using Xunit;

namespace RevitGen.Tests
{
    /// <summary>
    /// Unit tests for <see cref="SyntaxReceiver"/>.
    /// </summary>
    public class SyntaxReceiverTests
    {
        private static SyntaxReceiver BuildReceiver(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9));

            var receiver = new SyntaxReceiver();
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                receiver.OnVisitSyntaxNode(node);
            }
            return receiver;
        }

        [Fact]
        public void OnVisitSyntaxNode_PartialClassWithAttribute_IsAddedToCandidates()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"")]
    public partial class MyCommand { }
}";
            var receiver = BuildReceiver(source);
            Assert.Single(receiver.CandidateClasses);
            Assert.Equal("MyCommand", receiver.CandidateClasses[0].Identifier.ValueText);
        }

        [Fact]
        public void OnVisitSyntaxNode_NonPartialClass_IsNotAddedToCandidates()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"")]
    public class NotPartial { }
}";
            var receiver = BuildReceiver(source);
            Assert.Empty(receiver.CandidateClasses);
        }

        [Fact]
        public void OnVisitSyntaxNode_PartialClassWithoutAttribute_IsNotAddedToCandidates()
        {
            var source = @"
namespace MyAddin
{
    public partial class NoAttribute { }
}";
            var receiver = BuildReceiver(source);
            Assert.Empty(receiver.CandidateClasses);
        }

        [Fact]
        public void OnVisitSyntaxNode_MultiplePartialClasses_AllAddedToCandidates()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [SomeAttr]
    public partial class ClassA { }

    [AnotherAttr]
    public partial class ClassB { }
}";
            var receiver = BuildReceiver(source);
            Assert.Equal(2, receiver.CandidateClasses.Count);
        }

        [Fact]
        public void OnVisitSyntaxNode_PlainClass_IsIgnored()
        {
            var source = @"
namespace MyAddin
{
    public class PlainClass { }
}";
            var receiver = BuildReceiver(source);
            Assert.Empty(receiver.CandidateClasses);
        }
    }
}
