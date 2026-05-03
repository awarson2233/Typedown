using System;
using System.Collections.Generic;
using Typedown.Presentation.Utilities;
using Typedown.WinUI.Pages.SettingPages;

namespace Typedown.WinUI.Pages
{
    public static class Route
    {
        private static readonly IReadOnlyDictionary<Type, (string Key, string Fallback)> SettingsPageTitles = new Dictionary<Type, (string, string)>
        {
            [typeof(GeneralPage)] = ("General.Title", "General"),
            [typeof(ViewPage)] = ("View.Title", "View"),
            [typeof(EditorPage)] = ("Editor.Title", "Editor"),
            [typeof(ImagePage)] = ("Image.Title", "Image"),
            [typeof(ExportPage)] = ("Export.Title", "Export"),
            [typeof(AboutPage)] = ("About.Title", "About")
        };

        public static Type? GetRootPageType(string? name) => name switch
        {
            "Main" => typeof(MainPage),
            "Settings" => typeof(SettingsPage),
            _ => null
        };

        public static Type? GetSettingsPageType(string? name) => name switch
        {
            "About" => typeof(AboutPage),
            "Editor" => typeof(EditorPage),
            "Export" => typeof(ExportPage),
            "General" => typeof(GeneralPage),
            "Image" => typeof(ImagePage),
            "View" => typeof(ViewPage),
            _ => null
        };

        public static string GetSettingsPageTitle(Type? type)
        {
            if (type != null && SettingsPageTitles.TryGetValue(type, out var title))
            {
                return GetSettingsPageTitle(title.Key, title.Fallback);
            }

            return GetSettingsPageTitle("Settings", "Settings");
        }

        private static string GetSettingsPageTitle(string key, string fallback)
        {
            var title = Locale.GetString(key, Locale.ResourceSource.SettingsResources);
            return string.IsNullOrWhiteSpace(title) || string.Equals(title, key, StringComparison.Ordinal)
                ? fallback
                : title;
        }
    }
}
