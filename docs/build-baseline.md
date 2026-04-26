# Typedown 构建与运行基线

本文档记录 `winui3-migration` 分支在 WinUI3 迁移前的可验证基线。后续重构必须先保持这里的基线可复现，再进入平台边界抽取。

## 仓库状态

主仓库：

- 路径：`D:\source\repos\Typedown`
- 分支：`winui3-migration`
- fork remote：`awarson2233 https://github.com/awarson2233/Typedown`
- 当前基线提交：`a4aef049af87a972a036822e10d758ecf9c78820`
- 提交说明：`Fix VS solution build ordering for XamlUI`

XamlUI 仓库：

- 路径：`D:\source\repos\Typedown.XamlUI`
- 分支：`winui3-migration`
- fork remote：`awarson2233 https://github.com/awarson2233/Typedown.XamlUI`
- 当前基线提交：`e137473c5c7a1b2650fc9ce2a13ab98ad5de520d`
- 提交说明：`Restore net9 XamlUI host compatibility`

当前主项目不是自包含构建。`Dev\Typedown\Typedown.csproj` 和 `Dev\Typedown.Core\Typedown.Core.csproj` 都依赖相邻目录 `D:\source\repos\Typedown.XamlUI`。

## 配置含义

- `Debug|x64`：编辑器导航到 `http://localhost:3000`。使用该配置前必须在 `Dev\Typedown.Editor` 执行 `yarn start`。
- `Debug_Local|x64`：编辑器加载本地静态产物 `Dev\Typedown\Resources\Statics\index.html`。这是当前最稳定的无 dev server 基线。
- `Release|x64`：发布路径，仍加载静态产物。
- `ARM64`：当前不能仅凭 solution 配置判断已支持。当前 XamlUI/UWP host 架构不适合作为 ARM64 适配基础；正式 ARM64 适配推迟到 WinUI3 shell 切换后。

## 前端静态产物

首次验证或前端变更后，应重新生成静态产物：

```powershell
cd D:\source\repos\Typedown\Dev\Typedown.Editor
yarn
yarn build
```

产物输出到：

```text
D:\source\repos\Typedown\Dev\Typedown\Resources\Statics
```

## 基线构建

推荐使用脚本：

```powershell
cd D:\source\repos\Typedown
.\scripts\verify-baseline.ps1 -AllowMainDirty
```

提交后或 CI 类场景应使用严格模式：

```powershell
cd D:\source\repos\Typedown
.\scripts\verify-baseline.ps1
```

脚本会执行：

```powershell
& $msbuild D:\source\repos\Typedown\Dev\Typedown\Typedown.csproj /restore /t:Build /p:Configuration=Debug_Local /p:Platform=x64 /p:UseSharedCompilation=false /nologo /m:1 /nodeReuse:false /v:minimal
```

成功后可运行：

```powershell
D:\source\repos\Typedown\Dev\Typedown\bin\x64\Debug_Local\net9.0-windows10.0.26100.0\win-x64\Typedown.exe
```

## 仓库检查

提交后使用严格检查：

```powershell
.\scripts\verify-repos.ps1
```

正在编辑但需要验证当前构建时使用：

```powershell
.\scripts\verify-repos.ps1 -AllowMainDirty
```

在支线 worktree 中检查时，应分别指定主仓库分支和 XamlUI 分支：

```powershell
.\scripts\verify-repos.ps1 -MainRepo D:\source\repos\Typedown.worktrees\phase1-xamlui-dependency -ExpectedMainBranch work/phase1-xamlui-dependency -ExpectedXamlUIBranch winui3-migration
```

支线 worktree 中运行基线构建时同样需要指定分支：

```powershell
.\scripts\verify-baseline.ps1 -RepoRoot D:\source\repos\Typedown.worktrees\phase1-xamlui-dependency -ExpectedMainBranch work/phase1-xamlui-dependency -ExpectedXamlUIBranch winui3-migration -SkipEditorBuild
```

严格模式要求：

- 主仓库在 `winui3-migration`。
- XamlUI 仓库在 `winui3-migration`。
- 两个仓库均存在 git metadata。
- 两个仓库均有 `awarson2233` remote。
- 默认要求两个工作区干净。

## 当前风险

- XamlUI 仍是相邻仓库依赖，不是 submodule/subtree，也不是固定 NuGet。
- `Debug|x64` 依赖前端 dev server，不能作为稳定基线。
- `Debug_Local|x64` 依赖 `Resources\Statics` 已存在且可加载。
- ARM64 配置不能从 solution 下拉框推断；正式 ARM64 适配推迟到 WinUI3 shell 切换后。
- Packaging 仍是 Desktop Bridge / WAP，后续需要独立治理。

## 本次验证记录

验证时间：2026-04-26

执行命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-repos.ps1 -AllowMainDirty
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-baseline.ps1 -AllowMainDirty
```

结果：

- `verify-repos.ps1 -AllowMainDirty` 返回 exit 0。
- 主仓库位于 `winui3-migration`，HEAD 为 `a4aef049af87a972a036822e10d758ecf9c78820`。
- XamlUI 仓库位于 `winui3-migration`，HEAD 为 `e137473c5c7a1b2650fc9ce2a13ab98ad5de520d`。
- `yarn build` 返回 exit 0，并重新生成 `Dev\Typedown\Resources\Statics`。
- `Debug_Local|x64` MSBuild 返回 exit 0。
- 生成的可执行文件存在：`Dev\Typedown\bin\x64\Debug_Local\net9.0-windows10.0.26100.0\win-x64\Typedown.exe`。

已知 warning 噪声：

- 前端构建有既有 ESLint unused variable warning、bundle size warning、Browserslist 数据过期提示和 Node `url.parse()` deprecation warning。
- MSBuild 有 `Microsoft.NET.Sdk.WindowsDesktop` / `UseWpf` 或 `UseWindowsForms` 相关 SDK warning。
- XamlUI 的 CsWinRT 生成代码有大量 `CS8305` 预览 API warning。

这些 warning 当前不阻断 Phase 0。后续阶段不应把 warning 清理和迁移边界抽取混在同一次改动中。
