using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Typedown.Core.Converters
{
    /// <summary>
    /// Converts bool to IsVisible (bool) for Avalonia.
    /// In Avalonia there is no Visibility enum; use bool (true = visible, false = collapsed).
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool IsReverse { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var res = value is bool b && b;
            if (IsReverse) res = !res;
            return res;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var res = value is bool b && b;
            return IsReverse ? !res : res;
        }
    }
}
