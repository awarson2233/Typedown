using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase10PackagedFileActivationTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void PackagedManifest_RegistersMarkdownFileAssociations()
    {
        var manifestSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Package.appxmanifest"));

        AssertContains(manifestSource, "Category=\"windows.fileTypeAssociation\"");
        AssertContains(manifestSource, "<uap:FileTypeAssociation Name=\"markdown\">");
        AssertContains(manifestSource, "<uap:Logo>Assets\\Markdown.png</uap:Logo>");
        AssertContains(manifestSource, "<uap:FileType>.md</uap:FileType>");
        AssertContains(manifestSource, "<uap:FileType>.markdown</uap:FileType>");
        AssertDoesNotContain(manifestSource, "<uap:FileType>.txt</uap:FileType>");
    }

    [TestMethod]
    public void WinUIProject_IncludesMarkdownFileIconAsPackagedContent()
    {
        var projectSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Typedown.WinUI.csproj"));

        AssertDoesNotContain(projectSource, "<Content Remove=\"Assets\\Markdown.png\" />");
        AssertDoesNotContain(projectSource, "<Content Remove=\"Assets\\StoreLogo.scale-100.png\" />");
        AssertDoesNotContain(projectSource, "<Content Remove=\"Assets\\Square44x44Logo.scale-100.png\" />");
        AssertDoesNotContain(projectSource, "<Content Remove=\"Assets\\Square150x150Logo.scale-100.png\" />");
        AssertDoesNotContain(projectSource, "<Content Remove=\"Assets\\Wide310x150Logo.scale-100.png\" />");
        AssertDoesNotContain(projectSource, "<Content Remove=\"Assets\\SmallTile.scale-100.png\" />");
        AssertDoesNotContain(projectSource, "<Content Remove=\"Assets\\LargeTile.scale-100.png\" />");
        AssertDoesNotContain(projectSource, "<Content Remove=\"Assets\\SplashScreen.scale-100.png\" />");
    }

    [TestMethod]
    public void FileActivationHelper_PrefersPackagedFileActivationPayloadOverRawEnvironmentArgs()
    {
        var appSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "App.xaml.cs"));

        AssertContains(appSource, "var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();");
        AssertContains(appSource, "activationArgs?.Kind == ExtendedActivationKind.File");
        AssertContains(appSource, "activationArgs.Data is FileActivatedEventArgsContract fileArgs");
        AssertContains(appSource, "fileArgs.Files");
        AssertContains(appSource, ".Select(x => x.Path)");
        AssertContains(appSource, "return [baseProcessPath, .. filePaths];");
        AssertContains(appSource, "return Environment.GetCommandLineArgs();");
    }

    private static void AssertContains(string source, string snippet)
    {
        StringAssert.Contains(source, snippet);
    }

    private static void AssertDoesNotContain(string source, string snippet)
    {
        Assert.IsFalse(source.Contains(snippet, StringComparison.Ordinal), $"Did not expect to find snippet: {snippet}");
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
