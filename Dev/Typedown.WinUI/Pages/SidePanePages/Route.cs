using System;
namespace Typedown.WinUI.Pages.SidePanePages
{
    public static class Route
    {
        public static Type? GetSidePanePageType(string name) => name switch
        {
            "Toc" => typeof(TocPage),
            "Folder" => typeof(FolderPage),
            _ => null
        };
    }
}
