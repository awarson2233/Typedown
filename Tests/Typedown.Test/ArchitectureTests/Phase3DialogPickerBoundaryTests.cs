using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text.RegularExpressions;

namespace Typedown.Test.ArchitectureTests
{
    [TestClass]
    public class Phase3DialogPickerBoundaryTests
    {
        private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(TestContext.TestRunDirectory, "..", "..", "..", ".."));

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void FileViewModel_DoesNotReferenceConcreteDialogOrPickerTypes()
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "ViewModels", "FileViewModel.cs"));

            AssertNoTypeReference(source, "AppContentDialog");
            AssertNoTypeReference(source, "FileOpenPicker");
            AssertNoTypeReference(source, "FileSavePicker");
            AssertNoTypeReference(source, "FolderPicker");
        }

        [TestMethod]
        public void ImageAction_DoesNotReferenceConcreteDialogType()
        {
            var source = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Services", "ImageAction.cs"));

            AssertNoTypeReference(source, "AppContentDialog");
        }

        [TestMethod]
        public void DialogAndPickerInterfaces_DoNotExposePlatformTypes()
        {
            var dialogSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Interfaces", "IDialogService.cs"));
            var pickerSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Interfaces", "IFilePickerService.cs"));

            AssertNoTypeReference(dialogSource, "ContentDialogResult");
            AssertNoTypeReference(dialogSource, "XamlRoot");
            AssertNoTypeReference(dialogSource, "HWND");
            AssertNoTypeReference(dialogSource, "FileOpenPicker");
            AssertNoTypeReference(dialogSource, "FileSavePicker");
            AssertNoTypeReference(dialogSource, "FolderPicker");
            AssertNoDialogButtonMember(dialogSource, "Close");

            AssertNoTypeReference(pickerSource, "ContentDialogResult");
            AssertNoTypeReference(pickerSource, "FileOpenPicker");
            AssertNoTypeReference(pickerSource, "FileSavePicker");
            AssertNoTypeReference(pickerSource, "FolderPicker");
            AssertNoTypeReference(pickerSource, "XamlRoot");
            AssertNoTypeReference(pickerSource, "HWND");
            AssertNoTypeReference(pickerSource, "owner");
            AssertNoTypeReference(pickerSource, "window");
        }

        private static void AssertNoTypeReference(string source, string typeName)
        {
            Assert.IsFalse(Regex.IsMatch(source, $@"\b{Regex.Escape(typeName)}\b"), $"Unexpected reference to {typeName}.");
        }

        private static void AssertNoDialogButtonMember(string source, string memberName)
        {
            var pattern = $@"enum\s+DialogButton\s*\{{[\s\S]*\b{Regex.Escape(memberName)}\b";
            Assert.IsFalse(Regex.IsMatch(source, pattern), $"Unexpected DialogButton member {memberName}.");
        }
    }
}
