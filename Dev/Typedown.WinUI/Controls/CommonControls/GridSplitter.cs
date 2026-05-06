using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Typedown.WinUI.Controls;

public class GridSplitter : UserControl
{
    private static readonly Microsoft.UI.Input.InputCursor ResizeCursor =
        Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast);

    public static readonly DependencyProperty ColumnWidthProperty =
        DependencyProperty.Register(nameof(ColumnWidth), typeof(double), typeof(GridSplitter), new PropertyMetadata(0d));

    public double ColumnWidth
    {
        get => (double)GetValue(ColumnWidthProperty);
        set => SetValue(ColumnWidthProperty, value);
    }

    public static readonly DependencyProperty ColumnExpectWidthProperty =
        DependencyProperty.Register(nameof(ColumnExpectWidth), typeof(double), typeof(GridSplitter), new PropertyMetadata(0d));

    public double ColumnExpectWidth
    {
        get => (double)GetValue(ColumnExpectWidthProperty);
        set => SetValue(ColumnExpectWidthProperty, value);
    }

    public static readonly DependencyProperty ColumnMinWidthProperty =
        DependencyProperty.Register(nameof(ColumnMinWidth), typeof(double), typeof(GridSplitter), new PropertyMetadata(0d));

    public double ColumnMinWidth
    {
        get => (double)GetValue(ColumnMinWidthProperty);
        set => SetValue(ColumnMinWidthProperty, value);
    }

    public static readonly DependencyProperty ColumnMaxWidthProperty =
        DependencyProperty.Register(nameof(ColumnMaxWidth), typeof(double), typeof(GridSplitter), new PropertyMetadata(double.PositiveInfinity));

    public double ColumnMaxWidth
    {
        get => (double)GetValue(ColumnMaxWidthProperty);
        set => SetValue(ColumnMaxWidthProperty, value);
    }

    private readonly Border border = new()
    {
        Width = 9,
        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)
    };

    private double dragStartX;

    private double dragStartWidth;

    private bool dragging;

    public GridSplitter()
    {
        Margin = new Thickness(-4, 0, -4, 0);
        Content = border;
    }

    protected override void OnPointerEntered(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        ProtectedCursor = ResizeCursor;
    }

    protected override void OnPointerExited(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);

        if (!dragging)
        {
            ProtectedCursor = null;
        }
    }

    protected override void OnPointerPressed(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (Parent is not Grid parentGrid)
        {
            return;
        }

        dragging = true;
        dragStartX = e.GetCurrentPoint(parentGrid).Position.X;
        dragStartWidth = GetTargetColumn(parentGrid)?.ActualWidth ?? ColumnWidth;
        CapturePointer(e.Pointer);
        e.Handled = true;
    }

    protected override void OnPointerMoved(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!dragging || Parent is not Grid parentGrid)
        {
            return;
        }

        var currentX = e.GetCurrentPoint(parentGrid).Position.X;
        ColumnExpectWidth = dragStartWidth + currentX - dragStartX;
        ApplyColumnWidth();
        e.Handled = true;
    }

    protected override void OnPointerReleased(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        CompleteDrag(e);
    }

    protected override void OnPointerCanceled(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerCanceled(e);
        CompleteDrag(e);
    }

    private void CompleteDrag(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!dragging)
        {
            return;
        }

        dragging = false;
        ProtectedCursor = null;
        ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private void ApplyColumnWidth()
    {
        if (Parent is not Grid parentGrid)
        {
            return;
        }

        var column = GetTargetColumn(parentGrid);
        if (column is null)
        {
            return;
        }

        var limitedWidth = Math.Min(Math.Max(ColumnMinWidth, ColumnExpectWidth), ColumnMaxWidth);
        ColumnWidth = limitedWidth;
        column.Width = new GridLength(limitedWidth);
    }

    private ColumnDefinition? GetTargetColumn(Grid parentGrid)
    {
        var targetColumnIndex = Math.Max(0, Grid.GetColumn(this) - 1);
        return targetColumnIndex < parentGrid.ColumnDefinitions.Count
            ? parentGrid.ColumnDefinitions[targetColumnIndex]
            : null;
    }
}
