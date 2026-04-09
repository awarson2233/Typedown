using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Typedown.Core.Converters
{
    /// <summary>
    /// Converts empty/null string to visibility (bool).
    /// In Avalonia: true = visible, false = collapsed.
    /// </summary>
    public class EmptyToVisibilityConverter : IValueConverter
    {
        public bool IsReverse { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var res = string.IsNullOrEmpty(value as string);
            if (IsReverse) res = !res;
            return res;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
