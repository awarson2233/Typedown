# Phase 12 Typedown.UI + MVVM 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建立 `Dev\Typedown.UI` 项目和最小 MVVM 边界，让 `Typedown.WinUI` 只负责 WinUI3 shell/platform hosting，页面状态和页面级命令开始进入 `Typedown.UI`。

**Architecture:** Phase 12 只引入 MVVM 骨架，不迁移 legacy XAML 页面，不改当前 editor host 布局，不重写 WebView bridge。目标依赖方向是 `Typedown.WinUI -> Typedown.UI -> Typedown.Core.Contracts`，后续如确需 `Typedown.UI -> Typedown.Core` 必须单独记录原因。

**Tech Stack:** .NET 9, C#, WinUI 3, Microsoft.Extensions.DependencyInjection, MSTest architecture tests.

---

## 范围和非目标

Phase 12 要做：

- 新建 `Dev\Typedown.UI`。
- 引入最小 MVVM 基础类型：observable base、relay command、UI composition registration。
- 将当前 WinUI smoke 页面用到的展示状态迁入 `Typedown.UI.ViewModels`。
- 让 `Typedown.WinUI.Views.MainPage` 绑定 `MainPageViewModel`，但不改变 XAML 布局结构。
- 加 architecture tests 锁定依赖方向和禁止项。
- 更新文档和验证命令。

Phase 12 不做：

- 不批量迁移 `Dev\Typedown.Core\Controls`、`Pages`、`Resources`。
- 不移动 `WinUIEditorHost`、`WinUIEditorBridgeAdapter`、`WinUIEditorDocumentSession`。
- 不迁移 legacy `Dev\Typedown.XamlUI` host/run loop/HWND/DWM/dispatcher 代码。
- 不改 React/CRA/editor bundle。
- 不做 ARM64。
- 不要求 UI 视觉变化；验收标准是边界成立、构建通过、当前布局不变。

## 目标文件结构

新增：

```text
Dev\Typedown.UI\Typedown.UI.csproj
Dev\Typedown.UI\Composition\ServiceCollectionExtensions.cs
Dev\Typedown.UI\Mvvm\ObservableObject.cs
Dev\Typedown.UI\Mvvm\RelayCommand.cs
Dev\Typedown.UI\ViewModels\MainPageViewModel.cs
Dev\Typedown.UI\ViewModels\MigrationBoundaryItem.cs
docs\phase12-mvvm-ui-plan.md
```

修改：

```text
Typedown.sln
Dev\Typedown.WinUI\Typedown.WinUI.csproj
Dev\Typedown.WinUI\App.xaml.cs
Dev\Typedown.WinUI\Views\MainPage.xaml
Dev\Typedown.WinUI\Views\MainPage.xaml.cs
Tests\Typedown.ArchitectureTests\Phase10CoreContractsBoundaryTests.cs
docs\winui3-post-phase9-roadmap.md
docs\winui3-target-architecture.md
```

## 并行开发结论

可以并行，但不是无限并行。Phase 12 推荐使用一个集成 worktree，加 2 到 3 个子任务 worktree。

可并行：

- MVVM 基础类型和 `MainPageViewModel` 可由一个 agent 完成。
- architecture tests 和文档可由另一个 agent 完成。
- WinUI 接线可以在 MVVM API 确定后由第三个 agent 完成。

必须串行：

- `Typedown.sln` 和 `.csproj` 修改只能由一个 agent 独占。
- `MainPage.xaml` / `MainPage.xaml.cs` 只能在 `MainPageViewModel` API 固定后修改。
- 最终合并和验证必须回到主工作区或集成 worktree 串行执行。

推荐 worktree：

```powershell
git worktree add D:\source\repos\Typedown.worktrees\phase12-ui-skeleton -b work/phase12-ui-skeleton winui3-migration
git worktree add D:\source\repos\Typedown.worktrees\phase12-mvvm-core -b work/phase12-mvvm-core winui3-migration
git worktree add D:\source\repos\Typedown.worktrees\phase12-tests-docs -b work/phase12-tests-docs winui3-migration
```

合并顺序：

1. `work/phase12-ui-skeleton`
2. `work/phase12-mvvm-core`
3. `work/phase12-tests-docs`

## Task 1: 项目骨架和 solution 集成

**Files:**

- Create: `Dev\Typedown.UI\Typedown.UI.csproj`
- Modify: `Typedown.sln`
- Modify: `Dev\Typedown.WinUI\Typedown.WinUI.csproj`

- [ ] **Step 1: 新建 Typedown.UI 项目文件**

创建 `Dev\Typedown.UI\Typedown.UI.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <Configurations>Debug;Release;Debug_Local</Configurations>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Typedown.Core.Contracts\Typedown.Core.Contracts.csproj" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.8" />
  </ItemGroup>
</Project>
```

理由：Phase 12 的 UI 层先保持平台中立，不引用 `Microsoft.UI.Xaml`，避免把 UI 项目变成另一个 WinUI shell。

- [ ] **Step 2: 将 Typedown.UI 加入 solution**

使用非交互命令：

```powershell
dotnet sln .\Typedown.sln add .\Dev\Typedown.UI\Typedown.UI.csproj
```

预期：`Typedown.sln` 中新增 `Typedown.UI` 项目。

- [ ] **Step 3: 让 Typedown.WinUI 引用 Typedown.UI**

在 `Dev\Typedown.WinUI\Typedown.WinUI.csproj` 的 `ProjectReference` item group 中加入：

```xml
<ProjectReference Include="..\Typedown.UI\Typedown.UI.csproj" />
```

保留现有 `Typedown.Core.Contracts` 引用；不要加入 `Typedown.Core` 或 `Typedown.XamlUI`。

- [ ] **Step 4: 验证项目骨架构建**

运行：

```powershell
dotnet build .\Dev\Typedown.UI\Typedown.UI.csproj -c Debug /nologo /v:minimal
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false
```

预期：两个命令 exit 0。

## Task 2: MVVM 基础类型

**Files:**

- Create: `Dev\Typedown.UI\Mvvm\ObservableObject.cs`
- Create: `Dev\Typedown.UI\Mvvm\RelayCommand.cs`

- [ ] **Step 1: 创建 ObservableObject**

创建 `Dev\Typedown.UI\Mvvm\ObservableObject.cs`：

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Typedown.UI.Mvvm;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

- [ ] **Step 2: 创建 RelayCommand**

创建 `Dev\Typedown.UI\Mvvm\RelayCommand.cs`：

```csharp
using System.Windows.Input;

namespace Typedown.UI.Mvvm;

public sealed class RelayCommand : ICommand
{
    private readonly Action execute;
    private readonly Func<bool>? canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        execute();
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
```

- [ ] **Step 3: 构建 Typedown.UI**

运行：

```powershell
dotnet build .\Dev\Typedown.UI\Typedown.UI.csproj -c Debug /nologo /v:minimal
```

预期：exit 0。

## Task 3: MainPageViewModel 和 UI 注册

**Files:**

- Create: `Dev\Typedown.UI\ViewModels\MigrationBoundaryItem.cs`
- Create: `Dev\Typedown.UI\ViewModels\MainPageViewModel.cs`
- Create: `Dev\Typedown.UI\Composition\ServiceCollectionExtensions.cs`

- [ ] **Step 1: 创建展示 DTO**

创建 `Dev\Typedown.UI\ViewModels\MigrationBoundaryItem.cs`：

```csharp
namespace Typedown.UI.ViewModels;

public sealed record MigrationBoundaryItem(string Text);
```

- [ ] **Step 2: 创建 MainPageViewModel**

创建 `Dev\Typedown.UI\ViewModels\MainPageViewModel.cs`：

```csharp
using Typedown.UI.Mvvm;

namespace Typedown.UI.ViewModels;

public sealed class MainPageViewModel : ObservableObject
{
    private string contractsProbeSummary = "Phase 12 MVVM shell is waiting for WinUI platform service initialization.";
    private IReadOnlyList<MigrationBoundaryItem> serviceItems = Array.Empty<MigrationBoundaryItem>();

    public string Title { get; } = "Typedown WinUI3 Platform Services";

    public string Subtitle { get; } =
        "Phase 12 introduces the Typedown.UI MVVM boundary while preserving the Phase 11 WebView2 editor host layout.";

    public string ContractsProbeSummary
    {
        get => contractsProbeSummary;
        private set => SetProperty(ref contractsProbeSummary, value);
    }

    public IReadOnlyList<MigrationBoundaryItem> ValidatedItems { get; } =
    [
        new("WinUI3 shell keeps platform service implementations and activation behind Typedown.WinUI."),
        new("Typedown.UI owns page-level MVVM state and composition, not process startup or package deployment."),
        new("The Phase 11 editor host remains in Typedown.WinUI while the UI boundary is introduced."),
        new("The same Resources\\Statics editor bundle remains the runtime editor asset source.")
    ];

    public IReadOnlyList<MigrationBoundaryItem> ServiceItems
    {
        get => serviceItems;
        private set => SetProperty(ref serviceItems, value);
    }

    public IReadOnlyList<MigrationBoundaryItem> DeferredItems { get; } =
    [
        new("Real legacy page/control migration is deferred to Phase 13."),
        new("Real Typedown.Core document workflow integration remains after the MVVM skeleton."),
        new("WinUI platform services stay in Typedown.WinUI and are not registered by Typedown.UI."),
        new("ARM64 validation remains out of scope until after WinUI3 cutover.")
    ];

    public void ApplyPlatformServiceSummary(string summary, IEnumerable<string> serviceNames)
    {
        ContractsProbeSummary = summary;
        ServiceItems = serviceNames.Select(name => new MigrationBoundaryItem($"Validated service: {name}")).ToArray();
    }
}
```

- [ ] **Step 3: 创建 AddTypedownUI 注册扩展**

创建 `Dev\Typedown.UI\Composition\ServiceCollectionExtensions.cs`：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Typedown.UI.ViewModels;

namespace Typedown.UI.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTypedownUI(this IServiceCollection services)
    {
        services.AddTransient<MainPageViewModel>();
        return services;
    }
}
```

- [ ] **Step 4: 构建 Typedown.UI**

运行：

```powershell
dotnet build .\Dev\Typedown.UI\Typedown.UI.csproj -c Debug /nologo /v:minimal
```

预期：exit 0。

## Task 4: WinUI shell 接入 Typedown.UI MVVM

**Files:**

- Modify: `Dev\Typedown.WinUI\App.xaml.cs`
- Modify: `Dev\Typedown.WinUI\Views\MainPage.xaml`
- Modify: `Dev\Typedown.WinUI\Views\MainPage.xaml.cs`

- [ ] **Step 1: 在 App 中创建 UI service provider**

在 `App.xaml.cs` 中引入：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Typedown.UI.Composition;
```

在 `App` 类中增加字段：

```csharp
private ServiceProvider? uiServices;
```

在 `OnLaunched` 初始化 `platformServices` 后增加：

```csharp
uiServices ??= new ServiceCollection()
    .AddTypedownUI()
    .BuildServiceProvider();
```

导航参数改为一个 shell 参数对象。若不新增类型，也可先传 tuple，但推荐新增 private record：

```csharp
private sealed record MainPageNavigationContext(
    WinUIPlatformServices PlatformServices,
    IServiceProvider UiServices);
```

导航调用改为：

```csharp
_ = rootFrame.Navigate(typeof(MainPage), new MainPageNavigationContext(platformServices, uiServices));
```

- [ ] **Step 2: 修改 MainPage.xaml 的绑定源**

保留现有布局、颜色、行列结构和 editor host 位置，只替换绑定：

```xml
Text="{x:Bind ViewModel.Title, Mode=OneWay}"
Text="{x:Bind ViewModel.Subtitle, Mode=OneWay}"
Text="{x:Bind ViewModel.ContractsProbeSummary, Mode=OneWay}"
ItemsSource="{x:Bind ViewModel.ValidatedItems, Mode=OneWay}"
ItemsSource="{x:Bind ViewModel.ServiceItems, Mode=OneWay}"
ItemsSource="{x:Bind ViewModel.DeferredItems, Mode=OneWay}"
```

`DataTemplate` 的 `x:DataType` 改为：

```xml
x:DataType="ui:MigrationBoundaryItem"
```

并新增 namespace：

```xml
xmlns:ui="using:Typedown.UI.ViewModels"
```

模板内部文本改为：

```xml
Text="{x:Bind Text}"
```

不要移动 `<controls:WinUIEditorHost Grid.Row="1" />`。

- [ ] **Step 3: 修改 MainPage.xaml.cs 使用 MainPageViewModel**

目标结构：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Navigation;
using Typedown.UI.ViewModels;
using Typedown.WinUI.Services;

namespace Typedown.WinUI.Views
{
    public partial class MainPage : Page
    {
        public MainPageViewModel ViewModel { get; private set; }

        public MainPage()
        {
            InitializeComponent();
            ViewModel = new MainPageViewModel();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var context = e.Parameter;
            var services = ResolvePlatformServices(context);
            ViewModel = ResolveViewModel(context);
            ViewModel.ApplyPlatformServiceSummary(
                Phase10ContractsProbe.Describe(services),
                services.ServiceNames);
            Bindings.Update();
        }

        private static WinUIPlatformServices ResolvePlatformServices(object? parameter)
        {
            var platformProperty = parameter?.GetType().GetProperty("PlatformServices");
            return platformProperty?.GetValue(parameter) as WinUIPlatformServices
                ?? ((App)Application.Current).PlatformServices;
        }

        private static MainPageViewModel ResolveViewModel(object? parameter)
        {
            var providerProperty = parameter?.GetType().GetProperty("UiServices");
            var provider = providerProperty?.GetValue(parameter) as IServiceProvider;
            return provider?.GetService<MainPageViewModel>() ?? new MainPageViewModel();
        }
    }
}
```

实现时可以把 navigation context type 调整为 internal 类型，避免反射；关键是 `MainPage` 不再自己维护 `ValidatedItems`、`DeferredItems`、`ServiceItems` 字符串数组。

- [ ] **Step 4: 构建 WinUI**

运行：

```powershell
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false
```

预期：exit 0。

## Task 5: 架构测试

**Files:**

- Modify: `Tests\Typedown.ArchitectureTests\Phase10CoreContractsBoundaryTests.cs`
- Modify: `Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj`

- [ ] **Step 1: ArchitectureTests 引用 Typedown.UI**

在 `Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj` 中加入：

```xml
<ProjectReference Include="..\..\Dev\Typedown.UI\Typedown.UI.csproj" />
```

- [ ] **Step 2: 增加 Phase 12 依赖边界测试**

在 `Phase10CoreContractsBoundaryTests.cs` 增加测试：

```csharp
[TestMethod]
public void Phase12TypedownUI_ProvidesMvvmBoundaryWithoutWinUIShellDependency()
{
    var uiProjectPath = Path.Combine(RepoRoot, "Dev", "Typedown.UI", "Typedown.UI.csproj");
    var winuiProjectSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));
    var solutionSource = File.ReadAllText(Path.Combine(RepoRoot, "Typedown.sln"));

    Assert.IsTrue(File.Exists(uiProjectPath), "Expected Phase 12 Typedown.UI project.");

    var uiProjectSource = File.ReadAllText(uiProjectPath);
    AssertHasTypeReference(solutionSource, "Typedown.UI");
    AssertHasTypeReference(uiProjectSource, "<TargetFramework>net9.0</TargetFramework>");
    AssertHasTypeReference(uiProjectSource, @"..\Typedown.Core.Contracts\Typedown.Core.Contracts.csproj");
    AssertNoTypeReference(uiProjectSource, @"..\Typedown.WinUI\Typedown.WinUI.csproj");
    AssertNoTypeReference(uiProjectSource, @"..\Typedown.XamlUI\Typedown.XamlUI.csproj");
    AssertNoTypeReference(uiProjectSource, "Microsoft.UI.Xaml");
    AssertNoTypeReference(uiProjectSource, "Windows.UI.Xaml");
    AssertHasTypeReference(winuiProjectSource, @"..\Typedown.UI\Typedown.UI.csproj");
}
```

- [ ] **Step 3: 增加 MVVM 文件测试**

继续增加：

```csharp
[TestMethod]
public void Phase12TypedownUI_OwnsMainPageMvvmRegistration()
{
    var uiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.UI");
    var mainPageSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Views", "MainPage.xaml.cs"));
    var appSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "App.xaml.cs"));

    Assert.IsTrue(File.Exists(Path.Combine(uiRoot, "Mvvm", "ObservableObject.cs")));
    Assert.IsTrue(File.Exists(Path.Combine(uiRoot, "Mvvm", "RelayCommand.cs")));
    Assert.IsTrue(File.Exists(Path.Combine(uiRoot, "ViewModels", "MainPageViewModel.cs")));
    Assert.IsTrue(File.Exists(Path.Combine(uiRoot, "ViewModels", "MigrationBoundaryItem.cs")));
    Assert.IsTrue(File.Exists(Path.Combine(uiRoot, "Composition", "ServiceCollectionExtensions.cs")));

    AssertHasTypeReference(File.ReadAllText(Path.Combine(uiRoot, "Composition", "ServiceCollectionExtensions.cs")), "AddTypedownUI");
    AssertHasTypeReference(File.ReadAllText(Path.Combine(uiRoot, "ViewModels", "MainPageViewModel.cs")), "ObservableObject");
    AssertHasTypeReference(File.ReadAllText(Path.Combine(uiRoot, "ViewModels", "MainPageViewModel.cs")), "ApplyPlatformServiceSummary");
    AssertHasTypeReference(appSource, "AddTypedownUI()");
    AssertHasTypeReference(mainPageSource, "MainPageViewModel");
    AssertHasTypeReference(mainPageSource, "ViewModel.ApplyPlatformServiceSummary");
    AssertNoTypeReference(mainPageSource, "public string[] ValidatedItems");
    AssertNoTypeReference(mainPageSource, "public string[] DeferredItems");
}
```

- [ ] **Step 4: 跑 architecture tests**

运行：

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug /nologo /v:minimal
```

预期：所有测试通过。

## Task 6: 文档更新

**Files:**

- Modify: `docs\winui3-post-phase9-roadmap.md`
- Modify: `docs\winui3-target-architecture.md`
- Modify: `docs\build-baseline.md`

- [ ] **Step 1: 更新路线图 Phase 12**

在 `docs\winui3-post-phase9-roadmap.md` 的 Phase 12 中明确：

```text
Phase 12 引入 MVVM 骨架，但不改变当前页面布局。Typedown.UI 先承接 MainPageViewModel、MVVM 基础类型和 AddTypedownUI 注册入口；Typedown.WinUI 仍负责 Window、Frame、平台服务、WebView2 host 和 Package/Unpackaged 启动。
```

- [ ] **Step 2: 更新目标架构**

在 `docs\winui3-target-architecture.md` 的 `Typedown.UI` 模块说明中补充：

```text
Phase 12 后，Typedown.UI 的第一批职责是 MVVM 基础类型、MainPageViewModel 和 UI 注册边界。它仍不得引用 Typedown.WinUI，也不得包含 WinUI platform service 实现。
```

- [ ] **Step 3: 更新基线验证**

在 `docs\build-baseline.md` 的 WinUI3 验证命令中加入：

```powershell
dotnet build .\Dev\Typedown.UI\Typedown.UI.csproj -c Debug /nologo /v:minimal
```

## Task 7: 最终验证

**Files:**

- No direct file edits.

- [ ] **Step 1: 运行 Phase 12 最小验证**

```powershell
dotnet build .\Dev\Typedown.UI\Typedown.UI.csproj -c Debug /nologo /v:minimal
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug /nologo /v:minimal
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false
dotnet build .\Typedown.sln -c Debug_Local -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false
```

预期：

- `Typedown.UI` 构建通过。
- architecture tests 全部通过。
- WinUI `Debug|x64` 构建通过。
- solution `Debug_Local|x64` 构建通过。

- [ ] **Step 2: 验证 editor bundle 未丢失**

```powershell
Test-Path .\Dev\Typedown.WinUI\bin\x64\Debug\net9.0-windows10.0.26100.0\Resources\Statics\index.html
Test-Path .\Dev\Typedown.WinUI\bin\x64\Debug_Local\net9.0-windows10.0.26100.0\Resources\Statics\index.html
```

预期：两个命令都输出 `True`。

- [ ] **Step 3: 手动 VS 验证**

在 Visual Studio 中验证：

```text
Configuration: Debug_Local
Platform: x64
Startup Project: Typedown.WinUI
Launch Profile: Typedown.WinUI (Unpackaged)
```

预期：当前 WinUI3 页面布局不变，editor host 仍显示。

## Agent 分发建议

如果使用多 agent：

- Worker A：执行 Task 1。独占 `Typedown.sln`、`Dev\Typedown.UI\Typedown.UI.csproj`、`Dev\Typedown.WinUI\Typedown.WinUI.csproj`。
- Worker B：执行 Task 2 和 Task 3。只写 `Dev\Typedown.UI\Mvvm`、`Dev\Typedown.UI\ViewModels`、`Dev\Typedown.UI\Composition`。
- Worker C：执行 Task 5 和 Task 6。只写 `Tests\Typedown.ArchitectureTests` 和 `docs`。
- Integrator：执行 Task 4 和 Task 7，负责把 WinUI shell 接到已稳定的 `MainPageViewModel` API。

注意：Worker B 可以先在自己的 worktree 创建 `Dev\Typedown.UI` 文件，但最终由 Worker A 或 Integrator 统一处理 solution/project 文件，避免冲突。
