using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Styling;
using Typedown.Core.Enums;

namespace Typedown.Core.Converters
{
    /// <summary>
    /// Converts AppTheme to Avalonia ThemeVariant.
    /// In Avalonia, ElementTheme doesn't exist; use ThemeVariant instead.
    /// </summary>
    public class ElementThemeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is AppTheme theme)
            {
                return theme switch
                {
                    AppTheme.Light => ThemeVariant.Light,
                    AppTheme.Dark => ThemeVariant.Dark,
                    _ => ThemeVariant.Default,
                };
            }
            return ThemeVariant.Default;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
