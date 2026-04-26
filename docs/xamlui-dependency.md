# Typedown.XamlUI 依赖边界说明

本文档定义 `Typedown` 与相邻仓库 `Typedown.XamlUI` 的当前依赖边界，避免该依赖成为隐式知识。

## 1. 为什么当前仍需要 Typedown.XamlUI

`Typedown` 当前仍通过 UWP XAML Host 承载 UI 运行时能力，以下路径直接依赖 `Typedown.XamlUI`：

- `Dev\Typedown\Typedown.csproj` 中的 `ProjectReference`：`..\..\..\Typedown.XamlUI\Typedown.XamlUI\Typedown.XamlUI.csproj`
- `Dev\Typedown.Core\Typedown.Core.csproj` 中的 `ProjectReference`：`..\..\..\Typedown.XamlUI\Typedown.XamlUI\Typedown.XamlUI.csproj`
- `Dev\Typedown\Typedown.csproj` 中的 buildTransitive 导入：
  - `$(TypedownXamlUIProjectDir)buildTransitive\Typedown.XamlUI.props`
  - `$(TypedownXamlUIProjectDir)buildTransitive\Typedown.XamlUI.targets`
- `Dev\Typedown\Typedown.csproj` 构建输出阶段复制的运行时文件：
  - `$(TypedownXamlUIProjectDir)runtimes\win10-$(Platform)\native\Microsoft.UI.Xaml.dll`
  - `$(TypedownXamlUIProjectDir)lib\Microsoft.UI.Xaml.pri`
  - `$(TypedownXamlUIProjectDir)lib\Microsoft.UI.Xaml.xml`

结论：当前构建不是纯单仓库自包含构建，`Typedown.XamlUI` 是硬依赖。

## 2. 预期仓库位置与基线

- 主仓库标准路径：`D:\source\repos\Typedown`
- 主仓库主线分支：`winui3-migration`
- 主仓库 remote（fork）：`awarson2233 https://github.com/awarson2233/Typedown`
- 主仓库基线提交：`12ce36fc921527152e394ed9e6cb72df67f34eac`

- XamlUI 仓库路径：`D:\source\repos\Typedown.XamlUI`
- XamlUI 分支：`winui3-migration`
- XamlUI remote（fork）：`awarson2233 https://github.com/awarson2233/Typedown.XamlUI`
- XamlUI 基线提交：`e137473c5c7a1b2650fc9ce2a13ab98ad5de520d`

worktree 验证说明：

- 在支线验证（例如 `work/phase1-xamlui-dependency`）时，`verify-repos.ps1` / `verify-baseline.ps1` 的 `-MainRepo` 或 `-RepoRoot` 可以指向 `D:\source\repos\Typedown.worktrees\<phase>`。
- 即使主仓库使用 worktree 路径执行验证，XamlUI 仍应位于 `D:\source\repos\Typedown.XamlUI`。

## 3. 构建错误策略（Phase 1）

从 Phase 1 开始，`Dev\Typedown\Typedown.csproj` 和 `Dev\Typedown.Core\Typedown.Core.csproj` 都会在构建前校验：

- 是否存在 `$(TypedownXamlUIProjectDir)Typedown.XamlUI.csproj`
- 若不存在，直接触发明确 MSBuild `Error`，并提示查看本文档

这样缺失依赖时会在构建入口就失败，不再依赖后续的隐式错误链条。

## 4. 迁移风险与后续边界

- 风险 1：路径耦合。当前依赖相邻目录，不是 submodule/subtree，也不是固定 NuGet 包。
- 风险 2：构建耦合。主仓库依赖 XamlUI 的 `props/targets` 和运行时文件布局，XamlUI 改目录会直接影响主仓库。
- 风险 3：分支漂移。主仓库与 XamlUI 分支如果不同步，容易出现可编译但运行期异常。
- 风险 4：版本漂移。未固定包化产物时，任意 XamlUI commit 变更都可能改变主仓库行为。

后续迁移（WinUI3 shell 或彻底解耦）应以“显式制品边界”替换当前“相邻源码边界”。
