using System.Collections.Generic;
using System.Threading.Tasks;

namespace Typedown.Core.Interfaces
{
    public interface IFilePickerService
    {
        Task<string> PickOpenFileAsync(OpenFilePickerRequest request);

        Task<string> PickSaveFileAsync(SaveFilePickerRequest request);

        Task<string> PickFolderAsync(FolderPickerRequest request);
    }

    public class OpenFilePickerRequest
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

    public class SaveFilePickerRequest
    {
        public IList<SaveFileTypeChoice> FileTypeChoices { get; set; } = new List<SaveFileTypeChoice>();

        public string SuggestedFileName { get; set; }
    }

    public class FolderPickerRequest
    {
        public IList<string> FileTypeFilter { get; set; } = new List<string> { "*" };
    }
}
