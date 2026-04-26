# 构建矩阵与 ARM64 风险记录

> Phase 7 结论：当前阶段只记录构建矩阵和 ARM64 风险，不做 ARM64 适配。ARM64 适配必须等 WinUI3 shell 切换后重新制定计划。

## 当前构建入口

- Solution：`Typedown.sln`
- 主应用：`Dev\Typedown\Typedown.csproj`
- Core：`Dev\Typedown.Core\Typedown.Core.csproj`
- Packaging：`Tools\Typedown.Package\Typedown.Package.wapproj`
- 旧 XAML 宿主：仓库内 `Dev\Typedown.XamlUI`

当前稳定验证入口仍是：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-baseline.ps1 -Configuration Debug_Local -Platform x64
```

## Solution 配置现状

`Typedown.sln` 暴露以下 solution-level 配置：

- `Debug_Local|x64`
- `Debug_Local|x86`
- `Debug_Local|ARM64`
- `Debug|x64`
- `Debug|x86`
- `Debug|ARM64`
- `Release|x64`
- `Release|x86`
- `Release|ARM64`

其中 `Debug_Local|ARM64` 对主应用、package、Core、XamlUI、测试项目大多映射到 `ARM64`，但这不等于运行时已验证。当前架构仍依赖 legacy XAML host、WinRT/XAML 编译链和 `Dev\Typedown.XamlUI` 输出。

更重要的是，`Debug|ARM64` 和 `Release|ARM64` 中存在大量映射到 `x64` 的项目配置：

- 主应用 `Typedown`：`Debug|ARM64 -> Debug|x64`，`Release|ARM64 -> Release|x64`
- Packaging `Typedown.Package`：`Debug|ARM64 -> Debug|x64`，`Release|ARM64 -> Release|x64`
- DatabaseMigration：`Debug|ARM64 -> Debug|x64`，`Release|ARM64 -> Release|x64`
- Typedown.Test / Typedown.Core.Test / Typedown.UITest：`Debug|ARM64 -> Debug|x64`，`Release|ARM64 -> Release|x64`

因此不能把 solution 中存在 `ARM64` 配置理解为真实 ARM64 支持。

## 项目配置现状

`Dev\Typedown\Typedown.csproj`：

- `Platforms` 包含 `x64;x86;ARM64`
- `Platform=ARM64` 时设置 `RuntimeIdentifier=win-arm64`
- 当前仍引用仓库内 `Dev\Typedown.XamlUI`
- 当前仍复制 XamlUI 输出中的 `Microsoft.UI.Xaml.dll`、`Microsoft.UI.Xaml.pri`、`Microsoft.UI.Xaml.xml`
- 当前仍引用 `PdfiumViewer.Native.x86_64.no_v8-no_xfa`，这是明确的 x64 native 依赖风险

`Dev\Typedown.Core\Typedown.Core.csproj`：

- `Platforms` 包含 `x86;x64;arm64`
- `RuntimeIdentifiers` 包含 `win-x86;win-x64;win-arm64`
- `Platform=ARM64` 时设置 `RuntimeIdentifier=win-arm64`
- 当前仍启用 `UseUwp=true` 并引用仓库内 `Dev\Typedown.XamlUI`
- `AppxBundlePlatforms` 只对 `x64` 和 `x86` 设置，没有 ARM64 分支

`Tools\Typedown.Package\Typedown.Package.wapproj`：

- 声明了 `Debug_Local|ARM64`、`Debug|ARM64`、`Release|ARM64`
- `AppxBundlePlatforms` 只对 `x64` 和 `x86` 设置，没有 ARM64 分支
- `Debug|ARM64` 和 `Release|ARM64` 在 solution 中实际映射到 `x64`

## ARM64 风险

- 配置风险：solution-level `ARM64` 不一致，部分配置是真 ARM64，部分配置被映射到 x64。
- XAML 编译风险：历史 ARM64 尝试的首个有效失败点在 XAML compiler / WinRT metadata 传递链，而不是后续 `.xbf` 或资源复制错误。
- Legacy host 风险：当前 `Typedown.XamlUI` 是 UWP/XAML host 兼容层，仍依赖旧 XAML/WinRT 编译路径，不适合作为 ARM64 适配基础。
- Native 依赖风险：`PdfiumViewer.Native.x86_64.no_v8-no_xfa` 明确是 x64 native 包，后续 ARM64 需要替换或移除 PDF 路径。
- Packaging 风险：Desktop Bridge / MSIX packaging 当前没有完整 ARM64 bundle 记录，不能假设可打包。
- 验证风险：当前基线验证只承诺 `Debug_Local|x64`，没有承诺 `ARM64` 构建、部署或运行。

## 当前阶段禁止事项

- 不修改 `Typedown.sln` 的 ARM64 映射。
- 不新增 ARM64 构建脚本作为正式验证入口。
- 不替换 XamlUI / WinRT / XAML compiler 依赖。
- 不替换 PDF native 包。
- 不把 ARM64 构建失败当作本阶段要修复的问题。

## WinUI3 后续处理原则

切换到 WinUI3 shell 后，再单独制定 ARM64 适配计划：

1. 先让 `Typedown.WinUI` 在 `x64` 下达到功能等价。
2. 移除 legacy XAML host 对 ARM64 构建链的影响。
3. 重新整理 solution/platform 映射，确保 `ARM64` 不再隐式映射到 `x64`。
4. 处理 PDF/native 依赖的 ARM64 替代方案。
5. 为 `win-arm64` 增加独立构建、打包和真机运行验证。

## 只读检查命令

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\inspect-build-matrix.ps1
```

该脚本只读取 `Typedown.sln`、`Dev\Typedown\Typedown.csproj`、`Dev\Typedown.Core\Typedown.Core.csproj` 和 `Tools\Typedown.Package\Typedown.Package.wapproj`，用于快速暴露 ARM64/x64 映射和 bundle 配置风险。
