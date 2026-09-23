using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using CoreLocale = Typedown.Core.Utilities.Locale;

namespace Typedown.WinUI.Controls
{
    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    public partial class LocaleString : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public CoreLocale.ResourceSource Source { get; set; }

        protected override object ProvideValue(IXamlServiceProvider serviceProvider)
        {
            return CoreLocale.GetString(Key, Source);
        }
    }
}
