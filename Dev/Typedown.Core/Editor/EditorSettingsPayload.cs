using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.Editor
{
    public sealed record EditorSettingsPayload
    {
        [JsonPropertyName("markdown")]
        public string Markdown { get; init; } = string.Empty;

        [JsonPropertyName("basePath")]
        public string BasePath { get; init; } = string.Empty;

        [JsonPropertyName("sourceCode")]
        public bool SourceCode { get; init; }

        [JsonPropertyName("fontSize")]
        public int FontSize { get; init; } = 16;

        [JsonPropertyName("lineHeight")]
        public double LineHeight { get; init; } = 1.6;

        [JsonPropertyName("tabSize")]
        public int TabSize { get; init; } = 4;

        [JsonPropertyName("focusMode")]
        public bool FocusMode { get; init; }

        [JsonPropertyName("typewriter")]
        public bool Typewriter { get; init; }

        [JsonPropertyName("trimUnnecessaryCodeBlockEmptyLines")]
        public bool TrimUnnecessaryCodeBlockEmptyLines { get; init; }

        [JsonPropertyName("preferLooseListItem")]
        public bool PreferLooseListItem { get; init; } = true;

        [JsonPropertyName("autoPairBracket")]
        public bool AutoPairBracket { get; init; } = true;

        [JsonPropertyName("autoPairMarkdownSyntax")]
        public bool AutoPairMarkdownSyntax { get; init; } = true;

        [JsonPropertyName("autoPairQuote")]
        public bool AutoPairQuote { get; init; } = true;

        [JsonPropertyName("bulletListMarker")]
        public string BulletListMarker { get; init; } = "-";

        [JsonPropertyName("orderListDelimiter")]
        public string OrderListDelimiter { get; init; } = ".";

        [JsonPropertyName("codeBlockLineNumbers")]
        public bool CodeBlockLineNumbers { get; init; }

        [JsonPropertyName("listIndentation")]
        public int ListIndentation { get; init; } = 1;

        [JsonPropertyName("frontmatterType")]
        public string FrontmatterType { get; init; } = "-";

        [JsonPropertyName("sequenceTheme")]
        public string SequenceTheme { get; init; } = "simple";

        [JsonPropertyName("mermaidTheme")]
        public string MermaidTheme { get; init; } = "default";

        [JsonPropertyName("vegaTheme")]
        public string VegaTheme { get; init; } = "latimes";

        [JsonPropertyName("hideQuickInsertHint")]
        public bool HideQuickInsertHint { get; init; }

        [JsonPropertyName("hideLinkPopup")]
        public bool HideLinkPopup { get; init; }

        [JsonPropertyName("autoCheck")]
        public bool AutoCheck { get; init; }

        [JsonPropertyName("spellcheckEnabled")]
        public bool SpellcheckEnabled { get; init; }

        [JsonPropertyName("superSubScript")]
        public bool SuperSubScript { get; init; }

        [JsonPropertyName("footnote")]
        public bool Footnote { get; init; } = true;

        [JsonPropertyName("isGitlabCompatibilityEnabled")]
        public bool IsGitlabCompatibilityEnabled { get; init; }

        [JsonPropertyName("disableHtml")]
        public bool DisableHtml { get; init; }

        [JsonPropertyName("editorAreaWidth")]
        public string EditorAreaWidth { get; init; } = "880px";
    }
}
