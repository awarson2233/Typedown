using Microsoft.UI.Xaml.Markup;
using PresentationLocale = Typedown.Presentation.Utilities.Locale;

namespace Typedown.WinUI.Pages.SidePanePages
{
    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    public sealed class LocaleString : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public PresentationLocale.ResourceSource Source { get; set; }

        protected override object ProvideValue()
        {
            return PresentationLocale.GetString(Key, Source);
        }
    }
}
