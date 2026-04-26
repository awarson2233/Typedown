using System;
using System.Linq;
using System.Threading.Tasks;
using Typedown.Core.Interfaces;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Windows.Storage.Pickers;

namespace Typedown.Services
{
    public class FilePickerService : IFilePickerService
    {
        private readonly AppViewModel appViewModel;

        public FilePickerService(AppViewModel appViewModel)
        {
            this.appViewModel = appViewModel;
        }

        public async Task<string> PickOpenFileAsync(OpenFileRequest request)
        {
            var picker = new FileOpenPicker();
            request.FileTypeFilter.ToList().ForEach(picker.FileTypeFilter.Add);
            picker.SetOwnerWindow(GetMainWindow());
            var file = await picker.PickSingleFileAsync();
            return file?.Path;
        }

        public async Task<string> PickSaveFileAsync(SaveFileRequest request)
        {
            var picker = new FileSavePicker();
            foreach (var choice in request.FileTypeChoices)
                picker.FileTypeChoices.Add(choice.Name, choice.Extensions.ToList());
            picker.SuggestedFileName = request.SuggestedFileName;
            picker.SetOwnerWindow(GetMainWindow());
            var file = await picker.PickSaveFileAsync();
            return file?.Path;
        }

        public async Task<string> PickFolderAsync(PickFolderRequest request)
        {
            var picker = new FolderPicker();
            request.FileTypeFilter.ToList().ForEach(picker.FileTypeFilter.Add);
            picker.SetOwnerWindow(GetMainWindow());
            var folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }

        private nint GetMainWindow()
        {
            if (appViewModel.MainWindow == default)
                throw new InvalidOperationException("Main window handle is not available for file picker display.");
            return appViewModel.MainWindow;
        }
    }
}
