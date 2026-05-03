using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase14UiVisualBoundaryTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void CoreDecouplingPlan_DocumentsCurrentPureCoreAndMvvmBoundary()
    {
        var planPath = Path.Combine(
            RepoRoot,
            "docs",
            "superpowers",
            "plans",
            "2026-04-27-core-decoupling-slimming.md");
        Assert.IsTrue(File.Exists(planPath), "Expected the current Core decoupling and slimming plan document.");

        var source = File.ReadAllText(planPath);

        AssertContainsInOrder(
            source,
            "将 `Dev/Typedown.Core` 固化为纯逻辑 + MVVM 合同层",
            "`Typedown.Core` 不允许依赖 XAML、WinRT UI 类型、WebView2、文件选择器、窗口服务或 legacy 项目",
            "`Dev/Typedown.Presentation`：承接 shell-agnostic MVVM、资源读取、组合逻辑",
            "`Dev/Typedown.WinUI`：WinUI3 shell、XAML、平台服务适配、WebView2 host",
            "`Dev/Typedown.Core.Legacy` 与 `Dev/Typedown.UI` 已退场");
    }

    [TestMethod]
    public void Roadmap_ReordersPhase14AsVisualMigrationBeforeCutoverAndArm64()
    {
        var roadmapPath = Path.Combine(RepoRoot, "docs", "winui3-post-phase9-roadmap.md");
        var source = File.ReadAllText(roadmapPath);

        AssertContainsInOrder(
            source,
            "Phase 14  首批 1:1 可视 UI 迁移",
            "Phase 15  Debug_Local 主启动路径切换到 WinUI3",
            "Phase 16  legacy XamlUI 退场与构建清理",
            "Phase 17  ARM64 与打包验证");

        AssertContainsInOrder(
            source,
            "## Phase 14：首批 1:1 可视 UI 迁移",
            "`WinUIEditorHost` 仍留在 `Typedown.WinUI`",
            "本阶段不修改 solution 默认启动入口",
            "## Phase 15：Debug_Local 主启动路径切换到 WinUI3",
            "## Phase 17：ARM64 与打包验证");
    }

    [TestMethod]
    public void BuildBaseline_StatesPhase14VisualMigrationBoundary()
    {
        var baselinePath = Path.Combine(RepoRoot, "docs", "build-baseline.md");
        var source = File.ReadAllText(baselinePath);

        AssertContainsInOrder(
            source,
            "Phase 14 当前只做首批 `1:1` 可视 UI 迁移，不切默认 `Debug_Local` 主启动路径。",
            "`WinUIEditorHost`、Window/Dialog/FilePicker、`Package.appxmanifest`、`launchSettings.json` 仍由 `Typedown.WinUI` 持有；不要提前迁入 `Typedown.Presentation`。",
            "## WinUI3 Phase 14 文档与边界准备",
            "Phase 14 当前定义为首批 `1:1` 可视 UI 迁移，不是默认启动路径切换。",
            "dotnet test .\\Tests\\Typedown.ArchitectureTests\\Typedown.ArchitectureTests.csproj -c Debug /nologo /v:minimal");
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
