namespace Typedown.WinUI.Controls;

public sealed partial class StatusBar : UserControl
{
    private bool suppressSidePaneEvent;

    public static readonly DependencyProperty IsSidePaneOpenProperty = DependencyProperty.Register(
        nameof(IsSidePaneOpen),
        typeof(bool),
        typeof(StatusBar),
        new PropertyMetadata(true, OnIsSidePaneOpenChanged));

    public event EventHandler<bool>? SidePaneOpenChanged;

    public StatusBar()
    {
        InitializeComponent();
    }

    public bool IsSidePaneOpen
    {
        get => (bool)GetValue(IsSidePaneOpenProperty);
        set => SetValue(IsSidePaneOpenProperty, value);
    }

    public void SetSidePaneOpen(bool isOpen)
    {
        suppressSidePaneEvent = true;
        try
        {
            IsSidePaneOpen = isOpen;
        }
        finally
        {
            suppressSidePaneEvent = false;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) { }

    private static void OnIsSidePaneOpenChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not StatusBar statusBar || statusBar.suppressSidePaneEvent)
        {
            return;
        }

        statusBar.SidePaneOpenChanged?.Invoke(statusBar, (bool)e.NewValue);
    }
}
