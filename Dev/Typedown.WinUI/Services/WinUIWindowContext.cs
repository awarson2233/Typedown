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

        internal event EventHandler<nint>? WindowActivationChanged;

        internal event EventHandler<nint>? WindowStateChanged;

        public WinUIWindowContext(Window window)
        {
            this.window = window ?? throw new ArgumentNullException(nameof(window));
            WindowHandle = WindowNative.GetWindowHandle(window);
            viewRoot = window.Content?.XamlRoot;
            isActive = true;

            window.Activated += OnWindowActivated;
            window.Closed += OnWindowClosed;
            window.AppWindow.Changed += OnAppWindowChanged;
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
                window.AppWindow.Show();
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

            if (isClosed)
            {
                return;
            }

            if (IsActive)
            {
                WindowHandle = WindowNative.GetWindowHandle(window);
                viewRoot = window.Content?.XamlRoot ?? viewRoot;
            }

            WindowActivationChanged?.Invoke(this, WindowHandle);
        }

        private void OnAppWindowChanged(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
        {
            if (!isClosed)
            {
                WindowStateChanged?.Invoke(this, WindowHandle);
            }
        }

        private void OnWindowClosed(object sender, WindowEventArgs args)
        {
            isClosed = true;
            isActive = false;
            viewRoot = null;
            WindowHandle = default;
            window.Activated -= OnWindowActivated;
            window.Closed -= OnWindowClosed;
            window.AppWindow.Changed -= OnAppWindowChanged;
        }
    }
}
