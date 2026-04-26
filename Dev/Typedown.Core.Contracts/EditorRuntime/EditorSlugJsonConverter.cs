using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.EditorRuntime
{
    internal sealed class EditorSlugJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString() ?? string.Empty,
                JsonTokenType.Number => ReadNumberAsString(ref reader),
                JsonTokenType.Null => string.Empty,
                _ => throw new JsonException($"Unsupported editor TOC slug token: {reader.TokenType}."),
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        private static string ReadNumberAsString(ref Utf8JsonReader reader)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.GetRawText();
        }
    }
}
