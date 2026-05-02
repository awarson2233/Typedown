namespace Typedown.Core.Models
{
    public readonly record struct UiPoint(double X, double Y);

    public readonly record struct UiSize(double Width, double Height);

    public readonly record struct UiRect(double X, double Y, double Width, double Height);

    public readonly record struct UiThickness(double Left, double Top, double Right, double Bottom);
}
