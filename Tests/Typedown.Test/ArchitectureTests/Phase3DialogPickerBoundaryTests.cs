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

        private static void AssertNoTypeReference(string source, string typeName)
        {
            Assert.IsFalse(Regex.IsMatch(source, $@"\b{Regex.Escape(typeName)}\b"), $"Unexpected reference to {typeName}.");
        }
    }
}
