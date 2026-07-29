using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Typedown.Core.Models;
using Typedown.Core.Services;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Utilities;
using Typedown.Presentation.ViewModels;
using Typedown.WinUI.Controls;

namespace Typedown.ArchitectureTests;

[TestClass]
public class WinUIEditorBridgeAdapterTests
{
    [TestMethod]
    public void GetSettings_ComesFromSessionPayload()
    {
        var session = new FakeEditorDocumentSession();
        var adapter = new WinUIEditorBridgeAdapter(session);

        var getSettings = Invoke(adapter, """{"type":"invoke","id":"1","name":"GetSettings"}""");

        Assert.AreEqual(0, getSettings.GetProperty("args").GetProperty("code").GetInt32());
        Assert.AreEqual(@"C:\session-base", getSettings.GetProperty("args").GetProperty("data").GetProperty("basePath").GetString());
        Assert.AreEqual("session-seed", getSettings.GetProperty("args").GetProperty("data").GetProperty("markdown").GetString());
        Assert.AreEqual(18, getSettings.GetProperty("args").GetProperty("data").GetProperty("fontSize").GetInt32());
        Assert.AreEqual("night", getSettings.GetProperty("args").GetProperty("data").GetProperty("sequenceTheme").GetString());
    }

    [TestMethod]
    public void RemoteInvokeHandlers_CoverCommonTsSurfaceWithoutRejecting()
    {
        var session = new FakeEditorDocumentSession();
        var adapter = new WinUIEditorBridgeAdapter(session);

        var getCurrentTheme = Invoke(adapter, """{"type":"invoke","id":"0","name":"GetCurrentTheme"}""");
        var contentLoaded = Invoke(adapter, """{"type":"invoke","id":"1","name":"ContentLoaded"}""");
        var exportCallback = Invoke(adapter, """{"type":"invoke","id":"2","name":"ExportCallback","args":{"html":"<p>x</p>","context":{"kind":"pdf"}}}""");
        var printHtml = Invoke(adapter, """{"type":"invoke","id":"3","name":"PrintHTML","args":{"html":"<p>x</p>","context":{"kind":"print"}}}""");
        var loadImage = Invoke(adapter, """{"type":"invoke","id":"4","name":"LoadImage","args":{"url":"file:///image.png","width":10,"height":20}}""");
        var getSettings = Invoke(adapter, """{"type":"invoke","id":"41","name":"GetSettings"}""");
        var setClipboard = Invoke(adapter, """{"type":"invoke","id":"42","name":"SetClipboard","args":{"type":"text/plain","data":"hello"}}""");
        var getStringResources = Invoke(adapter, """{"type":"invoke","id":"5","name":"GetStringResources","args":{"names":["Bold","Italic"]}}""");
        var openNewWindow = Invoke(adapter, """{"type":"invoke","id":"6","name":"OpenNewWindow","args":"https://example.com"}""");
        var unhandledException = Invoke(adapter, """{"type":"invoke","id":"7","name":"UnhandledException","args":"boom"}""");

        Assert.AreEqual(0, getCurrentTheme.GetProperty("args").GetProperty("code").GetInt32());
        Assert.AreEqual(0, contentLoaded.GetProperty("args").GetProperty("code").GetInt32());
        Assert.IsTrue(adapter.IsContentLoaded);
        Assert.AreEqual(0, exportCallback.GetProperty("args").GetProperty("code").GetInt32());
        Assert.IsTrue(exportCallback.GetProperty("args").GetProperty("data").GetBoolean());
        Assert.AreEqual(0, printHtml.GetProperty("args").GetProperty("code").GetInt32());
        Assert.IsTrue(printHtml.GetProperty("args").GetProperty("data").GetBoolean());
        Assert.AreEqual(0, loadImage.GetProperty("args").GetProperty("code").GetInt32());
        Assert.AreEqual("file:///image.png", loadImage.GetProperty("args").GetProperty("data").GetProperty("url").GetString());
        Assert.AreEqual(0, getSettings.GetProperty("args").GetProperty("code").GetInt32());
        Assert.AreEqual(0, setClipboard.GetProperty("args").GetProperty("code").GetInt32());
        Assert.AreEqual("Bold", getStringResources.GetProperty("args").GetProperty("data").GetProperty("Bold").GetString());
        Assert.AreEqual(0, openNewWindow.GetProperty("args").GetProperty("code").GetInt32());
        Assert.AreEqual(0, unhandledException.GetProperty("args").GetProperty("code").GetInt32());
    }

    [TestMethod]
    public void DocumentSession_GetStringResources_FallsBackWithoutRemoteInvoke()
    {
        var session = new WinUIEditorDocumentSession();
        using var args = JsonDocument.Parse("""{"names":["Bold","MissingEditorLabel"]}""");

        var result = session.HandleRemoteInvokeAsync("GetStringResources", args.RootElement).GetAwaiter().GetResult();

        Assert.IsInstanceOfType<Dictionary<string, string>>(result);
        var resources = (Dictionary<string, string>)result!;
        Assert.AreEqual("MissingEditorLabel", resources["MissingEditorLabel"]);
        Assert.IsFalse(string.IsNullOrWhiteSpace(resources["Bold"]));
    }

    [TestMethod]
    public void DocumentSession_ResizeTable_FallsBackToLocalPayloadWithoutRemoteInvoke()
    {
        var session = new WinUIEditorDocumentSession();
        var adapter = new WinUIEditorBridgeAdapter(session);

        var rowColumn = Invoke(adapter, """{"type":"invoke","id":"resize-row","name":"ResizeTable","args":{"row":4,"column":5}}""");
        var rowsColumns = Invoke(adapter, """{"type":"invoke","id":"resize-rows","name":"ResizeTable","args":{"rows":6,"columns":7}}""");
        var emptyPayload = Invoke(adapter, """{"type":"invoke","id":"resize-empty","name":"ResizeTable","args":{}}""");
        var noPayload = Invoke(adapter, """{"type":"invoke","id":"resize-none","name":"ResizeTable"}""");

        var rowColumnData = rowColumn.GetProperty("args").GetProperty("data");
        var rowsColumnsData = rowsColumns.GetProperty("args").GetProperty("data");
        Assert.AreEqual(0, rowColumn.GetProperty("args").GetProperty("code").GetInt32());
        Assert.AreEqual(4, rowColumnData.GetProperty("row").GetInt32());
        Assert.AreEqual(5, rowColumnData.GetProperty("column").GetInt32());
        Assert.AreEqual(4, rowColumnData.GetProperty("rows").GetInt32());
        Assert.AreEqual(5, rowColumnData.GetProperty("columns").GetInt32());
        Assert.AreEqual(0, rowsColumns.GetProperty("args").GetProperty("code").GetInt32());
        Assert.AreEqual(6, rowsColumnsData.GetProperty("row").GetInt32());
        Assert.AreEqual(7, rowsColumnsData.GetProperty("column").GetInt32());
        Assert.AreEqual(6, rowsColumnsData.GetProperty("rows").GetInt32());
        Assert.AreEqual(7, rowsColumnsData.GetProperty("columns").GetInt32());
        Assert.AreEqual(1, emptyPayload.GetProperty("args").GetProperty("code").GetInt32());
        Assert.AreEqual(1, noPayload.GetProperty("args").GetProperty("code").GetInt32());
    }

    [TestMethod]
    public void DocumentFlushed_RoutesThroughAdapterToPresentationEventCenter()
    {
        var eventCenter = new EventCenter();
        var serviceProvider = new ServiceCollection().AddSingleton(eventCenter).BuildServiceProvider();
        var session = new WinUIEditorDocumentSession(serviceProvider: serviceProvider);
        var adapter = new WinUIEditorBridgeAdapter(session);
        EditorEventArgs? received = null;
        using var subscription = eventCenter.GetObservable<EditorEventArgs>("DocumentFlushed").Subscribe(args => received = args);

        adapter.Receive("""{"type":"message","name":"DocumentFlushed","args":{"documentId":"A","nextDocumentId":"B"}}""", _ => true);

        Assert.IsNotNull(received);
        Assert.AreEqual("A", received.Args["documentId"]?.ToString());
        Assert.AreEqual("B", received.Args["nextDocumentId"]?.ToString());
    }

    [TestMethod]
    public void DocumentFlushed_CommitsOnlyMatchingPendingGenerationAndSendsActivation()
    {
        var eventCenter = new EventCenter();
        var commandSink = new RecordingEditorCommandSink();
        var services = new ServiceCollection()
            .AddSingleton(eventCenter)
            .AddSingleton<IEditorCommandSink>(commandSink)
            .BuildServiceProvider();
        var editorViewModel = (EditorViewModel)RuntimeHelpers.GetUninitializedObject(typeof(EditorViewModel));
        SetAutoProperty(editorViewModel, nameof(EditorViewModel.ServiceProvider), services);
        SetAutoProperty(editorViewModel, nameof(EditorViewModel.History), new Typedown.Core.Models.ContentHistory());
        SetAutoProperty(editorViewModel, nameof(EditorViewModel.Toc), new Typedown.Core.Models.TocTreeItem());
        SetField(editorViewModel, "appliedReplacementRevisions", new Dictionary<string, string>(StringComparer.Ordinal));
        SetField(editorViewModel, "appliedReplacementRevisionOrder", new Queue<string>());
        SetField(editorViewModel, "documentId", "A");
        var fileViewModel = (FileViewModel)RuntimeHelpers.GetUninitializedObject(typeof(FileViewModel));
        SetAutoProperty(fileViewModel, nameof(FileViewModel.ServiceProvider), services);
        var formatViewModel = (FormatViewModel)RuntimeHelpers.GetUninitializedObject(typeof(FormatViewModel));
        SetAutoProperty(formatViewModel, nameof(FormatViewModel.ServiceProvider), services);
        SetAutoProperty(formatViewModel, nameof(FormatViewModel.FormatState), new Typedown.Core.Models.FormatState());
        var pendingType = typeof(FileViewModel).GetNestedType("PendingDocument", BindingFlags.NonPublic)!;
        var pending = Activator.CreateInstance(pendingType, "C", "# C", 42UL, "C:/docs/c.md", "C:/docs", true)!;
        SetField(fileViewModel, "pendingDocument", pending);
        var augmentedServices = new ServiceCollection()
            .AddSingleton(eventCenter)
            .AddSingleton<IEditorCommandSink>(commandSink)
            .AddSingleton(editorViewModel)
            .AddSingleton(fileViewModel)
            .AddSingleton(formatViewModel)
            .BuildServiceProvider();
        SetAutoProperty(editorViewModel, nameof(EditorViewModel.ServiceProvider), augmentedServices);
        SetAutoProperty(fileViewModel, nameof(FileViewModel.ServiceProvider), augmentedServices);
        SetAutoProperty(formatViewModel, nameof(FormatViewModel.ServiceProvider), augmentedServices);
        using var editorSubscription = eventCenter.GetObservable<EditorEventArgs>("DocumentFlushed")
            .Subscribe(args => editorViewModel.OnDocumentFlushed(args.Args));
        var adapter = new WinUIEditorBridgeAdapter(new WinUIEditorDocumentSession(serviceProvider: augmentedServices));
        Assert.AreSame(commandSink, fileViewModel.EditorCommandSink);

        adapter.Receive("""{"type":"message","name":"DocumentFlushed","args":{"documentId":"A","nextDocumentId":"B"}}""", _ => true);

        Assert.AreEqual("A", editorViewModel.CurrentDocumentId);
        Assert.AreEqual(0, commandSink.Messages.Count);

        commandSink.Result = false;
        adapter.Receive("""{"type":"message","name":"DocumentFlushed","args":{"documentId":"A","nextDocumentId":"C"}}""", _ => true);

        Assert.AreEqual("C", editorViewModel.CurrentDocumentId);
        Assert.AreEqual("# C", editorViewModel.Markdown);
        Assert.AreEqual("C:/docs/c.md", fileViewModel.FilePath);
        Assert.AreEqual(1, commandSink.Messages.Count);
        commandSink.Result = true;
        Assert.IsTrue(fileViewModel.RetryDocumentActivation("C"));
        Assert.AreEqual(2, commandSink.Messages.Count);
        Assert.AreEqual("ActivateDocument", commandSink.Messages[1].Name);
        using var activation = JsonDocument.Parse(JsonSerializer.Serialize(commandSink.Messages[1].Args));
        Assert.AreEqual("C", activation.RootElement.GetProperty("documentId").GetString());
        Assert.AreEqual("# C", activation.RootElement.GetProperty("text").GetString());
    }

    [TestMethod]
    public void ActiveHeadingChange_UpdatesSelectionWithoutFeedbackNavigation()
    {
        var eventCenter = new EventCenter();
        var commandSink = new RecordingEditorCommandSink();
        var services = new ServiceCollection()
            .AddSingleton(eventCenter)
            .AddSingleton<IEditorCommandSink>(commandSink)
            .BuildServiceProvider();
        var editor = (EditorViewModel)RuntimeHelpers.GetUninitializedObject(typeof(EditorViewModel));
        SetAutoProperty(editor, nameof(EditorViewModel.ServiceProvider), services);
        SetAutoProperty(editor, nameof(EditorViewModel.History), new Typedown.Core.Models.ContentHistory());
        SetAutoProperty(editor, nameof(EditorViewModel.Toc), new Typedown.Core.Models.TocTreeItem());
        SetField(editor, "tocSelectionDisposables", new System.Reactive.Disposables.SerialDisposable());
        SetField(editor, "documentId", "B");
        editor.OnStateChange(JObject.Parse("""
            {"documentId":"B","state":{"toc":[{"slug":"source"},{"slug":"target"}],"cur":{"slug":"source"}}}
            """));
        using var subscription = eventCenter.GetObservable<EditorEventArgs>("ActiveHeadingChange")
            .Subscribe(args => editor.OnActiveHeadingChange(args.Args));
        var adapter = new WinUIEditorBridgeAdapter(new WinUIEditorDocumentSession(serviceProvider: services));

        adapter.Receive("""{"type":"message","name":"ActiveHeadingChange","args":{"documentId":"B","cur":{"slug":"target"}}}""", _ => true);

        Assert.IsFalse(editor.ContentState.Toc[0].IsSelected);
        Assert.IsTrue(editor.ContentState.Toc[1].IsSelected);
        Assert.AreEqual(0, commandSink.Messages.Count);

        editor.ContentState.Toc[0].IsSelected = true;

        Assert.AreEqual(1, commandSink.Messages.Count);
        Assert.AreEqual("ScrollTo", commandSink.Messages[0].Name);
        using var scrollArgs = JsonDocument.Parse(JsonSerializer.Serialize(commandSink.Messages[0].Args));
        Assert.AreEqual("source", scrollArgs.RootElement.GetProperty("slug").GetString());
    }

    [TestMethod]
    public void DocumentSession_PendingImportBlocksPersistenceUntilMatchingFinal()
    {
        var path = Path.Combine(Path.GetTempPath(), $"typedown-{Guid.NewGuid():N}.md");
        try
        {
            File.WriteAllText(path, "disk-A");
            var session = new WinUIEditorDocumentSession("A", filePath: path);

            SendMessage(session, """{"name":"MarkdownChange","args":{"text":"B","documentId":"doc","revision":"r1","origin":"import","phase":"provisional"}}""");

            Assert.AreEqual("A", session.State.Text);
            Assert.IsFalse(session.Save().Success);
            Assert.AreEqual("disk-A", File.ReadAllText(path));

            SendMessage(session, """{"name":"MarkdownChange","args":{"text":"stale","documentId":"doc","revision":"old","origin":"import","phase":"final"}}""");
            Assert.AreEqual("A", session.State.Text);

            SendMessage(session, """{"name":"MarkdownChange","args":{"text":"B-prime","documentId":"doc","revision":"r1","origin":"import","phase":"final"}}""");
            Assert.AreEqual("B-prime", session.State.Text);
            Assert.IsTrue(session.Save().Success);
            Assert.AreEqual("B-prime", File.ReadAllText(path));

            SendMessage(session, """{"name":"MarkdownChange","args":{"text":"duplicate","documentId":"doc","revision":"r1","origin":"import","phase":"final"}}""");
            Assert.AreEqual("B-prime", session.State.Text);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public async Task PendingImportGate_RevisionsCancelStaleWaitersAndRequireMatchingFinal()
    {
        var gate = new PendingImportGate();
        gate.Begin("r1");
        var staleWaiter = gate.AcquireAsync(TimeSpan.FromSeconds(1));

        gate.Begin("r2");

        Assert.IsNull(await staleWaiter);
        Assert.IsFalse(gate.Complete("r1"));
        Assert.IsTrue(gate.IsPending);
        Assert.IsTrue(gate.Complete("r2"));
        using var lease = await gate.AcquireAsync(TimeSpan.Zero);
        Assert.IsNotNull(lease);
        Assert.IsTrue(lease.IsValid);
    }

    [TestMethod]
    public async Task PendingImportLease_DoesNotBlockUiEventAndInvalidatesOldGeneration()
    {
        var gate = new PendingImportGate();
        using var lease = await gate.AcquireAsync(TimeSpan.Zero);
        Assert.IsNotNull(lease);

        var beginR2 = Task.Run(() => gate.Begin("r2"));
        Assert.AreSame(beginR2, await Task.WhenAny(beginR2, Task.Delay(500)));
        Assert.IsTrue(gate.IsPending);
        Assert.AreEqual("r2", gate.Revision);
        Assert.IsFalse(lease.IsValid);

        Assert.IsTrue(gate.Complete("r2"));
        lease.Dispose();
        using var r2Lease = await gate.AcquireAsync(TimeSpan.Zero);
        Assert.IsNotNull(r2Lease);
        Assert.IsTrue(r2Lease.IsValid);
    }

    [TestMethod]
    public async Task PendingImportAcquire_UsesBoundedTimeoutAndCancellation()
    {
        var gate = new PendingImportGate();
        using var held = await gate.AcquireAsync(TimeSpan.Zero);
        Assert.IsNotNull(held);

        var stopwatch = Stopwatch.StartNew();
        Assert.IsNull(await gate.AcquireAsync(TimeSpan.Zero));
        Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromMilliseconds(200));

        stopwatch.Restart();
        Assert.IsNull(await gate.AcquireAsync(TimeSpan.FromMilliseconds(75)));
        Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(1));

        using var cancellation = new CancellationTokenSource(50);
        try
        {
            await gate.AcquireAsync(TimeSpan.FromSeconds(5), cancellation.Token);
            Assert.Fail("Expected cancellation.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestMethod]
    public async Task Save_IgnoresOldGenerationAfterWriteCompletes()
    {
        var path = Path.Combine(Path.GetTempPath(), $"typedown-save-{Guid.NewGuid():N}.md");
        var writer = new PausingAtomicFileWriter();
        var editor = CreatePersistenceEditor("old", 11, 22);
        var services = new ServiceCollection()
            .AddSingleton(editor)
            .AddSingleton<IAtomicFileWriter>(writer)
            .BuildServiceProvider();
        var file = CreatePersistenceFile(services, path);

        try
        {
            var save = InvokePrivateAsync<bool>(file, "Save", true, null);
            await writer.Entered.Task;
            editor.PendingImportGate.Begin("r2");
            editor.Markdown = "new";
            editor.CurrentHash = 33;
            Assert.IsTrue(editor.PendingImportGate.Complete("r2"));
            writer.Release.SetResult();

            Assert.IsFalse(await save);
            Assert.AreEqual("new", editor.Markdown);
            Assert.AreEqual(11UL, editor.FileHash);
            Assert.IsFalse(editor.Saved);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public async Task SaveAs_DoesNotSwitchPathAfterGenerationChangesDuringWrite()
    {
        var oldPath = Path.Combine(Path.GetTempPath(), $"typedown-old-{Guid.NewGuid():N}.md");
        var newPath = Path.Combine(Path.GetTempPath(), $"typedown-new-{Guid.NewGuid():N}.md");
        var writer = new PausingAtomicFileWriter();
        var editor = CreatePersistenceEditor("old", 11, 22);
        var picker = new FixedFilePickerService(newPath);
        var services = new ServiceCollection()
            .AddSingleton(editor)
            .AddSingleton<IAtomicFileWriter>(writer)
            .AddSingleton<IFilePickerService>(picker)
            .BuildServiceProvider();
        var file = CreatePersistenceFile(services, oldPath);

        try
        {
            var saveAs = InvokePrivateAsync<string?>(file, "SaveAs", new object?[] { null });
            await writer.Entered.Task;
            editor.PendingImportGate.Begin("r2");
            editor.Markdown = "new";
            editor.CurrentHash = 33;
            Assert.IsTrue(editor.PendingImportGate.Complete("r2"));
            writer.Release.SetResult();

            Assert.IsNull(await saveAs);
            Assert.AreEqual(oldPath, file.FilePath);
            Assert.AreEqual(11UL, editor.FileHash);
            Assert.IsFalse(editor.Saved);
        }
        finally
        {
            if (File.Exists(oldPath)) File.Delete(oldPath);
            if (File.Exists(newPath)) File.Delete(newPath);
        }
    }

    [TestMethod]
    public async Task AutoBackup_DiscardsOldGenerationTemporaryFile()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"typedown-backup-{Guid.NewGuid():N}.md");
        var writer = new PausingAtomicFileWriter();
        var backup = new AutoBackup(writer);
        var editor = CreatePersistenceEditor("old", 11, 22);
        var services = new ServiceCollection()
            .AddSingleton(editor)
            .AddSingleton<IAtomicFileWriter>(writer)
            .AddSingleton(backup)
            .BuildServiceProvider();
        var file = CreatePersistenceFile(services, sourcePath);
        var backupPath = backup.GetBackupFilePath(sourcePath)!;
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        await File.WriteAllTextAsync(backupPath, "new-generation-backup");
        using var lease = await editor.PendingImportGate.AcquireAsync(TimeSpan.Zero);
        Assert.IsNotNull(lease);

        try
        {
            var backupTask = InvokePrivateAsync<bool>(file, "AutoBackupFile", lease);
            await writer.Entered.Task;
            editor.PendingImportGate.Begin("r2");
            editor.Markdown = "new";
            editor.CurrentHash = 33;
            Assert.IsTrue(editor.PendingImportGate.Complete("r2"));
            writer.Release.SetResult();

            Assert.IsFalse(await backupTask);
            Assert.AreEqual("new-generation-backup", await File.ReadAllTextAsync(backupPath));
            Assert.IsFalse(File.Exists(writer.TemporaryPath));
        }
        finally
        {
            if (File.Exists(sourcePath)) File.Delete(sourcePath);
            if (File.Exists(backupPath)) File.Delete(backupPath);
            if (File.Exists(writer.TemporaryPath)) File.Delete(writer.TemporaryPath);
        }
    }

    [TestMethod]
    public void ImportFinal_CommitsPendingEditorHistoryBeforeAuthoritativeReplacement()
    {
        var commandSink = new RecordingEditorCommandSink();
        var services = new ServiceCollection().AddSingleton<IEditorCommandSink>(commandSink).BuildServiceProvider();
        var editor = (EditorViewModel)RuntimeHelpers.GetUninitializedObject(typeof(EditorViewModel));
        var history = new Typedown.Core.Models.ContentHistory();
        history.InitHistory("A");
        SetAutoProperty(editor, nameof(EditorViewModel.ServiceProvider), services);
        SetAutoProperty(editor, nameof(EditorViewModel.History), history);
        SetField(editor, "appliedReplacementRevisions", new Dictionary<string, string>(StringComparer.Ordinal));
        SetField(editor, "appliedReplacementRevisionOrder", new Queue<string>());
        SetField(editor, "documentId", "doc");

        editor.OnMarkdownChange(JObject.Parse("""{"text":"A2","documentId":"doc"}"""));
        editor.OnCursorChange(JObject.Parse("""{"cursor":{"anchor":{"line":0,"ch":2},"focus":{"line":0,"ch":2}},"documentId":"doc"}"""));
        editor.OnMarkdownChange(JObject.Parse("""{"text":"B","documentId":"doc","revision":"r1","origin":"import","phase":"provisional"}"""));
        editor.OnMarkdownChange(JObject.Parse("""{"text":"B-prime","documentId":"doc","revision":"r1","origin":"import","phase":"final"}"""));
        editor.OnCursorChange(JObject.Parse("""{"cursor":{"anchor":{"line":0,"ch":7},"focus":{"line":0,"ch":7}},"documentId":"doc"}"""));
        history.CommitPending();

        Assert.AreEqual("A2", history.Undo()?.Text);
        Assert.AreEqual("B-prime", history.Redo()?.Text);

        editor.OnMarkdownChange(JObject.Parse("""{"text":"C-after-import","documentId":"doc"}"""));
        editor.OnMarkdownChange(JObject.Parse("""{"text":"stale-retry-payload","documentId":"doc","revision":"r1","origin":"import","phase":"final"}"""));

        Assert.AreEqual("C-after-import", editor.Markdown);
        Assert.AreEqual(2, commandSink.Messages.Count(message => message.Name == "ReplacementCommitted"));
    }

    [TestMethod]
    public async Task AsyncSingleOperation_SkipsOverlappingPersistenceTicks()
    {
        var operation = new AsyncSingleOperation();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var persistenceOperations = 0;
        var first = operation.TryRunAsync(async () =>
        {
            Interlocked.Increment(ref persistenceOperations);
            entered.SetResult();
            await release.Task;
        });
        await entered.Task;

        var overlapping = await operation.TryRunAsync(() =>
        {
            Interlocked.Increment(ref persistenceOperations);
            return Task.CompletedTask;
        });
        release.SetResult();

        Assert.IsFalse(overlapping);
        Assert.IsTrue(await first);
        Assert.AreEqual(1, persistenceOperations);
        Assert.IsFalse(operation.IsRunning);
    }

    [TestMethod]
    public void DocumentSession_ResizeTable_ForwardsToRegisteredRemoteInvokeHandler()
    {
        var remoteInvoke = new RemoteInvoke();
        var serviceProvider = new ServiceCollection().AddSingleton(remoteInvoke).BuildServiceProvider();
        var handlerCalled = false;
        using var registration = remoteInvoke.Handle("ResizeTable", () =>
        {
            handlerCalled = true;
            return new { row = 9, column = 10, rows = 9, columns = 10 };
        });
        var session = new WinUIEditorDocumentSession(serviceProvider: serviceProvider);
        var adapter = new WinUIEditorBridgeAdapter(session);

        var resizeTable = Invoke(adapter, """{"type":"invoke","id":"resize","name":"ResizeTable","args":{"rows":6,"columns":7}}""");

        var data = resizeTable.GetProperty("args").GetProperty("data");
        Assert.IsTrue(handlerCalled);
        Assert.AreEqual(0, resizeTable.GetProperty("args").GetProperty("code").GetInt32());
        Assert.AreEqual(9, data.GetProperty("row").GetInt32());
        Assert.AreEqual(10, data.GetProperty("column").GetInt32());
    }

    [TestMethod]
    public void DocumentSession_ResizeTable_RemoteInvokeHandlerExceptionReturnsBridgeError()
    {
        var remoteInvoke = new RemoteInvoke();
        var serviceProvider = new ServiceCollection().AddSingleton(remoteInvoke).BuildServiceProvider();
        using var registration = remoteInvoke.Handle<object>("ResizeTable", () => throw new InvalidOperationException("dialog failed"));
        var session = new WinUIEditorDocumentSession(serviceProvider: serviceProvider);
        var adapter = new WinUIEditorBridgeAdapter(session);

        var resizeTable = Invoke(adapter, """{"type":"invoke","id":"resize","name":"ResizeTable","args":{"rows":6,"columns":7}}""");

        Assert.AreEqual(1, resizeTable.GetProperty("args").GetProperty("code").GetInt32());
        StringAssert.Contains(resizeTable.GetProperty("args").GetProperty("msg").GetString(), "dialog failed");
    }

    [TestMethod]
    public void OpenUri_InvokeAcceptsUriHrefAndStringPayloadShapes()
    {
        var session = new WinUIEditorDocumentSession();
        var adapter = new WinUIEditorBridgeAdapter(session);

        var uriPayload = Invoke(adapter, """{"type":"invoke","id":"open-uri","name":"OpenURI","args":{"uri":""}}""");
        var hrefPayload = Invoke(adapter, """{"type":"invoke","id":"open-href","name":"OpenURI","args":{"href":""}}""");
        var stringPayload = Invoke(adapter, """{"type":"invoke","id":"open-string","name":"OpenURI","args":""}""");

        Assert.AreEqual(0, uriPayload.GetProperty("args").GetProperty("code").GetInt32());
        Assert.IsFalse(uriPayload.GetProperty("args").GetProperty("data").GetBoolean());
        Assert.AreEqual(0, hrefPayload.GetProperty("args").GetProperty("code").GetInt32());
        Assert.IsFalse(hrefPayload.GetProperty("args").GetProperty("data").GetBoolean());
        Assert.AreEqual(0, stringPayload.GetProperty("args").GetProperty("code").GetInt32());
        Assert.IsFalse(stringPayload.GetProperty("args").GetProperty("data").GetBoolean());
        Assert.AreEqual("OpenURI", session.State.LastEventName);
    }

    [TestMethod]
    public void PendingRawMessageQueue_EnforcesCapacityFlushOrderAndRetryDropPolicy()
    {
        var queue = new PendingRawMessageQueue(capacity: 3, maxRetryCount: 2);
        var sent = new List<string>();

        queue.Enqueue("oldest", requireContentLoaded: false);
        queue.Enqueue("first", requireContentLoaded: false);
        queue.Enqueue("second", requireContentLoaded: false);
        queue.Enqueue("third", requireContentLoaded: false);

        queue.Flush(payload =>
        {
            sent.Add(payload);
            return payload != "first";
        }, isContentLoaded: true);

        Assert.AreEqual(3, queue.Count);
        CollectionAssert.AreEqual(new[] { "first" }, sent);

        queue.Flush(payload =>
        {
            sent.Add(payload);
            return payload != "first";
        }, isContentLoaded: true);

        Assert.AreEqual(0, queue.Count);
        CollectionAssert.AreEqual(new[] { "first", "first", "second", "third" }, sent);
    }

    [TestMethod]
    public void OpenUri_MessageIsHandledBySessionWithoutPresentationEventDependency()
    {
        var session = new WinUIEditorDocumentSession();
        var adapter = new WinUIEditorBridgeAdapter(session);

        adapter.Receive("""{"type":"message","name":"OpenURI","args":{}}""", _ => true);

        Assert.AreEqual("OpenURI", adapter.LastEventName);
        Assert.AreEqual("OpenURI", session.State.LastEventName);
    }

    [TestMethod]
    public void EditorHostCommands_SerializeWithProtocolCasingForPhase11Boundary()
    {
        var state = new EditorDocumentState
        {
            Text = "abc",
            BasePath = @"C:\docs"
        };

        var loadFile = JsonDocument.Parse(JsonSerializer.Serialize(EditorHostCommands.CreateLoadFile(state))).RootElement;
        var search = JsonDocument.Parse(JsonSerializer.Serialize(EditorHostCommands.CreateSearch(new EditorSearchRequest
        {
            Value = "needle",
            SearchIsCaseSensitive = true,
            SearchIsWholeWord = true
        }))).RootElement;
        var replace = JsonDocument.Parse(JsonSerializer.Serialize(EditorHostCommands.CreateReplace(new EditorReplaceRequest
        {
            SearchValue = "old",
            Value = "new",
            IsSingle = true
        }))).RootElement;
        var searchOpen = JsonDocument.Parse(JsonSerializer.Serialize(EditorHostCommands.CreateSearchOpenChange(EditorSearchPanelState.Replace))).RootElement;
        var theme = JsonDocument.Parse(JsonSerializer.Serialize(EditorHostCommands.CreateThemeChanged(new EditorThemePayload
        {
            Theme = "Dark",
            AccentColor = new EditorColorPayload(10, 20, 30, 1),
            Background = new EditorColorPayload(40, 50, 60, 1)
        }))).RootElement;
        var settings = JsonDocument.Parse(JsonSerializer.Serialize(EditorHostCommands.CreateSettingsChanged(new EditorSettingsChange("searchIsRegexp", true)))).RootElement;
        var export = JsonDocument.Parse(JsonSerializer.Serialize(EditorHostCommands.CreateExport(new EditorExportRequest
        {
            Type = "pdf",
            Context = new { kind = "print" },
            BasePath = @"C:\docs",
            Title = "note",
            Options = new { toc = true }
        }))).RootElement;

        Assert.AreEqual("LoadFile", loadFile.GetProperty("name").GetString());
        Assert.AreEqual("abc", loadFile.GetProperty("args").GetProperty("text").GetString());
        Assert.AreEqual(@"C:\docs", loadFile.GetProperty("args").GetProperty("basePath").GetString());
        Assert.AreEqual("Search", search.GetProperty("name").GetString());
        Assert.AreEqual("needle", search.GetProperty("args").GetProperty("value").GetString());
        Assert.IsTrue(search.GetProperty("args").GetProperty("opt").GetProperty("searchIsCaseSensitive").GetBoolean());
        Assert.AreEqual("Replace", replace.GetProperty("name").GetString());
        Assert.AreEqual("old", replace.GetProperty("args").GetProperty("searchValue").GetString());
        Assert.AreEqual("new", replace.GetProperty("args").GetProperty("value").GetString());
        Assert.IsTrue(replace.GetProperty("args").GetProperty("isSingle").GetBoolean());
        Assert.AreEqual("SearchOpenChange", searchOpen.GetProperty("name").GetString());
        Assert.AreEqual(2, searchOpen.GetProperty("args").GetProperty("open").GetInt32());
        Assert.AreEqual("ThemeChanged", theme.GetProperty("name").GetString());
        Assert.AreEqual("Dark", theme.GetProperty("args").GetProperty("theme").GetString());
        Assert.AreEqual(10, theme.GetProperty("args").GetProperty("accentColor").GetProperty("R").GetInt32());
        Assert.AreEqual(40, theme.GetProperty("args").GetProperty("background").GetProperty("R").GetInt32());
        Assert.AreEqual(50, theme.GetProperty("args").GetProperty("background").GetProperty("G").GetInt32());
        Assert.AreEqual(60, theme.GetProperty("args").GetProperty("background").GetProperty("B").GetInt32());
        Assert.AreEqual(1, theme.GetProperty("args").GetProperty("background").GetProperty("A").GetDouble());
        Assert.AreEqual("SettingsChanged", settings.GetProperty("name").GetString());
        Assert.IsTrue(settings.GetProperty("args").GetProperty("searchIsRegexp").GetBoolean());
        Assert.AreEqual("Export", export.GetProperty("name").GetString());
        Assert.AreEqual("pdf", export.GetProperty("args").GetProperty("type").GetString());
        Assert.AreEqual(@"C:\docs", export.GetProperty("args").GetProperty("basePath").GetString());
        Assert.AreEqual("note", export.GetProperty("args").GetProperty("title").GetString());
        Assert.IsTrue(export.GetProperty("args").GetProperty("options").GetProperty("toc").GetBoolean());
        Assert.IsFalse(export.GetProperty("args").TryGetProperty("Type", out _));
        Assert.IsFalse(export.GetProperty("args").TryGetProperty("BasePath", out _));
        Assert.IsFalse(settings.TryGetProperty("Name", out _));
        Assert.IsFalse(settings.TryGetProperty("Args", out _));
    }

    [TestMethod]
    public void MarkdownChange_DiffMessageUpdatesSessionDocumentState()
    {
        var session = new WinUIEditorDocumentSession("hello", @"C:\docs");
        var adapter = new WinUIEditorBridgeAdapter(session);

        adapter.Receive("""{"type":"diffmsg","name":"MarkdownChange","diff":false,"args":"{\"text\":\"hello\"}"}""", _ => true);
        adapter.Receive("""{"type":"diffmsg","name":"MarkdownChange","diff":true,"args":" world","start":14,"end":14}""", _ => true);

        Assert.AreEqual("MarkdownChange", adapter.LastEventName);
        Assert.AreEqual(11, session.State.Text.Length);
        Assert.IsFalse(session.State.IsSaved);
        Assert.AreEqual("MarkdownChange", session.State.LastEventName);
        Assert.AreEqual(11, adapter.CurrentMarkdownLength);
    }

    [TestMethod]
    public void FileLoaded_UpdatesLoadedAndSavedFlags()
    {
        var session = new WinUIEditorDocumentSession();
        var adapter = new WinUIEditorBridgeAdapter(session);

        adapter.Receive("""{"type":"message","name":"FileLoaded","args":{"text":"abc","filePath":"C:/docs/a.md","basePath":"C:/docs"}}""", _ => true);

        Assert.IsTrue(session.State.IsLoaded);
        Assert.IsTrue(session.State.IsSaved);
        Assert.AreEqual("abc", session.State.Text);
        Assert.AreEqual("C:/docs/a.md", session.State.FilePath);
        Assert.AreEqual("FileLoaded", session.State.LastEventName);
    }

    [TestMethod]
    public void DocumentSession_LoadSaveAndSaveCopy_PersistRealMarkdownFiles()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "typedown-phase11-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var inputPath = Path.Combine(tempRoot, "input.md");
        var copyPath = Path.Combine(tempRoot, "copy.md");
        File.WriteAllText(inputPath, "# before");

        try
        {
            var session = new WinUIEditorDocumentSession();

            var loadResult = session.LoadFile(inputPath);
            var replaceResult = session.ReplaceFileText("# after");
            var saveResult = session.Save();
            var saveCopyResult = session.SaveAs(copyPath, saveCopy: true);

            Assert.IsTrue(loadResult.Success);
            Assert.AreEqual(inputPath, session.State.FilePath);
            Assert.AreEqual("# before", loadResult.State.Text);
            Assert.IsTrue(loadResult.State.IsSaved);
            Assert.IsTrue(replaceResult.Success);
            Assert.AreEqual("# after", replaceResult.State.Text);
            Assert.IsFalse(replaceResult.State.IsSaved);
            Assert.IsTrue(saveResult.Success);
            Assert.IsTrue(session.State.IsSaved);
            Assert.AreEqual("# after", File.ReadAllText(inputPath));
            Assert.IsTrue(saveCopyResult.Success);
            Assert.AreEqual(copyPath, saveCopyResult.PersistedFilePath);
            Assert.AreEqual(inputPath, session.State.FilePath);
            Assert.AreEqual("# after", File.ReadAllText(copyPath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [TestMethod]
    public void DocumentSession_SaveWithoutPath_FailsSafelyForSmokeDocument()
    {
        var session = new WinUIEditorDocumentSession(initialMarkdown: "smoke");

        var result = session.Save();

        Assert.IsFalse(result.Success);
        Assert.AreEqual("smoke", session.State.Text);
        Assert.IsNull(session.State.FilePath);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
    }

    [TestMethod]
    public void DocumentSession_LoadMissingFile_ReturnsFailureAndPreservesState()
    {
        var session = new WinUIEditorDocumentSession(initialMarkdown: "seed", basePath: @"C:\seed");
        var initialState = session.State;

        var result = session.LoadFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.md"));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(initialState, session.State);
        Assert.AreEqual(initialState, result.State);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
    }

    [TestMethod]
    public void DocumentSession_SaveAsInvalidPath_ReturnsFailureAndPreservesState()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "typedown-phase11-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var session = new WinUIEditorDocumentSession();
            _ = session.ReplaceFileText("body");
            var initialState = session.State;

            var result = session.SaveAs(Path.Combine(tempRoot, "bad\0name.md"));

            Assert.IsFalse(result.Success);
            Assert.AreEqual(initialState, session.State);
            Assert.AreEqual(initialState, result.State);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [TestMethod]
    public void HostController_LoadFileBeforeEditorReady_SendsRealFileStateAfterHandshake()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "typedown-phase11-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var filePath = Path.Combine(tempRoot, "doc.md");
        File.WriteAllText(filePath, "# host-load");

        try
        {
            var session = new WinUIEditorDocumentSession();
            var sink = new RecordingHostSink();
            var controller = new WinUIEditorHostController(session, sink);

            var loadResult = controller.LoadFile(filePath);
            var sentBeforeReady = controller.TrySendLoadFile();
            controller.MarkEditorReady();
            var sentAfterReady = controller.TrySendLoadFile();

            Assert.IsTrue(loadResult.Success);
            Assert.IsFalse(sentBeforeReady);
            Assert.IsTrue(sentAfterReady);
            Assert.AreEqual(1, sink.Messages.Count);
            Assert.AreEqual("LoadFile", sink.Messages[0].Name);
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(sink.Messages[0]));
            Assert.AreEqual("# host-load", document.RootElement.GetProperty("args").GetProperty("text").GetString());
            Assert.AreEqual(Path.GetDirectoryName(filePath), document.RootElement.GetProperty("args").GetProperty("basePath").GetString());
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [TestMethod]
    public void HostController_SaveAndSaveAs_RunThroughSessionBoundary()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "typedown-phase11-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var filePath = Path.Combine(tempRoot, "doc.md");
        var saveAsPath = Path.Combine(tempRoot, "doc-copy.md");
        File.WriteAllText(filePath, "# before");

        try
        {
            var controller = new WinUIEditorHostController(new WinUIEditorDocumentSession(), new RecordingHostSink());

            var loadResult = controller.LoadFile(filePath);
            var replaceResult = controller.ReplaceFileText("# after");
            var saveResult = controller.Save();
            var saveAsResult = controller.SaveAs(saveAsPath, saveCopy: true);

            Assert.IsTrue(loadResult.Success);
            Assert.IsTrue(replaceResult.Success);
            Assert.IsTrue(saveResult.Success);
            Assert.IsTrue(saveAsResult.Success);
            Assert.AreEqual("# after", File.ReadAllText(filePath));
            Assert.AreEqual("# after", File.ReadAllText(saveAsPath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [TestMethod]
    public void EditorHostMessage_SerializesWithProtocolCasing()
    {
        var payload = JsonSerializer.Serialize(new EditorHostMessage("LoadFile", new { text = "abc", basePath = @"C:\docs" }));

        using var document = JsonDocument.Parse(payload);
        Assert.IsTrue(document.RootElement.TryGetProperty("name", out var name));
        Assert.IsTrue(document.RootElement.TryGetProperty("args", out var args));
        Assert.AreEqual("LoadFile", name.GetString());
        Assert.AreEqual("abc", args.GetProperty("text").GetString());
        Assert.IsFalse(document.RootElement.TryGetProperty("Name", out _));
        Assert.IsFalse(document.RootElement.TryGetProperty("Args", out _));
    }

    [TestMethod]
    public void MalformedPayload_DoesNotThrowAndLeavesSessionUsable()
    {
        var session = new WinUIEditorDocumentSession();
        var adapter = new WinUIEditorBridgeAdapter(session);
        var initialLength = adapter.CurrentMarkdownLength;

        adapter.Receive("{not-json", _ => true);

        Assert.AreEqual("MalformedPayload", adapter.LastEventName);
        Assert.AreEqual(initialLength, adapter.CurrentMarkdownLength);
        Assert.AreEqual(initialLength, session.State.Text.Length);
        Assert.AreEqual("Waiting", session.State.LastEventName);
    }

    private static EditorViewModel CreatePersistenceEditor(string markdown, ulong fileHash, ulong currentHash)
    {
        var editor = (EditorViewModel)RuntimeHelpers.GetUninitializedObject(typeof(EditorViewModel));
        editor.Markdown = markdown;
        editor.FileHash = fileHash;
        editor.CurrentHash = currentHash;
        editor.Saved = false;
        return editor;
    }

    private static FileViewModel CreatePersistenceFile(IServiceProvider services, string path)
    {
        var file = (FileViewModel)RuntimeHelpers.GetUninitializedObject(typeof(FileViewModel));
        SetAutoProperty(file, nameof(FileViewModel.ServiceProvider), services);
        SetAutoProperty(file, nameof(FileViewModel.FilePath), path);
        return file;
    }

    private static async Task<T> InvokePrivateAsync<T>(object target, string methodName, params object?[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return await (Task<T>)method.Invoke(target, args)!;
    }

    private static void SetAutoProperty(object target, string propertyName, object value)
    {
        SetField(target, $"<{propertyName}>k__BackingField", value);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.SetValue(target, value);
    }

    private static void SendMessage(WinUIEditorDocumentSession session, string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        session.HandleEditorEvent(new EditorEventMessage(
            root.GetProperty("name").GetString()!,
            root.GetProperty("args").Clone()));
    }

    private static JsonElement Invoke(WinUIEditorBridgeAdapter adapter, string payload)
    {
        string? response = null;
        adapter.Receive(payload, message =>
        {
            response = message;
            return true;
        });

        Assert.IsNotNull(response, $"Expected a response for payload: {payload}");
        using var document = JsonDocument.Parse(response);
        return document.RootElement.Clone();
    }

    private sealed class PausingAtomicFileWriter : IAtomicFileWriter
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string TemporaryPath { get; private set; } = string.Empty;

        public async Task WriteAllTextAsync(string path, string content)
        {
            Entered.TrySetResult();
            await Release.Task;
            await File.WriteAllTextAsync(path, content);
        }

        public async Task<string> WriteTemporaryAsync(string destinationPath, string content)
        {
            TemporaryPath = Path.Combine(Path.GetDirectoryName(destinationPath)!, $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");
            Directory.CreateDirectory(Path.GetDirectoryName(TemporaryPath)!);
            await File.WriteAllTextAsync(TemporaryPath, content);
            Entered.TrySetResult();
            await Release.Task;
            return TemporaryPath;
        }

        public void Commit(string temporaryPath, string destinationPath) => File.Move(temporaryPath, destinationPath, true);

        public void Discard(string? temporaryPath)
        {
            if (!string.IsNullOrEmpty(temporaryPath) && File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private sealed class FixedFilePickerService(string savePath) : IFilePickerService
    {
        public Task<string?> PickOpenFileAsync(OpenFileRequest request) => Task.FromResult<string?>(null);
        public Task<string?> PickSaveFileAsync(SaveFileRequest request) => Task.FromResult<string?>(savePath);
        public Task<string?> PickFolderAsync(PickFolderRequest request) => Task.FromResult<string?>(null);
    }

    private sealed class FakeEditorDocumentSession : IEditorDocumentSession
    {
        public EditorDocumentState State { get; private set; } = new()
        {
            Text = "session-seed",
            BasePath = @"C:\session-base",
            IsSaved = true,
            LastEventName = "Waiting"
        };

        public EditorSettingsSnapshot SettingsSnapshot { get; } = new(new EditorSettingsPayload
        {
            Markdown = "session-seed",
            BasePath = @"C:\session-base",
            FontSize = 18,
            SequenceTheme = "night"
        });

        public object? HandleRemoteInvoke(string name, JsonElement? args)
        {
            return name switch
            {
                "GetSettings" => SettingsSnapshot.Payload,
                "ContentLoaded" => "WinUI editor content loaded.",
                "ExportCallback" => true,
                "PrintHTML" => true,
                "ResizeTable" => new { row = 2, column = 3, rows = 2, columns = 3 },
                "LoadImage" => new { url = "file:///image.png" },
                "SetClipboard" => true,
                "GetStringResources" => new Dictionary<string, string> { ["Bold"] = "Bold", ["Italic"] = "Italic" },
                "OpenNewWindow" => true,
                "UnhandledException" => null,
                "GetCurrentTheme" => new { theme = "Light" },
                _ => throw new InvalidOperationException(name)
            };
        }

        public EditorPersistenceResult LoadFile(string filePath)
        {
            State = State with
            {
                FilePath = filePath,
                BasePath = Path.GetDirectoryName(filePath) ?? State.BasePath,
                IsLoaded = true,
                IsSaved = true,
                LastEventName = "LoadFile"
            };

            return new EditorPersistenceResult(true, State, PersistedFilePath: filePath);
        }

        public EditorPersistenceResult ReplaceFileText(string text, string? filePath = null, string? basePath = null)
        {
            State = State with
            {
                Text = text,
                FilePath = filePath ?? State.FilePath,
                BasePath = basePath ?? State.BasePath,
                IsSaved = false,
                LastEventName = "ReplaceFileText"
            };

            return new EditorPersistenceResult(true, State, PersistedFilePath: State.FilePath);
        }

        public EditorPersistenceResult Save()
        {
            State = State with
            {
                IsSaved = true,
                LastEventName = "Save"
            };

            return new EditorPersistenceResult(true, State, PersistedFilePath: State.FilePath);
        }

        public EditorPersistenceResult SaveAs(string filePath, bool saveCopy = false)
        {
            if (!saveCopy)
            {
                State = State with
                {
                    FilePath = filePath,
                    BasePath = Path.GetDirectoryName(filePath) ?? State.BasePath,
                    IsSaved = true,
                    LastEventName = "SaveAs"
                };
            }

            return new EditorPersistenceResult(true, State, PersistedFilePath: filePath, IsCopy: saveCopy);
        }

        public void HandleEditorEvent(EditorEventMessage message)
        {
            State = State with { LastEventName = message.Name };
        }
    }

    private sealed class RecordingEditorCommandSink : IEditorCommandSink
    {
        public List<(string Name, object? Args)> Messages { get; } = new();
        public bool Result { get; set; } = true;

        public bool Send(string name, object? args)
        {
            Messages.Add((name, args));
            return Result;
        }
    }

    private sealed class RecordingHostSink : IEditorHostSink
    {
        public List<EditorHostMessage> Messages { get; } = new();

        public bool Send(EditorHostMessage message)
        {
            Messages.Add(message);
            return true;
        }
    }
}
