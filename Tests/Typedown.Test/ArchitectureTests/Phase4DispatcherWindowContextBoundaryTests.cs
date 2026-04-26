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

        private static void AssertNoTypeReference(string source, string typeName)
        {
            Assert.IsFalse(Regex.IsMatch(source, Regex.Escape(typeName)), $"Unexpected reference to {typeName}.");
        }

        private static void AssertHasTypeReference(string source, string typeName)
        {
            Assert.IsTrue(Regex.IsMatch(source, $@"\b{Regex.Escape(typeName)}\b"), $"Expected reference to {typeName}.");
        }
    }
}
