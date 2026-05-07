using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using Typedown.Core.Models;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Utilities;

namespace Typedown.Presentation.ViewModels
{
    public sealed partial class FormatViewModel : INotifyPropertyChanged, IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public AppViewModel ViewModel => ServiceProvider.GetService<AppViewModel>();

        public EditorViewModel EditorViewModel => ServiceProvider.GetService<EditorViewModel>();

        public EventCenter EventCenter => ServiceProvider.GetService<EventCenter>();

        public FormatState FormatState { get; private set; } = new();

        public IEditorCommandSink EditorCommandSink => ServiceProvider.GetService<IEditorCommandSink>();

        public Command<string> SetFormatCommand { get; } = new();

        private readonly CompositeDisposable disposables = new();

        public FormatViewModel(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("SelectionFormats").Subscribe(x => OnSelectionFormats(x.Args)));
            disposables.Add(SetFormatCommand.OnExecute.Subscribe(x => SetFormatFun(x)));
        }

        public void OnSelectionFormats(JToken arg)
        {
            FormatState = new(arg["formats"]?.ToObject<List<FormatState.SelectionFormat>>());
            EditorViewModel.UpdateMuyaSelected();
        }

        private void SetFormatFun(string type)
        {
            EditorCommandSink?.Send("Format", type);
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
