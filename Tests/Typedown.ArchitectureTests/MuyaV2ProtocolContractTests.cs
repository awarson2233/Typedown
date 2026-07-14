using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Utilities;

namespace Typedown.ArchitectureTests;

[TestClass]
public sealed class MuyaV2ProtocolContractTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void CfHtmlFragmentExtraction_HandlesCommentsOffsetsAndBodyAttributes()
    {
        Assert.AreEqual("<b>fragment</b>", Common.ExtractHtmlFragment("header<!--StartFragment--><b>fragment</b><!--EndFragment-->tail"));
        Assert.AreEqual("<i>body</i>", Common.ExtractHtmlFragment("<html><body class=\"office\"><i>body</i></body></html>"));

        const string fragment = "<strong>offset</strong>";
        var prefix = "Version:1.0\r\nStartFragment:0000000000\r\nEndFragment:0000000000\r\n";
        var start = System.Text.Encoding.UTF8.GetByteCount(prefix);
        var end = start + System.Text.Encoding.UTF8.GetByteCount(fragment);
        var cfHtml = prefix.Replace("StartFragment:0000000000", $"StartFragment:{start:D10}", StringComparison.Ordinal)
            .Replace("EndFragment:0000000000", $"EndFragment:{end:D10}", StringComparison.Ordinal) + fragment;
        Assert.AreEqual(fragment, Common.ExtractHtmlFragment(cfHtml));
    }

    [TestMethod]
    public void MuyaHost_PreservesDocumentAndClipboardProtocols()
    {
        var host = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Editor", "src", "components", "Muya", "index.tsx"));
        var editor = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Editor", "src", "components", "Editor", "index.tsx"));
        var viewModel = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "ViewModels", "EditorViewModel.cs"));

        StringAssert.Contains(host, "editor.flush()\n        const outgoingMarkdown = editor.getMarkdown()");
        StringAssert.Contains(host, "DocumentFlushed', { documentId: outgoingId, nextDocumentId: props.pendingDocument.id }");
        StringAssert.Contains(host, "if (!editor || loadedDocumentIdRef.current === props.documentId) return");
        StringAssert.Contains(host, "FileLoaded', { text: markdownRef.current, documentId: props.documentId }");
        StringAssert.Contains(editor, "setPendingDocument({ text, id: nextId })");
        StringAssert.Contains(host, "else document.execCommand('copy')");
        StringAssert.Contains(host, "editor.pastePlainText(arg?.text ?? '')");
        StringAssert.Contains(viewModel, "Selection[\"isCollapsed\"]");
        StringAssert.Contains(host, "ActiveHeadingChange");
        StringAssert.Contains(viewModel, "OnActiveHeadingChange");
        StringAssert.Contains(viewModel, "FileViewModel.ActivatePendingDocument");
        StringAssert.Contains(viewModel, "History.InitHistory(markdown)");
        StringAssert.Contains(editor, "transport.addListener<{ text: string, basePath: string, documentId: string }>('ActivateDocument'");
        StringAssert.Contains(host, "loadedDocumentIdRef.current = props.documentId");
        StringAssert.Contains(editor, "if (documentIdRef.current) {");
        Assert.IsFalse(editor.Contains("!optionsRef.current?.sourceCode", StringComparison.Ordinal));
        var codeMirror = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Editor", "src", "components", "CodeMirror", "index.tsx"));
        StringAssert.Contains(codeMirror, "DocumentFlushed', { documentId: outgoingId, nextDocumentId: props.pendingDocument.id }");
        StringAssert.Contains(codeMirror, "FileLoaded', { text: markdownRef.current, documentId: props.documentId }");
        var markdownIndex = codeMirror.IndexOf("transport.postMessage('MarkdownChange', { text: markdownRef.current, documentId: outgoingId })", StringComparison.Ordinal);
        var flushIndex = codeMirror.IndexOf("transport.postMessageNoDiff('DocumentFlushed'", StringComparison.Ordinal);
        Assert.IsTrue(markdownIndex >= 0 && flushIndex > markdownIndex);
        StringAssert.Contains(codeMirror, "const { anchor, focus: head } = props.cursor ?? {}");
        StringAssert.Contains(editor, "setReplacement({ documentId: currentDocumentId, text, cursor: nextCursor, origin");
        StringAssert.Contains(editor, "current?.documentId === documentId && current.revision === revision ? undefined");
        StringAssert.Contains(host, "replacement.documentId !== props.documentId");
        StringAssert.Contains(codeMirror, "replacement.documentId !== props.documentId");
        StringAssert.Contains(host, "editor.setContent(replacement.text)");
        StringAssert.Contains(codeMirror, "editor.setValue(replacement.text)");
        StringAssert.Contains(editor, "origin === 'import') transport.postMessage('MarkdownChange', { text, documentId: currentDocumentId, revision, origin, phase: 'provisional' }");
        StringAssert.Contains(host, "revision: replacement.revision, origin: replacement.origin");
        StringAssert.Contains(editor, "(globalThis.crypto as any)?.randomUUID?.()");
        StringAssert.Contains(editor, "transport.addListener<{ documentId: string, revision: string }>('ReplacementCommitted'");
        StringAssert.Contains(viewModel, "appliedReplacementRevisions.ContainsKey(revisionKey)");
        StringAssert.Contains(viewModel, "appliedReplacementRevisions[revisionKey] = markdown");
        StringAssert.Contains(viewModel, "ReplacementRevisionWindow = 32");
        StringAssert.Contains(viewModel, "appliedReplacementRevisions.Remove(appliedReplacementRevisionOrder.Dequeue())");
        StringAssert.Contains(viewModel, "origin is not \"undo\" and not \"redo\"");
        StringAssert.Contains(editor, "phase: 'provisional'");
        StringAssert.Contains(host, "phase: 'final'");
        StringAssert.Contains(codeMirror, "phase: 'final'");
        StringAssert.Contains(viewModel, "phase == \"provisional\"");
        Assert.IsTrue(viewModel.IndexOf("appliedReplacementRevisions.ContainsKey(revisionKey)", StringComparison.Ordinal)
            < viewModel.IndexOf("phase == \"final\" && revision != PendingImportGate.Revision", StringComparison.Ordinal));
        StringAssert.Contains(viewModel, "phase == \"final\" && revision != PendingImportGate.Revision");
        StringAssert.Contains(viewModel, "WaitForPendingImportAsync");
        StringAssert.Contains(viewModel, "History.CommitPending()");
        StringAssert.Contains(viewModel, "text = Markdown, hash = CurrentHash");
        Assert.IsFalse(host.Contains("props.onReplacementConsumed(replacement.documentId, replacement.revision)", StringComparison.Ordinal));
        Assert.IsFalse(codeMirror.Contains("props.onReplacementConsumed(replacement.documentId, replacement.revision)", StringComparison.Ordinal));
        StringAssert.Contains(host, "retryCount >= 5");
        StringAssert.Contains(codeMirror, "retryCount >= 5");
        StringAssert.Contains(host, "SelectionFormats', { formats: live.formats, documentId: props.documentId }");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Typedown.sln"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate Typedown.sln.");
    }
}
