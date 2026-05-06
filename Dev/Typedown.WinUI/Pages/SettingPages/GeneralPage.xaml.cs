using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Typedown.Core.Enums;
using Typedown.Presentation.Utilities;
using Typedown.Presentation.ViewModels;
using Windows.Globalization;

namespace Typedown.WinUI.Pages.SettingPages
{
    public sealed partial class GeneralPage : Page
    {
        public AppViewModel? ViewModel { get; private set; }

        public SettingsViewModel? SettingsViewModel { get; private set; }

        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

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
                ViewModel = parameter.AppViewModel;
                SettingsViewModel = parameter.SettingsViewModel;
                DataContext = ViewModel;
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
                var settingLanguage = Settings.Language;
                var currentLanguage = ApplicationLanguages.PrimaryLanguageOverride;
                return Locale.SupportedLangs.ContainsKey(settingLanguage) != Locale.SupportedLangs.ContainsKey(currentLanguage)
                    || (Locale.SupportedLangs.ContainsKey(settingLanguage) && settingLanguage != currentLanguage);
            }
            catch
            {
                return false;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
