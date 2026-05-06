using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using PresentationLocale = Typedown.Presentation.Utilities.Locale;

namespace Typedown.WinUI.Controls
{
    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    public class LocaleString : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public PresentationLocale.ResourceSource Source { get; set; }

        protected override object ProvideValue(IXamlServiceProvider serviceProvider)
        {
            return PresentationLocale.GetString(Key, Source);
        }
    }
}
