using System;
using System.ComponentModel;
using Typedown.Core.Models;

namespace Typedown.Interfaces
{
    public interface IMarkdownEditor : IDisposable, INotifyPropertyChanged
    {
        bool PostMessage(string name, object arg);

        object GetDummyRectangle(UiRect rect);

        object MoveDummyRectangle(UiPoint offset);

        bool IsEditorLoadFailed { get; }

        bool IsEditorLoaded { get; }
    }
}
