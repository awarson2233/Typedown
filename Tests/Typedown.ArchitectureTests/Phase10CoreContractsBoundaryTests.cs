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
        var legacyPackageGuid = FindProjectGuid(solutionSource, "Typedown.Package");
        var xamlDesignGuid = FindProjectGuid(solutionSource, "XamlDesignApp");
        var xamlUiGuid = FindProjectGuid(solutionSource, "Typedown.XamlUI");

        AssertHasTypeReference(solutionSource, $"{contractsProjectGuid}.Debug|x64.Build.0");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Debug|x64.Build.0");
        AssertHasTypeReference(solutionSource, $"{winuiProjectGuid}.Debug|x64.Deploy.0");
        AssertNoTypeReference(solutionSource, $"{legacyPackageGuid}.Debug|x64.Build.0");
        AssertNoTypeReference(solutionSource, $"{legacyPackageGuid}.Debug|x64.Deploy.0");
        AssertNoTypeReference(solutionSource, $"{xamlDesignGuid}.Debug|x64.Build.0");
        AssertNoTypeReference(solutionSource, $"{xamlDesignGuid}.Debug|x64.Deploy.0");
        AssertNoTypeReference(solutionSource, $"{xamlUiGuid}.Debug|x64.Build.0");
        AssertNoTypeReference(solutionSource, $"{xamlUiGuid}.Debug|x64.Deploy.0");
    }

    [TestMethod]
    public void WinUIPackagedBaseline_UsesRepositoryDevCertificate()
    {
        var projectSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));
        var launchSettingsSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Properties", "launchSettings.json"));
        var manifestSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Package.appxmanifest"));
        var scriptSource = File.ReadAllText(Path.Combine(RepoRoot, "scripts", "install-winui-dev-certificate.ps1"));

        AssertHasTypeReference(projectSource, "<WindowsPackageType>MSIX</WindowsPackageType>");
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
            "platformServices.WindowContext.ViewRoot = rootFrame;",
            "_ = rootFrame.Navigate(typeof(MainPage), platformServices);",
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

        AssertHasTypeReference(pageSource, "activation stub contract surface");
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
