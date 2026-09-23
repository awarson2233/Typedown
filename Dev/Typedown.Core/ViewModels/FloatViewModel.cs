using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Typedown.Core.Editor;
using Typedown.Core.Utilities;
using Typedown.Core.Interfaces;

namespace Typedown.Core.ViewModels
{
    public sealed partial class FloatViewModel : INotifyPropertyChanged, IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public enum FindReplaceDialogState { None, Search, Replace }

        public FindReplaceDialogState FindReplaceDialogOpen { get; set; }

        public Command<FindReplaceDialogState> SearchCommand { get; } = new();

        public AppViewModel ViewModel => ServiceProvider.GetRequiredService<AppViewModel>();

        public IEditorSession EditorSession => ServiceProvider.GetRequiredService<IEditorSession>();

        public IFloatViewService FloatViewService => ServiceProvider.GetRequiredService<IFloatViewService>();

        private readonly CompositeDisposable disposables = new();

        public FloatViewModel(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            disposables.Add(EditorSession.Events.Subscribe(OnEditorEvent));
            disposables.Add(this.WhenPropertyChanged(nameof(FindReplaceDialogOpen)).Subscribe(_ => OnFindReplaceDialogOpenChange(FindReplaceDialogOpen)));
            disposables.Add(SearchCommand.OnExecute.Subscribe(Search));
        }

        private void OnEditorEvent(EditorEvent editorEvent)
        {
            switch (editorEvent)
            {
                case BlockMenuRequested request:
                    FloatViewService.OpenFrontMenu(request);
                    break;
                case FormatPickerRequested request:
                    FloatViewService.OpenFormatPicker(request);
                    break;
                case ImageEditorRequested request:
                    FloatViewService.OpenImageSelector(request);
                    break;
                case ImageToolbarRequested request:
                    FloatViewService.OpenImageToolbar(request);
                    break;
                case TableToolsRequested request:
                    FloatViewService.OpenTableTools(request);
                    break;
                case TooltipRequested request:
                    FloatViewService.OpenToolTip(request);
                    break;
                case TooltipDismissed:
                    FloatViewService.CloseToolTip();
                    break;
            }
        }

        public void Search(FindReplaceDialogState open)
        {
            FindReplaceDialogOpen = open;
            var text = ViewModel.EditorViewModel.SelectionText;
            ViewModel.EditorViewModel.SearchValue = text;
            if (!string.IsNullOrEmpty(text))
                ViewModel.EditorViewModel.OnSearch();
        }

        public void OnFindReplaceDialogOpenChange(FindReplaceDialogState open)
        {
            if (open == FindReplaceDialogState.None)
            {
                EditorSession.Post(new EndSearch());
            }
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
