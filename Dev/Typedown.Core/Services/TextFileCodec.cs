using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Typedown.Core.Services
{
    /// <summary>文本文件的字符编码；UTF-16 与 UTF-32 只能靠 BOM 认出。</summary>
    public enum TextFileEncoding
    {
        Utf8,
        Utf16LittleEndian,
        Utf16BigEndian,
        Utf32LittleEndian,
        Utf32BigEndian,
    }

    public enum LineEnding
    {
        Lf,
        CrLf,
        Cr,
    }

    /// <summary>打开文件时记下的字节形态，保存时按它还原。</summary>
    public sealed record TextFileFormat(TextFileEncoding Encoding, bool HasBom, LineEnding LineEnding)
    {
        /// <summary>新建文档：UTF-8 无 BOM、LF。</summary>
        public static TextFileFormat Default { get; } = new(TextFileEncoding.Utf8, false, LineEnding.Lf);
    }

    /// <summary>解码后的文件：<paramref name="Text"/> 只含 <c>\n</c> 换行，<paramref name="Format"/> 记着原来的编码、BOM 与换行符。</summary>
    public sealed record DecodedTextFile(string Text, TextFileFormat Format);

    /// <summary>
    /// 编辑器只见 <c>\n</c>：读文件时按 BOM 认编码（没有 BOM 即 UTF-8），记下原换行符（混合时取占多数者）后统一成 <c>\n</c>；
    /// 写文件时换回原换行符、按原编码与 BOM 编码。正文没改过时写出的字节与读入的相同（混合换行的文件除外，会统一成多数者）。
    /// </summary>
    public static class TextFileCodec
    {
        private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
        private static readonly byte[] Utf16LeBom = [0xFF, 0xFE];
        private static readonly byte[] Utf16BeBom = [0xFE, 0xFF];
        private static readonly byte[] Utf32LeBom = [0xFF, 0xFE, 0x00, 0x00];
        private static readonly byte[] Utf32BeBom = [0x00, 0x00, 0xFE, 0xFF];

        public static async Task<DecodedTextFile> ReadAsync(string path) => Decode(await File.ReadAllBytesAsync(path));

        public static DecodedTextFile Decode(ReadOnlySpan<byte> bytes)
        {
            var (encoding, hasBom, bomLength) = DetectEncoding(bytes);
            var raw = GetEncoding(encoding).GetString(bytes[bomLength..]);
            return new DecodedTextFile(NormalizeLineEndings(raw), new TextFileFormat(encoding, hasBom, DetectLineEnding(raw)));
        }

        public static byte[] Encode(string text, TextFileFormat format)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(format);
            var normalized = NormalizeLineEndings(text);
            var restored = format.LineEnding switch
            {
                LineEnding.CrLf => normalized.Replace("\n", "\r\n", StringComparison.Ordinal),
                LineEnding.Cr => normalized.Replace('\n', '\r'),
                _ => normalized,
            };
            var bom = format.HasBom ? GetBom(format.Encoding) : [];
            var encoding = GetEncoding(format.Encoding);
            var bytes = new byte[bom.Length + encoding.GetByteCount(restored)];
            bom.CopyTo(bytes, 0);
            encoding.GetBytes(restored, 0, restored.Length, bytes, bom.Length);
            return bytes;
        }

        /// <summary>把 <c>\r\n</c> 与单独的 <c>\r</c> 都换成 <c>\n</c>。</summary>
        public static string NormalizeLineEndings(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            return text.Contains('\r')
                ? text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n')
                : text;
        }

        /// <summary>出现次数最多的换行符；没有换行或并列时依次偏向 CRLF、LF。</summary>
        public static LineEnding DetectLineEnding(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            int crlf = 0, lf = 0, cr = 0;
            for (var i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '\r' when i + 1 < text.Length && text[i + 1] == '\n':
                        crlf++;
                        i++;
                        break;
                    case '\r':
                        cr++;
                        break;
                    case '\n':
                        lf++;
                        break;
                }
            }

            if (crlf == 0 && lf == 0 && cr == 0)
            {
                return LineEnding.Lf;
            }

            return crlf >= lf && crlf >= cr ? LineEnding.CrLf
                : lf >= cr ? LineEnding.Lf
                : LineEnding.Cr;
        }

        private static (TextFileEncoding Encoding, bool HasBom, int BomLength) DetectEncoding(ReadOnlySpan<byte> bytes)
        {
            // UTF-32 LE 的 BOM 以 UTF-16 LE 的 BOM 开头，要先判断。
            if (bytes.StartsWith(Utf32LeBom)) return (TextFileEncoding.Utf32LittleEndian, true, Utf32LeBom.Length);
            if (bytes.StartsWith(Utf32BeBom)) return (TextFileEncoding.Utf32BigEndian, true, Utf32BeBom.Length);
            if (bytes.StartsWith(Utf8Bom)) return (TextFileEncoding.Utf8, true, Utf8Bom.Length);
            if (bytes.StartsWith(Utf16LeBom)) return (TextFileEncoding.Utf16LittleEndian, true, Utf16LeBom.Length);
            if (bytes.StartsWith(Utf16BeBom)) return (TextFileEncoding.Utf16BigEndian, true, Utf16BeBom.Length);
            return (TextFileEncoding.Utf8, false, 0);
        }

        private static byte[] GetBom(TextFileEncoding encoding) => encoding switch
        {
            TextFileEncoding.Utf8 => Utf8Bom,
            TextFileEncoding.Utf16LittleEndian => Utf16LeBom,
            TextFileEncoding.Utf16BigEndian => Utf16BeBom,
            TextFileEncoding.Utf32LittleEndian => Utf32LeBom,
            TextFileEncoding.Utf32BigEndian => Utf32BeBom,
            _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null),
        };

        private static Encoding GetEncoding(TextFileEncoding encoding) => encoding switch
        {
            TextFileEncoding.Utf8 => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            TextFileEncoding.Utf16LittleEndian => new UnicodeEncoding(bigEndian: false, byteOrderMark: false),
            TextFileEncoding.Utf16BigEndian => new UnicodeEncoding(bigEndian: true, byteOrderMark: false),
            TextFileEncoding.Utf32LittleEndian => new UTF32Encoding(bigEndian: false, byteOrderMark: false),
            TextFileEncoding.Utf32BigEndian => new UTF32Encoding(bigEndian: true, byteOrderMark: false),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null),
        };
    }
}
