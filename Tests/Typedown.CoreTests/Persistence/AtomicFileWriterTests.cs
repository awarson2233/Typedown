using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Services;

namespace Typedown.CoreTests.Persistence;

[TestClass]
public class AtomicFileWriterTests
{
    private string testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), $"typedown-atomic-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(testDirectory))
        {
            try
            {
                Directory.Delete(testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [TestMethod]
    public async Task WriteAllTextAsync_CreatesNewFileWithContent()
    {
        var writer = new AtomicFileWriter();
        var filePath = Path.Combine(testDirectory, "test.md");

        await writer.WriteAllTextAsync(filePath, "Hello Atomic World");

        Assert.IsTrue(File.Exists(filePath));
        Assert.AreEqual("Hello Atomic World", await File.ReadAllTextAsync(filePath));
    }

    [TestMethod]
    public async Task WriteAllTextAsync_OverwritesExistingFile()
    {
        var writer = new AtomicFileWriter();
        var filePath = Path.Combine(testDirectory, "test.md");
        await File.WriteAllTextAsync(filePath, "Initial Content");

        await writer.WriteAllTextAsync(filePath, "Updated Content");

        Assert.AreEqual("Updated Content", await File.ReadAllTextAsync(filePath));
    }

    [TestMethod]
    public async Task WriteTemporaryAsync_CreatesHiddenTempFileInTargetDirectory()
    {
        var writer = new AtomicFileWriter();
        var targetPath = Path.Combine(testDirectory, "target.md");

        var tempPath = await writer.WriteTemporaryAsync(targetPath, "Temporary Content");

        Assert.IsTrue(File.Exists(tempPath));
        Assert.AreEqual(testDirectory, Path.GetDirectoryName(tempPath));
        StringAssert.StartsWith(Path.GetFileName(tempPath), ".target.md.");
        StringAssert.EndsWith(Path.GetFileName(tempPath), ".tmp");
        Assert.AreEqual("Temporary Content", await File.ReadAllTextAsync(tempPath));
    }

    [TestMethod]
    public async Task WriteTemporaryAsync_CreatesParentDirectoryIfMissing()
    {
        var writer = new AtomicFileWriter();
        var nestedPath = Path.Combine(testDirectory, "nested", "sub", "target.md");

        var tempPath = await writer.WriteTemporaryAsync(nestedPath, "Nested Content");

        Assert.IsTrue(File.Exists(tempPath));
        Assert.AreEqual("Nested Content", await File.ReadAllTextAsync(tempPath));
    }

    [TestMethod]
    public async Task Commit_MovesTemporaryFileToDestination_OverwritingExistingFile()
    {
        var writer = new AtomicFileWriter();
        var destinationPath = Path.Combine(testDirectory, "committed.md");
        await File.WriteAllTextAsync(destinationPath, "Old Version");

        var tempPath = await writer.WriteTemporaryAsync(destinationPath, "New Version");
        writer.Commit(tempPath, destinationPath);

        Assert.IsFalse(File.Exists(tempPath));
        Assert.IsTrue(File.Exists(destinationPath));
        Assert.AreEqual("New Version", await File.ReadAllTextAsync(destinationPath));
    }

    [TestMethod]
    public async Task Discard_DeletesTemporaryFile()
    {
        var writer = new AtomicFileWriter();
        var targetPath = Path.Combine(testDirectory, "target.md");

        var tempPath = await writer.WriteTemporaryAsync(targetPath, "To Be Discarded");
        Assert.IsTrue(File.Exists(tempPath));

        writer.Discard(tempPath);

        Assert.IsFalse(File.Exists(tempPath));
    }

    [TestMethod]
    public void Discard_HandlesNullOrMissingFileSafely()
    {
        var writer = new AtomicFileWriter();

        writer.Discard(null);
        writer.Discard(string.Empty);
        writer.Discard(Path.Combine(testDirectory, "non-existent.tmp"));
    }
}
