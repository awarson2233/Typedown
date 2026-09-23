using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using Typedown.Core.Editor;
using Typedown.Core.Editor.Legacy;
using Typedown.Core.Utilities;
using Typedown.Core.Interfaces;

namespace Typedown.Core.ViewModels
{
    public sealed partial class ParagraphViewModel : INotifyPropertyChanged, IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public AppViewModel ViewModel => ServiceProvider.GetRequiredService<AppViewModel>();

        public IEditorSession EditorSession => ServiceProvider.GetRequiredService<IEditorSession>();

        public ITableDialogService TableDialogService => ServiceProvider.GetRequiredService<ITableDialogService>();

        public Command<string> UpdateParagraphCommand { get; } = new();

        public Command<string> InsertParagraphCommand { get; } = new();

        public Command<Unit> DeleteParagraphCommand { get; } = new();

        public Command<Unit> DuplicateCommand { get; } = new();

        public Command<Unit> InsertTableCommand { get; } = new();

        private readonly CompositeDisposable disposables = new();

        public ParagraphViewModel(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            disposables.Add(UpdateParagraphCommand.OnExecute.Subscribe(x => UpdateParagraph(x)));
            disposables.Add(InsertParagraphCommand.OnExecute.Subscribe(x => InsertParagraph(x)));
            disposables.Add(DeleteParagraphCommand.OnExecute.Subscribe(_ => DeleteParagraph()));
            disposables.Add(DuplicateCommand.OnExecute.Subscribe(_ => Duplicate()));
            disposables.Add(InsertTableCommand.OnExecute.Subscribe(_ => InsertTable()));
        }

        /// <summary>菜单项以段落类型名作命令参数（paragraph、heading 1、pre、ul-task、upgrade heading…）。</summary>
        private void UpdateParagraph(string type)
        {
            if (type == LegacyMuyaVocabulary.PromoteHeadingName)
                EditorSession.Post(new PromoteHeading());
            else if (type == LegacyMuyaVocabulary.DemoteHeadingName)
                EditorSession.Post(new DemoteHeading());
            else if (LegacyMuyaVocabulary.TryParseParagraphName(type, out var kind))
                EditorSession.Post(new SetBlockKind(kind));
        }

        private void InsertParagraph(string type) =>
            EditorSession.Post(new InsertParagraph(type == "before" ? ParagraphPosition.Before : ParagraphPosition.After));

        private void DeleteParagraph() => EditorSession.Post(new DeleteParagraph());

        private void Duplicate() => EditorSession.Post(new DuplicateParagraph());

        private async void InsertTable()
        {
            var result = await TableDialogService.OpenInsertTableDialogAsync();
            if (result != null)
                EditorSession.Post(new InsertTable(result.Rows, result.Columns));
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
