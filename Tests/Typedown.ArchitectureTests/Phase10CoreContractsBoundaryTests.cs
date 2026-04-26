using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase10CoreContractsBoundaryTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void ContractsProject_ExistsAndTargetsPlatformNeutralNet9()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Typedown.Core.Contracts.csproj"));

        AssertHasTypeReference(source, "<TargetFramework>net9.0</TargetFramework>");
        AssertHasTypeReference(source, "<Nullable>enable</Nullable>");
        AssertNoTypeReference(source, "windows");
        AssertNoTypeReference(source, "UseUwp");
        AssertNoTypeReference(source, "CsWinRT");
        AssertNoTypeReference(source, "Typedown.XamlUI");
        AssertNoTypeReference(source, "WebView2");
    }

    [TestMethod]
    public void ContractsProject_OwnsTheNeutralInterfaceSurface()
    {
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Interfaces", "IAppDataPathProvider.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Interfaces", "IDialogService.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Interfaces", "IFilePickerService.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Interfaces", "IUiDispatcher.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Interfaces", "IWindowContext.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Interfaces", "IAppActivationService.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Editor", "IEditorDocumentSession.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Editor", "EditorDocumentState.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Editor", "EditorSettingsSnapshot.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Editor", "EditorSettingsPayload.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Editor", "EditorEventMessage.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Editor", "EditorHostMessage.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Editor", "EditorHostCommands.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Editor", "EditorPersistenceResult.cs")));
    }

    [TestMethod]
    public void EditorContracts_StayPlatformNeutralAndAvoidLegacyDependencies()
    {
        var editorFiles = Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Editor"), "*.cs", SearchOption.TopDirectoryOnly)
            .ToArray();

        Assert.IsTrue(editorFiles.Length >= 6, "Expected the Phase 11 editor contract surface.");

        foreach (var file in editorFiles)
        {
            var source = File.ReadAllText(file);

            AssertNoTypeReference(source, "Microsoft.UI");
            AssertNoTypeReference(source, "Windows.UI.Xaml");
            AssertNoTypeReference(source, "WebView2");
            AssertNoTypeReference(source, "Typedown.XamlUI");
            AssertNoTypeReference(source, "Newtonsoft");
        }
    }

    [TestMethod]
    public void CoreAndLegacyAppReferenceContractsProject()
    {
        var coreProject = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Typedown.Core.csproj"));
        var appProject = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown", "Typedown.csproj"));

        AssertHasTypeReference(coreProject, @"..\Typedown.Core.Contracts\Typedown.Core.Contracts.csproj");
        AssertHasTypeReference(appProject, @"..\Typedown.Core.Contracts\Typedown.Core.Contracts.csproj");
    }

    [TestMethod]
    public void WinUISpike_ReferencesContractsButNotCoreOrLegacyXamlHost()
    {
        var projectSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));

        AssertHasTypeReference(projectSource, @"..\Typedown.Core.Contracts\Typedown.Core.Contracts.csproj");
        AssertNoTypeReference(projectSource, @"..\Typedown.Core\Typedown.Core.csproj");
        AssertNoTypeReference(projectSource, @"..\Typedown.XamlUI\Typedown.XamlUI.csproj");
        AssertNoTypeReference(projectSource, "Typedown.XamlUI");
    }

    [TestMethod]
    public void WinUIPackagedBaseline_UsesSolutionDebugX64BuildAndDeployWithoutLegacyProjects()
    {
        var solutionSource = File.ReadAllText(Path.Combine(RepoRoot, "Typedown.sln"));
        var winuiProjectGuid = FindProjectGuid(solutionSource, "Typedown.WinUI");
        var contractsProjectGuid = FindProjectGuid(solutionSource, "Typedown.Core.Contracts");
        var legacyAppGuid = FindProjectGuid(solutionSource, "Typedown");
        var legacyPackageGuid = FindProjectGuid(solutionSource, "Typedown.Package");
        var editorGuid = FindProjectGuid(solutionSource, "Typedown.Editor");
        var legacyTestGuid = FindProjectGuid(solutionSource, "Typedown.Test");
        var legacyCoreGuid = FindProjectGuid(solutionSource, "Typedown.Core");
        var xamlDesignGuid = FindProjectGuid(solutionSource, "XamlDesignApp");
        var xamlUiGuid = FindProjectGuid(solutionSource, "Typedown.XamlUI");

        AssertHasTypeReference(solutionSource, $"{contractsProjectGuid}.Debug|x64.Build.0");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Debug|x64.Build.0");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Debug|x64.Deploy.0");
        AssertDebugX64DoesNotBuildOrDeployLegacyProject(solutionSource, legacyAppGuid);
        AssertDebugX64DoesNotBuildOrDeployLegacyProject(solutionSource, legacyPackageGuid);
        AssertDebugX64DoesNotBuildOrDeployLegacyProject(solutionSource, xamlDesignGuid);
        AssertDebugX64DoesNotBuildOrDeployLegacyProject(solutionSource, xamlUiGuid);

        AssertHasTypeReference(solutionSource, $"{contractsProjectGuid}.Debug_Local|x64.Build.0");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Debug_Local|x64.Build.0");
        AssertNoTypeReference(solutionSource, $"{winuiProjectGuid}.Debug_Local|ARM64.Deploy.0");
        AssertNoTypeReference(solutionSource, $"{winuiProjectGuid}.Debug_Local|x64.Deploy.0");
        AssertNoTypeReference(solutionSource, $"{winuiProjectGuid}.Debug_Local|x86.Deploy.0");
        AssertDebugLocalX64DoesNotBuildOrDeployLegacyProject(solutionSource, legacyAppGuid);
        AssertDebugLocalX64DoesNotBuildOrDeployLegacyProject(solutionSource, legacyPackageGuid);
        AssertDebugLocalX64DoesNotBuildOrDeployLegacyProject(solutionSource, editorGuid);
        AssertDebugLocalX64DoesNotBuildOrDeployLegacyProject(solutionSource, legacyTestGuid);
        AssertDebugLocalX64DoesNotBuildOrDeployLegacyProject(solutionSource, legacyCoreGuid);
        AssertDebugLocalX64DoesNotBuildOrDeployLegacyProject(solutionSource, xamlDesignGuid);
        AssertDebugLocalX64DoesNotBuildOrDeployLegacyProject(solutionSource, xamlUiGuid);
    }

    [TestMethod]
    public void WinUIPackagedBaseline_UsesRepositoryDevCertificate()
    {
        var projectSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));
        var launchSettingsSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Properties", "launchSettings.json"));
        var manifestSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Package.appxmanifest"));
        var scriptSource = File.ReadAllText(Path.Combine(RepoRoot, "scripts", "install-winui-dev-certificate.ps1"));

        AssertHasTypeReference(projectSource, "<WindowsPackageType>MSIX</WindowsPackageType>");
        AssertHasTypeReference(projectSource, "<WindowsAppSdkBootstrapInitialize>true</WindowsAppSdkBootstrapInitialize>");
        AssertHasTypeReference(projectSource, "<WindowsAppSDKBootstrapAutoInitializeOptions_OnPackageIdentity_NoOp>true</WindowsAppSDKBootstrapAutoInitializeOptions_OnPackageIdentity_NoOp>");
        AssertHasTypeReference(projectSource, "<WindowsAppSdkDeploymentManagerInitialize>false</WindowsAppSdkDeploymentManagerInitialize>");
        AssertHasTypeReference(projectSource, "<AppxPackageSigningEnabled>true</AppxPackageSigningEnabled>");
        AssertHasTypeReference(projectSource, "<PackageCertificateThumbprint>43B9C8C444BBB5C2DAC24C8925EDAFB6BF6163C5</PackageCertificateThumbprint>");
        AssertHasTypeReference(launchSettingsSource, "\"Typedown.WinUI (Package)\"");
        AssertHasTypeReference(launchSettingsSource, "\"commandName\": \"MsixPackage\"");
        AssertHasTypeReference(manifestSource, "Publisher=\"CN=Typedown WinUI Dev Test\"");
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.DevTest.pfx")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.DevTest.cer")));
        AssertHasTypeReference(scriptSource, "Typedown.WinUI.DevTest.pfx");
        AssertHasTypeReference(scriptSource, "Cert:\\CurrentUser\\My");
        AssertHasTypeReference(scriptSource, "Cert:\\CurrentUser\\TrustedPeople");
        AssertHasTypeReference(scriptSource, "Cert:\\CurrentUser\\Root");
    }

    [TestMethod]
    public void WinUIDebugLocal_DisablesMsixDeploymentManagerAutoInitialization()
    {
        var projectSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));
        var debugLocalPropertyGroup = Regex.Match(
            projectSource,
            @"<PropertyGroup\s+Condition=""'\$\(Configuration\)'=='Debug_Local'"">(?<body>.*?)</PropertyGroup>",
            RegexOptions.Singleline);

        Assert.IsTrue(debugLocalPropertyGroup.Success, "Expected Debug_Local-specific WinUI project properties.");
        AssertHasTypeReference(debugLocalPropertyGroup.Groups["body"].Value, "<WindowsPackageType>None</WindowsPackageType>");
        AssertHasTypeReference(debugLocalPropertyGroup.Groups["body"].Value, "<AppxPackageSigningEnabled>false</AppxPackageSigningEnabled>");
    }

    [TestMethod]
    public void XamlDesignApp_DebugLocalMappingsPointToExistingDebugConfigurations()
    {
        var solutionSource = File.ReadAllText(Path.Combine(RepoRoot, "Typedown.sln"));
        var projectGuid = FindProjectGuid(solutionSource, "XamlDesignApp");

        AssertHasTypeReference(solutionSource, $"{projectGuid}.Debug_Local|ARM64.ActiveCfg = Debug|ARM64");
        AssertHasTypeReference(solutionSource, $"{projectGuid}.Debug_Local|x64.ActiveCfg = Debug|x64");
        AssertHasTypeReference(solutionSource, $"{projectGuid}.Debug_Local|x86.ActiveCfg = Debug|x86");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug_Local|ARM64.ActiveCfg = Debug_Local|ARM64");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug_Local|x64.ActiveCfg = Debug_Local|x64");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug_Local|x86.ActiveCfg = Debug_Local|x86");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug_Local|ARM64.Deploy.0 = Debug_Local|ARM64");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug_Local|x64.Deploy.0 = Debug_Local|x64");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug_Local|x86.Deploy.0 = Debug_Local|x86");
    }

    [TestMethod]
    public void TypedownXamlUI_UsesOnlyExistingAnyCpuSolutionMappings()
    {
        var solutionSource = File.ReadAllText(Path.Combine(RepoRoot, "Typedown.sln"));
        var projectGuid = FindProjectGuid(solutionSource, "Typedown.XamlUI");

        AssertHasTypeReference(solutionSource, $"{projectGuid}.Debug_Local|ARM64.ActiveCfg = Debug|AnyCPU");
        AssertHasTypeReference(solutionSource, $"{projectGuid}.Debug_Local|x64.ActiveCfg = Debug|AnyCPU");
        AssertHasTypeReference(solutionSource, $"{projectGuid}.Debug_Local|x86.ActiveCfg = Debug|AnyCPU");
        AssertHasTypeReference(solutionSource, $"{projectGuid}.Debug|ARM64.ActiveCfg = Debug|AnyCPU");
        AssertHasTypeReference(solutionSource, $"{projectGuid}.Debug|x64.ActiveCfg = Debug|AnyCPU");
        AssertHasTypeReference(solutionSource, $"{projectGuid}.Debug|x86.ActiveCfg = Debug|AnyCPU");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug|x64.Build.0");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug|x64.Deploy.0");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug_Local|ARM64.ActiveCfg = Debug_Local|Any CPU");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug_Local|x64.ActiveCfg = Debug_Local|Any CPU");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug_Local|x86.ActiveCfg = Debug_Local|Any CPU");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug|ARM64.ActiveCfg = Debug|Any CPU");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug|x64.ActiveCfg = Debug|Any CPU");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug|x86.ActiveCfg = Debug|Any CPU");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug_Local|ARM64.ActiveCfg = Debug_Local|AnyCPU");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug_Local|x64.ActiveCfg = Debug_Local|AnyCPU");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug_Local|x86.ActiveCfg = Debug_Local|AnyCPU");
    }

    [TestMethod]
    public void WinUIPhase10b_ContainsRequiredPlatformServiceImplementations()
    {
        var servicesRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services");

        AssertContainsClass(servicesRoot, "WinUIAppDataPathProvider.cs", "WinUIAppDataPathProvider");
        AssertContainsClass(servicesRoot, "WinUIWindowContext.cs", "WinUIWindowContext");
        AssertContainsClass(servicesRoot, "WinUIUiDispatcher.cs", "WinUIUiDispatcher");
        AssertContainsClass(servicesRoot, "WinUIDialogService.cs", "WinUIDialogService");
        AssertContainsClass(servicesRoot, "WinUIFilePickerService.cs", "WinUIFilePickerService");
        AssertContainsClass(servicesRoot, "WinUIAppActivationService.cs", "WinUIAppActivationService");
        AssertContainsClass(servicesRoot, "WinUIPlatformServices.cs", "WinUIPlatformServices");
    }

    [TestMethod]
    public void WinUIPhase10b_SourceStaysOnWinUI3Boundary()
    {
        var sourceFiles = Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI"), "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI"), "*.xaml", SearchOption.AllDirectories))
            .ToArray();

        Assert.IsTrue(sourceFiles.Length > 0, "Expected WinUI source files.");

        foreach (var file in sourceFiles)
        {
            var source = File.ReadAllText(file);

            AssertNoTypeReference(source, "Windows.UI.Xaml");
            AssertNoTypeReference(source, "Typedown.XamlUI");
        }
    }

    [TestMethod]
    public void WinUIPhase11_AddsEditorHostWithoutLegacyHostDependency()
    {
        var hostPath = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "WinUIEditorHost.cs");
        var controllerPath = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "WinUIEditorHostController.cs");
        var adapterPath = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "WinUIEditorBridgeAdapter.cs");
        var sessionPath = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "WinUIEditorDocumentSession.cs");
        var projectSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));
        var pageSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Views", "MainPage.xaml"));
        var hostSource = File.ReadAllText(hostPath);
        var controllerSource = File.ReadAllText(controllerPath);
        var adapterSource = File.ReadAllText(adapterPath);
        var sessionSource = File.ReadAllText(sessionPath);

        Assert.IsTrue(File.Exists(hostPath), "Expected the Phase 11 WinUI editor host.");
        Assert.IsTrue(File.Exists(controllerPath), "Expected the Phase 11 WinUI editor host controller.");
        Assert.IsTrue(File.Exists(adapterPath), "Expected the Phase 11 WinUI editor bridge adapter.");
        Assert.IsTrue(File.Exists(sessionPath), "Expected the Phase 11 local editor document session.");
        AssertHasTypeReference(hostSource, "WebView2");
        AssertHasTypeReference(hostSource, "WebMessageReceived");
        AssertHasTypeReference(hostSource, "PostWebMessageAsString");
        AssertHasTypeReference(hostSource, "Resources");
        AssertHasTypeReference(hostSource, "Statics");
        AssertHasTypeReference(hostSource, "WinUIHostReady");
        AssertHasTypeReference(hostSource, "LoadFile");
        AssertHasTypeReference(hostSource, "IsContentLoaded");
        AssertHasTypeReference(hostSource, "TrySendPendingLoadFile");
        AssertHasTypeReference(hostSource, "IEditorDocumentSession");
        AssertHasTypeReference(hostSource, "SendLoadFile");
        AssertHasTypeReference(hostSource, "SendThemeChanged");
        AssertHasTypeReference(hostSource, "SendSearchOpenChange");
        AssertHasTypeReference(hostSource, "SendSettingsChanged");
        AssertHasTypeReference(hostSource, "LoadFile(");
        AssertHasTypeReference(hostSource, "Save(");
        AssertHasTypeReference(hostSource, "SaveAs(");
        AssertHasTypeReference(hostSource, "InitialFilePath");
        AssertHasTypeReference(hostSource, "AreDefaultContextMenusEnabled = false");
        AssertHasTypeReference(hostSource, "AreBrowserAcceleratorKeysEnabled = false");
        AssertHasTypeReference(hostSource, "AreDevToolsEnabled = false");
        AssertHasTypeReference(hostSource, "IsBuiltInErrorPageEnabled = false");
        AssertHasTypeReference(hostSource, "IsStatusBarEnabled = false");
        AssertHasTypeReference(hostSource, "IsZoomControlEnabled = false");
        AssertHasTypeReference(hostSource, "DetachCoreWebView");
        AssertHasTypeReference(hostSource, "AttachCoreWebView");
        AssertNoTypeReference(hostSource, "Task.Delay(250)");
        AssertHasTypeReference(projectSource, @"..\Typedown\Resources\Statics\**");
        AssertHasTypeReference(pageSource, "WinUIEditorHost");
        AssertHasTypeReference(sessionSource, "GetCurrentTheme");
        AssertHasTypeReference(sessionSource, "ContentLoaded");
        AssertHasTypeReference(sessionSource, "ExportCallback");
        AssertHasTypeReference(sessionSource, "PrintHTML");
        AssertHasTypeReference(sessionSource, "ResizeTable");
        AssertHasTypeReference(sessionSource, "LoadImage");
        AssertHasTypeReference(sessionSource, "GetStringResources");
        AssertHasTypeReference(sessionSource, "GetSettings");
        AssertHasTypeReference(sessionSource, "LoadFile(");
        AssertHasTypeReference(sessionSource, "ReplaceFileText(");
        AssertHasTypeReference(sessionSource, "Save(");
        AssertHasTypeReference(sessionSource, "SaveAs(");
        AssertHasTypeReference(sessionSource, "File.ReadAllText");
        AssertHasTypeReference(sessionSource, "File.WriteAllText");
        AssertHasTypeReference(sessionSource, "catch (");
        AssertHasTypeReference(sessionSource, "SetClipboard");
        AssertHasTypeReference(sessionSource, "OpenNewWindow");
        AssertHasTypeReference(sessionSource, "UnhandledException");
        AssertHasTypeReference(adapterSource, "FileLoaded");
        AssertHasTypeReference(adapterSource, "MarkdownChange");
        AssertHasTypeReference(adapterSource, "CursorChange");
        AssertHasTypeReference(adapterSource, "StateChange");
        AssertHasTypeReference(adapterSource, "\"diffmsg\"");
        AssertHasTypeReference(adapterSource, "JsonDocument.Parse");
        AssertHasTypeReference(adapterSource, "IEditorDocumentSession");
        AssertHasTypeReference(adapterSource, "HandleEditorEvent");
        AssertHasTypeReference(adapterSource, "HandleRemoteInvoke");
        AssertHasTypeReference(controllerSource, "IEditorHostSink");
        AssertHasTypeReference(controllerSource, "TrySendLoadFile");
        AssertHasTypeReference(controllerSource, "MarkEditorReady");
        AssertHasTypeReference(controllerSource, "LoadFile(");
        AssertHasTypeReference(controllerSource, "SaveAs(");
        AssertNoTypeReference(adapterSource, "D:\\source\\repos\\Typedown");
        AssertHasTypeReference(sessionSource, "IEditorDocumentSession");
        AssertHasTypeReference(sessionSource, "EditorDocumentState");
        AssertHasTypeReference(sessionSource, "EditorSettingsPayload");
        AssertHasTypeReference(sessionSource, "ContentLoaded");
        AssertHasTypeReference(sessionSource, "FileLoaded");
        AssertHasTypeReference(sessionSource, "MarkdownChange");
        AssertNoTypeReference(hostSource, "Typedown.XamlUI");
        AssertNoTypeReference(adapterSource, "Typedown.XamlUI");
        AssertNoTypeReference(sessionSource, "Typedown.XamlUI");
        AssertNoTypeReference(adapterSource, "Typedown.Core.Services");
        AssertNoTypeReference(hostSource, "Typedown.Core.Services");
        AssertNoTypeReference(hostSource, "is WinUIEditorHostSink");
        AssertNoTypeReference(projectSource, @"..\Typedown.Core\Typedown.Core.csproj");
    }

    [TestMethod]
    public void WinUIPhase11_PackagesEditorStaticBundleForMsix()
    {
        var projectSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));

        AssertHasTypeReference(projectSource, @"<Content Include=""..\Typedown\Resources\Statics\**\*""");
        AssertHasTypeReference(projectSource, "CopyToOutputDirectory=\"Always\"");
        AssertHasTypeReference(projectSource, "CopyToPublishDirectory=\"Always\"");
        AssertHasTypeReference(projectSource, "Target Name=\"AddEditorStaticBundleToPackagingOutputs\"");
        AssertHasTypeReference(projectSource, "AfterTargets=\"GetPackagingOutputs\"");
        AssertHasTypeReference(projectSource, "BeforeTargets=\"_ComputeAppxPackagePayload\"");
        AssertHasTypeReference(projectSource, @"<_EditorStaticBundleForPackaging Include=""..\Typedown\Resources\Statics\**\*"" />");
        AssertHasTypeReference(projectSource, "<PackagingOutputs Include=\"@(_EditorStaticBundleForPackaging)\">");
        AssertHasTypeReference(projectSource, @"<TargetPath>Resources\Statics\%(_EditorStaticBundleForPackaging.RecursiveDir)%(_EditorStaticBundleForPackaging.Filename)%(_EditorStaticBundleForPackaging.Extension)</TargetPath>");
        AssertHasTypeReference(projectSource, "<OutputGroup>CustomOutputGroupForPackaging</OutputGroup>");
        AssertNoTypeReference(projectSource, @"<TargetPath>Resources\Statics\</TargetPath>");
    }

    [TestMethod]
    public void PickerContract_PreservesNullCancelSemantics()
    {
        var contractSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "Interfaces", "IFilePickerService.cs"));
        var legacySource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown", "Services", "FilePickerService.cs"));
        var winuiSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services", "WinUIFilePickerService.cs"));

        AssertHasTypeReference(contractSource, "Task<string?> PickOpenFileAsync");
        AssertHasTypeReference(contractSource, "Task<string?> PickSaveFileAsync");
        AssertHasTypeReference(contractSource, "Task<string?> PickFolderAsync");
        AssertNoTypeReference(winuiSource, "return file?.Path ?? string.Empty;");
        AssertNoTypeReference(winuiSource, "return folder?.Path ?? string.Empty;");
        AssertHasTypeReference(winuiSource, "return file?.Path;");
        AssertHasTypeReference(winuiSource, "return folder?.Path;");
        AssertHasTypeReference(legacySource, "return file?.Path;");
        AssertHasTypeReference(legacySource, "return folder?.Path;");
    }

    [TestMethod]
    public void WinUIDialogService_PreservesNullCloseButtonSemantics()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services", "WinUIDialogService.cs"));

        AssertHasTypeReference(source, "CloseButtonText = request.CloseButtonText");
        AssertNoTypeReference(source, "?? \"Close\"");
    }

    [TestMethod]
    public void AppAndActivationService_KeepPhase10bActivationAsStub()
    {
        var appSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "App.xaml.cs"));
        var activationSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services", "WinUIAppActivationService.cs"));
        var platformSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services", "WinUIPlatformServices.cs"));
        var pageSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Views", "MainPage.xaml.cs"));

        AssertContainsInOrder(
            appSource,
            "platformServices ??= new WinUIPlatformServices(window);",
            "uiServices ??= new ServiceCollection()",
            ".AddTypedownUI()",
            ".BuildServiceProvider();",
            "platformServices.WindowContext.ViewRoot = rootFrame;",
            "_ = rootFrame.Navigate(typeof(MainPage), new MainPageNavigationContext(platformServices, uiServices));",
            "platformServices.AppActivationService.StartListening(platformServices.UiDispatcher);",
            "_ = platformServices.AppActivationService.Activate(Environment.GetCommandLineArgs());");

        AssertHasTypeReference(platformSource, "new WinUIAppDataPathProvider()");
        AssertHasTypeReference(platformSource, "new WinUIWindowContext(window)");
        AssertHasTypeReference(platformSource, "new WinUIUiDispatcher(window.DispatcherQueue)");
        AssertHasTypeReference(platformSource, "new WinUIDialogService(WindowContext)");
        AssertHasTypeReference(platformSource, "new WinUIFilePickerService(WindowContext)");
        AssertHasTypeReference(platformSource, "new WinUIAppActivationService(WindowContext)");

        AssertContainsInOrder(
            activationSource,
            "var userArgs = commandLineArgs?.Skip(1).ToArray() ?? Array.Empty<string>();",
            "var kind = ResolveKind(userArgs);",
            "var request = new AppActivationRequest(kind, userArgs);");
        AssertContainsInOrder(
            activationSource,
            "if (commandLineArgs is null || commandLineArgs.Length == 0)",
            "return AppActivationKind.FirstLaunch;",
            "return File.Exists(commandLineArgs[0])",
            "? AppActivationKind.OpenFileRequest",
            ": AppActivationKind.FirstLaunch;");
        AssertHasTypeReference(activationSource, "public void StartListening(IUiDispatcher dispatcher)");
        AssertHasTypeReference(appSource, "private sealed record MainPageNavigationContext");
        AssertHasTypeReference(pageSource, "MainPageViewModel");
    }

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

    [TestMethod]
    public void Phase13TypedownUI_KeepsFrameworkAndShellReferencesOutOfSource()
    {
        var uiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.UI");
        var uiSources = Directory
            .EnumerateFiles(uiRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(File.ReadAllText);

        foreach (var source in uiSources)
        {
            AssertNoTypeReference(source, "using Microsoft.UI.Xaml");
            AssertNoTypeReference(source, "using Windows.UI.Xaml");
            AssertNoTypeReference(source, "global using Microsoft.UI.Xaml");
            AssertNoTypeReference(source, "global using Windows.UI.Xaml");
            AssertNoTypeReference(source, "using Typedown.WinUI");
            AssertNoTypeReference(source, "using Typedown.XamlUI");
            AssertNoTypeReference(source, @"..\Typedown.WinUI\Typedown.WinUI.csproj");
            AssertNoTypeReference(source, @"..\Typedown.XamlUI\Typedown.XamlUI.csproj");
        }
    }

    [TestMethod]
    public void Phase13WinUI_KeepsShellOwnedPlatformSurfaces()
    {
        var winuiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
        var editorHostSource = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "WinUIEditorHost.cs"));
        var platformServicesSource = File.ReadAllText(Path.Combine(winuiRoot, "Services", "WinUIPlatformServices.cs"));
        var packageManifestSource = File.ReadAllText(Path.Combine(winuiRoot, "Package.appxmanifest"));
        var launchSettingsSource = File.ReadAllText(Path.Combine(winuiRoot, "Properties", "launchSettings.json"));

        AssertHasTypeReference(editorHostSource, "WebView2");
        AssertHasTypeReference(editorHostSource, "UserControl");
        AssertHasTypeReference(platformServicesSource, "WinUIPlatformServices");
        AssertHasTypeReference(packageManifestSource, "62082Surprise.Typedown.WinUI");
        AssertHasTypeReference(launchSettingsSource, "Typedown.WinUI (Package)");
        AssertHasTypeReference(launchSettingsSource, "Typedown.WinUI (Unpackaged)");
    }

    [TestMethod]
    public void Phase13TypedownUI_OwnsMainPageStaticTextResources()
    {
        var uiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.UI");
        var resourcePath = Path.Combine(uiRoot, "Resources", "MainPageTextResources.cs");
        var viewModelSource = File.ReadAllText(Path.Combine(uiRoot, "ViewModels", "MainPageViewModel.cs"));

        Assert.IsTrue(File.Exists(resourcePath), "Expected Phase 13 main page text resources to live in Typedown.UI.");

        var resourceSource = File.ReadAllText(resourcePath);
        AssertHasTypeReference(resourceSource, "MainPageTextResources");
        AssertHasTypeReference(resourceSource, "Title");
        AssertHasTypeReference(resourceSource, "Subtitle");
        AssertHasTypeReference(resourceSource, "PendingContractsProbeSummary");
        AssertHasTypeReference(resourceSource, "ValidatedItems");
        AssertHasTypeReference(resourceSource, "DeferredItems");
        AssertHasTypeReference(viewModelSource, "MainPageTextResources");
        AssertNoTypeReference(viewModelSource, "Phase 12 MVVM shell is waiting for WinUI platform service initialization.");
    }

    private static void AssertContainsClass(string root, string fileName, string className)
    {
        var path = Path.Combine(root, fileName);
        Assert.IsTrue(File.Exists(path), $"Expected file {path}.");
        AssertHasTypeReference(File.ReadAllText(path), className);
    }

    private static void AssertContainsInOrder(string source, params string[] snippets)
    {
        var currentIndex = -1;
        foreach (var snippet in snippets)
        {
            var nextIndex = source.IndexOf(snippet, currentIndex + 1, StringComparison.Ordinal);
            Assert.IsTrue(nextIndex >= 0, $"Expected to find snippet: {snippet}");
            Assert.IsTrue(nextIndex > currentIndex, $"Expected snippet to appear after the previous one: {snippet}");
            currentIndex = nextIndex;
        }
    }

    private static void AssertNoTypeReference(string source, string typeName)
    {
        source = NormalizePathSeparators(source);
        Assert.IsFalse(Regex.IsMatch(source, CreateBoundaryPattern(typeName)), $"Unexpected reference to {typeName}.");
    }

    private static void AssertHasTypeReference(string source, string typeName)
    {
        source = NormalizePathSeparators(source);
        Assert.IsTrue(Regex.IsMatch(source, CreateBoundaryPattern(typeName)), $"Expected reference to {typeName}.");
    }

    private static void AssertDebugX64DoesNotBuildOrDeployLegacyProject(string solutionSource, string projectGuid)
    {
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug|x64.Build.0");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug|x64.Deploy.0");
    }

    private static void AssertDebugLocalX64DoesNotBuildOrDeployLegacyProject(string solutionSource, string projectGuid)
    {
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug_Local|x64.Build.0");
        AssertNoTypeReference(solutionSource, $"{projectGuid}.Debug_Local|x64.Deploy.0");
    }

    private static string CreateBoundaryPattern(string typeName)
    {
        return $@"(?<![A-Za-z0-9_]){Regex.Escape(typeName)}(?![A-Za-z0-9_])";
    }

    private static string NormalizePathSeparators(string source)
    {
        return source.Replace("\\\\", "\\");
    }

    private static string FindProjectGuid(string solutionSource, string projectName)
    {
        var match = Regex.Match(
            solutionSource,
            $@"Project\(\""\{{[^}}]+\}}\""\)\s*=\s*\""{Regex.Escape(projectName)}\"".*?,\s*\""\{{(?<guid>[^}}]+)\}}\""",
            RegexOptions.Multiline);

        Assert.IsTrue(match.Success, $"Could not find project guid for {projectName}.");
        return $"{{{match.Groups["guid"].Value}}}";
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Typedown.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Typedown.sln from the architecture test output directory.");
    }
}
