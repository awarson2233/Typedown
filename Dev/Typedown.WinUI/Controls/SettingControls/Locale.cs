using System.Collections.Generic;
using CoreLocale = Typedown.Core.Utilities.Locale;

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

        public static IReadOnlyDictionary<string, string> SupportedLangs => CoreLocale.SupportedLangs;

        public static IReadOnlyDictionary<string, string> LangsOptions => CoreLocale.LangsOptions;

        public static IReadOnlyList<string> LangOptionKeys => CoreLocale.LangOptionKeys;

        public static string GetLangOptionDisplayName(string key) => CoreLocale.GetLangOptionDisplayName(key);

        public static string GetString(string key, ResourceSource source = ResourceSource.All)
        {
            return CoreLocale.GetString(key, (CoreLocale.ResourceSource)source);
        }

        public static string GetDialogString(string key)
        {
            return CoreLocale.GetDialogString(key);
        }
    }
}
