using System;
namespace Typedown.WinUI.Controls.SidePaneControls.Pages
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
