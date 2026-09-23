using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Typedown.CoreTests.Persistence;

/// <summary>
/// The Newtonsoft behaviour the storage code used to have. Config models now carry free-form <c>Addition</c> values
/// as System.Text.Json <see cref="System.Text.Json.JsonElement"/> where they used to be <see cref="JToken"/>, so the
/// reference serializer maps the two onto each other as plain JSON.
/// </summary>
internal static class NewtonsoftReference
{
    public static JsonSerializer Serializer { get; } = JsonSerializer.CreateDefault(new JsonSerializerSettings
    {
        Converters = { new JsonElementConverter() },
    });

    public static object? ToObject(JToken token, Type type) => token.ToObject(type, Serializer);

    public static JToken FromObject(object? value) => value is null ? JValue.CreateNull() : JToken.FromObject(value, Serializer);

    public static void AssertSameValue(object? expected, object? actual, string name)
    {
        var expectedToken = FromObject(expected);
        var actualToken = FromObject(actual);
        Assert.IsTrue(JToken.DeepEquals(expectedToken, actualToken), $"{name}: expected {expectedToken.ToString(Formatting.None)}, actual {actualToken.ToString(Formatting.None)}");
    }

    private sealed class JsonElementConverter : JsonConverter<System.Text.Json.JsonElement>
    {
        public override System.Text.Json.JsonElement ReadJson(JsonReader reader, Type objectType, System.Text.Json.JsonElement existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            using var document = System.Text.Json.JsonDocument.Parse(token.ToString(Formatting.None));
            return document.RootElement.Clone();
        }

        public override void WriteJson(JsonWriter writer, System.Text.Json.JsonElement value, JsonSerializer serializer)
        {
            JToken.Parse(value.GetRawText()).WriteTo(writer);
        }
    }
}
