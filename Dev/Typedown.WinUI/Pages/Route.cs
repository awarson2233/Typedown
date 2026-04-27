using System;
using Typedown.WinUI.Pages.SettingPages;

namespace Typedown.WinUI.Pages
{
    public static class Route
    {
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
            if (type == typeof(GeneralPage)) return "General";
            if (type == typeof(ViewPage)) return "View";
            if (type == typeof(EditorPage)) return "Editor";
            if (type == typeof(ImagePage)) return "Image";
            if (type == typeof(ExportPage)) return "Export";
            if (type == typeof(AboutPage)) return "About";
            return "Settings";
        }
    }
}
