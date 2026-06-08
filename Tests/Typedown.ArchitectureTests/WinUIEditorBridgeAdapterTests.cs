using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Services;
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
