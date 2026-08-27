using System;
using System.Threading;
using System.Threading.Tasks;
using Typedown.Core.Models;
using Typedown.Core.Models.RuntimeModels;

namespace Typedown.Core.Interfaces
{
    /// <summary>
    /// Unified abstraction seam for editor surfaces (AD-8).
    /// Both WebView2MuyaEditorSurface (transitional legacy) and NativeEditorSurface (target 100% C# / WinUI 3) implement this contract.
    /// </summary>
    public interface IEditorSurface : IDisposable
    {
        #region Engine & Document Metadata

        /// <summary>
        /// Gets the underlying engine kind of this editor surface.
        /// </summary>
        EditorEngineKind EngineKind { get; }

        /// <summary>
        /// Gets whether the editor surface has finished initialization and document loading.
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Gets or sets whether the editor is in read-only mode.
        /// </summary>
        bool IsReadOnly { get; set; }

        /// <summary>
        /// Gets or sets whether the document has unsaved modifications.
        /// </summary>
        bool IsDirty { get; set; }

        /// <summary>
        /// Gets the unique identifier of the currently loaded document.
        /// </summary>
        string DocumentId { get; }

        /// <summary>
        /// Gets or sets the file baseline hash.
        /// </summary>
        ulong FileHash { get; set; }

        /// <summary>
        /// Gets the current content hash.
        /// </summary>
        ulong CurrentHash { get; }

        #endregion

        #region Document & Text Loading / Access

        /// <summary>
        /// Loads markdown content into the editor surface.
        /// </summary>
        Task LoadMarkdownAsync(string markdown, string? filePath = null, string? basePath = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the current markdown text from the editor surface asynchronously.
        /// </summary>
        Task<string> GetMarkdownAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the markdown text directly.
        /// </summary>
        void SetMarkdown(string markdown, string? origin = null);

        /// <summary>
        /// Gets the current markdown text synchronously.
        /// </summary>
        string Markdown { get; }

        /// <summary>
        /// Clears all content from the editor.
        /// </summary>
        void Clear();

        #endregion

        #region Selection & Cursor

        /// <summary>
        /// Gets or sets the current text selection range.
        /// </summary>
        TextSelectionRange Selection { get; set; }

        /// <summary>
        /// Gets or sets the current cursor position.
        /// </summary>
        TextPosition CursorPosition { get; set; }

        /// <summary>
        /// Gets the currently selected text.
        /// </summary>
        string SelectedText { get; }

        /// <summary>
        /// Gets whether a non-empty text selection currently exists.
        /// </summary>
        bool HasSelection { get; }

        /// <summary>
        /// Sets the selection to the specified text positions.
        /// </summary>
        void Select(TextPosition start, TextPosition end);

        /// <summary>
        /// Selects all content in the editor.
        /// </summary>
        void SelectAll();

        /// <summary>
        /// Collapses the selection to the current cursor position.
        /// </summary>
        void CollapseSelection();

        #endregion

        #region History (Undo / Redo)

        /// <summary>
        /// Gets whether undo is currently available.
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// Gets whether redo is currently available.
        /// </summary>
        bool CanRedo { get; }

        /// <summary>
        /// Performs an undo operation.
        /// </summary>
        void Undo();

        /// <summary>
        /// Performs a redo operation.
        /// </summary>
        void Redo();

        /// <summary>
        /// Clears the undo/redo history stack.
        /// </summary>
        void ClearHistory();

        #endregion

        #region Clipboard & Text Operations

        /// <summary>
        /// Cuts the current selection to the clipboard.
        /// </summary>
        void Cut();

        /// <summary>
        /// Copies the current selection to the clipboard.
        /// </summary>
        void Copy();

        /// <summary>
        /// Pastes clipboard content into the current cursor position.
        /// </summary>
        Task PasteAsync();

        /// <summary>
        /// Deletes the currently selected text or block.
        /// </summary>
        void DeleteSelection();

        /// <summary>
        /// Inserts raw text at the current cursor position.
        /// </summary>
        void InsertText(string text);

        /// <summary>
        /// Inserts an image tag at the current cursor position.
        /// </summary>
        void InsertImage(HtmlImgTag image);

        /// <summary>
        /// Inserts an image with source, alt text, and title.
        /// </summary>
        void InsertImage(string src, string? alt = null, string? title = null);

        #endregion

        #region Formatting & Paragraph Commands

        /// <summary>
        /// Toggles bold formatting on the current selection.
        /// </summary>
        void ToggleBold();

        /// <summary>
        /// Toggles italic formatting on the current selection.
        /// </summary>
        void ToggleItalic();

        /// <summary>
        /// Toggles underline formatting on the current selection.
        /// </summary>
        void ToggleUnderline();

        /// <summary>
        /// Toggles strikethrough formatting on the current selection.
        /// </summary>
        void ToggleStrikethrough();

        /// <summary>
        /// Toggles highlight formatting on the current selection.
        /// </summary>
        void ToggleHighlight();

        /// <summary>
        /// Toggles inline code formatting on the current selection.
        /// </summary>
        void ToggleInlineCode();

        /// <summary>
        /// Toggles inline math formatting on the current selection.
        /// </summary>
        void ToggleInlineMath();

        /// <summary>
        /// Inserts a hyperlink at the current selection.
        /// </summary>
        void InsertLink(string url, string? text = null);

        /// <summary>
        /// Clears inline formatting from the current selection.
        /// </summary>
        void ClearFormat();

        /// <summary>
        /// Sets the current block to a normal paragraph.
        /// </summary>
        void SetParagraph();

        /// <summary>
        /// Sets the current block to an ATX heading with the specified level (1-6).
        /// </summary>
        void SetHeading(int level);

        /// <summary>
        /// Sets the current block to a fenced code block.
        /// </summary>
        void SetCodeBlock(string? language = null);

        /// <summary>
        /// Sets the current block to a math display block.
        /// </summary>
        void SetMathBlock();

        /// <summary>
        /// Sets the current block to a blockquote.
        /// </summary>
        void SetQuoteBlock();

        /// <summary>
        /// Sets the current block to an ordered list.
        /// </summary>
        void SetOrderList();

        /// <summary>
        /// Sets the current block to an unordered/bullet list.
        /// </summary>
        void SetBulletList();

        /// <summary>
        /// Sets the current block to a task/checklist.
        /// </summary>
        void SetTaskList();

        /// <summary>
        /// Inserts a table with the specified rows and columns.
        /// </summary>
        void SetTable(int rows, int cols);

        /// <summary>
        /// Inserts a thematic break / horizontal rule.
        /// </summary>
        void SetHorizontalLine();

        /// <summary>
        /// Inserts or toggles YAML front matter.
        /// </summary>
        void SetFrontMatter();

        /// <summary>
        /// Inserts a footnote reference.
        /// </summary>
        void SetFootnote();

        /// <summary>
        /// Dispatches a generic or extended editor command.
        /// </summary>
        bool ExecuteCommand(string commandName, object? parameter = null);

        #endregion

        #region Search & Navigation

        /// <summary>
        /// Executes a search with the given search query and options.
        /// </summary>
        void Find(string text, EditorSearchOptions? options = null);

        /// <summary>
        /// Navigates to the next search match.
        /// </summary>
        void FindNext();

        /// <summary>
        /// Navigates to the previous search match.
        /// </summary>
        void FindPrevious();

        /// <summary>
        /// Replaces the current search match with replacement text.
        /// </summary>
        void Replace(string text, string replacement, EditorSearchOptions? options = null);

        /// <summary>
        /// Replaces all search matches with replacement text.
        /// </summary>
        void ReplaceAll(string text, string replacement, EditorSearchOptions? options = null);

        /// <summary>
        /// Clears active search highlighting.
        /// </summary>
        void ClearSearch();

        /// <summary>
        /// Scrolls the editor to the heading corresponding to the slug.
        /// </summary>
        void ScrollToSlug(string slug);

        /// <summary>
        /// Scrolls the editor to the specified line number.
        /// </summary>
        void ScrollToLine(int line);

        /// <summary>
        /// Scrolls the editor to the specified text position.
        /// </summary>
        void ScrollToPosition(TextPosition position);

        /// <summary>
        /// Gets or sets the scroll viewport state.
        /// </summary>
        ScrollState ScrollState { get; set; }

        /// <summary>
        /// Focuses the editor surface.
        /// </summary>
        void Focus();

        #endregion

        #region Runtime State Snapshots

        /// <summary>
        /// Gets the current content state (TOC, word count).
        /// </summary>
        ContentState ContentState { get; }

        /// <summary>
        /// Gets the current format state at the active cursor/selection.
        /// </summary>
        FormatState FormatState { get; }

        /// <summary>
        /// Gets the current paragraph menu state.
        /// </summary>
        ParagraphState ParagraphState { get; }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the editor surface finishes loading and becomes ready.
        /// </summary>
        event EventHandler<EditorLoadedEventArgs>? Loaded;

        /// <summary>
        /// Occurs when the markdown text content changes.
        /// </summary>
        event EventHandler<EditorTextChangedEventArgs>? TextChanged;

        /// <summary>
        /// Occurs when the text selection range changes.
        /// </summary>
        event EventHandler<EditorSelectionChangedEventArgs>? SelectionChanged;

        /// <summary>
        /// Occurs when the cursor position changes.
        /// </summary>
        event EventHandler<EditorCursorChangedEventArgs>? CursorChanged;

        /// <summary>
        /// Occurs when undo/redo availability changes.
        /// </summary>
        event EventHandler<EditorHistoryChangedEventArgs>? HistoryChanged;

        /// <summary>
        /// Occurs when TOC or word count content state changes.
        /// </summary>
        event EventHandler<EditorContentStateChangedEventArgs>? ContentStateChanged;

        /// <summary>
        /// Occurs when inline format state at selection changes.
        /// </summary>
        event EventHandler<EditorFormatStateChangedEventArgs>? FormatStateChanged;

        /// <summary>
        /// Occurs when paragraph/block state changes.
        /// </summary>
        event EventHandler<EditorParagraphStateChangedEventArgs>? ParagraphStateChanged;

        /// <summary>
        /// Occurs when scroll viewport offset or dimensions change.
        /// </summary>
        event EventHandler<EditorScrollChangedEventArgs>? ScrollChanged;

        /// <summary>
        /// Occurs when a context menu is requested on the editor surface.
        /// </summary>
        event EventHandler<EditorContextMenuEventArgs>? ContextMenuRequested;

        #endregion
    }
}
