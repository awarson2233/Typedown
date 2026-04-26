# Subagent + 多 Worktree 执行计划

本文档定义 Phase 1 以后如何使用多个 git worktree 和 subagent 并行推进 WinUI3 迁移前架构解耦。它是执行策略，不替代 [winui3-migration-decoupling-plan.md](D:/source/repos/Typedown/docs/winui3-migration-decoupling-plan.md) 中的阶段目标。

## 前置条件

开始创建 worktree 前必须满足：

- 当前主工作区位于 `D:\source\repos\Typedown`。
- 当前分支为 `winui3-migration`。
- Phase 0 文档和脚本已提交，作为所有 worktree 的共同起点。
- `D:\source\repos\Typedown.XamlUI` 位于 `winui3-migration`，且工作区干净。
- `.\scripts\verify-baseline.ps1` 在主工作区通过。

建议先提交 Phase 0：

```powershell
cd D:\source\repos\Typedown
git add docs\winui3-migration-decoupling-plan.md docs\build-baseline.md scripts\verify-repos.ps1 scripts\verify-baseline.ps1
git commit -m "docs: freeze winui3 migration baseline"
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-baseline.ps1
```

## Worktree 目录策略

当前仓库没有 `.worktrees/` 或 `worktrees/` 目录，`.gitignore` 也没有忽略这类目录。为了避免误提交 worktree 内容，默认使用仓库外兄弟目录：

```text
D:\source\repos\Typedown.worktrees
```

不建议把 worktree 建在 `D:\source\repos\Typedown\.claude\worktrees`，因为当前 `.gitignore` 没有忽略 `.claude/`，而且该目录已经存在其他用途。

如后续坚持项目内 worktree，应先单独提交 `.gitignore` 变更：

```gitignore
.worktrees/
worktrees/
```

## 当前执行状态

- 已完成并合入 `winui3-migration`：`work/phase1-xamlui-dependency`、`work/phase2-appdata-paths`、`work/phase3-dialog-picker`、`work/phase4-dispatcher-window-context`、`work/phase5-editor-bridge`。
- 下一批必须串行：创建并验证 `work/phase6-app-activation`。
- `work/phase7-build-matrix` 只记录构建矩阵和 ARM64 风险，不做 ARM64 适配。
- `work/phase8-winui3-shell-spike` 只能在 Phase 3/4/6 稳定后开始。

Phase 6 会继续修改 `Dev\Typedown\Injection.cs` 和 shell service 注册。不得同时启动其他会触碰同一边界的实现 subagent。

## 分支与 Worktree 命名

所有执行分支从 `winui3-migration` 创建，命名统一使用：

```text
work/<phase>-<scope>
```

已完成的第一批 worktree：

```powershell
mkdir D:\source\repos\Typedown.worktrees

git worktree add D:\source\repos\Typedown.worktrees\phase1-xamlui-dependency -b work/phase1-xamlui-dependency winui3-migration
git worktree add D:\source\repos\Typedown.worktrees\phase2-appdata-paths -b work/phase2-appdata-paths winui3-migration
git worktree add D:\source\repos\Typedown.worktrees\phase5-editor-bridge -b work/phase5-editor-bridge winui3-migration
```

后续串行创建：

```powershell
git worktree add D:\source\repos\Typedown.worktrees\phase6-app-activation -b work/phase6-app-activation winui3-migration
```

## 并行拆分

第一批已完成：

- `work/phase1-xamlui-dependency`：治理 XamlUI 相邻仓库依赖。
- `work/phase2-appdata-paths`：抽出 AppData / settings / database path provider。
- `work/phase3-dialog-picker`：抽出 dialog 和 picker 服务。
- `work/phase4-dispatcher-window-context`：抽出 dispatcher 和 window context。
- `work/phase5-editor-bridge`：文档化并稳定编辑器 bridge 协议。

第二批在第一批合并后串行推进：

- `work/phase6-app-activation`：抽出单实例和激活服务。Phase 4 合并验证后执行。
- `work/phase7-build-matrix`：只记录构建矩阵和 ARM64 风险，不适配 ARM64。

第三批最后推进：

- `work/phase8-winui3-shell-spike`：创建最小 WinUI3 shell spike。

## 文件所有权

### Phase 1: XamlUI Dependency

负责文件：

- `docs/build-baseline.md`
- `docs/xamlui-dependency.md`
- `Dev\Typedown\Typedown.csproj`
- `Dev\Typedown.Core\Typedown.Core.csproj`

禁止修改：

- `Dev\Typedown\Injection.cs`
- `Dev\Typedown.Core\ViewModels\*.cs`
- `Dev\Typedown\Controls\MarkdownEditor.cs`

### Phase 2: AppData Paths

负责文件：

- `Dev\Typedown.Core\Interfaces\IAppDataPathProvider.cs`
- `Dev\Typedown\Services\AppDataPathProvider.cs`
- `Dev\Typedown.Core\Config.cs`
- `Dev\Typedown.Core\Services\AppDbContext.cs`
- `Tests\Typedown.Test\**`
- `Dev\Typedown\Injection.cs`

合并注意：

- 这是第一批中唯一允许修改 `Injection.cs` 的工作流。
- 如果 Phase 1 也需要注册或构建相关调整，不得碰 `Injection.cs`。

### Phase 5: Editor Bridge

负责文件：

- `docs/editor-bridge-protocol.md`
- `Dev\Typedown.Core\Interfaces\IEditorBridge.cs`
- `Dev\Typedown.Core\Services\EditorBridge.cs`
- `Dev\Typedown.Core\Services\Transport.cs`
- `Dev\Typedown.Core\Services\RemoteInvoke.cs`
- `Dev\Typedown\Controls\MarkdownEditor.cs`
- 可选：`Dev\Typedown.Editor\src\services\transport.ts`

禁止修改：

- React 依赖版本。
- `Dev\Typedown.Editor\package.json`。
- `Dev\Typedown.Editor\yarn.lock`，除非仅为锁定现有依赖而且有明确原因。

### Phase 3: Dialog / Picker

负责文件：

- `Dev\Typedown.Core\Interfaces\IDialogService.cs`
- `Dev\Typedown.Core\Interfaces\IFilePickerService.cs`
- `Dev\Typedown\Services\DialogService.cs`
- `Dev\Typedown\Services\FilePickerService.cs`
- `Dev\Typedown.Core\ViewModels\FileViewModel.cs`
- `Dev\Typedown.Core\Services\ImageAction.cs`
- `Dev\Typedown\Injection.cs`

约束：

- 不修改 React 依赖。
- 不做 ARM64 适配。
- 不提前抽 dispatcher/window context；如果 picker/dialog 实现需要 XamlRoot 或 HWND，可通过当前 shell 层实现内部访问，Phase 4 再统一收敛。
- 不迁移 XAML 控件，除非它们阻塞 ViewModel/服务中的直接 dialog/picker 构造替换。

### Phase 4: Dispatcher / Window Context

负责文件：

- `Dev\Typedown.Core\Interfaces\IUiDispatcher.cs`
- `Dev\Typedown.Core\Interfaces\IWindowContext.cs`
- `Dev\Typedown\Services\UiDispatcher.cs`
- `Dev\Typedown\Services\WindowContext.cs`
- `Dev\Typedown.Core\ViewModels\UIViewModel.cs`
- `Dev\Typedown.Core\ViewModels\AppViewModel.cs`
- `Dev\Typedown\Windows\MainWindow.cs`
- `Dev\Typedown\Services\WindowService.cs`
- `Dev\Typedown\Injection.cs`

约束：

- 必须基于已合入的 Phase 3。
- 不改 dialog/picker 行为，除非只是适配 Phase 3 已抽出的接口。
- 不做 ARM64 适配。

### Phase 6: App Activation

负责文件：

- `Dev\Typedown.Core\Interfaces\IAppActivationService.cs`
- `Dev\Typedown\Services\AppActivationService.cs`
- `Dev\Typedown\App.cs`
- `Dev\Typedown\Injection.cs`
- 必要时：`Dev\Typedown\Utilities\Common.cs`

约束：

- 必须基于已合入的 Phase 4。
- 保留现有单实例、命令行转发、已有实例激活行为。
- 不做 ARM64 适配。

### Phase 7: Build Matrix / ARM64 Deferred

负责文件：

- `docs/build-matrix.md`
- 可选：`scripts/inspect-build-matrix.ps1`

禁止修改：

- `Dev\Typedown.Core` 业务代码。
- `Dev\Typedown\Controls`。
- `Dev\Typedown.Editor`。
- `Typedown.sln`，除非只是文档化前的只读扫描，不提交配置变更。
- `Tools\Typedown.Package\Typedown.Package.wapproj`，不在当前阶段修改。

说明：

- 当前架构不适合 ARM64 构建适配。
- ARM64 适配必须推迟到 WinUI3 shell 切换后。
- 当前阶段只记录 solution/package 配置风险。

## Subagent 分工

每个 worktree 使用一个实现 subagent。实现 subagent 必须只在自己的 worktree 内修改文件，不得回到主工作区写文件。

推荐 subagent 任务：

```text
Subagent A: Phase 1 XamlUI dependency governance
目标：文档化并显式检查相邻 XamlUI 依赖。
Worktree: D:\source\repos\Typedown.worktrees\phase1-xamlui-dependency
Branch: work/phase1-xamlui-dependency
验证：.\scripts\verify-repos.ps1 ; .\scripts\verify-baseline.ps1 -SkipEditorBuild
```

```text
Subagent B: Phase 2 AppData path provider
目标：抽出 AppData / Settings / DB path provider，保持现有路径兼容。
Worktree: D:\source\repos\Typedown.worktrees\phase2-appdata-paths
Branch: work/phase2-appdata-paths
验证：dotnet test Tests\Typedown.Test\Typedown.Test.csproj --configuration Debug ; .\scripts\verify-baseline.ps1 -SkipEditorBuild
```

```text
Subagent C: Phase 5 Editor bridge protocol
目标：文档化 bridge 协议，并把协议层从 MarkdownEditor 中收束出来。
Worktree: D:\source\repos\Typedown.worktrees\phase5-editor-bridge
Branch: work/phase5-editor-bridge
验证：yarn build ; .\scripts\verify-baseline.ps1
```

每个实现 subagent 完成后，必须返回：

- 修改文件清单。
- 执行过的验证命令和 exit code。
- 未解决风险。
- 是否修改了约定外文件。
- commit hash。

## Review Gate

每个实现分支合并前都做两轮 review：

- 规格 review：检查是否只完成对应 phase，是否越界改动。
- 代码质量 review：检查接口边界、测试覆盖、构建脚本、异常处理、命名和可维护性。

review 可以用新的 subagent 完成，但 reviewer 只能只读扫描，不直接修改文件。发现问题后交回原实现 subagent 在对应 worktree 修复。

## 合并顺序

推荐合并顺序：

1. `work/phase1-xamlui-dependency`
2. `work/phase2-appdata-paths`
3. `work/phase5-editor-bridge`
4. `work/phase3-dialog-picker`
5. `work/phase4-dispatcher-window-context`
6. `work/phase6-app-activation`
7. `work/phase7-build-matrix`
8. `work/phase8-winui3-shell-spike`

原因：

- Phase 1 主要是构建/文档/脚本，冲突半径小。
- Phase 2 会先动 `Injection.cs` 和路径基础设施，应早于 dialog/picker。
- Phase 5 与 Phase 2 基本独立，但会动 `MarkdownEditor` 和 bridge，不应和 UI dispatcher/window context 同时合并。
- Phase 3、4、6 都会碰服务注册和窗口/平台接口，必须串行。当前 Phase 4 已完成，下一步从 Phase 6 开始。
- Phase 7 不做 ARM64 适配，只在 WinUI3 前记录风险，因此可以放到服务边界稳定后再补。

## 集成流程

主工作区作为 integration 工作区。每次只合并一个分支：

```powershell
cd D:\source\repos\Typedown
git fetch awarson2233
git merge --no-ff work/phase1-xamlui-dependency
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-baseline.ps1
```

如果合并失败：

- 不用 `git reset --hard`。
- 记录冲突文件。
- 回到对应 worktree 修复，重新提交，再回主工作区合并。

合并后若验证失败：

- 先保留当前状态。
- 用失败日志定位是合并冲突、脚本问题还是实现回归。
- 优先让原实现 subagent 在自己的 worktree 修复。

## Worktree 清理

每个分支合并并验证后再清理：

```powershell
git worktree remove D:\source\repos\Typedown.worktrees\phase1-xamlui-dependency
git branch -d work/phase1-xamlui-dependency
```

如果 Windows 文件锁导致清理失败：

- 先关闭 Visual Studio、Typedown.exe、node/yarn、MSBuild 相关进程。
- 再重试 `git worktree remove`。
- 不要直接删除仍被 git 记录的 worktree 目录。

## 不并行的事项

以下任务必须串行：

- 修改 `Dev\Typedown\Injection.cs` 的多个阶段。
- 修改 `FileViewModel` 的 picker/dialog、export、startup flow。
- 修改 `MarkdownEditor` 和 `WebViewController` 的 WebView host 逻辑。
- 修改 `Typedown.sln` 和 packaging 配置。
- 创建 WinUI3 shell spike。

## 下一步

1. 从当前 `winui3-migration` 创建 `work/phase6-app-activation`。
2. 启动一个实现 subagent，只负责 Phase 6。
3. 主工作区做 review、合并和基线验证。
4. Phase 6 合入后，再补 Phase 7 构建矩阵风险记录。
