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

    private static void AssertContainsClass(string root, string fileName, string className)
    {
        var path = Path.Combine(root, fileName);
        Assert.IsTrue(File.Exists(path), $"Expected file {path}.");
        AssertHasTypeReference(File.ReadAllText(path), className);
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
