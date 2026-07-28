# RevitGen 2.0 增量生成器设计

## 目标

将现有单一命令生成器升级为模块化 `IIncrementalGenerator`，在保持 `[RevitCommand]` 与 `[CommandHandler]` 现有用法兼容的前提下，补齐诊断和 Ribbon 能力，并增加 ExternalEvent、Updater、Revit 事件、DockablePane、Extensible Storage 与共享参数生成能力。

## 兼容范围

- 生成器目标框架保持 `netstandard2.0`，Roslyn 依赖升级到 4.8。
- 使用方需要 Visual Studio 2022 或等价的 Roslyn 4.8+ 编译环境。
- Revit 2020～2024 的生成代码以 .NET Framework 4.8 和 Revit 2020 API 为最低基线。
- Revit 2025+ 的示例和验证工程使用 `net8.0-windows`。
- 生成器通过编译引用中的 `RevitAPI` 程序集版本选择 API Profile；无法识别时使用 2020 基线并给出 Warning。
- 现有项目升级 NuGet 后不要求修改已有命令代码；新能力均为可选特性。
- RevitGen 1.x 保留为 Roslyn 3.9 兼容线，2.x 不再兼容旧编译器。

## 总体架构

`RevitGen.Generator` 内部按职责拆分，不新增运行时程序集：

1. **Discovery**：使用 `ForAttributeWithMetadataName` 精确发现带特性的类型或方法。
2. **Models**：把 Roslyn Symbol 转换成不可变、可比较的生成模型，不把 Symbol 传入输出阶段。
3. **Validation**：每个模块独立校验并返回模型或诊断。
4. **Emitters**：只根据模型和 Revit API Profile 生成源码。
5. **Bootstrap**：汇总 Ribbon、Updater、事件和 DockablePane 注册项，生成统一启动与关闭入口。
6. **Diagnostics**：集中定义 `REVITGENxxx` 编号、严重级别和消息。

命令代码按类型分别生成，Bootstrap 在 `.Collect()` 后统一生成。所有参与缓存的模型均使用值相等语义，避免无关源码变化触发重新生成。

## 第一阶段：命令生成基础

### 兼容行为

- 保留 `[RevitCommand(text)]`、`TabName`、`PanelName`、`Icon`、`ToolTip` 和 `UsingTransaction`。
- 保留生成的 `UIApplication`、`UIDocument`、`Document`、`ActiveView`、`Result`、`ErrorMessage` 与 `ElementSet`。
- 默认继续生成 `RevitGen.Runtime.RevitGenApplication`，既有 `.addin` 注册无需修改。
- 新增 `RevitGenBootstrap.Startup` 与 `Shutdown`，供已有 `IExternalApplication` 的项目复用。

### 正确性增强

- 使用完全限定类型名和基于命名空间的唯一 HintName。
- 对命名空间、嵌套类型和 C# 字符串进行统一转义。
- 合并同一 partial 类型的多份声明，确保每个类型只生成一次。
- 禁止泛型命令类型；支持全局命名空间和非泛型嵌套 partial 类型。
- 只允许一个实例、无参数、返回 `void` 的 `[CommandHandler]` 方法。
- 不再默认生成调试日志源码；诊断通过 Roslyn Diagnostic 输出。
- 事务未成功启动时不执行提交或回滚；异常时尽力回滚并保留原始异常信息。

### Ribbon 扩展

`RevitCommandAttribute` 新增以下可选属性：

- `Order`：Tab/Panel 内排序，默认 0；同值按完全限定类型名稳定排序。
- `SmallIcon`、`LargeIcon`：分别配置 16px 和 32px 图标；旧 `Icon` 继续作为 LargeIcon 回退值。
- `LongDescription`：按钮长说明。
- `HelpUrl`：配置 URL 类型的 ContextualHelp。
- `AddSeparatorBefore`：在普通按钮前插入分隔线。
- `GroupName`：相同 Tab、Panel、GroupName 的命令组成按钮组。
- `GroupType`：`None`、`Pulldown`、`SplitButton` 或 `Stacked`。

规则：Stacked 只接受 2～3 个命令；Pulldown 和 SplitButton 至少包含一个命令；组类型或组名冲突时报错。所有名称和文本在输出前转义。

新增 `[CommandAvailability]`，标记同一命令类中的一个实例方法。该方法必须返回 `bool`，参数为零个或一个 `UIApplication`。生成的命令类实现 `IExternalCommandAvailability`，并自动设置 `AvailabilityClassName`。

## 第二阶段：执行与注册生成器

### ExternalEvent

- `[RevitExternalEvent]` 标记 partial 类，`[ExternalEventHandler]` 标记唯一执行方法。
- 生成 `IExternalEventHandler` 实现、`UIApplication` 上下文、稳定名称和 `CreateExternalEvent()` 工厂。
- Handler 支持无参数或单个 `UIApplication` 参数，返回 `void`。
- ExternalEvent 只能由调用方在 Revit UI 上下文创建；生成器不在静态初始化中创建它。

### Updater

- `[RevitUpdater(name, guid)]` 标记 partial 类，`[UpdaterHandler]` 标记唯一处理方法。
- 生成 `IUpdater` 所需成员、`UpdaterId`、注册和注销代码。
- `[UpdaterTrigger(category, changeType)]` 可重复配置常用类别及新增、删除、几何和参数变更触发器。
- Handler 接受零个或一个 `UpdaterData` 参数，返回 `void`。
- 无触发器时不隐式注册全模型监听，并给出 Warning。

### Revit 事件

- `[RevitEventContainer]` 标记可实例化的 partial 类。
- `[RevitEvent(RevitEventKind.X)]` 标记事件处理方法。
- 首版覆盖 Idling、ViewActivated、DialogBoxShowing、DocumentOpened、DocumentClosing、DocumentClosed、DocumentChanged、DocumentSaved 和 DocumentSynchronizedWithCentral。
- Bootstrap 在 Startup 创建单例并订阅，在 Shutdown 对称取消订阅。
- 方法签名必须与事件对应参数一致，错误签名在编译期报错。

### DockablePane

- `[RevitDockablePane(guid, title)]` 标记继承自 WPF `FrameworkElement` 的 partial 类。
- 生成 `IDockablePaneProvider` 实现、`DockablePaneId` 和 Bootstrap 注册代码。
- 支持初始停靠位置、浮动区域和最小尺寸配置；不自动实例化非公开构造函数。
- GUID 重复、类型不兼容或缺少无参数构造能力时报错。

## 第三阶段：数据生成器

### Extensible Storage

- `[RevitSchema(guid, name)]` 标记 partial 数据类，`[RevitSchemaField]` 标记属性。
- 生成幂等 `GetOrCreateSchema()`、`Read(Entity)`、`Write(Entity)` 和 Element 读写辅助方法。
- 首版支持 `int`、`short`、`byte`、`bool`、`float`、`double`、`string`、`Guid`、`ElementId`、`XYZ` 及其数组。
- 不在首版生成带单位字段和 Map 字段，避免 Revit 2021 单位 API 变更及复杂键约束。
- Schema GUID、字段名或字段类型冲突在编译期报错；运行时已有同 GUID 不同结构时抛出包含差异信息的异常。

### 共享参数

- `[RevitSharedParameter(guid, name)]` 标记属性或声明类成员。
- 可选配置参数组、实例/类型绑定、BuiltInCategory 集合和可见性。
- 生成只读描述表、GUID 常量及批量 `EnsureBindings` 辅助方法。
- `EnsureBindings` 一次读取 BindingMap 后批量判断，避免循环内重复查询。
- 生成器不修改磁盘上的共享参数文件；缺少定义时返回明确结果，由调用方决定是否创建外部定义。

## Bootstrap 与生命周期

生成 `RevitGen.Runtime.RevitGenBootstrap`：

- `Startup(UIControlledApplication)` 按 Ribbon、DockablePane、Updater、事件顺序注册。
- 任一模块失败时记录模块名并返回 `Result.Failed`，已经完成的可撤销注册按逆序清理。
- `Shutdown(UIControlledApplication)` 按事件、Updater、DockablePane 的逆序释放；Ribbon 无 Revit API 删除入口，不做伪清理。
- 默认 `RevitGenApplication` 仅委托给 Bootstrap，避免模板继续膨胀。

## 诊断策略

- Error：无法生成正确代码或运行行为确定错误，例如签名错误、重复 GUID、非法按钮组。
- Warning：可以生成但配置可能无效，例如未知 Revit 版本、Updater 无触发器、无法静态验证的图标配置。
- Info：迁移建议，不改变编译结果。
- 每个诊断定位到对应特性或方法，而不是统一定位到类声明。
- 诊断从 `REVITGEN001` 连续管理，并补充 analyzer release tracking 文件。

## 测试与验证

每个模块均包含四层测试：

1. 特性解析和默认值测试。
2. 合法及非法输入的诊断测试。
3. 生成文本的关键行为测试。
4. 用户源码、生成源码与最小 Revit Stub 的联合编译测试。

额外建立 Revit 2020、2024、2025 三个兼容样例，分别验证旧 API 基线、.NET Framework 最后版本和 .NET 8 分界。性能测试验证只修改一个命令时，不重新执行无关命令的模型构建与输出步骤。

## 实施顺序

1. 修复当前测试环境和建立基线测试。
2. 升级 Roslyn 并迁移命令生成器到增量管线。
3. 完成诊断、转义、唯一命名和 Bootstrap。
4. 完成 Ribbon 扩展。
5. 依次完成 ExternalEvent、Updater、事件和 DockablePane。
6. 完成 Extensible Storage 与共享参数。
7. 增加 Revit 2020/2024/2025 兼容样例、README 和 NuGet 2.0 打包验证。

每一步都必须保持现有命令测试通过，并为新增能力补齐联合编译测试后再进入下一步。
