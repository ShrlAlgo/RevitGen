using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RevitGen.Generator;
using Xunit;

namespace RevitGen.Tests
{
    /// <summary>
    /// Integration tests for <see cref="RevitCommandGenerator"/> using in-memory Roslyn compilation.
    /// </summary>
    public class RevitCommandGeneratorTests
    {
        /// <summary>
        /// Runs the source generator against the supplied C# source code and returns
        /// all generated source files as (hintName → sourceText) pairs.
        /// </summary>
        private static IReadOnlyDictionary<string, string> RunGenerator(string userSource)
        {
            var attributeSource = @"
using System;
namespace RevitGen.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RevitCommandAttribute : Attribute
    {
        public string Text { get; }
        public string TabName { get; set; } = ""RevitGen"";
        public string PanelName { get; set; } = ""Commands"";
        public string Icon { get; set; } = """";
        public string ToolTip { get; set; } = """";
        public bool UsingTransaction { get; set; } = true;
        public RevitCommandAttribute(string text) { Text = text; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class CommandHandlerAttribute : Attribute { }
}";

            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: new[]
                {
                    CSharpSyntaxTree.ParseText(attributeSource, parseOptions),
                    CSharpSyntaxTree.ParseText(userSource, parseOptions)
                },
                references: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location)
                },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new RevitCommandGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
                .WithUpdatedParseOptions(parseOptions);

            driver = driver.RunGenerators(compilation);
            var result = driver.GetRunResult();

            return result.GeneratedTrees
                .ToDictionary(
                    t => System.IO.Path.GetFileName(t.FilePath),
                    t => t.GetText().ToString());
        }

        // ── Basic generation ──────────────────────────────────────────────────

        [Fact]
        public void Generator_WithValidCommand_ProducesPartialClassFile()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test Command"")]
    public partial class MyCommand
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);

            Assert.Contains("MyCommand.g.cs", files.Keys);
        }

        [Fact]
        public void Generator_WithValidCommand_ProducesApplicationFile()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test Command"")]
    public partial class MyCommand
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);

            Assert.Contains("RevitGenApplication.g.cs", files.Keys);
        }

        [Fact]
        public void Generator_WithNoCommands_ProducesNoCommandFiles()
        {
            var source = @"
namespace MyAddin
{
    public class PlainClass { }
}";
            var files = RunGenerator(source);

            // No command files; only the debug log may be emitted
            Assert.DoesNotContain(files.Keys, k => k.EndsWith(".g.cs") && k != "RevitGen_Debug_Log.g.cs");
        }

        // ── Partial class content ─────────────────────────────────────────────

        [Fact]
        public void GeneratedPartialClass_ImplementsIExternalCommand()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"")]
    public partial class MyCmd
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);
            var content = files["MyCmd.g.cs"];

            Assert.Contains("IExternalCommand", content);
            Assert.Contains("public Result Execute(", content);
        }

        [Fact]
        public void GeneratedPartialClass_ContainsContextProperties()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"")]
    public partial class MyCmd
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);
            var content = files["MyCmd.g.cs"];

            Assert.Contains("UIApplication", content);
            Assert.Contains("UIDocument", content);
            Assert.Contains("Document", content);
            Assert.Contains("ActiveView", content);
            Assert.Contains("Result", content);
            Assert.Contains("ErrorMessage", content);
        }

        [Fact]
        public void GeneratedPartialClass_WithUsingTransaction_WrapsInTransaction()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"", UsingTransaction = true)]
    public partial class MyCmd
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);
            var content = files["MyCmd.g.cs"];

            Assert.Contains("new Transaction(", content);
            Assert.Contains("trans.Start()", content);
            Assert.Contains("trans.Commit()", content);
        }

        [Fact]
        public void GeneratedPartialClass_WithoutUsingTransaction_NoTransactionCode()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"", UsingTransaction = false)]
    public partial class MyCmd
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);
            var content = files["MyCmd.g.cs"];

            Assert.DoesNotContain("new Transaction(", content);
        }

        [Fact]
        public void GeneratedPartialClass_DefaultUsingTransaction_WrapsInTransaction()
        {
            // UsingTransaction defaults to true, so transaction code should be generated
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"")]
    public partial class MyCmd
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);
            var content = files["MyCmd.g.cs"];

            // Default is true, so transaction wrapper should be present
            Assert.Contains("new Transaction(", content);
        }

        // ── Application class content ─────────────────────────────────────────

        [Fact]
        public void GeneratedApplicationClass_ContainsOnStartup()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"")]
    public partial class MyCmd
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);
            var content = files["RevitGenApplication.g.cs"];

            Assert.Contains("OnStartup", content);
            Assert.Contains("OnShutdown", content);
            Assert.Contains("IExternalApplication", content);
        }

        [Fact]
        public void GeneratedApplicationClass_IncludesButtonText()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""My Button"")]
    public partial class MyCmd
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);
            var content = files["RevitGenApplication.g.cs"];

            Assert.Contains("My Button", content);
        }

        [Fact]
        public void GeneratedApplicationClass_IncludesToolTip()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"", ToolTip = ""My ToolTip"")]
    public partial class MyCmd
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);
            var content = files["RevitGenApplication.g.cs"];

            Assert.Contains("My ToolTip", content);
        }

        [Fact]
        public void GeneratedApplicationClass_WithMultipleCommands_RegistersAllButtons()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Command One"")]
    public partial class CmdOne
    {
        [CommandHandler]
        private void Run() { }
    }

    [RevitCommand(""Command Two"")]
    public partial class CmdTwo
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);
            var appContent = files["RevitGenApplication.g.cs"];

            Assert.Contains("Command One", appContent);
            Assert.Contains("Command Two", appContent);
            Assert.Contains("CmdOne.g.cs", files.Keys);
            Assert.Contains("CmdTwo.g.cs", files.Keys);
        }

        // ── Edge cases ────────────────────────────────────────────────────────

        [Fact]
        public void Generator_NonPartialClassWithAttribute_IsIgnored()
        {
            // Non-partial classes should not be picked up by the SyntaxReceiver
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"")]
    public class NotPartial
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);

            Assert.DoesNotContain("NotPartial.g.cs", files.Keys);
            Assert.DoesNotContain("RevitGenApplication.g.cs", files.Keys);
        }

        [Fact]
        public void Generator_WithPathIcon_UsesLoadImageFromEmbeddedResource()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"", Icon = ""Resources/icon.png"")]
    public partial class MyCmd
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);
            var content = files["RevitGenApplication.g.cs"];

            Assert.Contains("LoadImageFromEmbeddedResource", content);
        }

        [Fact]
        public void Generator_WithResourceNameIcon_UsesBitmapToImageSource()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"", Icon = ""MyIcon"")]
    public partial class MyCmd
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);
            var content = files["RevitGenApplication.g.cs"];

            Assert.Contains("BitmapToImageSource", content);
        }

        [Fact]
        public void Generator_WithCustomTabAndPanel_GeneratesCorrectRibbonCode()
        {
            var source = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"", TabName = ""MyTab"", PanelName = ""MyPanel"")]
    public partial class MyCmd
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(source);
            var content = files["RevitGenApplication.g.cs"];

            Assert.Contains("MyTab", content);
            Assert.Contains("MyPanel", content);
            Assert.Contains("CreateRibbonTab", content);
            Assert.Contains("CreateRibbonPanel", content);
        }

        // ── Dynamic compilation validation ────────────────────────────────────

        /// <summary>
        /// Minimal stubs for the Revit API types referenced by the generated command
        /// partial class.  These provide enough type information for the C# compiler to
        /// resolve all symbols; they are never executed at runtime.
        /// </summary>
        private const string RevitApiStubs = @"
namespace Autodesk.Revit.DB
{
    public class Document { }
    public class View { }
    public class ElementSet { }
    public class Transaction : System.IDisposable
    {
        public Transaction(Document doc, string name) { }
        public void Start() { }
        public void Commit() { }
        public void RollBack() { }
        public void Dispose() { }
    }
}
namespace Autodesk.Revit.UI
{
    public enum Result { Succeeded, Cancelled, Failed }
    public class UIDocument
    {
        public Autodesk.Revit.DB.Document Document => new Autodesk.Revit.DB.Document();
        public Autodesk.Revit.DB.View ActiveView => null;
    }
    public class UIApplication { public UIDocument ActiveUIDocument => new UIDocument(); }
    public class ExternalCommandData { public UIApplication Application => new UIApplication(); }
    public interface IExternalCommand
    {
        Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements);
    }
}
namespace Autodesk.Revit.Attributes
{
    public enum TransactionMode { Manual, Automatic }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class TransactionAttribute : System.Attribute
    {
        public TransactionAttribute(TransactionMode mode) { }
    }
}";

        /// <summary>
        /// Compiles the supplied source trees together and returns the list of
        /// <see cref="Diagnostic"/> instances with severity Error or higher.
        /// </summary>
        private static IReadOnlyList<Diagnostic> CompileSources(params string[] sources)
        {
            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

            var trees = sources
                .Select(s => CSharpSyntaxTree.ParseText(s, parseOptions))
                .ToArray();

            var compilation = CSharpCompilation.Create(
                assemblyName: "DynamicValidationAssembly",
                syntaxTrees: trees,
                references: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location)
                },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();
        }

        [Fact]
        public void GeneratedPartialClass_CompilesWithoutErrors_WhenCombinedWithUserClassAndStubs()
        {
            // Arrange – run the generator to obtain the generated command partial class.
            var userSource = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"")]
    public partial class MyCmd
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(userSource);
            var generatedPartialClass = files["MyCmd.g.cs"];

            // The user-side partial class (without the generator attribute declarations,
            // since those were already resolved during generation).
            var userPartialClass = @"
namespace MyAddin
{
    public partial class MyCmd
    {
        private void Run() { }
    }
}";

            // Act – compile generated code + user partial class + Revit API stubs.
            var errors = CompileSources(generatedPartialClass, userPartialClass, RevitApiStubs);

            // Assert – no compilation errors.
            Assert.Empty(errors);
        }

        [Fact]
        public void GeneratedPartialClass_WithoutTransaction_CompilesWithoutErrors()
        {
            var userSource = @"
using RevitGen.Attributes;
namespace MyAddin
{
    [RevitCommand(""Test"", UsingTransaction = false)]
    public partial class MyCmd
    {
        [CommandHandler]
        private void Run() { }
    }
}";
            var files = RunGenerator(userSource);
            var generatedPartialClass = files["MyCmd.g.cs"];

            var userPartialClass = @"
namespace MyAddin
{
    public partial class MyCmd
    {
        private void Run() { }
    }
}";

            var errors = CompileSources(generatedPartialClass, userPartialClass, RevitApiStubs);

            Assert.Empty(errors);
        }
    }
}
