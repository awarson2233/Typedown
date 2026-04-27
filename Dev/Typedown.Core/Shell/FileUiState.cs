using System.IO;
using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.Shell
{
    public sealed record FileUiState
    {
        [JsonPropertyName("workFolder")]
        public string? WorkFolder { get; init; }

        [JsonPropertyName("filePath")]
        public string? FilePath { get; init; }

        [JsonPropertyName("imageBasePath")]
        public string? ImageBasePath { get; init; }

        [JsonPropertyName("fileName")]
        public string? FileName { get; init; }

        [JsonPropertyName("isUntitled")]
        public bool IsUntitled { get; init; }

        [JsonPropertyName("hasFilePath")]
        public bool HasFilePath { get; init; }

        public static FileUiState FromValues(
            string? workFolder,
            string? filePath,
            string? defaultImageBasePath)
        {
            var hasFilePath = !string.IsNullOrEmpty(filePath);

            return new FileUiState
            {
                WorkFolder = workFolder,
                FilePath = filePath,
                ImageBasePath = hasFilePath ? Path.GetDirectoryName(filePath) : defaultImageBasePath,
                FileName = Path.GetFileName(filePath),
                IsUntitled = !hasFilePath,
                HasFilePath = hasFilePath,
            };
        }
    }
}
