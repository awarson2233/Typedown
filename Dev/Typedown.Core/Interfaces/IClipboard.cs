using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Typedown.Core.Interfaces
{
    public enum TextDataFormat
    {
        Text,
        UnicodeText,
        Rtf,
        Html,
        CommaSeparatedValue,
        Xaml
    }

    public interface IClipboard
    {
        bool ContainsText(TextDataFormat format);

        Task<string> GetTextAsync(TextDataFormat format);

        void SetText(string text, TextDataFormat format);

        void SetText(string text);

        /// <summary>一次写入纯文本与 HTML 两种格式；为 <c>null</c> 的格式不写。</summary>
        void SetContent(string? plainText, string? html);

        Task<StringCollection> GetFileDropListAsync();

        Task SetFileDropListAsync(StringCollection fileDropList);

        Task<IClipboardImage> GetImageAsync();
    }

    public interface IClipboardImage
    {
        void SaveAsPng(string path);

        byte[] GetBytes();
    }
}
