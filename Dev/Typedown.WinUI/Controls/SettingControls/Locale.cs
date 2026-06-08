using System.Collections.Generic;
using PresentationLocale = Typedown.Presentation.Utilities.Locale;

namespace Typedown.WinUI.Controls.SettingControls
{
    public static class Locale
    {
        public enum ResourceSource
        {
            All,
            CommonResources,
            DialogResources,
            SettingsResources,
            Resources
        }

        public static IReadOnlyDictionary<string, string> SupportedLangs => PresentationLocale.SupportedLangs;

        public static IReadOnlyDictionary<string, string> LangsOptions => PresentationLocale.LangsOptions;

        public static IReadOnlyList<string> LangOptionKeys => PresentationLocale.LangOptionKeys;

        public static string GetLangOptionDisplayName(string key) => PresentationLocale.GetLangOptionDisplayName(key);

        public static string GetString(string key, ResourceSource source = ResourceSource.All)
        {
            return PresentationLocale.GetString(key, (PresentationLocale.ResourceSource)source);
        }

        public static string GetDialogString(string key)
        {
            return PresentationLocale.GetDialogString(key);
        }
    }
}
