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
