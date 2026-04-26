using System;

namespace Typedown.Core.Contracts.Editor
{
    public static class EditorUiCommands
    {
        public static EditorUiCommand CreateUndo()
        {
            return NoArgs(EditorUiCommandNames.Undo);
        }

        public static EditorUiCommand CreateRedo()
        {
            return NoArgs(EditorUiCommandNames.Redo);
        }

        public static EditorUiCommand CreateCut(string type)
        {
            return new EditorUiCommand(EditorUiCommandNames.Cut, new EditorClipboardCommandRequest(type));
        }

        public static EditorUiCommand CreateCopy(string type)
        {
            return new EditorUiCommand(EditorUiCommandNames.Copy, new EditorClipboardCommandRequest(type));
        }

        public static EditorUiCommand CreatePaste(string type, string text, string html)
        {
            return new EditorUiCommand(EditorUiCommandNames.Paste, new EditorPasteCommandRequest(type, text, html));
        }

        public static EditorUiCommand CreateDelete()
        {
            return NoArgs(EditorUiCommandNames.Delete);
        }

        public static EditorUiCommand CreateSelectAll()
        {
            return NoArgs(EditorUiCommandNames.SelectAll);
        }

        public static EditorUiCommand CreateFind(string action)
        {
            return new EditorUiCommand(EditorUiCommandNames.Find, new EditorFindActionRequest(action));
        }

        public static EditorUiCommand CreateFormat(string type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return new EditorUiCommand(EditorUiCommandNames.Format, type);
        }

        public static EditorUiCommand CreateUpdateParagraph(string type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return new EditorUiCommand(EditorUiCommandNames.UpdateParagraph, type);
        }

        public static EditorUiCommand CreateInsertParagraph(string type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return new EditorUiCommand(EditorUiCommandNames.InsertParagraph, type);
        }

        public static EditorUiCommand CreateDeleteParagraph()
        {
            return NoArgs(EditorUiCommandNames.DeleteParagraph);
        }

        public static EditorUiCommand CreateDuplicate()
        {
            return NoArgs(EditorUiCommandNames.Duplicate);
        }

        public static EditorUiCommand CreateInsertTable(int rows, int columns)
        {
            return new EditorUiCommand(EditorUiCommandNames.InsertTable, new EditorInsertTableRequest(rows, columns));
        }

        public static EditorUiCommand CreateSearchOpenChange(EditorSearchPanelState state)
        {
            return new EditorUiCommand(EditorUiCommandNames.SearchOpenChange, new EditorSearchOpenChangeRequest((int)state));
        }

        public static EditorUiCommand CreateScrollTo(string slug)
        {
            return new EditorUiCommand(EditorUiCommandNames.ScrollTo, new EditorScrollToRequest(slug));
        }

        private static EditorUiCommand NoArgs(string name)
        {
            return new EditorUiCommand(name, null);
        }
    }
}
