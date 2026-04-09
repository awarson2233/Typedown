using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Typedown.Core.Models;
using Typedown.Core.Utilities;

namespace Typedown.Core.Converters
{
    public class ShortcutKeyToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is ShortcutKey key ? Common.GetShortcutKeyText(key) : null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
