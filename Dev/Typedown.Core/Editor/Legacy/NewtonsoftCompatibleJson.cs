using System;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Typedown.Core.Editor.Legacy
{
    /// <summary>
    /// 旧页面的报文曾由 Newtonsoft 序列化。为了让切到 System.Text.Json 后线上字节保持不变，
    /// 字符串转义与浮点数格式按 Newtonsoft 的默认行为复刻：只转义控制字符、引号、反斜杠与三个行分隔符，
    /// 其余字符（中文、emoji、&lt; &gt; &amp;）原样输出。
    /// </summary>
    internal sealed class NewtonsoftCompatibleJavaScriptEncoder : JavaScriptEncoder
    {
        public static NewtonsoftCompatibleJavaScriptEncoder Instance { get; } = new();

        private NewtonsoftCompatibleJavaScriptEncoder()
        {
        }

        public override int MaxOutputCharactersPerInputCharacter => 6;

        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
        {
            for (var i = 0; i < textLength; i++)
            {
                var c = text[i];
                if (NeedsEscape(c))
                {
                    return i;
                }

                // 孤立代理项无法转成 UTF-8，交给编码器替换，免得写入器直接抛异常。
                if (char.IsHighSurrogate(c))
                {
                    if (i + 1 < textLength && char.IsLowSurrogate(text[i + 1]))
                    {
                        i++;
                        continue;
                    }

                    return i;
                }

                if (char.IsLowSurrogate(c))
                {
                    return i;
                }
            }

            return -1;
        }

        public override unsafe bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            Span<char> scratch = stackalloc char[6];
            scoped ReadOnlySpan<char> escaped = unicodeScalar switch
            {
                '\t' => "\\t",
                '\n' => "\\n",
                '\r' => "\\r",
                '\f' => "\\f",
                '\b' => "\\b",
                '\\' => "\\\\",
                '"' => "\\\"",
                _ => default,
            };

            if (escaped.IsEmpty)
            {
                if (!WillEncode(unicodeScalar))
                {
                    var length = new Rune(unicodeScalar).EncodeToUtf16(scratch);
                    escaped = scratch[..length];
                }
                else
                {
                    scratch[0] = '\\';
                    scratch[1] = 'u';
                    unicodeScalar.TryFormat(scratch[2..], out _, "x4", CultureInfo.InvariantCulture);
                    escaped = scratch;
                }
            }

            if (escaped.Length > bufferLength)
            {
                numberOfCharactersWritten = 0;
                return false;
            }

            escaped.CopyTo(new Span<char>(buffer, bufferLength));
            numberOfCharactersWritten = escaped.Length;
            return true;
        }

        public override bool WillEncode(int unicodeScalar) => unicodeScalar <= char.MaxValue && NeedsEscape((char)unicodeScalar);

        private static bool NeedsEscape(char c) => c < ' ' || c == '"' || c == '\\' || c == '\u0085' || c == '\u2028' || c == '\u2029';
    }

    /// <summary>按 Newtonsoft 的格式写浮点数：整数值带 <c>.0</c>，非有限值写成字符串。</summary>
    internal sealed class NewtonsoftCompatibleDoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType == JsonTokenType.String
                ? double.Parse(reader.GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture)
                : reader.GetDouble();
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (double.IsNaN(value))
            {
                writer.WriteStringValue("NaN");
                return;
            }

            if (double.IsInfinity(value))
            {
                writer.WriteStringValue(value > 0 ? "Infinity" : "-Infinity");
                return;
            }

            var text = value.ToString("R", CultureInfo.InvariantCulture);
            if (text.AsSpan().IndexOfAny('.', 'E', 'e') < 0)
            {
                text += ".0";
            }

            writer.WriteRawValue(text, skipInputValidation: true);
        }
    }
}
