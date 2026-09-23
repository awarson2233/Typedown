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
    /// Reads and writes Typedown's persisted JSON documents (settings.json, Config columns).
    /// </summary>
    public static class StorageJson
    {
        public static StorageJsonContext Context { get; } = new(new JsonSerializerOptions(StorageJsonContext.Default.Options)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });

        private static readonly JsonWriterOptions writerOptions = new()
        {
            Indented = true,
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
        /// Parses a JSON object. Throws <see cref="JsonException"/> for invalid JSON; returns an empty object when the
        /// root is not an object.
        /// </summary>
        public static JsonObject ParseObject(string json)
        {
            return JsonNode.Parse(json) as JsonObject ?? new JsonObject();
        }

        /// <summary>Indented, non-ASCII left unescaped so the file stays readable.</summary>
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

        /// <summary>Serializes <paramref name="value"/> as its runtime type, so derived config models keep their fields.</summary>
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
    }
}
