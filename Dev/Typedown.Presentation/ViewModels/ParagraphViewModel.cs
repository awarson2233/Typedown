using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Utilities;

namespace Typedown.Presentation.ViewModels
{
    public sealed partial class ParagraphViewModel : INotifyPropertyChanged, IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public EventCenter EventCenter => ServiceProvider.GetService<EventCenter>();

        public AppViewModel ViewModel => ServiceProvider.GetService<AppViewModel>();

        public RemoteInvoke RemoteInvoke => ServiceProvider.GetService<RemoteInvoke>();

        public IEditorCommandSink EditorCommandSink => ServiceProvider.GetService<IEditorCommandSink>();

        public ITableDialogService TableDialogService => ServiceProvider.GetService<ITableDialogService>();

        public Command<string> UpdateParagraphCommand { get; } = new();

        public Command<string> InsertParagraphCommand { get; } = new();

        public Command<Unit> DeleteParagraphCommand { get; } = new();

        public Command<Unit> DuplicateCommand { get; } = new();

        public Command<Unit> InsertTableCommand { get; } = new();

        private readonly CompositeDisposable disposables = new();

        public ParagraphViewModel(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            RemoteInvoke.Handle("ResizeTable", ResizeTable);
            UpdateParagraphCommand.OnExecute.Subscribe(x => UpdateParagraph(x));
            InsertParagraphCommand.OnExecute.Subscribe(x => InsertParagraph(x));
            DeleteParagraphCommand.OnExecute.Subscribe(_ => DeleteParagraph());
            DuplicateCommand.OnExecute.Subscribe(_ => Duplicate());
            InsertTableCommand.OnExecute.Subscribe(_ => InsertTable());
        }

        private void UpdateParagraph(string type) => EditorCommandSink?.Send("UpdateParagraph", type);

        private void InsertParagraph(string type) => EditorCommandSink?.Send("InsertParagraph", type);

        private void DeleteParagraph() => EditorCommandSink?.Send("DeleteParagraph", null);

        private void Duplicate() => EditorCommandSink?.Send("Duplicate", null);

        private async void InsertTable()
        {
            var result = await TableDialogService.OpenInsertTableDialogAsync();
            if (result != null)
                EditorCommandSink?.Send("InsertTable", new { rows = result.Rows, columns = result.Columns });
        }

        public async Task<object> ResizeTable()
        {
            var result = await TableDialogService.OpenResizeTableDialogAsync();
            return result != null ? new { rows = result.Rows, columns = result.Columns } : null;
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
