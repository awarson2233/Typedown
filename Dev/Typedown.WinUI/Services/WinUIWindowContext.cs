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

        public WinUIWindowContext(Window window)
        {
            this.window = window ?? throw new ArgumentNullException(nameof(window));
            WindowHandle = WindowNative.GetWindowHandle(window);
            viewRoot = window.Content?.XamlRoot;
            isActive = true;

            window.Activated += OnWindowActivated;
        }

        public nint WindowHandle { get; set; }

        public object? ViewRoot
        {
            get => viewRoot ?? window.Content?.XamlRoot;
            set => viewRoot = value;
        }

        public string? Title
        {
            get => window.Title;
            set => window.Title = value ?? string.Empty;
        }

        public bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }

        public void Activate()
        {
            window.Activate();
        }

        public void BringToFront()
        {
            window.Activate();
        }

        public void RequestClose()
        {
            window.Close();
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            IsActive = args.WindowActivationState != WindowActivationState.Deactivated;
            WindowHandle = WindowNative.GetWindowHandle(window);
            viewRoot = window.Content?.XamlRoot ?? viewRoot;
        }
    }
}
