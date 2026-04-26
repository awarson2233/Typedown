# Phase 10c: WinUI Package Baseline

## 阶段目标

Phase 10c 的目标是把 `Dev\Typedown.WinUI` 收敛成一个可直接承载 VS 调试的 WinUI shell 基线，并明确 Package 与 Unpackaged 的边界。

- `Typedown.WinUI (Unpackaged)` 是当前日常开发入口：`Debug_Local|x64`，不走 MSIX 部署，不依赖证书。
- `Typedown.WinUI (Package)` 是后续部署验证入口：`Debug|x64`，走 MSIX 部署，必须有受信任签名。
- 当前命令行和 VS 验证以 `Debug_Local|x64` 的 Unpackaged 启动为日常基线，以 `Debug|x64` 的 Package 作为部署基线。
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
- WinUI 项目在 `Debug_Local` 下显式使用 `<WindowsPackageType>None</WindowsPackageType>`，避免 Unpackaged 调试时进入 MSIX 部署链。
- WinUI 项目全局关闭 WinAppSDK DeploymentManager 自动初始化，并全局启用 Bootstrap 自动初始化；这样 `Debug + Unpackaged` 不会再进入 `DeploymentManagerAutoInitializer`，也不会在 `Microsoft.UI.Xaml.Application.Start` 前因 WinUI runtime 未 bootstrap 而触发 `REGDB_E_CLASSNOTREG`。
- `Package.appxmanifest` 改为 WinUI shell 自己的 package identity，不再使用模板占位值。
- `Typedown.sln` 的 `Debug|x64` 至少确保 `Typedown.WinUI` 自己具备 Build + Deploy，而旧 `Typedown.Package` 不再参与该配置的 Deploy。
- `Typedown.WinUI` 在 `Debug|x64` 下补上 `Deploy.0`，让 VS packaged 调试落在 WinUI 项目本身，而不是旧 `wapproj`。
- `Typedown.sln` 的 `Debug_Local|x64` 只构建 `Typedown.Core.Contracts` + `Typedown.WinUI`，不部署、不构建 legacy `Typedown.Package` / `XamlDesignApp` / `Typedown.XamlUI` / old tests。
- 仓库内提供 `Typedown.WinUI.DevTest.pfx` + `Typedown.WinUI.DevTest.cer`，仅用于本地 packaged debug / test 签名，不作为正式发布证书。

## Debug Local 与 Package 的区别

`Debug_Local|x64 + Typedown.WinUI (Unpackaged)` 是当前推荐日常入口。这个组合不需要证书，也不应该进入 MSIX 部署链。

`Debug|x64 + Typedown.WinUI (Package)` 是 Package/MSIX 验证入口。MSIX 部署必须有签名，且签名根证书必须被 Windows 信任；如果证书不被信任，会出现 `0x800B0109`，这是部署层限制，不是应用代码错误。

如果 `Debug_Local` 也使用 `<WindowsPackageType>MSIX</WindowsPackageType>`，Windows App SDK 会向 Unpackaged 启动产物注入 DeploymentManager 自动初始化，可能在启动前触发 `DeploymentInitializeOptions` 的 `REGDB_E_CLASSNOTREG (0x80040154)`。因此 `Debug_Local` 必须保持 `WindowsPackageType=None`。

当前 `Typedown.WinUI` 同时显式设置：

```xml
<WindowsAppSdkBootstrapInitialize>true</WindowsAppSdkBootstrapInitialize>
<WindowsAppSDKBootstrapAutoInitializeOptions_OnPackageIdentity_NoOp>true</WindowsAppSDKBootstrapAutoInitializeOptions_OnPackageIdentity_NoOp>
<WindowsAppSdkDeploymentManagerInitialize>false</WindowsAppSdkDeploymentManagerInitialize>
```

这让 `Debug + Typedown.WinUI (Unpackaged)` 作为误选配置时也能启动到 WinUI runtime；但日常基线仍推荐 `Debug_Local + Typedown.WinUI (Unpackaged)`，Package 验证仍推荐 `Debug + Typedown.WinUI (Package)`。

## Visual Studio 使用方式

日常无证书调试请使用：

1. Solution Configuration: `Debug_Local`
2. Solution Platform: `x64`
3. Startup Project: `Typedown.WinUI`
4. Launch Profile: `Typedown.WinUI (Unpackaged)`

首次在新机器上做 packaged F5 前，先运行：

`powershell -ExecutionPolicy Bypass -File .\scripts\install-winui-dev-certificate.ps1`

这一步会把仓库内的 WinUI dev/test 证书导入当前用户证书存储。若 Package 部署仍报 `0x800B0109`，需要以管理员方式将证书导入 `LocalMachine\TrustedPeople` / `LocalMachine\Root`，或改用 `Debug_Local + Unpackaged` 继续日常开发。

请在 VS 中明确选择下面这组组合：

1. Solution Configuration: `Debug`
2. Solution Platform: `x64`
3. Startup Project: `Typedown.WinUI`
4. Launch Profile: `Typedown.WinUI (Package)`

## 命令行验证说明

命令行可以验证 WinUI shell 本身可构建，以及单项目 MSIX signed package target 能跑通，但不能完整替代 VS 的 F5 部署体验。

- 日常 Unpackaged 构建：`dotnet build .\Typedown.sln -c Debug_Local -p:Platform=x64`
- Package solution 构建：`dotnet build .\Typedown.sln -c Debug -p:Platform=x64`
- 架构边界测试：`dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug`
- signed package publish：`dotnet msbuild .\Dev\Typedown.WinUI\Typedown.WinUI.csproj /restore /t:Publish /p:Configuration=Debug /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true`

最后一条命令能确认 WinUI 项目自己的 signed package target 可产出 `.msix`。但 VS 是否能直接完成 packaged 启动，仍需要用户在 VS 中实际点一次 `Typedown.WinUI (Package)` 确认。

## Phase 11 后的 Package 资源规则

WinUI editor host 复用旧前端产物 `Dev\Typedown\Resources\Statics`。该目录必须同时进入：

- `Debug_Local|x64` 的普通输出目录，用于 Unpackaged 日常调试。
- `Debug|x64` 的 Package payload，用于 `Typedown.WinUI (Package)` 部署启动。

`Dev\Typedown.WinUI\Typedown.WinUI.csproj` 中的 `AddEditorStaticBundleToPackagingOutputs` target 会在 `GetPackagingOutputs` 之后、`_ComputeAppxPackagePayload` 之前，把 `..\Typedown\Resources\Statics\**\*` 加入 `PackagingOutputs`，目标路径保持为 `Resources\Statics\%(RecursiveDir)%(Filename)%(Extension)`。

如果 packaged 启动时 Visual Studio 报：

```text
无法激活 Windows 应用商店应用“62082Surprise.Typedown.WinUI_m01jdq2q5rxw0!App”。
激活请求失败，错误为“系统找不到指定的文件”。
```

先检查当前注册包是否指向有效输出目录：

```powershell
Get-AppxPackage -Name 62082Surprise.Typedown.WinUI |
  Select-Object Name,PackageFullName,InstallLocation,Status,SignatureKind,IsDevelopmentMode
```

如果 `InstallLocation` 为空或不是当前 `Debug` 输出目录，重新注册 loose package：

```powershell
$pkg = Get-AppxPackage -Name 62082Surprise.Typedown.WinUI -ErrorAction SilentlyContinue
if ($pkg) { Remove-AppxPackage -Package $pkg.PackageFullName }
Add-AppxPackage -Register .\Dev\Typedown.WinUI\bin\x64\Debug\net9.0-windows10.0.26100.0\AppxManifest.xml
```

注册后至少验证：

```powershell
$loc = (Get-AppxPackage -Name 62082Surprise.Typedown.WinUI).InstallLocation
Test-Path (Join-Path $loc 'Typedown.WinUI.exe')
Test-Path (Join-Path $loc 'Resources\Statics\index.html')
```
