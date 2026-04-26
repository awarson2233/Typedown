using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Contracts.Editor;
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

        public void HandleEditorEvent(EditorEventMessage message)
        {
            State = State with { LastEventName = message.Name };
        }
    }
}
