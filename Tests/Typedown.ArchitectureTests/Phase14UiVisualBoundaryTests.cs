using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase14UiVisualBoundaryTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void TargetArchitecture_DocumentsCurrentCorePresentationWinUIBoundary()
    {
        var planPath = Path.Combine(RepoRoot, "docs", "native-migration-target-architecture.md");
        Assert.IsTrue(File.Exists(planPath), "Expected the native migration architecture document.");

        var source = File.ReadAllText(planPath);

        StringAssert.Contains(source, "Typedown.Core");
        StringAssert.Contains(source, "Typedown.Presentation");
        StringAssert.Contains(source, "Typedown.WinUI");
        StringAssert.Contains(source, "IEditorSurface");
    }

    [TestMethod]
    public void Roadmap_StatesCurrentGovernanceBeforeCutoverAndArm64()
    {
        var targetArchPath = Path.Combine(RepoRoot, "docs", "native-migration-target-architecture.md");
        var source = File.ReadAllText(targetArchPath);

        StringAssert.Contains(source, "Track A");
        StringAssert.Contains(source, "Track B");
        StringAssert.Contains(source, "Track C");
        StringAssert.Contains(source, "ARM64 MSBuild");
    }

    [TestMethod]
    public void BuildBaseline_StatesCurrentArchitectureAndPackageCertificateBoundary()
    {
        var coreCsproj = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Typedown.Core.csproj"));
        var presentationCsproj = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "Typedown.Presentation.csproj"));
        var winuiCsproj = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));

        StringAssert.Contains(coreCsproj, "<TargetFramework>net10.0</TargetFramework>");
        StringAssert.Contains(presentationCsproj, "<ProjectReference Include=\"..\\Typedown.Core\\Typedown.Core.csproj\" />");
        StringAssert.Contains(winuiCsproj, "<ProjectReference Include=\"..\\Typedown.Presentation\\Typedown.Presentation.csproj\" />");
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
