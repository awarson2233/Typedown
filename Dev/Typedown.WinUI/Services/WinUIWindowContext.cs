using System;
using Microsoft.UI.Xaml;
using Typedown.Presentation.Interfaces;
using WinRT.Interop;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIWindowContext : IWindowContext
    {
        private readonly Window window;
        private object? viewRoot;
        private bool isActive;
        private bool isClosed;

        public WinUIWindowContext(Window window)
        {
            this.window = window ?? throw new ArgumentNullException(nameof(window));
            WindowHandle = WindowNative.GetWindowHandle(window);
            viewRoot = window.Content?.XamlRoot;
            isActive = true;

            window.Activated += OnWindowActivated;
            window.Closed += OnWindowClosed;

            var appWindow = GetAppWindow(window);
            if (appWindow != null)
            {
                appWindow.SetIcon("Assets/logo.ico");
            }
        }

        private Microsoft.UI.Windowing.AppWindow? GetAppWindow(Window window)
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        }

        public nint WindowHandle { get; set; }

        public object ViewRoot
        {
            get
            {
                if (isClosed)
                {
                    throw new InvalidOperationException("WinUI window view root is unavailable after the window is closed.");
                }

                if (viewRoot is not null)
                {
                    return viewRoot;
                }

                if (window.Content?.XamlRoot is { } xamlRoot)
                {
                    return xamlRoot;
                }

                if (window.Content is { } content)
                {
                    return content;
                }

                throw new InvalidOperationException("WinUI window view root is not initialized.");
            }
            set => viewRoot = value;
        }

        public string Title
        {
            get => isClosed ? string.Empty : window.Title ?? string.Empty;
            set
            {
                if (!isClosed)
                {
                    window.Title = value ?? string.Empty;
                }
            }
        }

        public bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }

        public void Activate()
        {
            if (!isClosed)
            {
                window.Activate();
            }
        }

        public void BringToFront()
        {
            Activate();
        }

        public void RequestClose()
        {
            if (!isClosed)
            {
                window.Close();
            }
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            IsActive = args.WindowActivationState != WindowActivationState.Deactivated;

            if (isClosed || !IsActive)
            {
                return;
            }

            WindowHandle = WindowNative.GetWindowHandle(window);
            viewRoot = window.Content?.XamlRoot ?? viewRoot;
        }

        private void OnWindowClosed(object sender, WindowEventArgs args)
        {
            isClosed = true;
            isActive = false;
            viewRoot = null;
            window.Activated -= OnWindowActivated;
            window.Closed -= OnWindowClosed;
        }
    }
}
