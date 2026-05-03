using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Presentation;
using Typedown.Presentation.ViewModels;

namespace Typedown.ArchitectureTests;

[TestClass]
public class ShellUiStateTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void CheckpointApplicationViewModels_AreRegisteredByTypedownPresentationComposition()
    {
        var services = new ServiceCollection()
            .AddTypedownPresentation();

        var expectedViewModels = new[]
        {
            typeof(AppViewModel),
            typeof(EditorViewModel),
            typeof(FileViewModel),
            typeof(FloatViewModel),
            typeof(FormatViewModel),
            typeof(ParagraphViewModel),
            typeof(SettingsViewModel),
            typeof(UIViewModel),
        };

        foreach (var viewModelType in expectedViewModels)
        {
            var registration = services.SingleOrDefault(descriptor => descriptor.ServiceType == viewModelType);

            Assert.IsNotNull(registration, $"Expected registration for {viewModelType.Name}.");
            Assert.AreEqual(ServiceLifetime.Scoped, registration.Lifetime, $"{viewModelType.Name} lifetime changed.");
        }

        Assert.IsFalse(services.Any(descriptor => descriptor.ServiceType.Name is "ShellViewModel" or "MainPageViewModel" or "EditorRuntimeViewModel"));
    }

    [TestMethod]
    public void TypedownPresentation_SourceStaysPlatformNeutral()
    {
        var presentationRoot = Path.Combine(RepoRoot, "Dev", "Typedown.Presentation");
        var presentationSources = Directory
            .EnumerateFiles(presentationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(File.ReadAllText)
            .ToArray();

        Assert.IsTrue(presentationSources.Length > 0, "Expected Typedown.Presentation source files.");

        foreach (var source in presentationSources)
        {
            AssertNoReference(source, "using Microsoft.UI.Xaml");
            AssertNoReference(source, "using Windows.UI.Xaml");
            AssertNoReference(source, "global using Microsoft.UI.Xaml");
            AssertNoReference(source, "global using Windows.UI.Xaml");
            AssertNoReference(source, "using Typedown.WinUI");
            AssertNoReference(source, "using Typedown.XamlUI");
            AssertNoReference(source, @"..\Typedown.WinUI\Typedown.WinUI.csproj");
            AssertNoReference(source, @"..\Typedown.XamlUI\Typedown.XamlUI.csproj");
        }
    }

    private static void AssertNoReference(string source, string token)
    {
        Assert.IsFalse(source.Contains(token, StringComparison.Ordinal), $"Unexpected reference: {token}");
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
