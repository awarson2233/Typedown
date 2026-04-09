using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace Typedown.Core.Converters
{
    public class GridLengthToDoubleConverter : IValueConverter
    {
        public GridUnitType GridUnitType { get; set; } = GridUnitType.Pixel;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is GridLength length ? length.Value : 0d;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return new GridLength(value is double length ? length : 0, GridUnitType);
        }
    }
}
