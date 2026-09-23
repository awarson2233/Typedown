using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Typedown.Core.Serialization
{
    // Converters that make System.Text.Json accept and produce what Newtonsoft.Json did for Typedown's stored JSON
    // (settings.json and the Config columns): numbers in strings, strings from numbers and booleans, enums as numbers
    // or names, doubles written with a decimal place.

    public sealed class NewtonsoftDoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    return reader.GetDouble();
                case JsonTokenType.String:
                    if (double.TryParse(reader.GetString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
                        return value;
                    break;
                case JsonTokenType.True:
                    return 1;
                case JsonTokenType.False:
                    return 0;
            }
            throw new JsonException($"Cannot convert {reader.TokenType} to Double.");
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (!double.IsFinite(value))
            {
                // Newtonsoft's default FloatFormatHandling.String.
                writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
                return;
            }

            var text = value.ToString("R", CultureInfo.InvariantCulture);
            if (text.IndexOfAny(['.', 'E', 'e']) < 0)
                text += ".0";
            writer.WriteRawValue(text, skipInputValidation: true);
        }
    }

    public sealed class NewtonsoftInt32Converter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        // Newtonsoft converts fractional numbers with Convert.ToInt32 (banker's rounding).
                        return reader.TryGetInt32(out var value) ? value : Convert.ToInt32(reader.GetDouble());
                    case JsonTokenType.String:
                        if (int.TryParse(reader.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                            return value;
                        break;
                }
            }
            catch (OverflowException ex)
            {
                throw new JsonException("Value is out of range for Int32.", ex);
            }
            throw new JsonException($"Cannot convert {reader.TokenType} to Int32.");
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    public sealed class NewtonsoftBooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Number:
                    return reader.GetDouble() != 0;
                case JsonTokenType.String:
                    if (bool.TryParse(reader.GetString(), out var value))
                        return value;
                    break;
            }
            throw new JsonException($"Cannot convert {reader.TokenType} to Boolean.");
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }

    public sealed class NewtonsoftStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Null => null,
                // Newtonsoft parses numbers to long or double and formats them with the invariant culture.
                JsonTokenType.Number => reader.TryGetInt64(out var value)
                    ? value.ToString(CultureInfo.InvariantCulture)
                    : reader.GetDouble().ToString(CultureInfo.InvariantCulture),
                JsonTokenType.True => bool.TrueString,
                JsonTokenType.False => bool.FalseString,
                _ => throw new JsonException($"Cannot convert {reader.TokenType} to String."),
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        // Dictionary<string, ...> keys go through the string converter too.
        public override string ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString()!;
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WritePropertyName(value);
        }
    }

    /// <summary>Writes enums as numbers (Newtonsoft's default) and reads numbers or case-insensitive names.</summary>
    public sealed class NewtonsoftEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        private static readonly bool isUnsigned64 = Enum.GetUnderlyingType(typeof(TEnum)) == typeof(ulong);

        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out var signed))
                        return (TEnum)Enum.ToObject(typeof(TEnum), signed);
                    if (reader.TryGetUInt64(out var unsigned))
                        return (TEnum)Enum.ToObject(typeof(TEnum), unsigned);
                    break;
                case JsonTokenType.String:
                    if (Enum.TryParse<TEnum>(reader.GetString(), ignoreCase: true, out var value))
                        return value;
                    break;
            }
            throw new JsonException($"Cannot convert {reader.TokenType} to {typeof(TEnum).Name}.");
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            if (isUnsigned64)
                writer.WriteNumberValue(Convert.ToUInt64(value, CultureInfo.InvariantCulture));
            else
                writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }
    }
}
