using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.Shell
{
    public sealed record ShellChromeState
    {
        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("isSaved")]
        public bool IsSaved { get; init; }

        [JsonPropertyName("displaySaved")]
        public bool DisplaySaved { get; init; }

        [JsonPropertyName("isTopmost")]
        public bool IsTopmost { get; init; }

        [JsonPropertyName("captionHeight")]
        public double CaptionHeight { get; init; }

        [JsonPropertyName("compactMode")]
        public bool CompactMode { get; init; }

        [JsonPropertyName("currentPageName")]
        public string? CurrentPageName { get; init; }

        public static ShellChromeState FromValues(
            string? title,
            bool isSaved,
            bool displaySaved,
            bool isTopmost,
            double captionHeight,
            bool compactMode,
            string? currentPageName)
        {
            return new ShellChromeState
            {
                Title = title ?? string.Empty,
                IsSaved = isSaved,
                DisplaySaved = displaySaved,
                IsTopmost = isTopmost,
                CaptionHeight = captionHeight,
                CompactMode = compactMode,
                CurrentPageName = currentPageName,
            };
        }
    }
}
