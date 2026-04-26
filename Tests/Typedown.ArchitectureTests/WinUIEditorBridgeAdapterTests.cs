using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.WinUI.Controls;

namespace Typedown.ArchitectureTests;

[TestClass]
public class WinUIEditorBridgeAdapterTests
{
    [TestMethod]
    public void InvokeHandlers_ReturnSuccessForSmokeSafeCommands()
    {
        var adapter = new WinUIEditorBridgeAdapter(smokeMarkdown: "seed", basePath: @"C:\bundle");

        var getSettings = Invoke(adapter, """{"type":"invoke","id":"1","name":"GetSettings"}""");
        var getCurrentTheme = Invoke(adapter, """{"type":"invoke","id":"2","name":"GetCurrentTheme"}""");
        var setClipboard = Invoke(adapter, """{"type":"invoke","id":"3","name":"SetClipboard","args":{"type":"text/plain","data":"hello"}}""");
        var resizeTable = Invoke(adapter, """{"type":"invoke","id":"4","name":"ResizeTable","args":{"row":2,"column":3}}""");
        var unhandledException = Invoke(adapter, """{"type":"invoke","id":"5","name":"UnhandledException","args":"boom"}""");

        Assert.AreEqual(0, getSettings.GetProperty("args").GetProperty("code").GetInt32());
        Assert.AreEqual(@"C:\bundle", getSettings.GetProperty("args").GetProperty("data").GetProperty("basePath").GetString());
        Assert.AreEqual(0, getCurrentTheme.GetProperty("args").GetProperty("code").GetInt32());
        Assert.AreEqual("Light", getCurrentTheme.GetProperty("args").GetProperty("data").GetProperty("theme").GetString());
        Assert.AreEqual(0, setClipboard.GetProperty("args").GetProperty("code").GetInt32());
        Assert.IsTrue(setClipboard.GetProperty("args").GetProperty("data").GetBoolean());
        Assert.AreEqual(0, resizeTable.GetProperty("args").GetProperty("code").GetInt32());
        Assert.AreEqual(2, resizeTable.GetProperty("args").GetProperty("data").GetProperty("row").GetInt32());
        Assert.AreEqual(3, resizeTable.GetProperty("args").GetProperty("data").GetProperty("column").GetInt32());
        Assert.AreEqual(0, unhandledException.GetProperty("args").GetProperty("code").GetInt32());
    }

    [TestMethod]
    public void InvokeHandlers_CoverRemainingRemoteCommandsWithoutRejecting()
    {
        var adapter = new WinUIEditorBridgeAdapter(smokeMarkdown: "seed", basePath: @"C:\bundle");

        var contentLoaded = Invoke(adapter, """{"type":"invoke","id":"1","name":"ContentLoaded"}""");
        var exportCallback = Invoke(adapter, """{"type":"invoke","id":"2","name":"ExportCallback","args":{"html":"<p>x</p>","context":{"kind":"pdf"}}}""");
        var printHtml = Invoke(adapter, """{"type":"invoke","id":"3","name":"PrintHTML","args":{"html":"<p>x</p>","context":{"kind":"print"}}}""");
        var loadImage = Invoke(adapter, """{"type":"invoke","id":"4","name":"LoadImage","args":{"url":"file:///image.png","width":10,"height":20}}""");
        var getStringResources = Invoke(adapter, """{"type":"invoke","id":"5","name":"GetStringResources","args":{"names":["Bold","Italic"]}}""");
        var openNewWindow = Invoke(adapter, """{"type":"invoke","id":"6","name":"OpenNewWindow","args":"https://example.com"}""");

        Assert.AreEqual(0, contentLoaded.GetProperty("args").GetProperty("code").GetInt32());
        Assert.IsTrue(adapter.IsContentLoaded);
        Assert.AreEqual(0, exportCallback.GetProperty("args").GetProperty("code").GetInt32());
        Assert.IsTrue(exportCallback.GetProperty("args").GetProperty("data").GetBoolean());
        Assert.AreEqual(0, printHtml.GetProperty("args").GetProperty("code").GetInt32());
        Assert.IsTrue(printHtml.GetProperty("args").GetProperty("data").GetBoolean());
        Assert.AreEqual(0, loadImage.GetProperty("args").GetProperty("code").GetInt32());
        Assert.AreEqual("file:///image.png", loadImage.GetProperty("args").GetProperty("data").GetProperty("url").GetString());
        Assert.AreEqual("Bold", getStringResources.GetProperty("args").GetProperty("data").GetProperty("Bold").GetString());
        Assert.AreEqual(0, openNewWindow.GetProperty("args").GetProperty("code").GetInt32());
    }

    [TestMethod]
    public void DiffMessage_RebuildsMarkdownLengthFromPatchedPayload()
    {
        var adapter = new WinUIEditorBridgeAdapter();

        adapter.Receive("""{"type":"diffmsg","name":"MarkdownChange","diff":false,"args":"{\"text\":\"hello\"}"}""", _ => true);
        adapter.Receive("""{"type":"diffmsg","name":"MarkdownChange","diff":true,"args":" world","start":14,"end":14}""", _ => true);

        Assert.AreEqual("MarkdownChange", adapter.LastEventName);
        Assert.AreEqual(11, adapter.CurrentMarkdownLength);
    }

    [TestMethod]
    public void MalformedPayload_DoesNotThrowAndUpdatesState()
    {
        var adapter = new WinUIEditorBridgeAdapter();

        adapter.Receive("{not-json", _ => true);

        Assert.AreEqual("MalformedPayload", adapter.LastEventName);
        Assert.AreEqual(0, adapter.CurrentMarkdownLength);
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
}
