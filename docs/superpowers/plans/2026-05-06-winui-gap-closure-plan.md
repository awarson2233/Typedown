# Typedown WinUI Gap Closure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 补齐 Typedown.WinUI 当前最明确的功能缺口，优先恢复菜单模式、编辑器设置、导出配置页和 WinUI 壳层设置消费。

**Architecture:** 这次补齐按“边界先稳定，再补页面接线”的顺序推进。先修 `Presentation -> WinUI Host -> Editor Frontend` 的设置契约，再分别处理 WinUI 壳层设置消费与导出配置页绑定更新，最后补依赖前置修复的 `SpellcheckEnabled` 和可选的 `OpenNewWindow`。

**Tech Stack:** WinUI 3, CommunityToolkit SettingsControls, Typedown.Presentation, WebView2 editor host, React editor frontend, ARM64 MSBuild targeting x64.

---

## 0. 前置判断

- 当前仓库已有 `.worktrees/`，并且已被 git ignore。
- 当前主工作区 `winui3-migration` 存在未提交修改，不能直接作为并行 worker 的共享基线。
- 所有 worker 一律使用独立 worktree。
- 所有 worker 一律使用 `gpt-5.4`。
- 所有 WinUI 构建一律使用 ARM64 版 MSBuild 编译 x64 目标。

## 1. 先决条件

### 1.1 基线提交

- [ ] 先把当前主工作区中“希望作为并行实施基线”的修改做一次 checkpoint commit。
- [ ] 如果当前脏树里有不想带入 worker 的试验性修改，则不要提交，保持只在主工作区存在。

原因：

- git worktree 只能从 commit 出发，不能共享未提交状态。
- 不做 checkpoint，后续 3 个 worker 的基线会和你现在看到的状态不一致。

### 1.2 统一构建命令

所有 worker 都用这条命令验证 WinUI：

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\arm64\MSBuild.exe' `
  'Dev\Typedown.WinUI\Typedown.WinUI.csproj' `
  /restore /m /p:Configuration=Debug /p:Platform=x64
```

涉及架构边界或项目结构时，再补一轮：

```powershell
dotnet test 'Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj'
```

## 2. Worktree 布局

### 2.1 并行批次 1

- [ ] `.worktrees/winui-gap-editor-bridge`
- [ ] `.worktrees/winui-gap-shell-settings`
- [ ] `.worktrees/winui-gap-export-config`

推荐分支名：

- [ ] `work/winui-gap-editor-bridge`
- [ ] `work/winui-gap-shell-settings`
- [ ] `work/winui-gap-export-config`

### 2.2 串行批次 2

等批次 1 合并后，再建：

- [ ] `.worktrees/winui-gap-spellcheck`
- [ ] `.worktrees/winui-gap-open-new-window`（可选）

## 3. 并行拆分

### Worker A: Editor 设置契约修复

**模型：** `gpt-5.4`

**Worktree：** `.worktrees/winui-gap-editor-bridge`

**职责：**

- 修正 `GetSettings()` 初始下发与 `SettingsChanged` 增量通知的命名约定。
- 让查看菜单的三种模式真正作用到 editor 前端。
- 让编辑器设置页中当前已经暴露的设置真正作用到 editor 前端。
- 补齐 `TabSize` 的运行时通知链。

**目标文件：**

- `Dev/Typedown.Presentation/ViewModels/EditorViewModel.cs`
- `Dev/Typedown.Presentation/ViewModels/SettingsViewModel.cs`
- `Dev/Typedown.WinUI/Services/WinUIEditorSettingsNotifier.cs`
- `Dev/Typedown.WinUI/Controls/EditorControls/Hosting/EditorHostContracts.cs`
- `Dev/Typedown.WinUI/Controls/EditorControls/Hosting/WinUIEditorHost.cs`
- 如确有必要，再动：
  - `Dev/Typedown.Editor/src/components/Editor/index.tsx`
  - `Dev/Typedown.Editor/src/components/Muya/index.tsx`
  - `Dev/Typedown.Editor/src/components/CodeMirror/index.tsx`

**必须达成：**

- `SourceCode -> sourceCode`
- `FocusMode -> focusMode`
- `Typewriter -> typewriter`
- `FontSize -> fontSize`
- `LineHeight -> lineHeight`
- `EditorAreaWidth -> editorAreaWidth`
- `AutoPairQuote -> autoPairQuote`
- `AutoPairBracket -> autoPairBracket`
- `AutoPairMarkdownSyntax -> autoPairMarkdownSyntax`
- `TabSize -> tabSize`
- 搜索相关字段也统一保持前端现有 camelCase 约定

**限制：**

- 不改 WinUI 页面结构。
- 不新增新的设置抽象层。
- 优先复用现有 `RemoteInvoke / IEditorSettingsNotifier / IEditorCommandSink` 路径。

**验证：**

- WinUI x64 build 通过。
- 手工 smoke checklist：
  - 查看菜单中的 `源代码模式`
  - `专注模式`
  - `打字机模式`
  - `FontSize`
  - `LineHeight`
  - `EditorAreaWidth`
  - `TabSize`

### Worker B: WinUI 壳层设置消费

**模型：** `gpt-5.4`

**Worktree：** `.worktrees/winui-gap-shell-settings`

**职责：**

- 把外观 / 查看页里已经能改值、但壳层未消费的设置接到 WinUI 现有页面和窗口行为上。

**目标文件：**

- `Dev/Typedown.WinUI/Views/MainPage.xaml`
- `Dev/Typedown.WinUI/Views/MainPage.xaml.cs`
- `Dev/Typedown.WinUI/Controls/EditorControls/MainContent.xaml`
- `Dev/Typedown.WinUI/Controls/EditorControls/MainContent.xaml.cs`
- `Dev/Typedown.WinUI/Controls/EditorControls/StatusBar.xaml`
- `Dev/Typedown.WinUI/Controls/EditorControls/StatusBar.xaml.cs`
- `Dev/Typedown.WinUI/Controls/RootControl.xaml.cs`
- `Dev/Typedown.WinUI/App.xaml.cs`
- 如确有必要，再动：
  - `Dev/Typedown.Presentation/ViewModels/UIViewModel.cs`
  - `Dev/Typedown.WinUI/Pages/SettingPages/ViewPage.xaml(.cs)`

**优先项：**

1. `StatusBarOpen`
2. `SidePaneOpen`
3. `AppTheme`
4. `UseMicaEffect`
5. `Topmost`
6. `AppCompactMode`
7. `UseEditorMicaEffect`
8. `AnimationEnable`

**限制：**

- 尽量使用 WinUI 现有控件与现有页面结构。
- 不引入新的自定义窗口框架。
- 不重写设置页控件，重点是“消费设置值”。

**验证：**

- WinUI x64 build 通过。
- 手工 smoke checklist：
  - 状态栏显示/隐藏
  - 侧栏展开/收起
  - 主题切换
  - Mica 开关
  - Topmost
  - 紧凑模式
  - 动画总开关

### Worker C: 导出配置页接线修复

**模型：** `gpt-5.4`

**Worktree：** `.worktrees/winui-gap-export-config`

**职责：**

- 修正 `ExportConfigPage` 下各子配置页在 WinUI 中的绑定更新机制。
- 让 PDF / HTML / Image 配置页不依赖旧版 UWP 的 Fody 行为。

**目标文件：**

- `Dev/Typedown.WinUI/Pages/SettingPages/ExportConfigPage.xaml.cs`
- `Dev/Typedown.WinUI/Pages/SettingPages/ExportConfigPageParts/PDFConfig.xaml`
- `Dev/Typedown.WinUI/Pages/SettingPages/ExportConfigPageParts/PDFConfig.xaml.cs`
- `Dev/Typedown.WinUI/Pages/SettingPages/ExportConfigPageParts/HTMLConfig.xaml.cs`
- `Dev/Typedown.WinUI/Pages/SettingPages/ExportConfigPageParts/ImageConfig.xaml.cs`

**明确目标：**

- `PDFConfig` 的 `PageSizeComboxItems / PageSizeComboxSelectedItem / PageMarginComboxItems / PageMarginComboxSelectedItem` 在 WinUI 下要可靠刷新。
- `HTMLConfig` 与 `ImageConfig` 的加载、编辑、返回保存路径要可用。
- 保持现有 `SettingsCard / SettingsExpander` 控件族，不回退成旧控件。

**限制：**

- 不重写整页。
- 不复制旧版 UWP 的整套设置控件回来。
- 优先使用 WinUI3 / CommunityToolkit 的已有控件。

**验证：**

- WinUI x64 build 通过。
- 手工 smoke checklist：
  - 新建导出配置
  - 打开 PDF 配置页
  - 切换纸张尺寸预设
  - 切换页边距预设
  - 修改 HTML `Extra Head / Extra Body`
  - 修改 Image `DPI`
  - 返回后重新进入，确认保存成功

## 4. 串行补充

### Worker D: SpellcheckEnabled

**模型：** `gpt-5.4`

**前置依赖：**

- Worker A 已合并。

**Worktree：** `.worktrees/winui-gap-spellcheck`

**职责：**

- 重新启用 `EditorPage` 中的 `SpellcheckEnabled` 设置项。
- 补齐 `SettingsViewModel.notifySet`。
- 补齐 `EditorViewModel.GetSettings()` 初始下发。
- 确认 editor 前端现有 `spellcheckEnabled` 能被实际消费。

**目标文件：**

- `Dev/Typedown.WinUI/Pages/SettingPages/EditorPage.xaml`
- `Dev/Typedown.WinUI/Pages/SettingPages/EditorPage.xaml.cs`
- `Dev/Typedown.Presentation/ViewModels/SettingsViewModel.cs`
- `Dev/Typedown.Presentation/ViewModels/EditorViewModel.cs`
- 如确有必要，再动前端对应消费点

### Worker E: OpenNewWindow（可选）

**模型：** `gpt-5.4`

**前置依赖：**

- 前四项核心缺口已完成。

**Worktree：** `.worktrees/winui-gap-open-new-window`

**职责：**

- 决定是补齐 WinUI 多窗口能力，还是显式降级并调整入口行为。
- 这一项单独排后，避免影响前四项主线收敛。

## 5. 审查与合并顺序

- [ ] 先审 `Worker A`
- [ ] 再审 `Worker C`
- [ ] 再审 `Worker B`
- [ ] 合并 `A + C + B`
- [ ] 然后启动并审 `Worker D`
- [ ] `Worker E` 最后单独决策

原因：

- `Worker A` 直接恢复菜单模式和编辑器设置主链路，收益最高。
- `Worker C` 与 A/B 文件重叠最少，容易先收敛。
- `Worker B` 虽然独立，但 UI 表面改动较多，放在 A/C 之后更容易审。
- `SpellcheckEnabled` 依赖 A 的设置契约修复，不能抢跑。

## 6. Subagent 派发规则

后续真正派发时，每个 subagent 都必须满足：

- `agent_type: worker`
- `model: gpt-5.4`
- `fork_context: false`
- 只给它自己的目标文件、目标行为、验证命令
- 明确告诉它：
  - 你不是独自在代码库中工作
  - 不要回退他人的修改
  - 只处理分配给你的文件责任边界
  - 所有构建都用 ARM64 MSBuild 编译 x64

## 7. 风险点

- 当前主工作区是脏树，不先 checkpoint 就无法保证各 worker 基线一致。
- `Worker A` 如果选择直接改前端 TS，而不是先修 WinUI 侧契约，会把问题扩散到 `Dev/Typedown.Editor`，需要在审查时卡住。
- `Worker B` 最容易出现“值能变但窗口行为不刷新”的半接线状态，审查时必须看实时消费者，不只看 `Binding`。
- `Worker C` 最容易出现“页面能打开但状态不同步”的假阳性，必须要求返回保存再重进验证。

## 8. 执行建议

推荐先执行并行批次 1，只开 3 个 worker：

1. `Editor bridge`
2. `Shell settings`
3. `Export config`

等这 3 个都审过并合入，再开：

4. `Spellcheck`
5. `OpenNewWindow`（可选）

