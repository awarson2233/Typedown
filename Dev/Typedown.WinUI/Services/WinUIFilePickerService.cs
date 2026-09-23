using System;
using System.Linq;
using System.Threading.Tasks;
using Typedown.Core.Interfaces;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIFilePickerService : IFilePickerService
    {
        private readonly IWindowContext windowContext;

        public WinUIFilePickerService(IWindowContext windowContext)
        {
            this.windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
        }

        public async Task<string?> PickOpenFileAsync(OpenFileRequest request)
        {
            request ??= new OpenFileRequest();

            var picker = new FileOpenPicker();
            InitializeWithWindow.Initialize(picker, windowContext.WindowHandle);

            foreach (var filter in request.FileTypeFilter.DefaultIfEmpty("*"))
            {
                picker.FileTypeFilter.Add(filter);
            }

            var file = await picker.PickSingleFileAsync();
            return file?.Path;
        }

        public async Task<string?> PickSaveFileAsync(SaveFileRequest request)
        {
            request ??= new SaveFileRequest();

            var picker = new FileSavePicker
            {
                SuggestedFileName = request.SuggestedFileName ?? string.Empty
            };

            InitializeWithWindow.Initialize(picker, windowContext.WindowHandle);

            foreach (var choice in request.FileTypeChoices)
            {
                if (choice.Extensions.Count > 0)
                {
                    picker.FileTypeChoices[choice.Name] = choice.Extensions.ToList();
                }
            }

            if (picker.FileTypeChoices.Count == 0)
            {
                picker.FileTypeChoices["All files"] = new[] { "." };
            }

            var file = await picker.PickSaveFileAsync();
            return file?.Path;
        }

        public async Task<string?> PickFolderAsync(PickFolderRequest request)
        {
            request ??= new PickFolderRequest();

            var picker = new FolderPicker();
            InitializeWithWindow.Initialize(picker, windowContext.WindowHandle);

            foreach (var filter in request.FileTypeFilter.DefaultIfEmpty("*"))
            {
                picker.FileTypeFilter.Add(filter);
            }

            var folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }
    }
}
