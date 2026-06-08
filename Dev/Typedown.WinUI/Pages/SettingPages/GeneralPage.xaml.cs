using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.ComponentModel;
using Typedown.Core.Enums;
using Typedown.Presentation.ViewModels;
using Typedown.WinUI.Utilities;

namespace Typedown.WinUI.Pages.SettingPages
{
    public sealed partial class GeneralPage : Page
    {
        public AppViewModel? ViewModel { get; private set; }

        public SettingsViewModel? SettingsViewModel { get; private set; }

        public SettingsViewModel? Settings => ViewModel?.SettingsViewModel;

        private SettingsViewModel? subscribedSettings;

        public GeneralPage()
        {
            NavigationCacheMode = NavigationCacheMode.Enabled;
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is SettingsNavigationParameter parameter)
            {
                if (!ReferenceEquals(subscribedSettings, parameter.SettingsViewModel))
                {
                    DetachSettingsChangeHandler();
                    subscribedSettings = parameter.SettingsViewModel;
                    subscribedSettings.PropertyChanged += OnSettingsPropertyChanged;
                }

                ViewModel = parameter.AppViewModel;
                SettingsViewModel = parameter.SettingsViewModel;
                DataContext = ViewModel;
                Bindings.StopTracking();
                Bindings.Update();
            }
        }

        public static Visibility IsStartupOpenFolderItemLoad(FolderStartupAction action)
        {
            return action == FolderStartupAction.OpenFolder ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool IsLangChanged(string settingLang)
        {
            try
            {
                return WinUILocale.IsRestartRequiredForLanguage(settingLang);
            }
            catch
            {
                return false;
            }
        }

        private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName)
                || e.PropertyName == nameof(SettingsViewModel.FolderStartupAction)
                || e.PropertyName == nameof(SettingsViewModel.Language))
            {
                Bindings.Update();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachSettingsChangeHandler();
            Bindings.StopTracking();
        }

        private void DetachSettingsChangeHandler()
        {
            if (subscribedSettings is not null)
            {
                subscribedSettings.PropertyChanged -= OnSettingsPropertyChanged;
                subscribedSettings = null;
            }
        }
    }
}
