using PropertyChanged;
﻿using Typedown.Core.Enums;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System.Globalization;

namespace Typedown.Core.Controls.SettingControls.SettingItems
{
[DoNotNotify]
        public sealed partial class GeneralSetting : UserControl
    {
        public AppViewModel ViewModel => DataContext as AppViewModel;

        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

        public GeneralSetting()
        {
            InitializeComponent();
        }

        public static bool IsStartupOpenFolderItemLoad(FolderStartupAction action)
        {
            return action == FolderStartupAction.OpenFolder;
        }

        private bool IsLangChanged(string settingLang)
        {
            try
            {
                var settingLanguage = Settings.Language;
                var currentLanguage = CultureInfo.CurrentUICulture.Name;
                return Locale.SupportedLangs.ContainsKey(settingLanguage) != Locale.SupportedLangs.ContainsKey(currentLanguage) || (Locale.SupportedLangs.ContainsKey(settingLanguage) && settingLanguage != currentLanguage);
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
