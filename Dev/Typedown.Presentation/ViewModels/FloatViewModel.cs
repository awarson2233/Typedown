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

        public AppViewModel ViewModel => ServiceProvider.GetRequiredService<AppViewModel>();

        public EventCenter EventCenter => ServiceProvider.GetRequiredService<EventCenter>();

        public IEditorCommandSink EditorCommandSink => ServiceProvider.GetRequiredService<IEditorCommandSink>();

        public IFloatViewService FloatViewService => ServiceProvider.GetRequiredService<IFloatViewService>();

        private readonly CompositeDisposable disposables = new();

        public FloatViewModel(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("OpenFrontMenu").Subscribe(x => OnOpenFrontMenu(x.Args)));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("OpenFormatPicker").Subscribe(x => OnOpenFormatPicker(x.Args)));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("OpenFindReplace").Subscribe(_ => OnOpenFindReplace()));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("OpenImageSelector").Subscribe(x => OnOpenImageSelector(x.Args)));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("OpenTableTools").Subscribe(x => OnOpenTableTools(x.Args)));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("OpenImageToolbar").Subscribe(x => OnOpenImageToolbar(x.Args)));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("OpenToolTip").Subscribe(x => OnOpenToolTip(x.Args)));
            disposables.Add(this.WhenPropertyChanged(nameof(FindReplaceDialogOpen)).Subscribe(_ => OnFindReplaceDialogOpenChange(FindReplaceDialogOpen)));
            disposables.Add(SearchCommand.OnExecute.Subscribe(Search));
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
                EditorCommandSink?.Send("SearchOpenChange", new { open = (int)open });
            }
        }

        public void OnOpenImageToolbar(JToken args)
        {
            FloatViewService.OpenImageToolbar(args);
        }

        public void OnOpenFindReplace()
        {
            Search(FindReplaceDialogState.Search);
        }

        public void OnOpenFrontMenu(JToken args)
        {
            FloatViewService.OpenFrontMenu(args);
        }

        public void OnOpenFormatPicker(JToken args)
        {
            FloatViewService.OpenFormatPicker(args);
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
