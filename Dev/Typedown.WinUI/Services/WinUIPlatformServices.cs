using System;
using Microsoft.UI.Xaml;
using Typedown.Core.Interfaces;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIPlatformServices
    {
        public WinUIPlatformServices(Window window)
        {
            if (window is null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            WindowContext = new WinUIWindowContext(window);
            UiDispatcher = new WinUIUiDispatcher(window.DispatcherQueue);
            AppDataPathProvider = new WinUIAppDataPathProvider();
            DialogService = new WinUIDialogService(WindowContext);
            FilePickerService = new WinUIFilePickerService(WindowContext);
            AppActivationService = new WinUIAppActivationService(WindowContext);
        }

        public IAppDataPathProvider AppDataPathProvider { get; }

        public IDialogService DialogService { get; }

        public IFilePickerService FilePickerService { get; }

        public IUiDispatcher UiDispatcher { get; }

        public IWindowContext WindowContext { get; }

        public IAppActivationService AppActivationService { get; }

        public string[] ServiceNames { get; } =
        {
            nameof(WinUIAppDataPathProvider),
            nameof(WinUIWindowContext),
            nameof(WinUIUiDispatcher),
            nameof(WinUIDialogService),
            nameof(WinUIFilePickerService),
            nameof(WinUIAppActivationService)
        };
    }
}
