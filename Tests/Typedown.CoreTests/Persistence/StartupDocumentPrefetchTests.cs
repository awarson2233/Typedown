using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Enums;
using Typedown.Core.Services;

namespace Typedown.CoreTests.Persistence;

[TestClass]
public class StartupDocumentPrefetchTests
{
    private string testDirectory = null!;
    private AutoBackup autoBackup = null!;

    [TestInitialize]
    public void Setup()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), $"typedown-startup-prefetch-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDirectory);
        autoBackup = new AutoBackup(new AtomicFileWriter());
    }

    [TestCleanup]
    public void Cleanup()
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

    [TestMethod]
    public void Resolve_PrefersCommandLineMarkdownOverLastFile()
    {
        var target = StartupDocumentTarget.Resolve(["Typedown.WinUI.exe", "--flag", @"C:\docs\a.md"], FileStartupAction.OpenLast, @"C:\docs\last.md");

        Assert.AreEqual(new StartupDocumentTarget(StartupDocumentOrigin.CommandLine, @"C:\docs\a.md"), target);
    }

    [TestMethod]
    public void Resolve_UsesLastFileOnlyWhenOpenLastIsSelected()
    {
        Assert.AreEqual(
            new StartupDocumentTarget(StartupDocumentOrigin.LastFile, @"C:\docs\last.md"),
            StartupDocumentTarget.Resolve(["Typedown.WinUI.exe"], FileStartupAction.OpenLast, @"C:\docs\last.md"));
        Assert.IsNull(StartupDocumentTarget.Resolve(["Typedown.WinUI.exe"], FileStartupAction.None, @"C:\docs\last.md"));
        Assert.IsNull(StartupDocumentTarget.Resolve(["Typedown.WinUI.exe"], FileStartupAction.OpenLast, "  "));
        Assert.IsNull(StartupDocumentTarget.Resolve(["Typedown.WinUI.exe"], FileStartupAction.OpenLast, null));
    }

    [TestMethod]
    public async Task TakeAsync_ReturnsTheSnapshotReadAtStart()
    {
        var path = Path.Combine(testDirectory, "doc.md");
        File.WriteAllText(path, "# prefetched");
        var target = new StartupDocumentTarget(StartupDocumentOrigin.CommandLine, path);
        var prefetch = new StartupDocumentPrefetch(autoBackup);

        prefetch.Start(target);
        var first = await prefetch.TakeAsync(target);
        File.WriteAllText(path, "# changed later");
        var second = await prefetch.TakeAsync(target);

        Assert.IsTrue(first.Exists);
        Assert.AreEqual("# prefetched", first.GetText());
        Assert.IsNull(first.Backup);
        Assert.AreEqual("# changed later", second.GetText(), "The prefetched snapshot is handed out only once.");
    }

    [TestMethod]
    public async Task TakeAsync_ReadsAgainWhenThePrefetchedPathDiffers()
    {
        var prefetchedPath = Path.Combine(testDirectory, "a.md");
        var requestedPath = Path.Combine(testDirectory, "b.md");
        File.WriteAllText(prefetchedPath, "a");
        File.WriteAllText(requestedPath, "b");
        var prefetch = new StartupDocumentPrefetch(autoBackup);

        prefetch.Start(new StartupDocumentTarget(StartupDocumentOrigin.LastFile, prefetchedPath));
        var snapshot = await prefetch.TakeAsync(new StartupDocumentTarget(StartupDocumentOrigin.LastFile, requestedPath));

        Assert.AreEqual(requestedPath, snapshot.Path);
        Assert.AreEqual("b", snapshot.GetText());
    }

    [TestMethod]
    public async Task ReadAsync_ReportsMissingFile()
    {
        var snapshot = await StartupFileSnapshot.ReadAsync(Path.Combine(testDirectory, "missing.md"), autoBackup);

        Assert.IsFalse(snapshot.Exists);
        Assert.IsNull(snapshot.Backup);
    }

    [TestMethod]
    public async Task ReadAsync_RethrowsTheOriginalReadErrorFromGetText()
    {
        var path = Path.Combine(testDirectory, "locked.md");
        File.WriteAllText(path, "locked");

        StartupFileSnapshot snapshot;
        using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            snapshot = await StartupFileSnapshot.ReadAsync(path, autoBackup);

        Assert.IsTrue(snapshot.Exists);
        Assert.ThrowsException<IOException>(() => snapshot.GetText());
    }

    [TestMethod]
    public async Task ReadAsync_IncludesTheBackup()
    {
        var path = Path.Combine(testDirectory, "backed-up.md");
        File.WriteAllText(path, "on disk");
        try
        {
            Assert.IsTrue(await autoBackup.Backup(path, "from backup"));

            var snapshot = await StartupFileSnapshot.ReadAsync(path, autoBackup);

            Assert.AreEqual("on disk", snapshot.GetText());
            Assert.AreEqual("from backup", snapshot.Backup);
        }
        finally
        {
            autoBackup.DeleteBackup(path);
        }
    }
}
