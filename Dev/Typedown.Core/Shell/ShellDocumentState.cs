using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.Shell
{
    public sealed record ShellDocumentState
    {
        [JsonPropertyName("file")]
        public FileUiState File { get; init; } = new();

        [JsonPropertyName("chrome")]
        public ShellChromeState Chrome { get; init; } = new();

        public static ShellDocumentState FromValues(FileUiState file, ShellChromeState chrome)
        {
            return new ShellDocumentState
            {
                File = file,
                Chrome = chrome,
            };
        }
    }
}
