using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Services;
using Typedown.Core.Utilities;

namespace Typedown.CoreTests.Persistence;

[TestClass]
public class AutoBackupTests
{
    private string testDirectory = null!;
    private IAtomicFileWriter writer = null!;
    private AutoBackup autoBackup = null!;

    [TestInitialize]
    public void Setup()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), $"typedown-autobackup-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDirectory);
        writer = new AtomicFileWriter();
        autoBackup = new AutoBackup(writer);
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
    public void GetBackupFilePath_ReturnsExpectedPath_WithSimpleHash2Prefix()
    {
        var sourcePath = @"C:\docs\readme.md";
        var backupFilePath = autoBackup.GetBackupFilePath(sourcePath);

        Assert.IsNotNull(backupFilePath);
        var expectedHash = Common.SimpleHash2(sourcePath);
        var expectedFileName = $"{expectedHash}_readme.md";
        Assert.AreEqual(expectedFileName, Path.GetFileName(backupFilePath));
    }

    [TestMethod]
    public void GetBackupFilePath_ReturnsNullForEmptyOrWhitespacePath()
    {
        Assert.IsNull(autoBackup.GetBackupFilePath(null!));
        Assert.IsNull(autoBackup.GetBackupFilePath(string.Empty));
        Assert.IsNull(autoBackup.GetBackupFilePath("   "));
    }

    [TestMethod]
    public async Task Backup_CreatesBackupFileWithExpectedContent()
    {
        var sourcePath = Path.Combine(testDirectory, "test-doc.md");
        const string markdownContent = "# Document Content";

        try
        {
            var success = await autoBackup.Backup(sourcePath, markdownContent);
            Assert.IsTrue(success);

            var backupContent = await autoBackup.GetBackup(sourcePath);
            Assert.AreEqual(markdownContent, backupContent);
        }
        finally
        {
            autoBackup.DeleteBackup(sourcePath);
        }
    }

    [TestMethod]
    public async Task GetBackup_ReturnsNull_WhenBackupDoesNotExist()
    {
        var sourcePath = Path.Combine(testDirectory, $"non-existent-{Guid.NewGuid():N}.md");
        var content = await autoBackup.GetBackup(sourcePath);
        Assert.IsNull(content);
    }

    [TestMethod]
    public async Task DeleteBackup_RemovesBackupFile()
    {
        var sourcePath = Path.Combine(testDirectory, "to-delete.md");
        await autoBackup.Backup(sourcePath, "Content to be deleted");

        var backupPath = autoBackup.GetBackupFilePath(sourcePath);
        Assert.IsNotNull(backupPath);
        Assert.IsTrue(File.Exists(backupPath));

        autoBackup.DeleteBackup(sourcePath);

        Assert.IsFalse(File.Exists(backupPath));
        Assert.IsNull(await autoBackup.GetBackup(sourcePath));
    }

    [TestMethod]
    public async Task PrepareAndCommitBackup_Succeeds()
    {
        var sourcePath = Path.Combine(testDirectory, "manual-commit.md");
        const string content = "Manual Commit Content";

        try
        {
            var tempPath = await autoBackup.PrepareBackup(sourcePath, content);
            Assert.IsNotNull(tempPath);
            Assert.IsTrue(File.Exists(tempPath));

            var committed = autoBackup.CommitBackup(sourcePath, tempPath);
            Assert.IsTrue(committed);
            Assert.IsFalse(File.Exists(tempPath));

            var retrieved = await autoBackup.GetBackup(sourcePath);
            Assert.AreEqual(content, retrieved);
        }
        finally
        {
            autoBackup.DeleteBackup(sourcePath);
        }
    }

    [TestMethod]
    public void CommitBackup_DiscardsTemporaryFileOnInvalidSource()
    {
        var tempPath = Path.Combine(testDirectory, "orphan.tmp");
        File.WriteAllText(tempPath, "temp content");

        var result = autoBackup.CommitBackup(string.Empty, tempPath);
        Assert.IsFalse(result);
    }
}
