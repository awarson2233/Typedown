# Phase 13 UI Migration Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 以低风险批次把页面级 UI 内容迁入 `Dev\Typedown.UI`，让 `Typedown.WinUI` 逐步收敛为 WinUI3 shell、平台服务、WebView2 host 和启动/打包入口。

**Architecture:** `Typedown.UI` 继续保持平台中立 UI 编排层，允许依赖 `Typedown.Core.Contracts`，禁止依赖 `Typedown.WinUI`、`Typedown.XamlUI`、`Microsoft.UI.Xaml` 或 `Windows.UI.Xaml`。`Typedown.WinUI` 只做框架适配：Window/Frame、platform services、WebView2 host、MSIX/Unpackaged 启动。

**Tech Stack:** .NET 9, WinUI 3, Windows App SDK, WebView2, MSTest architecture tests, MVVM without framework-specific UI dependencies.

---

## Scope

Phase 13 不做 WinUI3 默认启动切换，不做 ARM64，不删除 legacy `Typedown.XamlUI`，不升级 React/CRA 前端。Phase 13 只迁移低风险 UI 资源、页面状态和无平台服务依赖的 UI composition。

## Phase 13 Inventory

清点命令：

```powershell
rg -n "Windows\.UI\.Xaml|Microsoft\.UI\.Xaml|Page|UserControl|ResourceDictionary|Converter|IValueConverter|DataContext|ViewModel" Dev\Typedown Dev\Typedown.Core Dev\Typedown.XamlUI Dev\Typedown.WinUI -S
```

结果结论：原始输出非常大，主要噪声来自 `Dev\Typedown.XamlUI\buildTransitive\AppManifest.xml` 和 `Dev\Typedown.Core\Resources\Strings\**\*.resw` 的模板文本。Phase 13 第一批只迁移当前 WinUI smoke 页面里的纯展示状态/文本，不迁移 legacy XAML host、WebView2 host、WinUI platform services 或 Core 多语言资源。

### Safe resources

- `Dev\Typedown.UI\ViewModels\MainPageViewModel.cs` 中的标题、副标题、迁移边界说明、deferred 说明文本。
- 后续可新增 `Dev\Typedown.UI\Resources\MainPageTextResources.cs` 一类平台中立资源容器，用于承接 smoke 页面静态文本。

### Safe view state

- `Dev\Typedown.UI\ViewModels\MainPageViewModel.cs`
- `Dev\Typedown.UI\ViewModels\MigrationBoundaryItem.cs`
- `Dev\Typedown.UI\Mvvm\ObservableObject.cs`
- `Dev\Typedown.UI\Mvvm\RelayCommand.cs`
- `Dev\Typedown.UI\Composition\ServiceCollectionExtensions.cs`

### Deferred platform UI

- `Dev\Typedown.WinUI\Views\MainPage.xaml`
- `Dev\Typedown.WinUI\Views\MainPage.xaml.cs`
- `Dev\Typedown.WinUI\Controls\WinUIEditorHost.cs`
- `Dev\Typedown.WinUI\Controls\WinUIEditorHostController.cs`
- `Dev\Typedown.WinUI\Controls\WinUIEditorBridgeAdapter.cs`
- `Dev\Typedown.WinUI\Services\WinUIPlatformServices.cs`
- `Dev\Typedown.WinUI\Services\WinUIDialogService.cs`
- `Dev\Typedown.WinUI\Services\WinUIFilePickerService.cs`
- `Dev\Typedown.WinUI\Services\WinUIAppActivationService.cs`
- `Dev\Typedown.WinUI\Package.appxmanifest`
- `Dev\Typedown.WinUI\Properties\launchSettings.json`

### Deferred legacy host

- `Dev\Typedown\Controls\MarkdownEditor.cs`
- `Dev\Typedown.Core\ViewModels\*.cs`
- `Dev\Typedown.Core\Resources\Strings\**\*.resw`
- `Dev\Typedown.XamlUI\*.cs`
- `Dev\Typedown.XamlUI\Resources\*.xaml`
- `Dev\Typedown.XamlUI\buildTransitive\*`
- `Dev\Typedown.XamlUI\lib\*`
- `Dev\Typedown.XamlUI\runtimes\*`

## Task 1: 清点可迁移 UI 面

**Files:**
- Modify: `docs\phase13-ui-migration-plan.md`
- Modify: `docs\winui3-post-phase9-roadmap.md`

- [x] **Step 1: 生成候选清单**

运行：

```powershell
rg -n "Windows\.UI\.Xaml|Microsoft\.UI\.Xaml|Page|UserControl|ResourceDictionary|Converter|IValueConverter|DataContext|ViewModel" Dev\Typedown Dev\Typedown.Core Dev\Typedown.XamlUI Dev\Typedown.WinUI -S
```

预期：得到 legacy UI、Core UI 耦合点、WinUI shell 当前 UI 面的候选列表。

- [x] **Step 2: 分类候选项**

在本文档追加 `Phase 13 Inventory` 小节，按以下四类记录候选文件：

```text
1. Safe resources: 资源、字符串、纯数据描述，不依赖平台服务。
2. Safe view state: ViewModel、展示状态、命令描述，不引用 XAML framework。
3. Deferred platform UI: picker/dialog/window/WebView2/activation 相关，保留在 Typedown.WinUI。
4. Deferred legacy host: XamlUI host/run loop/HWND/PRI/WinRT 相关，Phase 15 前不移动。
```

- [x] **Step 3: 更新路线图**

在 `docs\winui3-post-phase9-roadmap.md` 的 Phase 13 中补充实际候选清单链接和第一批迁移范围。

## Task 2: 加强架构边界测试

**Files:**
- Modify: `Tests\Typedown.ArchitectureTests\Phase10CoreContractsBoundaryTests.cs`

- [x] **Step 1: 添加 UI 禁止引用测试**

在 architecture tests 中覆盖：

```csharp
AssertNoTypeReference(uiProjectSource, "Microsoft.UI.Xaml");
AssertNoTypeReference(uiProjectSource, "Windows.UI.Xaml");
AssertNoTypeReference(uiProjectSource, @"..\Typedown.WinUI\Typedown.WinUI.csproj");
AssertNoTypeReference(uiProjectSource, @"..\Typedown.XamlUI\Typedown.XamlUI.csproj");
```

预期：测试继续通过，证明 Phase 13 前置边界稳定。

- [x] **Step 2: 添加 WinUI shell ownership 测试**

锁定以下职责仍在 `Typedown.WinUI`：

```text
Controls\WinUIEditorHost.cs
Services\WinUIPlatformServices.cs
Package.appxmanifest
Properties\launchSettings.json
```

预期：后续 worker 不会把 WebView2 host、平台服务或启动配置误迁到 `Typedown.UI`。

- [x] **Step 3: 运行架构测试**

运行：

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug /nologo /v:minimal
```

预期：全部通过。

## Task 3: 迁移第一批纯 UI 状态

**Files:**
- Modify: `Dev\Typedown.UI\ViewModels\*.cs`
- Modify: `Dev\Typedown.UI\Composition\ServiceCollectionExtensions.cs`
- Modify: `Dev\Typedown.WinUI\Views\MainPage.xaml.cs`

- [x] **Step 1: 从 WinUI code-behind 查找纯展示状态**

运行：

```powershell
rg -n "public .*\\{ get;|ObservableCollection|IReadOnlyList|Title|Subtitle|Summary|Status|Command" Dev\Typedown.WinUI\Views Dev\Typedown.WinUI\Controls Dev\Typedown.UI -S
```

预期：只迁移不依赖 `Microsoft.UI.Xaml`、`WebView2`、window handle、dispatcher 的纯状态。

- [x] **Step 2: 将纯展示状态移动到 `Typedown.UI`**

迁移规则：

```text
可以进入 Typedown.UI: 标题、副标题、边界说明、状态文本、纯 RelayCommand、纯 ViewModel。
留在 Typedown.WinUI: FrameworkElement、Page、WebView2、Window、DispatcherQueue、FileOpenPicker、ContentDialog。
```

- [x] **Step 3: 通过 DI 暴露新增 ViewModel**

在 `AddTypedownUI()` 中注册新增 UI ViewModel，保持 `Typedown.WinUI` 只解析接口或具体 ViewModel，不在 code-behind 中组装业务状态。

- [x] **Step 4: 运行最小验证**

运行：

```powershell
dotnet build .\Dev\Typedown.UI\Typedown.UI.csproj -c Debug /nologo /v:minimal
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug /nologo /v:minimal
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false
```

预期：0 errors。

## Task 4: 迁移低风险资源和文本

**Files:**
- Create or Modify: `Dev\Typedown.UI\Resources\*.cs`
- Modify: `Dev\Typedown.UI\Typedown.UI.csproj`
- Modify: `Dev\Typedown.WinUI\Views\MainPage.xaml`

- [x] **Step 1: 只迁移非 XAML framework 资源**

允许迁移：

```text
静态文本、迁移边界描述、状态标签、纯颜色/尺寸 token 的平台中立表示。
```

禁止迁移：

```text
ResourceDictionary、Style、ControlTemplate、ThemeResource、Acrylic/Mica、WebView2 资源。
```

- [x] **Step 2: 从 ViewModel 或 resource provider 引用文本**

让 `MainPage.xaml` 继续绑定 `ViewModel`，不要让 `Typedown.UI` 直接持有 XAML 页面。

- [x] **Step 3: 验证布局不变**

运行 WinUI `Debug|x64` 构建，并手动确认当前页面布局没有因 Phase 13 第一批迁移改变。

## Task 5: Phase 13 收尾验证

**Files:**
- Modify: `docs\build-baseline.md`
- Modify: `docs\winui3-post-phase9-roadmap.md`

- [x] **Step 1: 运行完整验证**

```powershell
dotnet build .\Dev\Typedown.UI\Typedown.UI.csproj -c Debug /nologo /v:minimal
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug /nologo /v:minimal
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false
dotnet build .\Typedown.sln -c Debug_Local -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false
```

预期：全部通过。

- [x] **Step 2: 验证 editor static bundle**

```powershell
Test-Path .\Dev\Typedown.WinUI\bin\x64\Debug\net9.0-windows10.0.26100.0\Resources\Statics\index.html
Test-Path .\Dev\Typedown.WinUI\bin\x64\Debug_Local\net9.0-windows10.0.26100.0\Resources\Statics\index.html
```

预期：两个命令都输出 `True`。

- [x] **Step 3: 更新文档状态**

验证记录：

```text
dotnet build Dev\Typedown.UI: 0 warnings, 0 errors
dotnet build Dev\Typedown.Core.Contracts: 0 warnings, 0 errors
dotnet test Tests\Typedown.ArchitectureTests: 53 passed
dotnet build Dev\Typedown.WinUI Debug|x64: 0 warnings, 0 errors
dotnet build Typedown.sln Debug_Local|x64: 0 warnings, 0 errors
Debug Resources\Statics\index.html: True
Debug_Local Resources\Statics\index.html: True
```

2026-04-27 二批集成说明：第一次并行验证时 `Typedown.UI` 构建遇到 `Typedown.Core.Contracts` 输出文件被 Defender 临时锁定的 `CS2012`，随后串行重跑通过；该问题不是代码失败。

注意：`Dev\Typedown\Resources\Statics` 是 ignored 前端生成产物。新 worktree 初始没有该目录时，需先从主工作区复制现有 bundle 或在 `Dev\Typedown.Editor` 重新生成，再验证 WinUI 输出目录。

在 `docs\winui3-post-phase9-roadmap.md` 中把 Phase 13 标记为“第二批已完成”，并记录仍 deferred 的平台 UI、legacy host 项和下一批迁移方向。

## Parallel Strategy

- Task 1 和 Task 2 可以并行：一个 worker 只写文档清单，一个 worker 只写 architecture tests。
- Task 3 必须在 Task 1 清点后执行。
- Task 4 可以和 Task 3 分开 worktree，但不能同时修改 `MainPage.xaml`。
- Task 5 必须串行，由集成 worker 执行。

## Acceptance Gate

- `Typedown.UI` 不引用 WinUI/XamlUI framework 或 shell 项目。
- `Typedown.WinUI` 仍拥有 WebView2 host、platform services、launch profiles 和 package manifest。
- `Debug_Local|x64 + Unpackaged` 和 `Debug|x64` 构建路径均保持可用。
- Phase 13 不改变 UI 布局，不删除 legacy `Typedown.XamlUI`，不处理 ARM64。

## 第二批 runtime contract 状态

- Worker A 抽取 legacy runtime UI state 为 `Dev\Typedown.Core.Contracts\EditorRuntime` 平台中立 DTO：`EditorFormatState`、`EditorMenuItemState`、`EditorMenuState`、`EditorParagraphState`、`EditorContentState`、`EditorTocItem`、`EditorWordCount`。
- `EditorParagraphState.FromMenuState` 保留 legacy checked/enabled 计算路径，用于后续 1:1 UI 还原时脱离 `Typedown.Core`、WinUI/XAML host 复用菜单状态语义。
- `EditorRuntime` DTO 显式锁定 legacy JSON bridge 字段名：`wordCount`、`toc`、`cur`、`word`、`character`、`slug`、`lvl`、`content`、`isSelected`，并覆盖 CodeMirror fallback 的数字 `slug` 到字符串兼容。

## Phase 13 第二批: legacy 文本资源读取层

第二批在 `Typedown.UI.Resources` 增加平台中立 legacy `.resw` 读取/索引层，当前只覆盖 `en`、`zh-Hans`、`zh-Hant` 的 `CommonResources`、`DialogResources`、`Resources`、`SettingsResources`。读取策略是运行时解析现有 `Dev/Typedown.Core/Resources/Strings` 下的 `.resw`，不复制 75 语言资源，不迁移 XAML `ResourceDictionary`、Style 或 Converter；索引过滤 `.resw` 模板样例 key，并支持未知 culture 与缺失 localized key 回退到 `en`。当前三种 culture 的四个目标 group key 集合一致，没有发现无法 1:1 承载的文本 key。
补强测试覆盖 `en` / `zh-Hans` / `zh-Hant` 与四个资源 group 的 key count、key shape、模板 key 排除、DialogResources 真实 key、zh-Hant 差异文本、未知 culture fallback 和未知 key 返回 `null`。

## Phase 13 settings/shortcut 契约状态

settings/shortcut worker 在 `Typedown.Core.Contracts.Settings` 增加平台中立 DTO：`EditorShortcutModifierFlags`、`EditorShortcutKey`、`TypedownDefaultShortcuts`、`SettingsUiSnapshot`。默认快捷键快照来自 legacy `SettingsViewModel.Shortcut.cs` 的当前默认值，只保存 modifier flags 与稳定 virtual-key numeric code，不引用 `Windows.System.VirtualKey`、WinUI、XamlUI 或 legacy ViewModel。

第三批 settings 契约只保留 legacy `SettingsViewModel` 中存在的单一设置属性和三组图片插入明细，不增加新聚合字段。`SettingsUiSnapshot` 不接入 `Config`、文件系统或旧持久化逻辑；JSON 字段通过 `JsonPropertyName` 锁定为 legacy camelCase 形状，包括 `editorAreaWidth`、`spellcheckEnabled`、`spellcheckLang`、`insertClipboardImageAction/copyPath/useUploadConfigId`、`insertLocalImageAction/copyPath/useUploadConfigId`、`insertWebImageAction/copyPath/useUploadConfigId`，不包含 `fontFamily` 或 `imageInsertStrategy`。
