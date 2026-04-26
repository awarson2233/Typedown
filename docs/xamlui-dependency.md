# Typedown.XamlUI 依赖边界说明

本文档定义 `Typedown` 与仓库内 legacy XAML host `Typedown.XamlUI` 的当前依赖边界，避免该依赖成为隐式知识。

## 1. 为什么当前仍需要 Typedown.XamlUI

`Typedown` 当前仍通过 UWP XAML Host 承载 UI 运行时能力，以下路径直接依赖 `Typedown.XamlUI`：

- `Dev\Typedown\Typedown.csproj` 中的 `ProjectReference`：`..\Typedown.XamlUI\Typedown.XamlUI.csproj`
- `Dev\Typedown.Core\Typedown.Core.csproj` 中的 `ProjectReference`：`..\Typedown.XamlUI\Typedown.XamlUI.csproj`
- `Typedown.sln` 中的项目路径：`Dev\Typedown.XamlUI\Typedown.XamlUI.csproj`
- `Dev\Typedown\Typedown.csproj` 中的 buildTransitive 导入：
  - `$(TypedownXamlUIProjectDir)buildTransitive\Typedown.XamlUI.props`
  - `$(TypedownXamlUIProjectDir)buildTransitive\Typedown.XamlUI.targets`
- `Dev\Typedown\Typedown.csproj` 构建输出阶段复制的运行时文件：
  - `$(TypedownXamlUIProjectDir)runtimes\win10-$(Platform)\native\Microsoft.UI.Xaml.dll`
  - `$(TypedownXamlUIProjectDir)lib\Microsoft.UI.Xaml.pri`
  - `$(TypedownXamlUIProjectDir)lib\Microsoft.UI.Xaml.xml`

结论：当前构建已经将 `Typedown.XamlUI` 纳入主仓库，但它仍是 legacy XAML host 硬依赖。

## 1.1. 命名与长期职责边界

当前已选择 `UI` 与 `WinUI` 分离的 WinUI3 迁移方案。因此 `Typedown.XamlUI` 的长期定位必须明确：

- `Typedown.XamlUI` 不是未来的 `Typedown.UI`。
- `Typedown.XamlUI` 当前职责是 legacy XAML host / 旧 XAML 宿主层，用来维持现有 .NET 9 可运行基线。
- 当前源码已纳入主仓库 `Dev\Typedown.XamlUI`，但仍不能把它视为未来 `Typedown.UI`。
- `Typedown.UI` 应留给 WinUI3 迁移后的页面、控件、资源、UI ViewModel 和 UI 编排。
- `Typedown.WinUI` 应承载 WinUI3 App/Window、平台服务实现、WebView2 宿主、DI 注册、单实例激活和打包入口。
- WinUI3 shell 达到功能等价后，legacy XAML host 应逐步删除。

这条边界的目的不是保留旧宿主，而是避免把旧 UWP/XAML runtime glue 误沉淀进新的 UI 模块。

## 2. 预期仓库位置与基线

- 主仓库标准路径：`D:\source\repos\Typedown`
- 主仓库主线分支：`winui3-migration`
- 主仓库 remote（fork）：`awarson2233 https://github.com/awarson2233/Typedown`
- 主仓库基线提交：`12ce36fc921527152e394ed9e6cb72df67f34eac`

- XamlUI 仓库内路径：`D:\source\repos\Typedown\Dev\Typedown.XamlUI`
- XamlUI 源码来源：`D:\source\repos\Typedown.XamlUI` 的 `work/vs-debug-build-fixes` 状态
- XamlUI 纳入策略：不保留外部 `.git`，不提交 `bin` / `obj`，保留 `buildTransitive`、`lib`、`ref`、`runtimes`、`tools` 等当前构建需要的源码/制品边界

worktree 验证说明：

- 在支线验证（例如 `work/phase1-xamlui-dependency`）时，`verify-repos.ps1` / `verify-baseline.ps1` 的 `-MainRepo` 或 `-RepoRoot` 可以指向 `D:\source\repos\Typedown.worktrees\<phase>`。
- XamlUI 现在应随主仓库 worktree 一起存在于 `<worktree>\Dev\Typedown.XamlUI`。

## 3. 构建错误策略（Phase 1）

从 Phase 1 开始，`Dev\Typedown\Typedown.csproj` 和 `Dev\Typedown.Core\Typedown.Core.csproj` 都会在构建前校验：

- 是否存在 `$(TypedownXamlUIProjectDir)Typedown.XamlUI.csproj`
- 若不存在，直接触发明确 MSBuild `Error`，并提示查看本文档

这样缺失依赖时会在构建入口就失败，不再依赖后续的隐式错误链条。

## 4. 迁移风险与后续边界

- 风险 1：构建耦合。主仓库依赖 XamlUI 的 `props/targets`、`lib` 和 `runtimes` 布局，XamlUI 改目录会直接影响主仓库。
- 风险 2：历史包化耦合。`Typedown.XamlUI.csproj` 仍保留 NuGet pack 相关布局，当前只是源码纳入，不代表长期包边界已经设计完成。
- 风险 3：legacy host 耦合。该模块仍是旧 UWP/XAML runtime glue，不应成为 WinUI3 新架构核心。
- 风险 4：版本漂移。相邻仓库仍可能存在，但主仓库以 `Dev\Typedown.XamlUI` 为准；后续变更应优先提交到主仓库分支。

后续迁移（WinUI3 shell 或彻底解耦）应以 `Typedown.WinUI` / `Typedown.UI` 的新模块边界替换当前 legacy host 边界。

推荐迁移顺序：

1. 当前阶段使用仓库内 `Dev\Typedown.XamlUI` 维持可运行基线。
2. Phase 8 固化 `Typedown.UI` / `Typedown.WinUI` / legacy host 的模块边界。
3. Phase 9 创建最小 `Typedown.WinUI` shell spike。
4. 只把 WinUI3 兼容的新页面、控件、资源和 UI ViewModel 放入 `Typedown.UI`。
5. 等 WinUI3 shell 功能等价后，再删除 legacy host 依赖。

Phase 8 的完整边界清单见 [winui3-target-architecture.md](./winui3-target-architecture.md)。
