using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase14UiVisualBoundaryTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void TargetArchitecture_DocumentsCurrentCorePresentationWinUIBoundary()
    {
        var planPath = Path.Combine(RepoRoot, "docs", "winui3-target-architecture.md");
        Assert.IsTrue(File.Exists(planPath), "Expected the current WinUI3 architecture document.");

        var source = File.ReadAllText(planPath);

        AssertContainsInOrder(
            source,
            "Typedown.Core",
            "Current project references: none.",
            "Typedown.Presentation",
            "Current project references: Typedown.Core only.",
            "Typedown.WinUI",
            "Current project references: Typedown.Core and Typedown.Presentation.",
            "Typedown",
            "Current project references: Typedown.Core, Typedown.Presentation,");

        AssertContainsInOrder(
            source,
            "Forbidden directions:",
            "Core must not reference Presentation, WinUI, XAML, WinRT UI types, WebView2, or the legacy host.",
            "Presentation must not reference WinUI, XAML, WebView2, package assets, activation infrastructure, or the legacy host.",
            "WinUI must not reference the legacy XAML host.");
    }

    [TestMethod]
    public void Roadmap_StatesCurrentGovernanceBeforeCutoverAndArm64()
    {
        var roadmapPath = Path.Combine(RepoRoot, "docs", "winui3-post-phase9-roadmap.md");
        var source = File.ReadAllText(roadmapPath);

        AssertContainsInOrder(
            source,
            "Phase A  Architecture governance refresh",
            "Phase B  WinUI3 parity completion",
            "Phase C  Debug_Local cutover to WinUI3",
            "Phase D  Legacy XAML host retirement",
            "Phase E  ARM64 and packaged validation");

        AssertContainsInOrder(
            source,
            "## Phase A: Architecture governance refresh",
            "Assert Core and Presentation target `net10.0` and stay platform-neutral.",
            "Treat WinUI packaged signing as project/script-supported but certificate-asset-local unless repository assets are restored.",
            "## Phase C: Debug_Local cutover to WinUI3",
            "## Phase E: ARM64 and packaged validation");
    }

    [TestMethod]
    public void BuildBaseline_StatesCurrentArchitectureAndPackageCertificateBoundary()
    {
        var baselinePath = Path.Combine(RepoRoot, "docs", "build-baseline.md");
        var source = File.ReadAllText(baselinePath);

        AssertContainsInOrder(
            source,
            "`Dev\\Typedown.Core\\Typedown.Core.csproj` targets `net10.0` and has no project references.",
            "`Dev\\Typedown.Presentation\\Typedown.Presentation.csproj` targets `net10.0` and references only `Typedown.Core`.",
            "`Dev\\Typedown.WinUI\\Typedown.WinUI.csproj` targets `net10.0-windows10.0.26100.0`, references Core and Presentation",
            "`Dev\\Typedown\\Typedown.csproj` is the legacy compatibility app.");

        AssertContainsInOrder(
            source,
            "Packaged validation path:",
            "Package signing enabled",
            "The development certificate `.cer` and `.pfx` files are not currently checked in",
            "Core and Presentation must stay free of WinUI, XAML, WebView2, package, and legacy host references.");
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
