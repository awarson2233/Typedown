# Phase 14 UI Visual Migration Plan

**Goal:** 把 Phase 14 定义为首批 `1:1` 可视 UI 迁移，而不是默认启动路径切换。该阶段只为 WinUI3 页面建立可视骨架、状态来源边界和验收清单，不实现新的平台 UI 或 editor host。

**Architecture:** `Typedown.UI` 继续作为平台中立 UI 编排层，承接 shell chrome、command surface、status 和 side panel 的状态来源与 view-model 组合；`Typedown.WinUI` 继续拥有 WinUI3 页面/XAML、`WinUIEditorHost`、Window/Dialog/FilePicker、Package、`launchSettings` 和启动入口；`Typedown.XamlUI` 继续保留为 legacy 对照路径。

**Out of scope:** Phase 14 不是直接切默认启动，不删除 `Typedown.XamlUI`，不迁移 WebView2 host，不迁移 Window/Dialog/FilePicker/Package/`launchSettings`，不处理 ARM64。

---

## Scope

Phase 14 的唯一目标是建立首批 `1:1` 可视 UI 迁移的架构边界和实施顺序，让 WinUI3 页面能逐步承接当前 shell 的可见骨架，但仍由 `Typedown.WinUI` 承载平台 UI 与 editor host。

首批可视面定义为：

- shell chrome：标题、保存状态、窗口级命令露出位、顶栏/标题栏占位
- command surface：菜单/工具栏命令的可见编排面，不迁移底层执行入口
- status：文档标题、保存标记、只读/编辑状态、搜索/替换开关等可见状态
- side panel：大纲/设置/信息等侧栏占位与显示状态，不迁移窗口级弹出或 picker

这些可视面在 Phase 14 只要求做到 `1:1` 骨架和状态来源边界明确，不要求完成默认启动切换，也不要求完成 legacy `MarkdownEditor` / `WinUIEditorHost` 的宿主替换。

## Ownership Boundary

### Typedown.UI owns

- 平台中立的 UI runtime state 聚合
- shell chrome、command surface、status、side panel 的 view-model / DTO / resource 组合
- 从 `Typedown.Core.Contracts` 读取 editor runtime state、legacy text resources、settings/shortcut、editor command、file/shell visible state 的页面级映射
- 用于 WinUI3 页面绑定的可视语义命名和分组

### Typedown.WinUI owns

- WinUI3 页面、XAML、control template、visual state 和实际布局实现
- `WinUIEditorHost`、`WinUIEditorHostController`、`WinUIEditorBridgeAdapter`
- Window、Dialog、FilePicker、activation、dispatcher 等平台服务
- `Package.appxmanifest`、`Properties\launchSettings.json`、Package/Unpackaged 启动入口

### Legacy Typedown.XamlUI owns until later phases

- 旧 host/run loop/HWND/XAML hosting 路径
- legacy 对照启动入口
- 任何仍未迁完的 WinUI2/UWP XAML host 行为

## Phase 14 Deliverables

1. 写清首批 `1:1` 可视 UI 迁移范围，明确只迁页面可见骨架和状态来源。
2. 锁定 `Typedown.UI` 继续禁止引用 `Microsoft.UI.Xaml`、`Windows.UI.Xaml`、`Typedown.WinUI`、`Typedown.XamlUI`。
3. 锁定 `Typedown.WinUI` 继续拥有 `WinUIEditorHost`、`Package.appxmanifest`、`launchSettings.json`。
4. 明确 `WebView2` host 仍留在 `Typedown.WinUI`，只消费 `Typedown.UI` / `Typedown.Core.Contracts` 暴露的状态与命令面。
5. 为后续 Phase 15 主启动路径切换预留等价核对清单，但本阶段不切入口。

## Visual Mapping Inventory

Phase 14 首批建议按以下顺序推进：

1. shell chrome 可视骨架：标题、保存标记、caption 区状态文案
2. command surface 可视骨架：主命令栏、上下文命令可见性、启用态
3. status 可视骨架：文档状态、搜索/替换状态、只读态、页面提示
4. side panel 可视骨架：大纲/设置/信息等面板切换与占位

以下内容继续留在后续阶段：

- `WinUIEditorHost` 内部实现与 bridge 协议
- legacy `MarkdownEditor` / `Typedown.XamlUI` 删除
- Window/Dialog/FilePicker 平台迁移
- Package/Unpackaged 默认入口切换
- ARM64 restore/build/package 验证

## Exit Criteria

Phase 14 完成的标志应当是：

- 文档和架构测试把“首批 `1:1` 可视 UI 迁移”边界锁定下来
- `Typedown.UI` 的状态来源边界仍保持平台中立
- `Typedown.WinUI` 的平台 UI / WebView2 / 启动资产 ownership 未被误迁
- 后续主启动路径切换被顺延到 Phase 15 之后的专门阶段

如果需要切换 `Debug_Local|x64` 默认入口，应作为后续单独阶段执行，并在切换前先完成 WinUI3 与 legacy shell 的等价核对。

## Implementation Status

状态：复制迁移骨架已打通，进入逐控件修复阶段。

当前采用复制优先的迁移方式：先用 shell 命令把 legacy XAML 复制到 `Typedown.WinUI`，并在 `LegacyCopied` 保留原始对照件。生产启动结构已调整为 WinUI3 原生标题栏分层：`Window -> RootControl(title bar row + GlobalFrame) -> MainPage`；`MainPage.xaml` 继续挂载 `MenuBar`、`MainContent`、`StatusBar` 等同名 WinUI3 控件。标题栏只负责图标、应用名和系统窗口按钮区域，`MenuBar` 仍作为标题栏下方的 toolbar/command surface。

为先保证 WinUI3 项目可构建，部分复杂 legacy XAML 子树暂时从 XAML 编译中排除，使用同名 code-only stub 承载尚未恢复的结构。当前仍排除的是菜单项/上下文菜单项和 `FindReplace.xaml`；这些排除项是阶段性脚手架，不是最终 UI 重写方案。后续应继续从 `LegacyCopied` 或 legacy 源复制单个控件，再逐段修 namespace、绑定、attached property、converter 和平台 API 差异。

`MenuBar.xaml` 已作为第一段真实 XAML 恢复进 WinUI3 编译：保留 legacy 的 `muxc:MenuBar`、标题区、settings button 和 drag bar 占位；移除当前 WinUI3 项目不存在的 `ui:XamlWindow.Drag` attached property，避免 XamlCompiler pass2 失败。菜单项仍由同名 stub 承载，真实命令绑定留到后续行为迁移阶段。

`StatusBar.xaml`、`MainContent.xaml`、`EditorContainer.xaml`、`LeftPane.xaml` 和 `SearchPane.xaml` 已恢复进 WinUI3 XAML 编译。当前主框架由真实 XAML 承载顶部菜单、中间三列布局、左侧 NavigationView/SearchPane、编辑器容器、滚动条占位和底部状态栏；`EditorContainer` 的 legacy context flyout 子树暂时移除，因为它会触发 XamlCompiler pass2 失败，后续应单独恢复上下文菜单项。

`RootControl.xaml` 已恢复进 WinUI3 XAML 编译，`App.xaml.cs` 不再直接导航 `MainPage`，而是承载 `RootControl` 并把 `MainPageNavigationContext` 交给内部 `GlobalFrame` 导航。`RootControl` 暴露 `TitleBarElement`，`App.xaml.cs` 通过 `Window.SetTitleBar(...)` 使用 WinUI3 原生标题栏扩展区域；`Window.ExtendsContentIntoTitleBar`、`AppWindow.TitleBar.ExtendsContentIntoTitleBar` 和 `MicaBackdrop` 已启用。legacy `Caption.xaml` 保留为迁移对照/后续可删候选，不再作为内容层 title/toolbar 渲染。

布局重叠问题已做基础修复：`SearchPane` 改为仅在 `IsSearchPaneOpen` 时加载，避免默认覆盖左栏；`EditorInitErrorView` 默认不加载，避免覆盖 `WinUIEditorHost` 并拦截鼠标；`FindReplace` 改为跟随 `IsFindReplaceLoad` 加载。左侧栏初始宽度恢复为 `300`，splitter 已改用 `CommunityToolkit.WinUI.Controls.Sizers` 提供的 WinUI `GridSplitter`，用于替代临时自定义 splitter。

`StatusBar` 左侧侧栏按钮已恢复基础交互：按钮状态不再是静态 `False`，而是通过 `SidePaneOpenChanged` 事件通知 `MainPage`，再调用 `MainContent.SetSidePaneOpen(...)` 切换侧栏展开/收起。当前这是 WinUI3 迁移阶段的轻量状态桥，后续真实 settings/view-model 接入时再替换为原版 `Settings.SidePaneOpen` 双向绑定。

architecture tests 现在区分生产 WinUI3 源与 `LegacyCopied` 对照源：生产源继续禁止 `Windows.UI.Xaml` / `Typedown.XamlUI` 边界泄漏，对照源允许保留 legacy 字符串作为迁移参照。`WinUIEditorHost` 的 ownership 仍锁定在 `Typedown.WinUI`，当前由 `EditorContainer` stub 承载，而不是要求 `MainPage.xaml` 直接包含 host。

下一轮逐段恢复建议顺序：

1. `FindReplace.xaml`：恢复搜索/替换浮层视觉，继续暂缓真实命令。
2. `EditorContainer` context flyout：逐个恢复 `ContextFormatItem`、`ImageItem` 等菜单项，定位 pass2 失败来源。
3. title bar polish：完善原生 title bar 命中区域、caption button 颜色和 Mica/非 Mica 背景细节。

验证记录：

```text
dotnet build .\Dev\Typedown.Core.Contracts\Typedown.Core.Contracts.csproj -c Debug /nologo /v:minimal /m:1 /nodeReuse:false: 0 warnings, 0 errors
dotnet build .\Dev\Typedown.UI\Typedown.UI.csproj -c Debug /nologo /v:minimal /m:1 /nodeReuse:false: 0 warnings, 0 errors
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug /nologo /v:minimal: 79 passed
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false: 0 warnings, 0 errors
```
