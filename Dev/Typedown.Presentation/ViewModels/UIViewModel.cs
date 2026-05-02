using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Typedown.Core;
using Typedown.Core.Enums;
using Typedown.Core.Interfaces;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Utilities;

namespace Typedown.Presentation.ViewModels
{
    public sealed partial class UIViewModel : INotifyPropertyChanged, IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public AppViewModel AppViewModel => ServiceProvider.GetService<AppViewModel>();

        public EditorViewModel EditorViewModel => ServiceProvider.GetService<EditorViewModel>();

        public FileViewModel FileViewModel => ServiceProvider.GetService<FileViewModel>();

        public SettingsViewModel SettingsViewModel => ServiceProvider.GetService<SettingsViewModel>();

        public RemoteInvoke RemoteInvoke => ServiceProvider.GetService<RemoteInvoke>();

        public string MainWindowTitle { get; private set; }

        public AppTheme ActualTheme { get; private set; }

        public double CaptionHeight { get; set; } = 32;

        private readonly CompositeDisposable disposables = new();

        private readonly IUiDispatcher dispatcher;

        public UIViewModel(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            dispatcher = ServiceProvider.GetService<IUiDispatcher>();
            disposables.Add(RemoteInvoke.Handle<JToken, object>("GetStringResources", GetStringResources));
            _ = dispatcher.RunIdleAsync(() => InitializeBinding());
        }

        private void InitializeBinding()
        {
            if (disposables.IsDisposed)
                return;
            disposables.Add(EditorViewModel.WhenPropertyChanged(nameof(EditorViewModel.DisplaySaved)).Subscribe(_ => UpdateTitle()));
            disposables.Add(FileViewModel.WhenPropertyChanged(nameof(FileViewModel.FileName)).Subscribe(_ => UpdateTitle()));
            disposables.Add(SettingsViewModel.WhenPropertyChanged(nameof(SettingsViewModel.AppTheme)).Subscribe(_ => UpdateActualTheme()));
            UpdateTitle();
            UpdateActualTheme();
        }

        private object GetStringResources(JToken args)
        {
            try
            {
                return args["names"].ToObject<List<string>>().ToDictionary(x => x, x => Locale.GetString(x));
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private void UpdateActualTheme()
        {
            _ = dispatcher?.RunIdleAsync(() =>
            {
                try
                {
                    if (SettingsViewModel.AppTheme != AppTheme.Default)
                        SetActualTheme(SettingsViewModel.AppTheme);
                }
                catch
                {
                    // Ignore
                }
            });
        }

        public void SetActualTheme(AppTheme actualTheme)
        {
            if (actualTheme == AppTheme.Light || actualTheme == AppTheme.Dark)
                ActualTheme = actualTheme;
        }

        private void UpdateTitle()
        {
            try
            {
                var title = new StringBuilder();
                if (!AppViewModel.EditorViewModel.DisplaySaved)
                    title.Append('*');
                if (AppViewModel.FileViewModel.FileName != null)
                    title.Append(AppViewModel.FileViewModel.FileName + " - ");
                title.Append(Config.AppName);
                MainWindowTitle = title.ToString();
            }
            catch
            {
                // Ignore
            }
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
