using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.Settings;

public sealed record SettingsUiSnapshot
{
    [JsonPropertyName("sidePaneOpen")]
    public bool SidePaneOpen { get; init; }

    [JsonPropertyName("sidePaneWidth")]
    public double SidePaneWidth { get; init; } = 300d;

    [JsonPropertyName("statusBarOpen")]
    public bool StatusBarOpen { get; init; } = true;

    [JsonPropertyName("sourceCode")]
    public bool SourceCode { get; init; }

    [JsonPropertyName("typewriter")]
    public bool Typewriter { get; init; }

    [JsonPropertyName("focusMode")]
    public bool FocusMode { get; init; }

    [JsonPropertyName("fontSize")]
    public double FontSize { get; init; } = 16d;

    [JsonPropertyName("lineHeight")]
    public double LineHeight { get; init; } = 1.6d;

    [JsonPropertyName("editorAreaWidth")]
    public string EditorWidth { get; init; } = "1200px";

    [JsonPropertyName("language")]
    public string Language { get; init; } = "default";

    [JsonPropertyName("spellcheckEnabled")]
    public bool Spellcheck { get; init; }

    [JsonPropertyName("spellcheckLang")]
    public string SpellcheckLanguage { get; init; } = string.Empty;

    [JsonPropertyName("insertClipboardImageAction")]
    public int InsertClipboardImageAction { get; init; }

    [JsonPropertyName("insertClipboardImageCopyPath")]
    public string InsertClipboardImageCopyPath { get; init; } = "./images";

    [JsonPropertyName("insertClipboardImageUseUploadConfigId")]
    public int? InsertClipboardImageUseUploadConfigId { get; init; }

    [JsonPropertyName("insertLocalImageAction")]
    public int InsertLocalImageAction { get; init; }

    [JsonPropertyName("insertLocalImageCopyPath")]
    public string InsertLocalImageCopyPath { get; init; } = "./images";

    [JsonPropertyName("insertLocalImageUseUploadConfigId")]
    public int? InsertLocalImageUseUploadConfigId { get; init; }

    [JsonPropertyName("insertWebImageAction")]
    public int InsertWebImageAction { get; init; }

    [JsonPropertyName("insertWebImageCopyPath")]
    public string InsertWebImageCopyPath { get; init; } = "./images";

    [JsonPropertyName("insertWebImageUseUploadConfigId")]
    public int? InsertWebImageUseUploadConfigId { get; init; }
}
