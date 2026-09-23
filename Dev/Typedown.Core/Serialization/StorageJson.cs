using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Typedown.Core.Serialization
{
    /// <summary>
    /// Reads and writes Typedown's persisted JSON documents (settings.json, Config columns) with System.Text.Json
    /// while keeping the text layout Newtonsoft's <c>JToken.ToString()</c> produced.
    /// </summary>
    public static class StorageJson
    {
        public static StorageJsonContext Context { get; } = new(new JsonSerializerOptions(StorageJsonContext.Default.Options)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });

        private static readonly JsonDocumentOptions documentOptions = new()
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        private static readonly JsonWriterOptions writerOptions = new()
        {
            Indented = true,
            NewLine = Environment.NewLine,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static JsonTypeInfo<T> GetTypeInfo<T>()
        {
            return (JsonTypeInfo<T>)GetTypeInfo(typeof(T));
        }

        public static JsonTypeInfo GetTypeInfo(Type type)
        {
            return Context.GetTypeInfo(type) ?? throw new NotSupportedException($"Type '{type}' is not registered in {nameof(StorageJsonContext)}.");
        }

        /// <summary>
        /// Parses a JSON object. Top-level duplicate keys keep the last value, as Newtonsoft's <c>JObject.Parse</c> does.
        /// Throws <see cref="JsonException"/> for invalid JSON; returns an empty object when the root is not an object.
        /// </summary>
        public static JsonObject ParseObject(string json)
        {
            using var document = JsonDocument.Parse(json, documentOptions);
            var result = new JsonObject();
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return result;

            foreach (var property in document.RootElement.EnumerateObject())
                result[property.Name] = ToNode(property.Value);
            return result;
        }

        /// <summary>Indented, <see cref="Environment.NewLine"/> line breaks, non-ASCII left unescaped.</summary>
        public static string Write(JsonNode node)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, writerOptions))
                node.WriteTo(writer);
            return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
        }

        public static JsonNode? SerializeToNode<T>(T value)
        {
            return JsonSerializer.SerializeToNode(value, GetTypeInfo<T>());
        }

        /// <summary>Serializes <paramref name="value"/> as its runtime type, like Newtonsoft's <c>JObject.FromObject</c>.</summary>
        public static JsonNode? SerializeToNode(object value)
        {
            return JsonSerializer.SerializeToNode(value, GetTypeInfo(value.GetType()));
        }

        public static T? Deserialize<T>(JsonNode node)
        {
            return node.Deserialize(GetTypeInfo<T>());
        }

        public static object? Deserialize(JsonNode node, Type type)
        {
            return node.Deserialize(GetTypeInfo(type));
        }

        private static JsonNode? ToNode(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.Object => JsonObject.Create(element.Clone()),
                JsonValueKind.Array => JsonArray.Create(element.Clone()),
                _ => JsonValue.Create(element.Clone()),
            };
        }
    }
}
