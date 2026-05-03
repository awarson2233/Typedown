using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Typedown.Presentation.Interfaces
{
    public interface IFileOperation
    {
        bool Delete(StringCollection files);

        bool Copy(StringCollection files, string to);

        bool Move(StringCollection files, string to);

        bool Rename(string from, string to);

        Task CutToClipboardAsync(StringCollection files);

        Task CopyToClipboardAsync(StringCollection files);

        bool IsPasteEnabled { get; }

        void PasteFromClipboard(string to);

        bool IsFilenameValid(string sourceFolder, string fileName);
    }
}
