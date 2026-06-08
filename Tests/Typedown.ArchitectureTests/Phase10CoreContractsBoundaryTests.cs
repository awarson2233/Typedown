using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase10CoreContractsBoundaryTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string[] PresentationOwnedPorts =
    [
        "IClipboard.cs",
        "IFileExport.cs",
        "IFileOperation.cs",
        "IFloatViewService.cs",
        "IKeyboardAccelerator.cs",
        "ITableDialogService.cs",
        "IWindowService.cs"
    ];

    [TestMethod]
    public void ContractsProject_ExistsAndTargetsPlatformNeutralNet10()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Typedown.Core.csproj"));

        AssertHasTypeReference(source, "<TargetFramework>net10.0</TargetFramework>");
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
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Interfaces", "IAppDataPathProvider.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Interfaces", "IEditorBridge.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Interfaces", "IFileConverter.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Interfaces", "IPowerShellService.cs")));

        var presentationInterfacesRoot = Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "Interfaces");
        Assert.IsTrue(File.Exists(Path.Combine(presentationInterfacesRoot, "IWindowContext.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationInterfacesRoot, "IUiDispatcher.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationInterfacesRoot, "IDialogService.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationInterfacesRoot, "IFilePickerService.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationInterfacesRoot, "IAppActivationService.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationInterfacesRoot, "IEditorCommandSink.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationInterfacesRoot, "IEditorSettingsNotifier.cs")));
        foreach (var fileName in PresentationOwnedPorts)
        {
            Assert.IsTrue(File.Exists(Path.Combine(presentationInterfacesRoot, fileName)), $"{fileName} must be owned by Typedown.Presentation.");
        }

        Assert.IsTrue(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "Hosting", "EditorHostContracts.cs")));

        Assert.IsFalse(Directory.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Editor")));
        Assert.IsFalse(Directory.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Contracts")));
    }

    [TestMethod]
    public void Core_DoesNotOwnPresentationViewModelPorts()
    {
        var coreInterfacesRoot = Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Interfaces");

        foreach (var fileName in PresentationOwnedPorts)
        {
            Assert.IsFalse(File.Exists(Path.Combine(coreInterfacesRoot, fileName)), $"{fileName} belongs in Typedown.Presentation.");
        }
    }

    [TestMethod]
    public void EditorContracts_StayPlatformNeutralAndAvoidLegacyDependencies()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "Hosting", "EditorHostContracts.cs"));

        AssertHasTypeReference(source, "IEditorDocumentSession");
        AssertHasTypeReference(source, "IEditorHostSink");
        AssertHasTypeReference(source, "EditorHostMessage");
        AssertHasTypeReference(source, "EditorHostCommands");
        AssertHasTypeReference(source, "EditorSettingsPayload");
        AssertHasTypeReference(source, "EditorPersistenceResult");
        AssertNoTypeReference(source, "Typedown.Core.Contracts");
        AssertNoTypeReference(source, "Typedown.Presentation.ViewModels");
        AssertNoTypeReference(source, "Microsoft.UI.Xaml");
        AssertNoTypeReference(source, "Windows.UI.Xaml");
        AssertNoTypeReference(source, "Microsoft.Web.WebView2");
        AssertNoTypeReference(source, "Typedown.XamlUI");
    }

    [TestMethod]
    public void PureCoreBoundary_SourceTreeAvoidsUiWinRtWebViewAndLegacyDependencies()
    {
        var coreFiles = Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "Dev", "Typedown.Core"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.IsTrue(coreFiles.Length > 0, "Expected C# source files under Dev/Typedown.Core.");

        foreach (var file in coreFiles)
        {
            var source = File.ReadAllText(file);

            AssertNoTypeReference(source, "Microsoft.UI.Xaml");
            AssertNoTypeReference(source, "Windows.UI.Xaml");
            AssertNoTypeReference(source, "Microsoft.Web.WebView2");
            AssertNoTypeReference(source, "Windows.Storage.Pickers");
            AssertNoTypeReference(source, "Typedown.Core.Legacy");
        }
    }

    [TestMethod]
    public void CorePresentationAndShellProjects_FollowCurrentDependencyBoundary()
    {
        var coreProject = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Typedown.Core.csproj"));
        var presentationProject = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "Typedown.Presentation.csproj"));
        var winuiProject = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));

        AssertNoTypeReference(coreProject, "<ProjectReference");
        AssertHasTypeReference(presentationProject, @"..\Typedown.Core\Typedown.Core.csproj");
        AssertNoTypeReference(presentationProject, @"..\Typedown.WinUI\Typedown.WinUI.csproj");
        AssertNoTypeReference(presentationProject, @"..\Typedown.XamlUI\Typedown.XamlUI.csproj");
        AssertNoTypeReference(presentationProject, @"..\Typedown.UI\Typedown.UI.csproj");

        AssertHasTypeReference(winuiProject, @"..\Typedown.Core\Typedown.Core.csproj");
        AssertHasTypeReference(winuiProject, @"..\Typedown.Presentation\Typedown.Presentation.csproj");
        AssertNoTypeReference(winuiProject, @"..\Typedown.UI\Typedown.UI.csproj");
        AssertNoTypeReference(winuiProject, @"..\Typedown.Core.Legacy\Typedown.Core.Legacy.csproj");
        AssertNoTypeReference(winuiProject, @"..\Typedown.XamlUI\Typedown.XamlUI.csproj");
    }

    [TestMethod]
    public void WinUISpike_ReferencesNewCoreButNotLegacyXamlHost()
    {
        var projectSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));

        AssertHasTypeReference(projectSource, @"..\Typedown.Core\Typedown.Core.csproj");
        AssertHasTypeReference(projectSource, "<Platforms>x64;ARM64</Platforms>");
        AssertHasTypeReference(projectSource, "win-x64;win-arm64");
        AssertHasTypeReference(projectSource, "<RuntimeIdentifier>win-arm64</RuntimeIdentifier>");
        AssertNoTypeReference(projectSource, @"..\Typedown.Core.Legacy\Typedown.Core.Legacy.csproj");
        AssertNoTypeReference(projectSource, @"..\Typedown.XamlUI\Typedown.XamlUI.csproj");
        AssertNoTypeReference(projectSource, "Typedown.XamlUI");
    }

    [TestMethod]
    public void WinUIPackagedBaseline_UsesSolutionDebugX64BuildAndDeployWithoutLegacyProjects()
    {
        var solutionSource = File.ReadAllText(Path.Combine(RepoRoot, "Typedown.sln"));
        var winuiProjectGuid = FindProjectGuid(solutionSource, "Typedown.WinUI");
        var coreProjectGuid = FindProjectGuid(solutionSource, "Typedown.Core");
        var presentationProjectGuid = FindProjectGuid(solutionSource, "Typedown.Presentation");
        var editorGuid = FindProjectGuid(solutionSource, "Typedown.Editor");
        var xamlDesignGuid = FindProjectGuid(solutionSource, "XamlDesignApp");

        AssertHasTypeReference(solutionSource, $"{coreProjectGuid}.Debug|x64.Build.0");
        AssertHasTypeReference(solutionSource, $"{presentationProjectGuid}.Debug|x64.Build.0");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Debug|x64.Build.0");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Debug|x64.Deploy.0");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Debug|ARM64.ActiveCfg = Debug|ARM64");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Debug|ARM64.Build.0 = Debug|ARM64");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Debug|ARM64.Deploy.0 = Debug|ARM64");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Release|ARM64.ActiveCfg = Release|ARM64");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Release|ARM64.Build.0 = Release|ARM64");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Release|ARM64.Deploy.0 = Release|ARM64");
        AssertDebugX64DoesNotBuildOrDeployLegacyProject(solutionSource, xamlDesignGuid);

        AssertHasTypeReference(solutionSource, $"{coreProjectGuid}.Debug_Local|x64.Build.0");
        AssertHasTypeReference(solutionSource, $"{presentationProjectGuid}.Debug_Local|x64.Build.0");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Debug_Local|x64.Build.0");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Debug_Local|ARM64.ActiveCfg = Debug_Local|ARM64");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Debug_Local|ARM64.Build.0 = Debug_Local|ARM64");
        AssertNoTypeReference(solutionSource, $"{winuiProjectGuid}.Debug_Local|ARM64.Deploy.0");
        AssertNoTypeReference(solutionSource, $"{winuiProjectGuid}.Debug|ARM64.ActiveCfg = Debug|x64");
        AssertNoTypeReference(solutionSource, $"{winuiProjectGuid}.Debug_Local|ARM64.ActiveCfg = Debug_Local|x64");
        AssertNoTypeReference(solutionSource, $"{winuiProjectGuid}.Release|ARM64.ActiveCfg = Release|x64");
        AssertNoTypeReference(solutionSource, $"{winuiProjectGuid}.Debug_Local|x64.Deploy.0");
        AssertNoTypeReference(solutionSource, $"{winuiProjectGuid}.Debug_Local|x86.Deploy.0");
        AssertDebugLocalX64DoesNotBuildOrDeployLegacyProject(solutionSource, editorGuid);
        AssertDebugLocalX64DoesNotBuildOrDeployLegacyProject(solutionSource, xamlDesignGuid);

        foreach (var removedProjectName in new[] { "Typedown", "Typedown.Package", "Typedown.Test", "Typedown.Core.Test", "Typedown.UITest", "Typedown.XamlUI" })
        {
            AssertNoTypeReference(solutionSource, $"= \"{removedProjectName}\",");
        }
    }

    [TestMethod]
    public void RemovedLegacyCoreAndTypedownUiProjects_DoNotExistInSourceOrSolution()
    {
        var solutionSource = File.ReadAllText(Path.Combine(RepoRoot, "Typedown.sln"));
        var databaseMigrationProjectSource = File.ReadAllText(Path.Combine(RepoRoot, "Tools", "DatabaseMigration", "DatabaseMigration.csproj"));
        var xamlDesignProjectSource = File.ReadAllText(Path.Combine(RepoRoot, "Tools", "XamlDesignApp", "XamlDesignApp.csproj"));

        Assert.IsFalse(Directory.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Legacy")));
        Assert.IsFalse(Directory.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.UI")));
        Assert.IsFalse(Directory.Exists(Path.Combine(RepoRoot, "Dev", "Typedown")));
        Assert.IsFalse(Directory.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.XamlUI")));
        Assert.IsFalse(Directory.Exists(Path.Combine(RepoRoot, "Tests", "Typedown.Test")));
        Assert.IsFalse(Directory.Exists(Path.Combine(RepoRoot, "Tests", "Typedown.UITest")));
        Assert.IsFalse(Directory.Exists(Path.Combine(RepoRoot, "Tests", "Typedown.Universal.Test")));
        Assert.IsFalse(Directory.Exists(Path.Combine(RepoRoot, "Tools", "Typedown.Package")));
        AssertNoTypeReference(solutionSource, "Typedown.Core.Legacy");
        AssertNoTypeReference(solutionSource, @"Dev\Typedown.Core.Legacy\Typedown.Core.Legacy.csproj");
        AssertNoTypeReference(solutionSource, "Typedown.UI");
        AssertNoTypeReference(solutionSource, @"Dev\Typedown.UI\Typedown.UI.csproj");
        AssertNoTypeReference(solutionSource, @"Dev\Typedown\Typedown.csproj");
        AssertNoTypeReference(solutionSource, @"Dev\Typedown.XamlUI\Typedown.XamlUI.csproj");
        AssertNoTypeReference(solutionSource, @"Tests\Typedown.Test\Typedown.Test.csproj");
        AssertNoTypeReference(solutionSource, @"Tests\Typedown.UITest\Typedown.UITest.csproj");
        AssertNoTypeReference(solutionSource, @"Tests\Typedown.Universal.Test\Typedown.Core.Test.csproj");
        AssertNoTypeReference(solutionSource, @"Tools\Typedown.Package\Typedown.Package.wapproj");
        AssertNoTypeReference(databaseMigrationProjectSource, @"..\..\Dev\Typedown.Core.Legacy\Typedown.Core.Legacy.csproj");
        AssertNoTypeReference(xamlDesignProjectSource, @"..\..\Dev\Typedown.Core.Legacy\Typedown.Core.Legacy.csproj");
        AssertHasTypeReference(databaseMigrationProjectSource, @"..\..\Dev\Typedown.Core\Typedown.Core.csproj");
    }

    [TestMethod]
    public void WinUIPackagedBaseline_UsesConfiguredManualDevCertificate()
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
        Assert.IsFalse(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.DevTest.pfx")));
        Assert.IsFalse(File.Exists(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.DevTest.cer")));
        AssertHasTypeReference(scriptSource, "Typedown.WinUI.DevTest.pfx");
        AssertHasTypeReference(scriptSource, "Typedown.WinUI.DevTest.cer");
        AssertHasTypeReference(scriptSource, "WinUI dev certificate not found");
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
    public void WinUIAppDataPathProvider_AvoidsApplicationDataWhenUnpackaged()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services", "WinUIAppDataPathProvider.cs"));
        var packageIdentityProbe = source.IndexOf("HasPackageIdentity", StringComparison.Ordinal);
        var applicationDataAccess = source.IndexOf("ApplicationData.Current.LocalFolder.Path", StringComparison.Ordinal);

        Assert.IsTrue(packageIdentityProbe >= 0, "WinUI app-data paths must probe package identity before using ApplicationData.Current.");
        Assert.IsTrue(applicationDataAccess >= 0, "Packaged WinUI app-data paths should still use ApplicationData.Current.LocalFolder.");
        Assert.IsTrue(packageIdentityProbe < applicationDataAccess, "Unpackaged Debug_Local must avoid throwing WinRT 0x80073D54 before falling back.");
        AssertHasTypeReference(source, "GetCurrentPackageFullName");
        AssertHasTypeReference(source, "ERROR_INSUFFICIENT_BUFFER");
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
    public void RemovedLegacyXamlHost_DoesNotRemainInSolutionOrWinUIBoundary()
    {
        var solutionSource = File.ReadAllText(Path.Combine(RepoRoot, "Typedown.sln"));
        var winuiProject = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));

        AssertNoTypeReference(solutionSource, "\"Typedown.XamlUI\"");
        AssertNoTypeReference(winuiProject, "Typedown.XamlUI");
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
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}LegacyCopied{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
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
        var hostingRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "Hosting");
        var hostPath = Path.Combine(hostingRoot, "WinUIEditorHost.cs");
        var controllerPath = Path.Combine(hostingRoot, "WinUIEditorHostController.cs");
        var adapterPath = Path.Combine(hostingRoot, "WinUIEditorBridgeAdapter.cs");
        var sessionPath = Path.Combine(hostingRoot, "WinUIEditorDocumentSession.cs");
        var editorContainerPath = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "EditorContainer.xaml.cs");
        var projectSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));
        var editorContainerSource = File.ReadAllText(editorContainerPath);
        var hostSource = File.ReadAllText(hostPath);
        var controllerSource = File.ReadAllText(controllerPath);
        var adapterSource = File.ReadAllText(adapterPath);
        var sessionSource = File.ReadAllText(sessionPath);

        Assert.IsTrue(File.Exists(hostPath), "Expected the Phase 11 WinUI editor host.");
        Assert.IsTrue(File.Exists(controllerPath), "Expected the Phase 11 WinUI editor host controller.");
        Assert.IsTrue(File.Exists(adapterPath), "Expected the Phase 11 WinUI editor bridge adapter.");
        Assert.IsTrue(File.Exists(sessionPath), "Expected the Phase 11 local editor document session.");
        Assert.IsTrue(File.Exists(editorContainerPath), "Expected the copied visual editor container to host the Phase 11 WinUI editor host.");
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
        AssertHasTypeReference(projectSource, @"Resources\Statics\**");
        AssertHasTypeReference(editorContainerSource, "WinUIEditorHost");
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
        AssertHasTypeReference(projectSource, @"..\Typedown.Core\Typedown.Core.csproj");
        AssertNoTypeReference(projectSource, @"..\Typedown.Core.Legacy\Typedown.Core.Legacy.csproj");
    }

    [TestMethod]
    public void WinUIPhase11_PackagesEditorStaticBundleForMsix()
    {
        var projectSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));

        AssertHasTypeReference(projectSource, @"<Content Include=""Resources\Statics\**\*""");
        AssertHasTypeReference(projectSource, "CopyToOutputDirectory=\"Always\"");
        AssertHasTypeReference(projectSource, "CopyToPublishDirectory=\"Always\"");
        AssertHasTypeReference(projectSource, "Target Name=\"BuildTypedownEditorStaticBundle\"");
        AssertHasTypeReference(projectSource, "TYPEDOWN_EDITOR_BUILD_OUTPUT=$(MSBuildProjectDirectory)\\$(TypedownEditorStaticBundleStagingPath)");
        AssertHasTypeReference(projectSource, "Typedown.Editor static bundle was not generated.");
        AssertHasTypeReference(projectSource, @"<Content Include=""$(TypedownEditorStaticBundleStagingPath)\**\*"" CopyToOutputDirectory=""Always"" CopyToPublishDirectory=""Always"" />");
        AssertNoTypeReference(projectSource, "Target Name=\"BuildTypedownEditorStaticBundle\" BeforeTargets=");
        AssertHasTypeReference(projectSource, "Target Name=\"AddEditorStaticBundleToPackagingOutputs\"");
        AssertHasTypeReference(projectSource, "AfterTargets=\"GetPackagingOutputs\"");
        AssertHasTypeReference(projectSource, "BeforeTargets=\"_ComputeAppxPackagePayload\"");
        AssertHasTypeReference(projectSource, @"<_EditorStaticBundleForPackaging Include=""Resources\Statics\**\*"" />");
        AssertHasTypeReference(projectSource, "<PackagingOutputs Include=\"@(_EditorStaticBundleForPackaging)\">");
        AssertHasTypeReference(projectSource, @"<TargetPath>Resources\Statics\%(_EditorStaticBundleForPackaging.RecursiveDir)%(_EditorStaticBundleForPackaging.Filename)%(_EditorStaticBundleForPackaging.Extension)</TargetPath>");
        AssertHasTypeReference(projectSource, "<OutputGroup>CustomOutputGroupForPackaging</OutputGroup>");
        AssertNoTypeReference(projectSource, @"<TargetPath>Resources\Statics\</TargetPath>");
    }

    [TestMethod]
    public void WinUIEditorMenus_CompileXamlWhileLegacyCodeBehindStaysOutOfBuild()
    {
        var projectSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));
        var editorContainerSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "EditorContainer.xaml"));
        var editorContainerCodeBehindSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "EditorContainer.xaml.cs"));
        var editorHostSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "Hosting", "WinUIEditorHost.cs"));
        var menuBarSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "MenuBar.xaml"));
        var menuStubSource = string.Join(
            "\n",
            File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "MenuBarItems", "MenuBarItemBase.cs")),
            File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "MenuBarItems", "FileItem.xaml.cs")));
        var contextStubSource = string.Join(
            "\n",
            File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "ContextMenuItems", "CodeFencesItem.xaml.cs")),
            File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "ContextMenuItems", "FormatItem.xaml.cs")));
        var imageItemSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "EditorControls", "ContextMenuItems", "ImageItem.xaml.cs"));

        AssertNoTypeReference(projectSource, @"<Page Remove=""Controls\EditorControls\MenuBarItems\*.xaml""");
        AssertNoTypeReference(projectSource, @"<Page Remove=""Controls\EditorControls\ContextMenuItems\*.xaml""");
        AssertNoTypeReference(projectSource, "LegacyCopied");
        AssertNoTypeReference(projectSource, @"<Compile Remove=""Controls\EditorControls\MenuBarItems\*.xaml.cs""");
        AssertNoTypeReference(projectSource, @"<Compile Remove=""Controls\EditorControls\ContextMenuItems\*.xaml.cs""");
        AssertNoTypeReference(projectSource, @"<Compile Remove=""Controls\EditorControls\MenuBarItems\MenuBarItemBase.cs""");
        AssertNoTypeReference(projectSource, @"..\Typedown.Core.Legacy\Typedown.Core.Legacy.csproj");

        AssertHasTypeReference(menuBarSource, "<items:FileItem");
        AssertHasTypeReference(menuBarSource, "<items:EditItem");
        AssertHasTypeReference(menuBarSource, "<items:ParagraphItem");
        AssertHasTypeReference(menuBarSource, "<items:FormatItem");
        AssertHasTypeReference(menuBarSource, "<items:ViewItem");

        AssertHasTypeReference(menuStubSource, "partial class FileItem");
        AssertHasTypeReference(menuStubSource, "InitializeComponent()");
        AssertNoTypeReference(contextStubSource, "partial class ImageItem");
        AssertHasTypeReference(imageItemSource, "partial class ImageItem");
        AssertHasTypeReference(imageItemSource, "InitializeComponent()");
        AssertHasTypeReference(editorContainerCodeBehindSource, "EnsureEditorContextFlyout");
        AssertHasTypeReference(editorContainerCodeBehindSource, "editorContextFlyout = new MenuFlyout()");
        AssertHasTypeReference(editorContainerCodeBehindSource, "editorContextFlyout.Opening += OnFlyoutOpening");
        AssertHasTypeReference(editorContainerCodeBehindSource, "editorContextFlyout.Items.Add(menuFormatItem)");
        AssertHasTypeReference(editorContainerCodeBehindSource, "editorContextFlyout.Items.Add(menuImageItem)");
        AssertHasTypeReference(editorContainerCodeBehindSource, "EnsureEditorContextFlyout().ShowAt(MarkdownEditorPresenter");
        AssertHasTypeReference(editorHostSource, "CoreWebView2.ContextMenuRequested += OnContextMenuRequested");
        AssertHasTypeReference(editorHostSource, "e.Handled = true");
    }

    [TestMethod]
    public void PickerContract_PreservesNullCancelSemantics()
    {
        var contractSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "Interfaces", "IFilePickerService.cs"));
        var winuiSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services", "WinUIFilePickerService.cs"));

        AssertHasTypeReference(contractSource, "Task<string?> PickOpenFileAsync");
        AssertHasTypeReference(contractSource, "Task<string?> PickSaveFileAsync");
        AssertHasTypeReference(contractSource, "Task<string?> PickFolderAsync");
        AssertNoTypeReference(winuiSource, "return file?.Path ?? string.Empty;");
        AssertNoTypeReference(winuiSource, "return folder?.Path ?? string.Empty;");
        AssertHasTypeReference(winuiSource, "return file?.Path;");
        AssertHasTypeReference(winuiSource, "return folder?.Path;");
    }

    [TestMethod]
    public void WinUIDialogService_PreservesNullCloseButtonSemantics()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services", "WinUIDialogService.cs"));

        AssertHasTypeReference(source, "CloseButtonText = request.CloseButtonText");
        AssertNoTypeReference(source, "?? \"Close\"");
    }

    [TestMethod]
    public void AppAndActivationService_KeepStartupActivationPipelineAndCommandLineOpenNewWindowFlow()
    {
        var appSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "App.xaml.cs"));
        var activationSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services", "WinUIAppActivationService.cs"));
        var platformSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services", "WinUIPlatformServices.cs"));
        var pageSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Views", "MainPage.xaml.cs"));
        var rootSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "RootControl.xaml.cs"));

        AssertContainsInOrder(
            appSource,
            "var startupCommandLineArgs = ResolveStartupCommandLineArgs();",
            "platformServices ??= new WinUIPlatformServices(window);",
            "if (rootServices is null)",
            "rootServices = new ServiceCollection()",
            ".AddSingleton(platformServices.WindowContext)",
            ".AddSingleton(platformServices.AppDataPathProvider)",
            ".AddTypedownPresentation()",
            ".BuildServiceProvider();",
            "if (uiScope is null)",
            "uiScope = rootServices.CreateScope();",
            "uiServices = uiScope.ServiceProvider;",
            "uiServices.GetRequiredService<AppViewModel>().CommandLineArgs = startupCommandLineArgs;",
            "rootControl.MainPageNavigationParameter = new MainPageNavigationContext(platformServices, uiServices",
            "platformServices.WindowContext.ViewRoot = rootControl;",
            "platformServices.AppActivationService.StartListening(platformServices.UiDispatcher);",
            "_ = platformServices.AppActivationService.Activate(startupCommandLineArgs);");

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
        AssertHasTypeReference(appSource, "private static string[] ResolveStartupCommandLineArgs()");
        AssertHasTypeReference(appSource, "AppInstance.GetCurrent().GetActivatedEventArgs()");
        AssertHasTypeReference(appSource, "ExtendedActivationKind.File");
        AssertHasTypeReference(appSource, "IFileActivatedEventArgs");
        AssertHasTypeReference(appSource, "NewWindowCommand.OnExecute.Subscribe");
        AssertHasTypeReference(appSource, "ProcessStartInfo");
        AssertHasTypeReference(appSource, "Environment.ProcessPath");
        AssertHasTypeReference(appSource, "UseShellExecute = true");
        AssertHasTypeReference(appSource, "private sealed record MainPageNavigationContext");
        AssertHasTypeReference(appSource, "RootControl");
        AssertHasTypeReference(rootSource, "Frame.Navigate(typeof(Views.MainPage), MainPageNavigationParameter)");
        AssertHasTypeReference(pageSource, "AppViewModel");
        AssertHasTypeReference(pageSource, "DataContext = ViewModel");
        AssertNoTypeReference(pageSource, "MainPageViewModel");
    }

    [TestMethod]
    public void Phase12TypedownPresentation_ProvidesMvvmBoundaryWithoutWinUIShellDependency()
    {
        var presentationProjectPath = Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "Typedown.Presentation.csproj");
        var winuiProjectSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));
        var solutionSource = File.ReadAllText(Path.Combine(RepoRoot, "Typedown.sln"));

        Assert.IsTrue(File.Exists(presentationProjectPath), "Expected Typedown.Presentation project.");

        var presentationProjectSource = File.ReadAllText(presentationProjectPath);
        AssertHasTypeReference(solutionSource, "Typedown.Presentation");
        AssertHasTypeReference(presentationProjectSource, "<TargetFramework>net10.0</TargetFramework>");
        AssertHasTypeReference(presentationProjectSource, @"..\Typedown.Core\Typedown.Core.csproj");
        AssertNoTypeReference(presentationProjectSource, @"..\Typedown.WinUI\Typedown.WinUI.csproj");
        AssertNoTypeReference(presentationProjectSource, @"..\Typedown.XamlUI\Typedown.XamlUI.csproj");
        AssertNoTypeReference(presentationProjectSource, "Microsoft.UI.Xaml");
        AssertNoTypeReference(presentationProjectSource, "Windows.UI.Xaml");
        AssertHasTypeReference(winuiProjectSource, @"..\Typedown.Presentation\Typedown.Presentation.csproj");
        AssertNoTypeReference(winuiProjectSource, @"..\Typedown.UI\Typedown.UI.csproj");
    }

    [TestMethod]
    public void Phase12TypedownPresentation_OwnsCheckpointApplicationMvvmRegistration()
    {
        var presentationRoot = Path.Combine(RepoRoot, "Dev", "Typedown.Presentation");
        var mainPageSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Views", "MainPage.xaml.cs"));
        var appSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "App.xaml.cs"));

        Assert.IsTrue(File.Exists(Path.Combine(presentationRoot, "ViewModels", "AppViewModel.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationRoot, "ViewModels", "EditorViewModel.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationRoot, "ViewModels", "FileViewModel.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationRoot, "ViewModels", "FloatViewModel.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationRoot, "ViewModels", "FormatViewModel.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationRoot, "ViewModels", "ParagraphViewModel.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationRoot, "ViewModels", "SettingsViewModel.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationRoot, "ViewModels", "SettingsViewModel.Shortcut.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationRoot, "ViewModels", "UIViewModel.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(presentationRoot, "PresentationServiceCollectionExtensions.cs")));

        var compositionSource = File.ReadAllText(Path.Combine(presentationRoot, "PresentationServiceCollectionExtensions.cs"));
        AssertHasTypeReference(compositionSource, "AddTypedownPresentation");
        AssertHasTypeReference(compositionSource, "AddScoped<AppViewModel>");
        AssertHasTypeReference(compositionSource, "AddScoped<SettingsViewModel>");
        AssertNoTypeReference(compositionSource, "AddScoped<MainPageViewModel>");
        AssertNoTypeReference(compositionSource, "AddScoped<ShellViewModel>");
        AssertNoTypeReference(compositionSource, "AddScoped<EditorRuntimeViewModel>");
        AssertHasTypeReference(appSource, "AddTypedownPresentation()");
        AssertNoTypeReference(appSource, "AddTypedownUI()");
        AssertHasTypeReference(mainPageSource, "AppViewModel");
        AssertHasTypeReference(mainPageSource, "DataContext = ViewModel");
        AssertNoTypeReference(mainPageSource, "MainPageViewModel");
        AssertNoTypeReference(mainPageSource, "ApplyPlatformServiceSummary");
    }

    [TestMethod]
    public void Phase13TypedownPresentation_KeepsFrameworkAndShellReferencesOutOfSource()
    {
        var presentationRoot = Path.Combine(RepoRoot, "Dev", "Typedown.Presentation");
        var presentationSources = Directory
            .EnumerateFiles(presentationRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(File.ReadAllText);

        foreach (var source in presentationSources)
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
        var editorHostSource = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "Hosting", "WinUIEditorHost.cs"));
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
    public void Phase13TypedownPresentation_DoesNotRetainTypedownUiShellViewModels()
    {
        var presentationRoot = Path.Combine(RepoRoot, "Dev", "Typedown.Presentation");

        Assert.IsFalse(File.Exists(Path.Combine(presentationRoot, "ViewModels", "MainPageViewModel.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(presentationRoot, "ViewModels", "MigrationBoundaryItem.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(presentationRoot, "ViewModels", "ShellViewModel.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(presentationRoot, "ViewModels", "EditorRuntimeViewModel.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(presentationRoot, "ViewModels", "EditorTocNodeViewModel.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(presentationRoot, "Resources", "MainPageTextResources.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(presentationRoot, "Resources", "ShellTextResources.cs")));
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
