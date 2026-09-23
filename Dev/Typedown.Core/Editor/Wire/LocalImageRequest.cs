using System;
using System.Collections.Generic;
using System.IO;

namespace Typedown.Core.Editor.Wire
{
    /// <summary><see cref="LocalImageRequest.Parse"/> 的结论，宿主据此回应页面的图片请求。</summary>
    public enum LocalImageRequestKind
    {
        /// <summary>不是本地图片主机的请求，宿主不接手。</summary>
        NotLocalImage,

        /// <summary>合法的本地图片请求，<see cref="LocalImageRequest.Path"/> 与 <see cref="LocalImageRequest.ContentType"/> 有值；文件是否存在由宿主判断。</summary>
        Accepted,

        /// <summary>方法不是 GET / HEAD，回 405。</summary>
        MethodNotAllowed,

        /// <summary>路径不合规（相对段、非法字符、设备名、不是图片扩展名等），回 403。</summary>
        Forbidden,
    }

    /// <summary>
    /// 页面的本地图片请求：<c>https://typedown.image/&lt;盘符&gt;/&lt;逐段百分号编码的路径段&gt;</c>（docs/editor-protocol.md 第 3 节「本地图片」）。
    /// 页面已按文档目录把相对路径解析成绝对路径并消解了 <c>.</c>、<c>..</c>，所以这里对路径只做校验、不做任何解析：
    /// 出现 <c>.</c>、<c>..</c>、空段、Windows 文件名的非法字符、以点或空格结尾的段、设备名一律拒绝，
    /// 拼出的路径经 <see cref="System.IO.Path.GetFullPath(string)"/> 规范化后必须与原样相同；只放行图片扩展名。
    /// 不支持 UNC（第一段只能是单个盘符字母），宿主不代文档访问网络共享。
    /// </summary>
    public readonly record struct LocalImageRequest(LocalImageRequestKind Kind, string? Path, string? ContentType)
    {
        public const string HostName = "typedown.image";

        /// <summary>WebView2 的 <c>AddWebResourceRequestedFilter</c> 用的匹配串。</summary>
        public const string FilterPattern = "https://" + HostName + "/*";

        /// <summary>单个图片文件的大小上限，超过时宿主回 403。</summary>
        public const long MaxBytes = 64L * 1024 * 1024;

        private const string Prefix = "https://" + HostName + "/";

        private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".apng"] = "image/apng",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".jfif"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".avif"] = "image/avif",
            [".bmp"] = "image/bmp",
            [".ico"] = "image/x-icon",
            [".svg"] = "image/svg+xml",
        };

        private static readonly HashSet<string> DeviceNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$",
            "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM¹", "COM²", "COM³",
            "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "LPT¹", "LPT²", "LPT³",
        };

        private static readonly LocalImageRequest NotLocal = new(LocalImageRequestKind.NotLocalImage, null, null);
        private static readonly LocalImageRequest Denied = new(LocalImageRequestKind.Forbidden, null, null);

        public static LocalImageRequest Parse(string method, string uri)
        {
            if (!uri.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return NotLocal;
            }

            if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase))
            {
                return new LocalImageRequest(LocalImageRequestKind.MethodNotAllowed, null, null);
            }

            // 路径按原文切分，不经 System.Uri（它会先消解 . 与 ..、改写部分转义，校验就看不到原样了）
            var rawPath = uri.AsSpan(Prefix.Length);
            var end = rawPath.IndexOfAny('?', '#');
            if (end >= 0)
            {
                rawPath = rawPath[..end];
            }

            var rawSegments = rawPath.ToString().Split('/');
            if (rawSegments.Length < 2)
            {
                return Denied;
            }

            var segments = new string[rawSegments.Length];
            for (var i = 0; i < rawSegments.Length; i++)
            {
                string segment;
                try
                {
                    segment = Uri.UnescapeDataString(rawSegments[i]);
                }
                catch (UriFormatException)
                {
                    return Denied;
                }

                segments[i] = segment;
            }

            var drive = segments[0];
            if (drive.Length != 1 || !char.IsAsciiLetter(drive[0]))
            {
                return Denied;
            }

            for (var i = 1; i < segments.Length; i++)
            {
                if (!IsPlainSegment(segments[i]))
                {
                    return Denied;
                }
            }

            var path = char.ToUpperInvariant(drive[0]) + @":\" + string.Join('\\', segments, 1, segments.Length - 1);
            if (!ContentTypes.TryGetValue(System.IO.Path.GetExtension(path), out var contentType))
            {
                return Denied;
            }

            string full;
            try
            {
                full = System.IO.Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return Denied;
            }

            return string.Equals(full, path, StringComparison.Ordinal)
                ? new LocalImageRequest(LocalImageRequestKind.Accepted, path, contentType)
                : Denied;
        }

        /// <summary>一个普通的文件名或目录名：非空、不是 . / ..、没有非法字符、不以点或空格结尾、不是设备名。</summary>
        private static bool IsPlainSegment(string segment)
        {
            if (segment.Length == 0 || segment[^1] is '.' or ' ')
            {
                return false;
            }

            foreach (var c in segment)
            {
                if (c < 0x20 || c is '\\' or '/' or ':' or '*' or '?' or '"' or '<' or '>' or '|')
                {
                    return false;
                }
            }

            // 设备名带扩展名（NUL.png、COM1.txt.png）同样指向设备
            var dot = segment.IndexOf('.');
            var stem = (dot < 0 ? segment : segment[..dot]).TrimEnd(' ');
            return !DeviceNames.Contains(stem);
        }
    }
}
