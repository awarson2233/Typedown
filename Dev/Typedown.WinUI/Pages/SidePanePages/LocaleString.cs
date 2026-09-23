using Microsoft.UI.Xaml.Markup;
using CoreLocale = Typedown.Core.Utilities.Locale;

namespace Typedown.WinUI.Pages.SidePanePages
{
    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    public sealed partial class LocaleString : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public CoreLocale.ResourceSource Source { get; set; }

        protected override object ProvideValue()
        {
            return CoreLocale.GetString(Key, Source);
        }
    }
}
