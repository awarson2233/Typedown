using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Typedown.Core.Models;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Utilities;

namespace Typedown.Presentation.ViewModels
{
    public sealed partial class FloatViewModel : INotifyPropertyChanged, IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public enum FindReplaceDialogState { None, Search, Replace }

        public FindReplaceDialogState FindReplaceDialogOpen { get; set; }

        public Command<FindReplaceDialogState> SearchCommand { get; } = new();

        public AppViewModel ViewModel => ServiceProvider.GetService<AppViewModel>();

        public EventCenter EventCenter => ServiceProvider.GetService<EventCenter>();

        public IEditorCommandSink EditorCommandSink => ServiceProvider.GetService<IEditorCommandSink>();

        public IFloatViewService FloatViewService => ServiceProvider.GetService<IFloatViewService>();

        private readonly CompositeDisposable disposables = new();

        public FloatViewModel(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            EventCenter.GetObservable<EditorEventArgs>("OpenFrontMenu").Subscribe(x => OnOpenFrontMenu(x.Args));
            EventCenter.GetObservable<EditorEventArgs>("OpenFormatPicker").Subscribe(x => OnOpenFormatPicker(x.Args));
            EventCenter.GetObservable<EditorEventArgs>("OpenImageSelector").Subscribe(x => OnOpenImageSelector(x.Args));
            EventCenter.GetObservable<EditorEventArgs>("OpenTableTools").Subscribe(x => OnOpenTableTools(x.Args));
            EventCenter.GetObservable<EditorEventArgs>("OpenImageToolbar").Subscribe(x => OnOpenImageToolbar(x.Args));
            EventCenter.GetObservable<EditorEventArgs>("OpenToolTip").Subscribe(x => OnOpenToolTip(x.Args));
            this.WhenPropertyChanged(nameof(FindReplaceDialogOpen)).Subscribe(_ => OnFindReplaceDialogOpenChange(FindReplaceDialogOpen));
            SearchCommand.OnExecute.Subscribe(Search);
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
            EditorCommandSink?.Send("SearchOpenChange", new { open = (int)open });
        }

        public void OnOpenImageToolbar(JToken args)
        {
            FloatViewService.OpenImageToolbar(args);
        }

        public void OnOpenFrontMenu(JToken args)
        {
            FloatViewService.OpenFrontMenu(args);
        }

        public void OnOpenFormatPicker(JToken args)
        {
            throw new NotImplementedException();
        }

        public void OnOpenImageSelector(JToken args)
        {
            FloatViewService.OpenImageSelector(args);
        }

        public void OnOpenTableTools(JToken args)
        {
            FloatViewService.OpenTableTools(args);
        }

        public void OnOpenToolTip(JToken args)
        {
            FloatViewService.OpenToolTip(args);
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
