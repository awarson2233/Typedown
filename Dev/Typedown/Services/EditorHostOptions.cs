using System.Collections.Generic;

namespace Typedown.Services
{
    public static class EditorHostOptions
    {
        public static IReadOnlyList<string> WebView2Args { get; } = new List<string>()
        {
            "--disable-web-security",
            "--allow-file-access-from-files",
            "--flag-switches-begin",
            "--enable-features=msOverlayScrollbarWinStyle",
            "--flag-switches-end"
        };
    }
}
