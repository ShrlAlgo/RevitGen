using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using RevitGen.Attributes;
using RevitGen.Generator;

using Xunit;

namespace RevitGen.Tests
{
    /// <summary>
    /// 验证 RevitGen 2.0 增量入口、诊断和各独立功能模块。
    /// </summary>
    public class IncrementalGeneratorTests
    {
        /// <summary>
        /// 运行增量生成器并返回完整结果。
        /// </summary>
        private static GeneratorDriverRunResult RunGenerator(string source)
        {
            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10);
            var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string
                ?? throw new InvalidOperationException("无法读取测试运行时程序集列表。");
            var trustedAssemblies = trustedPlatformAssemblies
                .Split(System.IO.Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path));
            var compilation = CSharpCompilation.Create(
                "IncrementalTests",
                new[] { CSharpSyntaxTree.ParseText(source, parseOptions) },
                trustedAssemblies.Concat(new[]
                {
                    MetadataReference.CreateFromFile(typeof(RevitCommandAttribute).Assembly.Location)
                }),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                    new[] { new RevitIncrementalGenerator().AsSourceGenerator() },
                    parseOptions: parseOptions)
                .RunGenerators(compilation);
            return driver.GetRunResult();
        }

        /// <summary>
        /// 将生成树转换为便于断言的文件字典。
        /// </summary>
        private static IReadOnlyDictionary<string, string> GetFiles(GeneratorDriverRunResult result)
        {
            return result.GeneratedTrees.ToDictionary(
                tree => System.IO.Path.GetFileName(tree.FilePath),
                tree => tree.GetText().ToString());
        }

        [Fact]
        public void ValidCommand_GeneratesUniqueCommandAndApplicationFiles()
        {
            var result = RunGenerator(@"
using RevitGen.Attributes;
namespace Demo
{
    [RevitCommand(""测试命令"")]
    public partial class CommandA
    {
        [CommandHandler]
        private void Run() { }
    }
}");
            var files = GetFiles(result);

            Assert.Contains(files.Keys, key => key.EndsWith(".Command.g.cs", StringComparison.Ordinal));
            Assert.Contains("RevitGenApplication.g.cs", files.Keys);
            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void NonPartialCommand_ReportsDiagnostic()
        {
            var result = RunGenerator(@"
using RevitGen.Attributes;
[RevitCommand(""测试"")]
public class InvalidCommand
{
    [CommandHandler]
    private void Run() { }
}");

            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "REVITGEN101");
        }

        [Fact]
        public void RibbonText_IsEscapedInGeneratedApplication()
        {
            var result = RunGenerator(@"
using RevitGen.Attributes;
[RevitCommand(""带\""引号"", ToolTip = ""第一行\n第二行"")]
public partial class EscapedCommand
{
    [CommandHandler]
    private void Run() { }
}");
            var app = GetFiles(result)["RevitGenApplication.g.cs"];

            Assert.Contains("带\\\"引号", app);
            Assert.Contains("第一行\\n第二行", app);
        }

        [Fact]
        public void ExternalEvent_GeneratesHandlerImplementation()
        {
            var result = RunGenerator(@"
using RevitGen.Attributes;
[RevitExternalEvent(Name = ""刷新模型"")]
public partial class RefreshEvent
{
    [ExternalEventHandler]
    private void Run() { }
}");
            var generated = GetFiles(result).Single(item => item.Key.EndsWith(".ExternalEvent.g.cs")).Value;

            Assert.Contains("IExternalEventHandler", generated);
            Assert.Contains("CreateExternalEvent", generated);
        }

        [Fact]
        public void UpdaterWithoutTrigger_GeneratesWarningAndUpdater()
        {
            var result = RunGenerator(@"
using RevitGen.Attributes;
[RevitUpdater(""墙更新"", ""5A8B279B-84A3-41E7-A758-4E35A7E3D77F"")]
public partial class WallUpdater
{
    [UpdaterHandler]
    private void Run() { }
}");
            var files = GetFiles(result);

            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "REVITGEN105");
            Assert.Contains(files.Keys, key => key.EndsWith(".Updater.g.cs"));
        }

        [Fact]
        public void Schema_GeneratesEntityConversions()
        {
            var result = RunGenerator(@"
using RevitGen.Attributes;
[RevitSchema(""F23F2674-3408-4325-B920-A79DD011F9B8"", ""DemoSchema"")]
public partial class SchemaData
{
    [RevitSchemaField]
    public int Count { get; set; }
}");
            var generated = GetFiles(result).Single(item => item.Key.EndsWith(".Schema.g.cs")).Value;

            Assert.Contains("GetOrCreateSchema", generated);
            Assert.Contains("ToEntity", generated);
            Assert.Contains("FromEntity", generated);
        }

        [Fact]
        public void SharedParameters_GenerateSingleBatchBindingHelper()
        {
            var result = RunGenerator(@"
using RevitGen.Attributes;
public partial class Parameters
{
    [RevitSharedParameter(""B1B04ED9-C90F-4553-9F24-2CA3D9F4316E"", ""构件编码"")]
    public static readonly string Code = """";
}");
            var files = GetFiles(result);
            Assert.True(
                files.Keys.Any(key => key.EndsWith(".SharedParameters.g.cs")),
                "生成文件: " + string.Join(", ", files.Keys) +
                "; 诊断: " + string.Join(" | ", result.Diagnostics.Select(item => item.ToString())));
            var generated = files.Single(item => item.Key.EndsWith(".SharedParameters.g.cs")).Value;

            Assert.Contains("EnsureSharedParameterBindings", generated);
            Assert.Equal(1, Count(generated, "ForwardIterator()"));
        }

        [Fact]
        public void InvalidStackedGroup_ReportsConfigurationDiagnostic()
        {
            var result = RunGenerator(@"
using RevitGen.Attributes;
[RevitCommand(""单个堆叠按钮"", GroupName = ""错误分组"", GroupType = RibbonGroupType.Stacked)]
public partial class InvalidStackedCommand
{
    [CommandHandler]
    private void Run() { }
}");

            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "REVITGEN104");
        }

        [Fact]
        public void SchemaArray_UsesArrayFieldAndListConversion()
        {
            var result = RunGenerator(@"
using RevitGen.Attributes;
[RevitSchema(""AC3A543C-E6DC-4BD9-9C23-F4BC37F5E2D2"", ""ArraySchema"")]
public partial class ArraySchemaData
{
    [RevitSchemaField]
    public int[] Values { get; set; }
}");
            var generated = GetFiles(result).Single(item => item.Key.EndsWith(".Schema.g.cs")).Value;

            Assert.Contains("AddArrayField", generated);
            Assert.Contains("IList<int>", generated);
        }

        [Fact]
        public void UnsupportedSchemaField_ReportsConfigurationDiagnostic()
        {
            var result = RunGenerator(@"
using RevitGen.Attributes;
[RevitSchema(""9C3B20A7-6FC3-4415-B910-C9D47951CB59"", ""InvalidSchema"")]
public partial class InvalidSchemaData
{
    [RevitSchemaField]
    public decimal Amount { get; set; }
}");

            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "REVITGEN104");
        }

        /// <summary>
        /// 统计文本出现次数。
        /// </summary>
        private static int Count(string source, string value)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }
    }
}
