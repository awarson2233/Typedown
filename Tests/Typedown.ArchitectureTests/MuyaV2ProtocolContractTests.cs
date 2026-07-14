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
