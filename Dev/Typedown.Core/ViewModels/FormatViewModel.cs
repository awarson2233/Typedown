using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Typedown.Core.Editor;
using Typedown.Core.Editor.Legacy;
using Typedown.Core.Models;
using Typedown.Core.Utilities;

namespace Typedown.Core.ViewModels
{
    public sealed partial class FormatViewModel : INotifyPropertyChanged, IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public AppViewModel ViewModel => ServiceProvider.GetRequiredService<AppViewModel>();

        public EditorViewModel EditorViewModel => ServiceProvider.GetRequiredService<EditorViewModel>();

        public IEditorSession EditorSession => ServiceProvider.GetRequiredService<IEditorSession>();

        public FormatState FormatState { get; private set; } = new();

        public Command<string> SetFormatCommand { get; } = new();

        private readonly CompositeDisposable disposables = new();

        public FormatViewModel(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            disposables.Add(EditorSession.Events.OfType<MarksChanged>().Subscribe(ApplyMarks));
            disposables.Add(SetFormatCommand.OnExecute.Subscribe(x => SetFormatFun(x)));
        }

        private void ApplyMarks(MarksChanged marks)
        {
            FormatState = new(marks.Marks);
            EditorViewModel.UpdateMuyaSelected();
        }

        public void ResetFormatState()
        {
            FormatState = new FormatState();
        }

        /// <summary>菜单项以格式名作命令参数（strong、em、u、clear…）。</summary>
        private void SetFormatFun(string type)
        {
            if (type == LegacyMuyaVocabulary.ClearFormatName)
            {
                EditorSession.Post(new ClearInlineMarks());
            }
            else if (LegacyMuyaVocabulary.TryParseFormatName(type, out var mark))
            {
                EditorSession.Post(new ToggleInlineMark(mark));
            }
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
