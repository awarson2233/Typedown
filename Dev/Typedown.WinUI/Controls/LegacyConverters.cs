using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Typedown.WinUI.Controls;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool IsReverse { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var visible = value is bool b && b;
        if (IsReverse) visible = !visible;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var visible = value is Visibility visibleValue && visibleValue == Visibility.Visible;
        return IsReverse ? !visible : visible;
    }
}

public sealed class BoolToObjectConverter : IValueConverter
{
    public object? TrueValue { get; set; }
    public object? FalseValue { get; set; }

    public object? Convert(object value, Type targetType, object parameter, string language) => value is bool b && b ? TrueValue : FalseValue;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

public sealed class GridLengthToDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value is GridLength length ? length.Value : 0d;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => value is double d ? new GridLength(d) : new GridLength(0d);
}