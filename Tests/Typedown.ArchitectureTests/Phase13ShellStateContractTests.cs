using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase13ShellStateContractTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void PresentationViewModels_MatchCheckpointApplicationSurface()
    {
        var viewModelRoot = Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "ViewModels");
        var viewModelFiles = Directory
            .EnumerateFiles(viewModelRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEquivalent(
            new[]
            {
                "AppViewModel.cs",
                "EditorViewModel.cs",
                "FileViewModel.cs",
                "FloatViewModel.cs",
                "FormatViewModel.cs",
                "ParagraphViewModel.cs",
                "SettingsViewModel.cs",
                "SettingsViewModel.Shortcut.cs",
                "UIViewModel.cs",
            },
            viewModelFiles);
    }

    [TestMethod]
    public void PresentationApplicationViewModels_StayPlatformNeutral()
    {
        var sourceFiles = Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "ViewModels"), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(File.ReadAllText);

        foreach (var source in sourceFiles)
        {
            AssertNoReference(source, "Microsoft.UI.Xaml");
            AssertNoReference(source, "Windows.UI.Xaml");
            AssertNoReference(source, "Typedown.WinUI");
            AssertNoReference(source, "Typedown.XamlUI");
        }
    }

    private static void AssertNoReference(string source, string text)
    {
        Assert.IsFalse(source.Contains(text, StringComparison.Ordinal), $"Unexpected reference: {text}");
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
