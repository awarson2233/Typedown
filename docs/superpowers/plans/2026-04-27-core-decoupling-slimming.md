# Core 解耦与精简实施计划

> **给 agent / subagent 的要求：** 执行本计划时优先使用 `superpowers:subagent-driven-development`，也可以使用 `superpowers:executing-plans` 逐项执行。每个阶段都要小步提交，禁止大范围重写。

**目标：** 将 `Dev/Typedown.Core` 固化为纯逻辑 + MVVM 合同层；完成后删除旧 `Dev/Typedown.Core.Legacy` 与过渡期 `Dev/Typedown.UI`，避免继续保留重复边界。

**架构方向：** `Typedown.Core` 不允许依赖 XAML、WinRT UI 类型、WebView2、文件选择器、窗口服务或 legacy 项目。可复用的纯逻辑与平台无关服务接口进入 Core；shell-agnostic ViewModel、资源读取、编辑器命令组合进入 `Typedown.Presentation`；WinUI3 适配放在 `Typedown.WinUI`。

**技术栈：** .NET 9 class library、MSTest 架构测试、WinUI3 / Windows App SDK shell。

---

## 当前基线

主工作区当前在 `winui3-migration`，已合入：

- `994a97e refactor: decouple core from legacy uwp shell`
- `92442b8 feat: wire WinUI editor menu xaml baseline`
- `fd8a8f7 fix: show WinUI editor context menu`
- `bb7d09a fix: read legacy text resources from legacy core`
- `1301277 docs: plan core decoupling and slimming`

当前结构：

- `Dev/Typedown.Core`：已有 `Editor`、`EditorRuntime`、`Settings`、`Shell`、`Interfaces`，是新的纯 Core 起点。
- `Dev/Typedown.Presentation`：承接 shell-agnostic MVVM、资源读取、组合逻辑。
- `Dev/Typedown.WinUI`：WinUI3 shell、XAML、平台服务适配、WebView2 host。
- `Dev/Typedown.Core.Legacy` 与 `Dev/Typedown.UI` 已退场；当前主线不再把它们作为源码模块或 solution 项目保留。
- `Dev/Typedown.WinUI/LegacyCopied`：已复制的 legacy XAML/code-behind 参考文件，必须排除编译。

当前最大剩余耦合：

- WinUI3 setting 页面和 setting controls 仍有大量 legacy namespace 痕迹，例如 `Typedown.Core.Models`、`Typedown.Core.Services`、`Typedown.Core.ViewModels`。
- 部分 setting XAML/code-behind 当前仍被 csproj 排除，后续需要按页面逐个恢复。
- `Typedown.Presentation.Resources.LegacyTextResourceReader` 仍暂时读取迁入 Presentation 的资源目录；后续资源边界继续由架构测试固化。

---

## 边界规则

### Core 允许包含

- 平台无关 record / enum / value object。
- 编辑器 host 消息合同。
- 编辑器运行态快照。
- settings snapshot/defaults/shortcut 定义。
- shell 状态。
- 平台服务接口，例如 dispatcher、dialog、file picker、window context。

### Core 禁止包含

- `Microsoft.UI.Xaml`
- `Windows.UI.Xaml`
- `Microsoft.Web.WebView2`
- `Windows.Storage.Pickers`
- `Typedown.Core.Legacy`
- 任何 WinUI/UWP 控件、窗口、页面、XAML code-behind。

### UI 允许包含

- 纯 MVVM ViewModel。
- Core state 到 UI state 的组合。
- 资源读取/catalog。
- 不直接触碰 WinUI3 XAML 类型。

### WinUI 允许包含

- XAML / code-behind。
- WebView2 host。
- Windows App SDK 服务适配。
- 把 UI ViewModel 绑定到页面和控件。

---

## 阶段 1：锁死 pure Core 边界

**目标：** 先用架构测试防止后续迁移把 UI 依赖带回 Core。

**重点文件：**

- `Tests/Typedown.ArchitectureTests/Phase10CoreContractsBoundaryTests.cs`
- `Tests/Typedown.ArchitectureTests/Phase13SettingsContractTests.cs`
- `Tests/Typedown.ArchitectureTests/Phase13RuntimeStateContractTests.cs`

**步骤：**

1. 增加 Core 扫描测试，遍历 `Dev/Typedown.Core/**/*.cs`。
2. 明确断言 Core 不包含：

```csharp
AssertNoTypeReference(source, "Microsoft.UI.Xaml");
AssertNoTypeReference(source, "Windows.UI.Xaml");
AssertNoTypeReference(source, "Microsoft.Web.WebView2");
AssertNoTypeReference(source, "Windows.Storage.Pickers");
AssertNoTypeReference(source, "Typedown.Core.Legacy");
```

3. 如果测试失败，先把 UI 依赖移动到 `Dev/Typedown.WinUI/Services`，Core 只保留接口。
4. 验证：

```powershell
dotnet test Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false
```

5. 提交：

```powershell
git add Tests\Typedown.ArchitectureTests Dev\Typedown.Core Dev\Typedown.WinUI
git commit -m "test: lock pure core dependency boundary"
```

---

## 阶段 2：迁移 settings models

**目标：** 将设置相关 DTO、enum、默认值从 legacy Core 迁入 pure Core，使 setting 页面后续可以接 UI ViewModel，而不是旧 ViewModel。

**重点文件：**

- `Dev/Typedown.Core/Settings/*.cs`
- `Dev/Typedown.UI/ViewModels/*Settings*.cs`
- `Dev/Typedown.WinUI/Controls/SettingControls/**`
- `Dev/Typedown.WinUI/Pages/SettingPages/**`
- `Tests/Typedown.ArchitectureTests/Phase13SettingsContractTests.cs`

**迁移顺序：**

1. 低风险 enum / DTO：

```text
ImageUploadMethod
InsertImageAction
ExportType
PrintOrientation
StartupAction
AppTheme
```

2. 设置 snapshot：

```text
GeneralSettingsSnapshot
ViewSettingsSnapshot
EditorSettingsSnapshot
ShortcutSettingsSnapshot
ImageSettingsSnapshot
ExportSettingsSnapshot
UploadSettingsSnapshot
```

3. 默认值与校验：

```text
TypedownDefaultSettings
TypedownDefaultShortcuts
SettingsValidation
```

**步骤：**

1. 先列出现有 legacy 引用：

```powershell
rg -n "Typedown.Core.Models|Typedown.Core.ViewModels|Typedown.Core.Services" Dev\Typedown.WinUI\Controls\SettingControls Dev\Typedown.WinUI\Pages\SettingPages -g "*.cs" -g "*.xaml"
```

2. 每次只迁移一个 family。
3. XAML 若暂时不能完整恢复，允许保留 stub/disabled handler，但不能新增 legacy 引用。
4. 验证：

```powershell
dotnet build Dev\Typedown.Core\Typedown.Core.csproj -c Debug -p:UseSharedCompilation=false
dotnet build Dev\Typedown.UI\Typedown.UI.csproj -c Debug -p:UseSharedCompilation=false
dotnet build Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 -p:UseSharedCompilation=false
dotnet test Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false
```

5. 提交：

```powershell
git add Dev\Typedown.Core Dev\Typedown.UI Dev\Typedown.WinUI Tests\Typedown.ArchitectureTests
git commit -m "refactor: move settings models to pure core"
```

---

## 阶段 3：迁移 editor runtime state

**目标：** 将编辑器运行态从 legacy model 迁移到 `Typedown.Core/EditorRuntime`，让菜单栏、右键菜单、状态栏以后只消费 Core runtime state。

**重点文件：**

- `Dev/Typedown.Core/EditorRuntime/*.cs`
- `Dev/Typedown.UI/ViewModels/*Editor*.cs`
- `Dev/Typedown.WinUI/Controls/EditorControls/**`
- `Tests/Typedown.ArchitectureTests/Phase13RuntimeStateContractTests.cs`

**候选类型：**

```text
EditorContentState
EditorFormatState
EditorMenuItemState
EditorMenuState
EditorParagraphState
EditorTocItem
EditorWordCount
```

**步骤：**

1. 对照 legacy：

```powershell
rg -n "class .*State|record .*State" Dev\Typedown.Core.Legacy\Models\RuntimeModels Dev\Typedown.Core\EditorRuntime
```

2. 只补充 WinUI3 菜单状态和 editor bridge 真正需要的数据字段。
3. 命令执行仍放在 `Typedown.UI.ViewModels` 或 `WinUIEditorHostController`，Core 只表达状态和 command request。
4. 验证并提交：

```powershell
dotnet build Dev\Typedown.Core\Typedown.Core.csproj -c Debug -p:UseSharedCompilation=false
dotnet build Dev\Typedown.UI\Typedown.UI.csproj -c Debug -p:UseSharedCompilation=false
dotnet build Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 -p:UseSharedCompilation=false
dotnet test Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false
git add Dev\Typedown.Core Dev\Typedown.UI Dev\Typedown.WinUI Tests\Typedown.ArchitectureTests
git commit -m "refactor: move editor runtime state to pure core"
```

---

## 阶段 4：重建 settings MVVM

**目标：** 让 WinUI3 setting 页面绑定 `Typedown.UI.ViewModels`，不再绑定 legacy ViewModels/Services。

**重点文件：**

- `Dev/Typedown.UI/ViewModels/Settings*.cs`
- `Dev/Typedown.WinUI/Pages/SettingPages/*.xaml`
- `Dev/Typedown.WinUI/Pages/SettingPages/*.xaml.cs`
- `Dev/Typedown.WinUI/Controls/SettingControls/**/*.xaml`
- `Dev/Typedown.WinUI/Controls/SettingControls/**/*.xaml.cs`

**恢复顺序：**

```text
GeneralPage
ViewPage
EditorPage
ShortcutPage
ImagePage
ImageUploadPage
ExportPage
ExportConfigPage
UploadConfigPage
AboutPage
```

**步骤：**

1. 为 active WinUI settings 文件加测试，禁止：

```csharp
AssertNoTypeReference(source, "Typedown.Core.Models");
AssertNoTypeReference(source, "Typedown.Core.Services");
AssertNoTypeReference(source, "Typedown.Core.ViewModels");
AssertNoTypeReference(source, "Typedown.Core.Legacy");
```

2. 逐页恢复 csproj 中的 `Page Remove` / `Compile Remove`。
3. 每页只接基础绑定和可编译 handler，复杂业务可以先通过 ViewModel command stub 承接。
4. 每页独立验证、独立提交：

```powershell
dotnet build Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 -p:UseSharedCompilation=false
dotnet test Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false
git commit -m "feat: reconnect WinUI general settings page"
```

---

## 阶段 5：迁移文本资源，移除 legacy 资源读取

**目标：** 不再从 `Typedown.Core.Legacy/Resources/Strings` 读取 UI 文案。

**重点文件：**

- `Dev/Typedown.UI/Resources/LegacyTextResourceReader.cs`
- `Dev/Typedown.UI/Resources/TextResourceCatalog.cs`
- `Tests/Typedown.ArchitectureTests/Phase13LegacyTextResourceTests.cs`

**步骤：**

1. 判断资源归属：UI 文案放 `Typedown.UI`，Core 只保留资源 key 或 enum。
2. 把 `Resources/Strings` 复制/迁移到 `Dev/Typedown.UI/Resources/Strings` 或稳定的嵌入资源位置。
3. 将测试命名从 `LegacyTextResources_*` 改为 `TextResources_*`。
4. 保留 key count 和多语言 shape 测试：

```text
en
zh-Hans
zh-Hant
CommonResources
DialogResources
Resources
SettingsResources
```

5. 验证并提交：

```powershell
dotnet test Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false
dotnet build Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 -p:UseSharedCompilation=false
git add Dev\Typedown.UI Tests\Typedown.ArchitectureTests
git commit -m "refactor: move text resources out of legacy core"
```

---

## 阶段 6：收缩 legacy surface

**目标：** legacy 项目只作为旧 UWP 参考与兼容层，不参与 WinUI3 主路径。

**重点文件：**

- `Typedown.sln`
- `Dev/Typedown.Core.Legacy/Typedown.Core.Legacy.csproj`
- `Dev/Typedown.WinUI/Typedown.WinUI.csproj`
- `Tests/Typedown.ArchitectureTests/Phase10CoreContractsBoundaryTests.cs`

**步骤：**

1. 扫描剩余 legacy 依赖：

```powershell
rg -n "Typedown.Core.Legacy|Typedown.Core.Models|Typedown.Core.ViewModels|Typedown.Core.Services" Dev Tests -g "*.cs" -g "*.xaml" -g "*.csproj"
```

2. 期望剩余引用只出现在：

```text
Dev/Typedown.Core.Legacy
Dev/Typedown.WinUI/LegacyCopied
Tests/Typedown.ArchitectureTests 中明确检查 legacy 边界的测试
```

3. 确认 `Debug_Local|x64` 不 build/deploy legacy UWP 项目。
4. 验证并提交：

```powershell
dotnet test Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false
dotnet build Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 -p:UseSharedCompilation=false
git add Typedown.sln Dev\Typedown.Core.Legacy Dev\Typedown.WinUI Tests\Typedown.ArchitectureTests
git commit -m "chore: shrink legacy core integration surface"
```

---

## 建议执行方式

每个大阶段使用独立 worktree，例如：

```powershell
git worktree add .worktrees/core-settings-slice -b codex/core-settings-slice
```

推荐拆分：

- subagent 1：settings models + settings contract tests。
- subagent 2：editor runtime state + menu state。
- subagent 3：text resources migration。
- 主 agent：集成、编译、架构测试、解决冲突。

执行约束：

- 不修改 `Dev/Typedown.WinUI/LegacyCopied`，除非明确刷新参考副本。
- 不把 `Typedown.Core.Legacy` 引回 `Typedown.WinUI.csproj`。
- setting 页面恢复必须一页一页做，不要一次恢复所有 excluded XAML/code-behind。
- 每个阶段结束必须提交。

---

## 最终验证门槛

任何阶段完成前必须运行：

```powershell
dotnet build Dev\Typedown.Core\Typedown.Core.csproj -c Debug -p:UseSharedCompilation=false
dotnet build Dev\Typedown.UI\Typedown.UI.csproj -c Debug -p:UseSharedCompilation=false
dotnet build Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 -p:UseSharedCompilation=false
dotnet test Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false
```

期望：

- Core build 通过。
- UI build 通过。
- WinUI3 build 通过，0 warning / 0 error。
- 架构测试 0 failure。
