using System.Collections.Generic;
using System.Threading.Tasks;

namespace Typedown.Presentation.Interfaces
{
    public interface IFilePickerService
    {
        Task<string> PickOpenFileAsync(OpenFileRequest request);

        Task<string> PickSaveFileAsync(SaveFileRequest request);

        Task<string> PickFolderAsync(PickFolderRequest request);
    }

    public class OpenFileRequest
    {
        public IList<string> FileTypeFilter { get; set; } = new List<string>();
    }

    public class SaveFileTypeChoice
    {
        public SaveFileTypeChoice()
        {
        }

        public SaveFileTypeChoice(string name, IEnumerable<string> extensions)
        {
            Name = name;
            Extensions = new List<string>(extensions);
        }

        public string Name { get; set; } = string.Empty;

        public IList<string> Extensions { get; set; } = new List<string>();
    }

    public class SaveFileRequest
    {
        public IList<SaveFileTypeChoice> FileTypeChoices { get; set; } = new List<SaveFileTypeChoice>();

        public string SuggestedFileName { get; set; }
    }

    public class PickFolderRequest
    {
        public IList<string> FileTypeFilter { get; set; } = new List<string> { "*" };
    }
}
