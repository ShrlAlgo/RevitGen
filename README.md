# RevitGen

A powerful Roslyn source generator that accelerates Revit add-in development by automatically generating boilerplate code for external commands, ribbon buttons, and application registration.

一个基于 Roslyn 的强大源生成器，通过自动生成外部命令、功能区按钮和应用注册所需的样板代码，大幅加速 Revit 插件开发。

---

## Project Architecture / 项目架构

| Project | Description |
|---------|-------------|
| `RevitGen.Common` | Defines the `[RevitCommand]` and `[CommandHandler]` attributes consumed by user code. |
| `RevitGen.Generator` | The Roslyn `ISourceGenerator` that reads those attributes and emits C# at compile-time. |
| `RevitGen` | NuGet packaging project — bundles the above two DLLs into a single installable package. |
| `RevitGen.Tests` | xUnit test project that validates the generator logic using in-memory Roslyn compilations. |
| `RevitGenTest` | A sample Revit add-in project that demonstrates real-world usage of the package. |

---

## Prerequisites / 前置条件

- The consuming project **must** use the new **SDK-style** `.csproj` format.  
  If your project is not SDK-style, install the **.NET Upgrade Assistant** extension from  
  `https://marketplace.visualstudio.com/vs` (or via Visual Studio → Extensions → Manage Extensions),  
  then right-click the project → **Upgrade** → *Convert project to SDK style*.

- 使用该包的项目必须采用新的 `.Net SDK` 样式。如不是，可通过 VS 扩展市场搜索安装 `.NET Upgrade Assistant`，然后右键项目 → 升级 → 将项目转换为 SDK 样式。

---

## Getting Started / 快速开始

### 1. Install the NuGet Package / 安装 NuGet 包

**Local development (from source) / 本地开发（从源代码）：**

1. Right-click the `RevitGen` project → **Pack** to generate a `.nupkg`.
2. Update `nuget.config` to point at the folder containing the generated package.

**Published package:**

```xml
<PackageReference Include="RevitGen" Version="1.0.3" />
```

### 2. Decorate Your Command Class / 标记命令类

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitGen.Attributes;
using RevitGenTest.Properties;

namespace MyAddin
{
    // Example 1 – icon loaded from a .resx resource (embedded resource)
    // 示例 1 – 图标来自 .resx 资源（嵌入式资源）
    [RevitCommand("My First Command",
        ToolTip    = "An auto-generated Revit command!",
        PanelName  = "Core Features",
        Icon       = nameof(Resources.CodeList_32px))]
    public partial class RevitAddinOne
    {
        [CommandHandler]
        private void Run()
        {
            var walls = new FilteredElementCollector(Document)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .ToElements();

            if (walls.Count == 0)
            {
                this.ErrorMessage = "No walls found in the project.";
                this.Result = Result.Failed;
                return;
            }

            TaskDialog.Show("Success", $"Found {walls.Count} wall(s).");
        }
    }

    // Example 2 – icon loaded from an embedded file path
    // 示例 2 – 图标来自嵌入式文件路径
    [RevitCommand("My Second Command",
        ToolTip    = "Another auto-generated command!",
        PanelName  = "Core Features",
        Icon       = "Resources/CodeList_32px.png")]
    public partial class RevitAddinTwo
    {
        [CommandHandler]
        private void Run()
        {
            TaskDialog.Show("Hello", "Command executed!");
        }
    }
}
```

---

## Attribute Reference / 特性参数说明

### `[RevitCommand(text, ...)]`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `text` *(required)* | `string` | — | Button label shown in the ribbon. / 功能区按钮显示的文字。 |
| `TabName` | `string` | `"RevitGen"` | Ribbon tab name. / 功能区选项卡名称。 |
| `PanelName` | `string` | `"Commands"` | Ribbon panel name. / 功能区面板名称。 |
| `Icon` | `string` | `""` | Resource name (no extension) for `.resx`-embedded icons, or relative file path for embedded-resource files. / `.resx` 中的资源名（无扩展名），或嵌入式资源文件的相对路径。 |
| `ToolTip` | `string` | `""` | Tooltip shown on hover. / 鼠标悬停时显示的提示文字。 |
| `UsingTransaction` | `bool` | `true` | When `true` (default), the generated `Execute` method wraps the handler in a Revit transaction. Set to `false` for read-only commands. / 为 `true`（默认）时，自动为命令包裹 Revit 事务。只读命令可设为 `false`。 |

### `[CommandHandler]`

Mark exactly **one** parameterless `void` method per class as the command entry point.  
每个命令类中标记**一个**无参数、返回 `void` 的方法作为命令执行入口。

---

## Auto-Generated Members / 自动生成的成员

The generator produces a second `partial` half of your class that exposes:

生成器会为你的 `partial` 类自动生成以下成员：

| Member | Type | Description |
|--------|------|-------------|
| `UIApplication` | `UIApplication` | The active Revit application. |
| `UIDocument` | `UIDocument` | The active UI document. |
| `Document` | `Document` | The active Revit document. |
| `ActiveView` | `View` | The currently active view. |
| `Result` | `Result` | Set to `Result.Failed` or `Result.Cancelled` to control the command outcome. |
| `ErrorMessage` | `string` | Set to populate Revit's error message. |
| `ElementSet` | `ElementSet` | Passed from the `Execute` method signature. |

---

## Building and Testing / 构建与测试

```bash
# Restore all projects
dotnet restore RevitGen.sln

# Build the source generator and common library
dotnet build RevitGen.Generator/RevitGen.Generator.csproj
dotnet build RevitGen.Common/RevitGen.Common.csproj

# Run all unit tests
dotnet test RevitGen.Tests/RevitGen.Tests.csproj
```

---

## FAQ

**Q: Why don't I see any generated files in my project?**  
A: Make sure your class is declared `partial` and is decorated with `[RevitCommand(...)]`. The generator only processes `partial` classes. Also verify that `RevitGen.Common` and `RevitGen.Generator` are both referenced (the NuGet package handles this automatically).

**Q: Can I use RevitGen with a non-SDK-style project (.csproj)?**  
A: Source generators require the SDK-style project format. Use the **.NET Upgrade Assistant** Visual Studio extension to convert your project (right-click → **Upgrade** → *Convert project to SDK style*).

**Q: My command runs but no Ribbon button appears. What should I check?**  
A: Make sure the generated `RevitGenApplication` class is registered as an `IExternalApplication` in your `.addin` manifest. The generator produces `RevitGenApplication.g.cs` in the `RevitGen.Runtime` namespace; reference it from your manifest file.

**Q: `UsingTransaction = false` — when should I use it?**  
A: Set `UsingTransaction = false` for read-only commands (e.g., selecting elements, showing dialogs, reporting data) that do not modify the Revit model. This avoids the overhead of starting an unnecessary transaction.

**Q: How do I load an icon for my button?**  
A: Two approaches are supported:
- **Embedded file** – set `Icon` to a relative file path with an extension, e.g. `Icon = "Resources/MyIcon.png"`, and mark the file as an *Embedded Resource* in your project.
- **ResX resource** – add the image to a `.resx` file and set `Icon` to the resource name (no extension), e.g. `Icon = nameof(Resources.MyIcon)`.

---

## Contributing / 贡献指南

1. Fork the repository and create a feature branch from `main`.
2. Make your changes and add or update unit tests in `RevitGen.Tests`.
3. Ensure all tests pass with `dotnet test RevitGen.Tests/RevitGen.Tests.csproj`.
4. Open a Pull Request targeting the `dev` branch for review.

---

## License / 许可证

This project is licensed under the terms in the [LICENSE](LICENSE) file.
