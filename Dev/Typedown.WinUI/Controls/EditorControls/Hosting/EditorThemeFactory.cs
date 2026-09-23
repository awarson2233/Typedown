using Microsoft.UI.Xaml;
using Typedown.Core.Editor;
using Windows.UI.ViewManagement;

namespace Typedown.WinUI.Controls
{
    /// <summary>
    /// 编辑器主题的唯一来源：页面启动时索取的主题（GetCurrentTheme）与系统主题切换时推送的主题（ApplyTheme）
    /// 都从这里生成，强调色一律取系统强调色，背景色与宿主的编辑区背景一致。
    /// </summary>
    internal static class EditorThemeFactory
    {
        public static readonly EditorColor FallbackAccent = new(27, 102, 107, 1);

        public static readonly EditorColor DarkBackground = new(40, 40, 40, 1);

        public static readonly EditorColor LightBackground = new(249, 249, 249, 1);

        private static readonly UISettings uiSettings = new();

        /// <summary>按宿主当前的实际主题生成；<see cref="ElementTheme.Default"/> 按浅色处理。</summary>
        public static EditorTheme Create(ElementTheme actualTheme)
        {
            var isDark = actualTheme == ElementTheme.Dark;
            return new EditorTheme(isDark, ResolveSystemAccent(), isDark ? DarkBackground : LightBackground);
        }

        private static EditorColor ResolveSystemAccent()
        {
            try
            {
                var accent = uiSettings.GetColorValue(UIColorType.Accent);
                return new EditorColor(accent.R, accent.G, accent.B, 1);
            }
            catch
            {
                return FallbackAccent;
            }
        }
    }
}
