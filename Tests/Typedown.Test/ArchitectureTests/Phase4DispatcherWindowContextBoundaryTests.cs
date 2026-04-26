using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text.RegularExpressions;

namespace Typedown.Test.ArchitectureTests
{
    [TestClass]
    public class Phase4DispatcherWindowContextBoundaryTests
    {
        private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(TestContext.TestRunDirectory, "..", "..", "..", ".."));

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void UiDispatcherInterface_DoesNotExposePlatformDispatcherTypes()
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Interfaces", "IUiDispatcher.cs"));

            AssertNoTypeReference(source, "CoreDispatcher");
            AssertNoTypeReference(source, "CoreDispatcherPriority");
            AssertNoTypeReference(source, "DispatchedHandler");
            AssertNoTypeReference(source, "Windows.UI");
        }

        [TestMethod]
        public void WindowContextInterface_DoesNotExposePlatformTypeNames()
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Interfaces", "IWindowContext.cs"));

            AssertNoTypeReference(source, "XamlRoot");
            AssertNoTypeReference(source, "HWND");
            AssertNoTypeReference(source, "CoreWindow");
            AssertNoTypeReference(source, "Windows.UI");
        }

        [TestMethod]
        public void ViewModels_DoNotReachForCoreDispatcherDirectly()
        {
            var uiViewModelSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "ViewModels", "UIViewModel.cs"));
            var fileViewModelSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "ViewModels", "FileViewModel.cs"));

            AssertNoTypeReference(uiViewModelSource, "CoreApplication");
            AssertNoTypeReference(uiViewModelSource, "CoreDispatcher");
            AssertNoTypeReference(fileViewModelSource, "CoreApplication");
            AssertNoTypeReference(fileViewModelSource, "CoreDispatcher");
            AssertNoTypeReference(fileViewModelSource, "XamlRoot?.Content?.Dispatcher");
        }

        [TestMethod]
        public void ShellServices_UseWindowContextInsteadOfAppViewModelWindowFields()
        {
            var dialogServiceSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown", "Services", "DialogService.cs"));
            var pickerServiceSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown", "Services", "FilePickerService.cs"));

            AssertNoTypeReference(dialogServiceSource, "appViewModel.XamlRoot");
            AssertNoTypeReference(pickerServiceSource, "appViewModel.MainWindow");
            AssertHasTypeReference(dialogServiceSource, "IWindowContext");
            AssertHasTypeReference(pickerServiceSource, "IWindowContext");
        }

        [TestMethod]
        public void MainWindow_InitializesDispatcherAndWindowContextBeforeDataContext()
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown", "Windows", "MainWindow.cs"));

            AssertContainsInOrder(
                source,
                "UiDispatcher?.Attach(Dispatcher);",
                "WindowContext?.Bind(this);",
                "DataContext = AppViewModel;");
        }

        [TestMethod]
        public void WindowService_GetCursorPos_GuardsAgainstMissingWindow()
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown", "Services", "WindowService.cs"));

            AssertHasTypeReference(source, "InvalidOperationException");
            AssertHasTypeReference(source, "XamlWindow.GetWindow(relativeTo)");
            AssertHasTypeReference(source, "is not attached to a window");
        }

        private static void AssertNoTypeReference(string source, string typeName)
        {
            Assert.IsFalse(Regex.IsMatch(StripComments(source), CreateBoundaryPattern(typeName)), $"Unexpected reference to {typeName}.");
        }

        private static void AssertHasTypeReference(string source, string typeName)
        {
            Assert.IsTrue(Regex.IsMatch(StripComments(source), CreateBoundaryPattern(typeName)), $"Expected reference to {typeName}.");
        }

        private static void AssertContainsInOrder(string source, params string[] snippets)
        {
            var currentIndex = -1;
            foreach (var snippet in snippets)
            {
                var nextIndex = source.IndexOf(snippet, currentIndex + 1);
                Assert.IsTrue(nextIndex >= 0, $"Expected to find snippet: {snippet}");
                Assert.IsTrue(nextIndex > currentIndex, $"Expected snippet to appear after the previous one: {snippet}");
                currentIndex = nextIndex;
            }
        }

        private static string CreateBoundaryPattern(string typeName)
        {
            return $@"(?<![A-Za-z0-9_]){Regex.Escape(typeName)}(?![A-Za-z0-9_])";
        }

        private static string StripComments(string source)
        {
            source = Regex.Replace(source, @"//.*?$", string.Empty, RegexOptions.Multiline);
            source = Regex.Replace(source, @"/\*[\s\S]*?\*/", string.Empty);
            return source;
        }
    }
}
