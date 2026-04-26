using System;
using System.Collections.Generic;

namespace Typedown.Core.Contracts.Editor
{
    public static class EditorHostCommands
    {
        public static EditorHostMessage CreateLoadFile(EditorDocumentState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            return new EditorHostMessage("LoadFile", new
            {
                text = state.Text,
                basePath = state.BasePath
            });
        }

        public static EditorHostMessage CreateReplaceFileText(string text, string? basePath = null)
        {
            return new EditorHostMessage("ReplaceFileText", new
            {
                text,
                basePath
            });
        }

        public static EditorHostMessage CreateSearch(EditorSearchRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new EditorHostMessage("Search", new
            {
                value = request.Value,
                opt = new
                {
                    searchIsCaseSensitive = request.SearchIsCaseSensitive,
                    searchIsWholeWord = request.SearchIsWholeWord,
                    searchIsRegexp = request.SearchIsRegexp,
                    selection = request.Selection
                }
            });
        }

        public static EditorHostMessage CreateReplace(EditorReplaceRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new EditorHostMessage("Replace", new
            {
                searchValue = request.SearchValue,
                value = request.Value,
                isSingle = request.IsSingle,
                opt = new
                {
                    searchIsCaseSensitive = request.SearchIsCaseSensitive,
                    searchIsWholeWord = request.SearchIsWholeWord,
                    searchIsRegexp = request.SearchIsRegexp,
                    selection = request.Selection
                }
            });
        }

        public static EditorHostMessage CreateSearchOpenChange(EditorSearchPanelState state)
        {
            return new EditorHostMessage("SearchOpenChange", new
            {
                open = (int)state
            });
        }

        public static EditorHostMessage CreateThemeChanged(EditorThemePayload payload)
        {
            ArgumentNullException.ThrowIfNull(payload);
            return new EditorHostMessage("ThemeChanged", payload);
        }

        public static EditorHostMessage CreateSettingsChanged(EditorSettingsChange change)
        {
            ArgumentNullException.ThrowIfNull(change);

            return new EditorHostMessage("SettingsChanged", new Dictionary<string, object?>
            {
                [change.Name] = change.Value
            });
        }

        public static EditorHostMessage CreateExport(EditorExportRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            return new EditorHostMessage("Export", request);
        }
    }
}
