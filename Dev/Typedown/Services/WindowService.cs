using System;
using System.Reactive.Subjects;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.XamlUI;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Typedown.Services
{
    public class WindowService : IWindowService
    {
        private readonly IWindowContext windowContext;

        public WindowService(IWindowContext windowContext)
        {
            this.windowContext = windowContext;
        }

        public Subject<nint> WindowStateChanged { get; } = new();

        public Subject<nint> WindowIsActivedChanged { get; } = new();

        public void RaiseWindowStateChanged(nint hWnd) => WindowStateChanged.OnNext(hWnd);

        public void RaiseWindowIsActivedChanged(nint hWnd) => WindowIsActivedChanged.OnNext(hWnd);

        public nint GetWindow(object element) => XamlWindow.GetWindow((UIElement)element)?.Handle ?? windowContext.WindowHandle;

        public nint GetXamlSourceHandle(object element) => XamlWindow.GetWindow((UIElement)element)?.XamlSourceHandle ?? default;

        public UiPoint GetCursorPos(object relativeTo)
        {
            if (relativeTo == null)
                throw new ArgumentNullException(nameof(relativeTo));
            var relativeElement = (UIElement)relativeTo;

            var window = XamlWindow.GetWindow(relativeElement);
            if (window == null || relativeElement.XamlRoot?.Content == null)
                throw new InvalidOperationException("The specified UI element is not attached to a window.");

            PInvoke.GetCursorPos(out var screenPos);
            PInvoke.GetWindowRect(window.XamlSourceHandle, out var xamlRootRect);
            var pos = new Point((screenPos.X - xamlRootRect.left) / window.ScalingFactor, (screenPos.Y - xamlRootRect.top) / window.ScalingFactor);
            var point = relativeElement.XamlRoot.Content.TransformToVisual(relativeElement).TransformPoint(pos);
            return new(point.X, point.Y);
        }
    }
}
