# Phase 10c: WinUI Package Baseline

## 阶段目标

Phase 10c 的目标是把 `Dev\Typedown.WinUI` 收敛成一个可直接承载 VS packaged 调试的 WinUI shell 基线。

- `Typedown.WinUI (Package)` 是本阶段的主路径。
- `Typedown.WinUI (Unpackaged)` 保留为辅助调试路径，用于快速验证 shell 启动，不作为长期主路径表述。
- 当前命令行和 VS 验证以 `Debug|x64` 为主。
- ARM64 留到 WinUI 3 cutover 后继续处理；本阶段不把 ARM64 作为验收项，但也不把它表述为永久不支持。

## 本阶段最小根因

之前 `Typedown.WinUI` 已经有 `MsixPackage` launch profile，但还停留在半配置状态：

- `Typedown.WinUI.csproj` 仍然是 `<WindowsPackageType>None</WindowsPackageType>`。
- `Package.appxmanifest` 还是模板 identity/publisher。
- `Typedown.sln` 的 `Debug|x64` 仍把 legacy `Typedown.Package.wapproj`、`XamlDesignApp`，以及旧 `Typedown` / `Typedown.Core` / `Typedown.XamlUI` 链路纳入 solution build。

这会导致 VS 在选择 `Typedown.WinUI` + `Typedown.WinUI (Package)` 时，仍有机会沿 solution 配置去构建或部署无关旧项目，进而出现 `XamlDesignApp` 重复程序集属性之类的噪声失败。

## 现在的基线

Phase 10c 将 packaged 基线收回到 `Dev\Typedown.WinUI` 自身：

- WinUI 项目启用单项目 MSIX：`<WindowsPackageType>MSIX</WindowsPackageType>`。
- `Package.appxmanifest` 改为 WinUI shell 自己的 package identity，不再使用模板占位值。
- `Typedown.sln` 的 `Debug|x64` 至少确保 `Typedown.WinUI` 自己具备 Build + Deploy，而旧 `Typedown.Package` 不再参与该配置的 Deploy。
- `Typedown.WinUI` 在 `Debug|x64` 下补上 `Deploy.0`，让 VS packaged 调试落在 WinUI 项目本身，而不是旧 `wapproj`。
- 仓库内提供 `Typedown.WinUI.DevTest.pfx` + `Typedown.WinUI.DevTest.cer`，仅用于本地 packaged debug / test 签名，不作为正式发布证书。

## Visual Studio 使用方式

首次在新机器上做 packaged F5 前，先运行：

`powershell -ExecutionPolicy Bypass -File .\scripts\install-winui-dev-certificate.ps1`

这一步会把仓库内的 WinUI dev/test 证书导入当前用户的 `My`、`TrustedPeople` 和 `Root`，用于本地 signed package 构建与 VS packaged 部署信任。

请在 VS 中明确选择下面这组组合：

1. Solution Configuration: `Debug`
2. Solution Platform: `x64`
3. Startup Project: `Typedown.WinUI`
4. Launch Profile: `Typedown.WinUI (Package)`

辅助调试仍可切到：

1. Solution Configuration: `Debug`
2. Solution Platform: `x64`
3. Startup Project: `Typedown.WinUI`
4. Launch Profile: `Typedown.WinUI (Unpackaged)`

## 命令行验证说明

命令行可以验证 WinUI shell 本身可构建，以及单项目 MSIX signed package target 能跑通，但不能完整替代 VS 的 F5 部署体验。

- 常规构建：`dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64`
- 架构边界测试：`dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug`
- solution 构建：`dotnet build .\Typedown.sln -c Debug -p:Platform=x64`
- signed package publish：`dotnet msbuild .\Dev\Typedown.WinUI\Typedown.WinUI.csproj /restore /t:Publish /p:Configuration=Debug /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true`

最后一条命令能确认 WinUI 项目自己的 signed package target 可产出 `.msix`。但 VS 是否能直接完成 packaged 启动，仍需要用户在 VS 中实际点一次 `Typedown.WinUI (Package)` 确认。
