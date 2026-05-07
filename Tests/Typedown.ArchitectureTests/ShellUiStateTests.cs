using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core;
using Typedown.Core.Services;
using Typedown.Presentation;
using Typedown.Presentation.Services;
using Typedown.Presentation.ViewModels;
using Typedown.Services;

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

        AssertHasScopedRegistration<ImageAction>(services);
        AssertHasScopedRegistration<ImageUpload>(services);
        Assert.IsFalse(services.Any(descriptor => descriptor.ServiceType.Name is "ShellViewModel" or "MainPageViewModel" or "EditorRuntimeViewModel"));
    }

    [TestMethod]
    public void CoreApplicationServices_KeepSharedScopedLifetimeAndExplicitSingletons()
    {
        var services = new ServiceCollection()
            .AddTypedownCore();

        AssertHasScopedRegistration<AutoBackup>(services);
        AssertHasScopedRegistration<EventCenter>(services);
        AssertHasScopedRegistration<RemoteInvoke>(services);
        AssertHasScopedRegistration<Transport>(services);
        AssertHasSingletonRegistration<AccessHistory>(services);
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

    private static void AssertHasSingletonRegistration<TService>(IEnumerable<ServiceDescriptor> services)
    {
        var registration = services.SingleOrDefault(descriptor => descriptor.ServiceType == typeof(TService));

        Assert.IsNotNull(registration, $"Expected registration for {typeof(TService).Name}.");
        Assert.AreEqual(ServiceLifetime.Singleton, registration.Lifetime, $"{typeof(TService).Name} lifetime changed.");
    }

    private static void AssertHasScopedRegistration<TService>(IEnumerable<ServiceDescriptor> services)
    {
        var registration = services.SingleOrDefault(descriptor => descriptor.ServiceType == typeof(TService));

        Assert.IsNotNull(registration, $"Expected registration for {typeof(TService).Name}.");
        Assert.AreEqual(ServiceLifetime.Scoped, registration.Lifetime, $"{typeof(TService).Name} lifetime changed.");
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
