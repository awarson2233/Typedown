# Typedown 构建与运行基线

本文档记录 `winui3-migration` 分支在 WinUI3 迁移前的可验证基线。后续重构必须先保持这里的基线可复现，再进入平台边界抽取。

## 仓库状态

主仓库：

- 路径：`D:\source\repos\Typedown`
- 分支：`winui3-migration`
- fork remote：`awarson2233 https://github.com/awarson2233/Typedown`
- 当前基线提交：`a4aef049af87a972a036822e10d758ecf9c78820`
- 提交说明：`Fix VS solution build ordering for XamlUI`

XamlUI legacy host：

- 路径：`D:\source\repos\Typedown\Dev\Typedown.XamlUI`
- 来源：`D:\source\repos\Typedown.XamlUI` 的 `work/vs-debug-build-fixes` 状态
- 纳入方式：源码纳入主仓库，不保留外部 `.git`，不提交 `bin` / `obj`

当前主项目已经把 `Typedown.XamlUI` 纳入仓库内 legacy host 路径。`Dev\Typedown\Typedown.csproj` 和 `Dev\Typedown.Core\Typedown.Core.csproj` 都引用 `Dev\Typedown.XamlUI\Typedown.XamlUI.csproj`。
该依赖的边界、props/targets 与运行时复制文件请见 [xamlui-dependency.md](./xamlui-dependency.md)。

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

在支线 worktree 中检查时，只需要指定主仓库分支；XamlUI 已随 worktree 存在于仓库内：

```powershell
.\scripts\verify-repos.ps1 -MainRepo D:\source\repos\Typedown.worktrees\phase8-ui-winui-boundary -ExpectedMainBranch work/phase8-ui-winui-boundary
```

支线 worktree 中运行基线构建时同样需要指定分支：

```powershell
.\scripts\verify-baseline.ps1 -RepoRoot D:\source\repos\Typedown.worktrees\phase8-ui-winui-boundary -ExpectedMainBranch work/phase8-ui-winui-boundary -SkipEditorBuild
```

严格模式要求：

- 主仓库在 `winui3-migration`。
- 主仓库存在 git metadata。
- 主仓库有 `awarson2233` remote。
- `Dev\Typedown.XamlUI\Typedown.XamlUI.csproj` 存在。
- 默认要求主工作区干净。

## 当前风险

- XamlUI 已纳入主仓库，但仍是 legacy XAML host，不是长期 `Typedown.UI` 模块。
- legacy `Dev\Typedown` 的 `Debug|x64` 依赖前端 dev server，不能作为稳定基线。
- legacy `Dev\Typedown` 的 `Debug_Local|x64` 依赖 `Resources\Statics` 已存在且可加载。
- WinUI3 当前日常基线是 `Debug_Local|x64 + Typedown.WinUI (Unpackaged)`；Package 验证基线是 `Debug|x64 + Typedown.WinUI (Package)`。
- WinUI3 Package 路径必须验证 `Resources\Statics\index.html` 同时存在于 `Debug` 输出和 package payload。
- ARM64 配置不能从 solution 下拉框推断；正式 ARM64 适配推迟到 WinUI3 shell 切换后。
- Packaging 仍是 Desktop Bridge / WAP，后续需要独立治理。

## WinUI3 Phase 13 收尾验证入口

Phase 13 之后，WinUI3 shell/editor host 的最小收尾验证为：

```powershell
dotnet build .\Dev\Typedown.Core.Contracts\Typedown.Core.Contracts.csproj -c Debug /nologo /v:minimal /m:1 /nodeReuse:false
dotnet build .\Dev\Typedown.UI\Typedown.UI.csproj -c Debug /nologo /v:minimal
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug /nologo /v:minimal
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false
dotnet build .\Typedown.sln -c Debug_Local -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false
```

构建后检查：

```powershell
Test-Path .\Dev\Typedown.WinUI\bin\x64\Debug\net9.0-windows10.0.26100.0\Resources\Statics\index.html
Test-Path .\Dev\Typedown.WinUI\bin\x64\Debug_Local\net9.0-windows10.0.26100.0\Resources\Statics\index.html
```

如果需要验证 packaged loose registration：

```powershell
Add-AppxPackage -Register .\Dev\Typedown.WinUI\bin\x64\Debug\net9.0-windows10.0.26100.0\AppxManifest.xml
Start-Process "shell:AppsFolder\62082Surprise.Typedown.WinUI_m01jdq2q5rxw0!App"
```

## 本次验证记录

验证时间：2026-04-27

执行命令：

```powershell
dotnet build .\Dev\Typedown.Core.Contracts\Typedown.Core.Contracts.csproj -c Debug /nologo /v:minimal /m:1 /nodeReuse:false
dotnet build .\Dev\Typedown.UI\Typedown.UI.csproj -c Debug /nologo /v:minimal /m:1 /nodeReuse:false
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug /nologo /v:minimal
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false
dotnet build .\Typedown.sln -c Debug_Local -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false
Test-Path .\Dev\Typedown.WinUI\bin\x64\Debug\net9.0-windows10.0.26100.0\Resources\Statics\index.html
Test-Path .\Dev\Typedown.WinUI\bin\x64\Debug_Local\net9.0-windows10.0.26100.0\Resources\Statics\index.html
```

结果：

- `Typedown.Core.Contracts` Debug 构建返回 0 warning / 0 error。
- `Typedown.UI` Debug 构建返回 0 warning / 0 error。
- `Typedown.ArchitectureTests` 返回 53 passed。
- `Typedown.WinUI` Debug|x64 构建返回 0 warning / 0 error。
- `Typedown.sln` Debug_Local|x64 构建返回 0 warning / 0 error。
- Debug 与 Debug_Local 输出目录均存在 `Resources\Statics\index.html`。
- 首次并行运行 `Typedown.Core.Contracts` 与 `Typedown.UI` 构建时出现过一次 `CS2012` 输出文件被 Defender 临时锁定；串行重跑后通过，不作为代码失败记录。

验证时间：2026-04-26

执行命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-repos.ps1 -AllowMainDirty
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-baseline.ps1 -AllowMainDirty
```

结果：

- `verify-repos.ps1 -AllowMainDirty` 返回 exit 0。
- 主仓库位于 `winui3-migration`，HEAD 为 `a4aef049af87a972a036822e10d758ecf9c78820`。
- XamlUI legacy host 位于 `Dev\Typedown.XamlUI`，由 `verify-repos.ps1` 检查项目文件存在。
- `yarn build` 返回 exit 0，并重新生成 `Dev\Typedown\Resources\Statics`。
- `Debug_Local|x64` MSBuild 返回 exit 0。
- 生成的可执行文件存在：`Dev\Typedown\bin\x64\Debug_Local\net9.0-windows10.0.26100.0\win-x64\Typedown.exe`。

已知 warning 噪声：

- 前端构建有既有 ESLint unused variable warning、bundle size warning、Browserslist 数据过期提示和 Node `url.parse()` deprecation warning。
- MSBuild 有 `Microsoft.NET.Sdk.WindowsDesktop` / `UseWpf` 或 `UseWindowsForms` 相关 SDK warning。
- XamlUI 的 CsWinRT 生成代码有大量 `CS8305` 预览 API warning。

这些 warning 当前不阻断 Phase 0。后续阶段不应把 warning 清理和迁移边界抽取混在同一次改动中。
