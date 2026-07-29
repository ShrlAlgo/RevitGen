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
<PackageReference Include="RevitGen" Version="2.0.0" />
```

### 2. Decorate Your Command Class / 标记命令类

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitGen.Attributes;
using RevitGenDemo.Properties;

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
| `Order` | `int` | `0` | Stable order inside a panel or group. / 面板或按钮组内的稳定顺序。 |
| `SmallIcon` | `string` | `""` | 16px embedded icon. / 16px 嵌入图标。 |
| `LargeIcon` | `string` | `""` | 32px embedded icon; falls back to `Icon`. / 32px 嵌入图标，为空时回退到 `Icon`。 |
| `LongDescription` | `string` | `""` | Long button description. / 按钮长说明。 |
| `HelpUrl` | `string` | `""` | Contextual help URL. / 上下文帮助地址。 |
| `AddSeparatorBefore` | `bool` | `false` | Adds a separator before a normal button. / 在普通按钮前添加分隔线。 |
| `GroupName` | `string` | `""` | Commands with the same value form one group. / 同名命令组成按钮组。 |
| `GroupType` | `RibbonGroupType` | `None` | `Pulldown`, `SplitButton`, or `Stacked`. / 下拉、拆分或堆叠按钮。 |

### `[CommandHandler]`

Mark exactly **one** parameterless `void` method per class as the command entry point.  
每个命令类中标记**一个**无参数、返回 `void` 的方法作为命令执行入口。

### `[CommandAvailability]`

Optionally mark one instance method returning `bool`. It may be parameterless or accept one `UIApplication`. The command automatically implements `IExternalCommandAvailability`.

可选标记一个返回 `bool` 的实例方法；方法可以无参数，或接收一个 `UIApplication`。命令会自动实现 `IExternalCommandAvailability`。

```csharp
[CommandAvailability]
private bool CanExecute(UIApplication application)
{
    return application.ActiveUIDocument != null;
}
```

---

## Additional Generators / 其他生成器

### ExternalEvent

```csharp
[RevitExternalEvent(Name = "Refresh model")]
public partial class RefreshExternalEvent
{
    [ExternalEventHandler]
    private void Run(UIApplication application) { }
}

// Must be called from a valid Revit UI context.
var handler = new RefreshExternalEvent();
var externalEvent = handler.CreateExternalEvent();
```

The handler method returns `void` and accepts zero parameters or one `UIApplication`. `ExternalEvent.Create` is never executed from a generated static initializer.

处理方法返回 `void`，接收零个参数或一个 `UIApplication`。生成器不会在静态初始化阶段创建 ExternalEvent。

### Updater

```csharp
[RevitUpdater("Wall geometry updater", "DC7C2E9A-F242-4DFD-BB0C-52B7E5B0B940")]
[UpdaterTrigger((int)BuiltInCategory.OST_Walls, RevitChangeType.Geometry)]
public partial class WallUpdater
{
    [UpdaterHandler]
    private void Run(UpdaterData data) { }
}
```

The generated Bootstrap registers and unregisters the updater. Multiple `[UpdaterTrigger]` attributes are supported. An updater without a trigger produces `REVITGEN105` and is not given an implicit whole-model trigger.

Bootstrap 自动注册和注销 Updater。支持多个 `[UpdaterTrigger]`；未配置触发器时给出 `REVITGEN105`，不会隐式监听整个模型。

### Revit events

```csharp
[RevitEventContainer]
public partial class ApplicationEvents
{
    [RevitEvent(RevitEventKind.Idling)]
    private void OnIdling(object sender, IdlingEventArgs args) { }
}
```

Bootstrap creates one container instance at startup and performs symmetric unsubscribe at shutdown. Supported events are listed by `RevitEventKind`.

Bootstrap 在启动时创建一个事件容器实例，并在关闭时对称取消订阅。支持范围以 `RevitEventKind` 为准。

### DockablePane

```csharp
[RevitDockablePane("A19756A4-2A3A-4488-A3AF-AE15D8698795", "My Pane")]
public partial class MyPane : UserControl
{
}
```

The class must derive from a WPF `FrameworkElement` and be constructible without arguments. RevitGen generates `IDockablePaneProvider` and startup registration.

类型必须继承 WPF `FrameworkElement`，并可通过无参数构造。RevitGen 自动生成 `IDockablePaneProvider` 和启动注册代码。

### Extensible Storage

```csharp
[RevitSchema("31578522-277A-4F10-BAD7-43C68B36AF50", "ComponentData", VendorId = "SZMD")]
public partial class ComponentData
{
    [RevitSchemaField]
    public string Code { get; set; }

    [RevitSchemaField]
    public int Version { get; set; }
}

var entity = data.ToEntity();
var restored = ComponentData.FromEntity(entity);
```

The generated `GetOrCreateSchema()` is idempotent. The first release supports Revit simple field types; unit-bearing and map fields are intentionally excluded from the cross-version baseline.

生成的 `GetOrCreateSchema()` 可重复调用。首版覆盖 Revit 简单字段类型；带单位字段和 Map 字段暂不进入跨版本基线。

### Shared parameters

```csharp
public partial class SharedParameters
{
    [RevitSharedParameter(
        "6DB4557F-69B7-4665-86A7-C821158AC763",
        "Component code",
        Categories = new[] { (int)BuiltInCategory.OST_Walls })]
    public static readonly string ComponentCode = "Component code";
}
```

`GetSharedParameterDefinitions()` returns generated metadata. `EnsureSharedParameterBindings(...)` scans `BindingMap` once and binds every missing definition found in the supplied shared-parameter file. It reports missing external definitions instead of modifying the file silently.

`GetSharedParameterDefinitions()` 返回生成的描述表。`EnsureSharedParameterBindings(...)` 只遍历一次 `BindingMap`，批量绑定共享参数文件中已有的缺失定义；不会静默修改共享参数文件。

---

## Revit Version Compatibility / Revit 版本兼容

- RevitGen 2.x uses Roslyn 4.8 and requires Visual Studio 2022 or an equivalent compiler.
- The generator itself targets `netstandard2.0` and runs only at compile time.
- `RevitGenDemo` verifies Revit 2020 on .NET Framework.
- `RevitGenDemo2025` verifies Revit 2025 on .NET 8.
- Generated code avoids a compile-time `System.Drawing` dependency, so ResX icons work across the runtime boundary.

RevitGen 2.x 使用 Roslyn 4.8，需要 Visual Studio 2022 或等价编译器。生成器本身仍为 `netstandard2.0` 且只在编译期运行；仓库分别使用 Revit 2020 与 Revit 2025 Demo 验证 .NET Framework 和 .NET 8。

---

## Generated Bootstrap / 生成的启动入口

The default manifest entry remains `RevitGen.Runtime.RevitGenApplication`. Existing projects do not need to change it. Projects with their own `IExternalApplication` can call:

默认清单入口仍为 `RevitGen.Runtime.RevitGenApplication`，旧项目无需修改。已有自定义 `IExternalApplication` 的项目可调用：

```csharp
return RevitGen.Runtime.RevitGenBootstrap.Startup(application);
// and in OnShutdown:
return RevitGen.Runtime.RevitGenBootstrap.Shutdown(application);
```

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
