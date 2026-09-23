using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Typedown.Core.Models;
using Typedown.Core.Models.RuntimeModels;

namespace Typedown.Core.Utilities
{
    public static class Common
    {
        public static T GetBreakPointValue<T>(this double width, T largeValue, T mediumValue, T smallValue)
        {
            if (width >= 1008)
                return largeValue;
            if (width >= 641)
                return mediumValue;
            return smallValue;
        }

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                Process.Start(new ProcessStartInfo(url.Replace("&", "^&")) { UseShellExecute = true });
            }
        }

        public static void OpenFileLocation(string filePath)
        {
            Process.Start("explorer.exe", $"/select, \"{filePath}\"");
        }

        public static ulong SimpleHash(string str)
        {
            ulong hashedValue = 3074457345618258791ul;
            for (int i = 0; i < str.Length; i++)
            {
                hashedValue += str[i];
                hashedValue *= 3074457345618258799ul;
            }
            return hashedValue;
        }

        public static string SimpleHash2(string str)
        {
            var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(str));
            var result = hash.ToBase36String();
            return result.Substring(0, Math.Min(6, result.Length));
        }

        public static string ToBase36String(this byte[] toConvert)
        {
            const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
            var dividend = new BigInteger(toConvert);
            var builder = new StringBuilder();
            while (dividend != 0)
            {
                dividend = BigInteger.DivRem(dividend, 36, out var remainder);
                builder.Insert(0, alphabet[Math.Abs((int)remainder)]);
            }
            return builder.ToString();
        }

        public static string DefaultMarkdwn { get => "\n"; }

        public static string ExtractHtmlFragment(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            const string startComment = "<!--StartFragment-->";
            const string endComment = "<!--EndFragment-->";
            var start = html.IndexOf(startComment, StringComparison.OrdinalIgnoreCase);
            var end = html.IndexOf(endComment, StringComparison.OrdinalIgnoreCase);
            if (start >= 0 && end > start)
                return html[(start + startComment.Length)..end];

            var startMatch = Regex.Match(html, @"(?im)^StartFragment:\s*(\d+)");
            var endMatch = Regex.Match(html, @"(?im)^EndFragment:\s*(\d+)");
            if (startMatch.Success && endMatch.Success
                && int.TryParse(startMatch.Groups[1].Value, out start)
                && int.TryParse(endMatch.Groups[1].Value, out end))
            {
                var bytes = Encoding.UTF8.GetBytes(html);
                if (start >= 0 && end > start && end <= bytes.Length)
                    return Encoding.UTF8.GetString(bytes, start, end - start);
            }

            var body = Regex.Match(html, @"(?is)<body\b[^>]*>(.*?)</body\s*>");
            return body.Success ? body.Groups[1].Value : html;
        }

        public static string GetShortcutKeyText(this ShortcutKey key)
        {
            return string.Join('+', GetShortcutKeyTextList(key));
        }

        public static List<string> GetShortcutKeyTextList(this ShortcutKey key)
        {
            if (key == null) return new();
            var result = new List<string>();
            if (key.Modifiers.HasFlag(KeyboardModifiers.Control))
                result.Add(GetVirtualKeyNameText(KeyboardKey.Control));
            if (key.Modifiers.HasFlag(KeyboardModifiers.Menu))
                result.Add(GetVirtualKeyNameText(KeyboardKey.Menu));
            if (key.Modifiers.HasFlag(KeyboardModifiers.Shift))
                result.Add(GetVirtualKeyNameText(KeyboardKey.Shift));
            if (key.Modifiers.HasFlag(KeyboardModifiers.Windows))
                result.Add("Win");
            result.Add(GetVirtualKeyNameText(key.Key));
            return result;
        }

        public static string GetVirtualKeyNameText(this KeyboardKey key)
        {
            if (key == KeyboardKey.Delete)
                return "Delete";
            if (key == KeyboardKey.LeftWindows || key == KeyboardKey.RightWindows)
                return "Win";
            var buffer = new StringBuilder(32);
            var scanCode = PInvoke.MapVirtualKey((uint)key, PInvoke.MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC);
            var lParam = scanCode << 16;
            PInvoke.GetKeyNameText(lParam, buffer, buffer.Capacity);
            return buffer.ToString();
        }

        /// <summary>
        /// 把 <typeparamref name="T"/> 上可读写的公开实例属性逐个拷到 <paramref name="target"/>，值相等的不写。
        /// 按静态类型 <typeparamref name="T"/> 取属性（带 <see cref="DynamicallyAccessedMembersAttribute"/>），
        /// 裁剪与 Native AOT 会为调用方给出的具体类型保留这些属性。
        /// </summary>
        public static void CopyProperties<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this T source, T target)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(target);

            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length != 0)
                    continue;

                var oldValue = property.GetValue(target);
                var newValue = property.GetValue(source);
                if (!(oldValue?.Equals(newValue) ?? oldValue == newValue))
                    property.SetValue(target, newValue);
            }
        }

        public static bool FileContentEqual(string filePath, byte[] content)
        {
            try
            {
                if (new FileInfo(filePath).Length != content.Length)
                    return false;
                return File.ReadAllBytes(filePath).SequenceEqual(content);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool FileContentEqual(string filePath1, string filePath2)
        {
            try
            {
                if (new FileInfo(filePath1).Length != new FileInfo(filePath2).Length)
                    return false;
                return File.ReadAllBytes(filePath1).SequenceEqual(File.ReadAllBytes(filePath2));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetTempFileName(string extension)
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid() + extension);
        }

        [return: MaybeNull]
        public static HtmlImgTag MatchHtmlImg(string html)
        {
            var tagRegex = @"(?<=<!--StartFragment-->\s*)(<img)[^>]*(/>|>)(?=\s*<!--EndFragment-->)";
            var tagMatch = Regex.Match(html, tagRegex, RegexOptions.IgnoreCase);
            if (!tagMatch.Success)
                return null;
            var tag = tagMatch.ToString();
            var attrMatch = (string attr) => Regex.Match(tag, @"(?<=" + attr + @"=(""|'))[^""']*(?=(""|'))", RegexOptions.IgnoreCase);
            var src = attrMatch("src").ToString();
            var alt = attrMatch("alt").ToString();
            var title = attrMatch("title").ToString();
            return new(src, alt, title);
        }
    }
}
