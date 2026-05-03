using System.Reactive.Subjects;
using Typedown.Core.Models;

namespace Typedown.Presentation.Interfaces
{
    public interface IWindowService
    {
        Subject<nint> WindowStateChanged { get; }

        Subject<nint> WindowIsActivedChanged { get; }

        nint GetWindow(object element);

        nint GetXamlSourceHandle(object element);

        UiPoint GetCursorPos(object relativeTo);
    }
}
