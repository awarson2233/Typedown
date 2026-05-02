using System;
using Typedown.Controls.SidePanelControls.Pages;

namespace Typedown.Controls.SidePaneControls.Pages
{
    public static class Route
    {
        public static Type GetSidePanePageType(string name) => name switch
        {
            "Toc" => typeof(TocPage),
            "Folder" => typeof(FolderPage),
            _ => null
        };
    }
}
