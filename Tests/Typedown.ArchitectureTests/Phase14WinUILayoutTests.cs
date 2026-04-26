using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase14WinUILayoutTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void MainPageXaml_DefinesPhase14VisualShellRegions()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Views", "MainPage.xaml"));

        AssertContains(source, "x:Name=\"AppDocumentChrome\"");
        AssertContains(source, "x:Name=\"PrimaryToolbar\"");
        AssertContains(source, "x:Name=\"EditorShell\"");
        AssertContains(source, "x:Name=\"MigrationStatusPanel\"");
        AssertContains(source, "x:Name=\"BottomStatusBar\"");
        AssertContains(source, "<controls:WinUIEditorHost");
        AssertContains(source, "Text=\"Phase 14 migration status\"");
        AssertContains(source, "Text=\"Editor host\"");
    }

    private static void AssertContains(string source, string snippet)
    {
        StringAssert.Contains(source, snippet);
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
