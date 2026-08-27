using System;
using Typedown.Core.Models;

namespace Typedown.Core.Interfaces
{
    /// <summary>
    /// Represents the underlying engine kind of the editor surface.
    /// </summary>
    public enum EditorEngineKind
    {
        /// <summary>
        /// Transitional WebView2 + Muya JavaScript editor surface.
        /// </summary>
        WebView2Muya = 0,

        /// <summary>
        /// 100% C# / WinUI 3 native editor surface.
        /// </summary>
        Native = 1
    }

    /// <summary>
    /// Represents a zero-based line/column position and character offset in the document.
    /// </summary>
    public readonly record struct TextPosition(int Line, int Column, int Offset = 0)
    {
        public static TextPosition Zero => new(0, 0, 0);

        public override string ToString() => $"Ln {Line + 1}, Col {Column + 1} (Offset {Offset})";
    }

    /// <summary>
    /// Represents a text selection range with anchor, focus, and collapsed state.
    /// </summary>
    public record TextSelectionRange(TextPosition Anchor, TextPosition Focus, bool IsCollapsed = true, string SelectedText = "")
    {
        public static TextSelectionRange Empty => new(TextPosition.Zero, TextPosition.Zero, true, string.Empty);

        public bool HasSelection => !IsCollapsed && !string.IsNullOrEmpty(SelectedText);
    }

    /// <summary>
    /// Options for search and replace operations.
    /// </summary>
    public class EditorSearchOptions
    {
        public bool CaseSensitive { get; set; }
        public bool WholeWord { get; set; }
        public bool UseRegex { get; set; }
        public bool SearchUp { get; set; }
    }

    /// <summary>
    /// Event arguments for text content changes.
    /// </summary>
    public class EditorTextChangedEventArgs : EventArgs
    {
        public string Text { get; }
        public string? Revision { get; }
        public string? Origin { get; }
        public ulong Hash { get; }
        public bool IsDirty { get; }

        public EditorTextChangedEventArgs(string text, string? revision = null, string? origin = null, ulong hash = 0, bool isDirty = true)
        {
            Text = text;
            Revision = revision;
            Origin = origin;
            Hash = hash;
            IsDirty = isDirty;
        }
    }

    /// <summary>
    /// Event arguments for selection changes.
    /// </summary>
    public class EditorSelectionChangedEventArgs : EventArgs
    {
        public TextSelectionRange Selection { get; }
        public string SelectedText => Selection.SelectedText;
        public bool HasSelection => Selection.HasSelection;

        public EditorSelectionChangedEventArgs(TextSelectionRange selection)
        {
            Selection = selection;
        }
    }

    /// <summary>
    /// Event arguments for cursor position changes.
    /// </summary>
    public class EditorCursorChangedEventArgs : EventArgs
    {
        public TextPosition Position { get; }
        public string? BlockType { get; }

        public EditorCursorChangedEventArgs(TextPosition position, string? blockType = null)
        {
            Position = position;
            BlockType = blockType;
        }
    }

    /// <summary>
    /// Event arguments for undo/redo history availability changes.
    /// </summary>
    public class EditorHistoryChangedEventArgs : EventArgs
    {
        public bool CanUndo { get; }
        public bool CanRedo { get; }

        public EditorHistoryChangedEventArgs(bool canUndo, bool canRedo)
        {
            CanUndo = canUndo;
            CanRedo = canRedo;
        }
    }

    /// <summary>
    /// Event arguments for content state (TOC, word count) changes.
    /// </summary>
    public class EditorContentStateChangedEventArgs : EventArgs
    {
        public ContentState State { get; }

        public EditorContentStateChangedEventArgs(ContentState state)
        {
            State = state;
        }
    }

    /// <summary>
    /// Event arguments for format state changes.
    /// </summary>
    public class EditorFormatStateChangedEventArgs : EventArgs
    {
        public FormatState State { get; }

        public EditorFormatStateChangedEventArgs(FormatState state)
        {
            State = state;
        }
    }

    /// <summary>
    /// Event arguments for paragraph/block state changes.
    /// </summary>
    public class EditorParagraphStateChangedEventArgs : EventArgs
    {
        public ParagraphState State { get; }

        public EditorParagraphStateChangedEventArgs(ParagraphState state)
        {
            State = state;
        }
    }

    /// <summary>
    /// Event arguments for scroll changes.
    /// </summary>
    public class EditorScrollChangedEventArgs : EventArgs
    {
        public ScrollState State { get; }

        public EditorScrollChangedEventArgs(ScrollState state)
        {
            State = state;
        }
    }

    /// <summary>
    /// Event arguments when the editor surface completes loading and readiness.
    /// </summary>
    public class EditorLoadedEventArgs : EventArgs
    {
        public string DocumentId { get; }
        public EditorEngineKind EngineKind { get; }

        public EditorLoadedEventArgs(string documentId, EditorEngineKind engineKind)
        {
            DocumentId = documentId;
            EngineKind = engineKind;
        }
    }

    /// <summary>
    /// Event arguments for context menu requests on the editor surface.
    /// </summary>
    public class EditorContextMenuEventArgs : EventArgs
    {
        public double X { get; }
        public double Y { get; }
        public string? TargetType { get; }

        public EditorContextMenuEventArgs(double x, double y, string? targetType = null)
        {
            X = x;
            Y = y;
            TargetType = targetType;
        }
    }
}
