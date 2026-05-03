using System.Reactive.Subjects;
using Microsoft.UI.Xaml;
using Typedown.Core.Models;
using Typedown.Presentation.Interfaces;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIWindowService : IWindowService
    {
        private readonly IWindowContext windowContext;

        public WinUIWindowService(IWindowContext windowContext)
        {
            this.windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
        }

        public Subject<nint> WindowIsActivedChanged { get; } = new();

        public Subject<nint> WindowStateChanged { get; } = new();

        public UiPoint GetCursorPos(object relativeTo)
        {
            throw new NotSupportedException("WinUI cursor position conversion is not wired in this migration slice.");
        }

        public nint GetWindow(object element)
        {
            return element switch
            {
                Window => windowContext.WindowHandle,
                FrameworkElement => windowContext.WindowHandle,
                null => throw new ArgumentNullException(nameof(element)),
                _ => windowContext.WindowHandle
            };
        }

        public nint GetXamlSourceHandle(object element)
        {
            throw new NotSupportedException("WinUI XamlSource handle lookup is not available in the WinUI 3 composition root.");
        }
    }
}
