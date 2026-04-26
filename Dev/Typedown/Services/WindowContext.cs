using System;
using Typedown.Core.Interfaces;
using Typedown.Core.Utilities;
using Typedown.XamlUI;
using Windows.UI.Xaml;

namespace Typedown.Services
{
    public class WindowContext : IWindowContext
    {
        private const int ScClose = 0xF060;

        private XamlWindow window;

        public nint WindowHandle { get; set; }

        public object ViewRoot { get; set; }

        public string Title { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public void Bind(XamlWindow value)
        {
            window = value;
            Refresh();
        }

        public void Refresh()
        {
            if (window == null)
                return;

            WindowHandle = window.Handle;
            Title = window.Title ?? string.Empty;
            IsActive = window.IsActive;
            ViewRoot ??= (window.Content as UIElement)?.XamlRoot;
        }

        public void Activate()
        {
            if (window != null)
                _ = window.TryActive();
            BringToFront();
        }

        public void BringToFront()
        {
            if (WindowHandle != default)
                PInvoke.SetForegroundWindow(WindowHandle);
        }

        public void RequestClose()
        {
            if (WindowHandle != default)
                PInvoke.PostMessage(WindowHandle, (uint)PInvoke.WindowMessage.WM_SYSCOMMAND, (nint)ScClose, IntPtr.Zero);
        }

        public void Clear()
        {
            window = null;
            WindowHandle = default;
            ViewRoot = null;
            Title = string.Empty;
            IsActive = false;
        }
    }
}
