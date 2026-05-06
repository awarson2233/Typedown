using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Typedown.Presentation.ViewModels;
using Typedown.Presentation.Utilities;

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
        DataContextChanged += OnDataContextChanged;
    }

    public bool IsSidePaneOpen
    {
        get => (bool)GetValue(IsSidePaneOpenProperty);
        set => SetValue(IsSidePaneOpenProperty, value);
    }

    public AppViewModel? ViewModel => DataContext as AppViewModel;

    public SettingsViewModel? Settings => ViewModel?.SettingsViewModel;

    public EditorViewModel? Editor => ViewModel?.EditorViewModel;

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

    private string CharacterUnit(int number) => number != 1 ? Locale.GetString("Characters") : Locale.GetString("Character");

    private string WordUnit(int number) => number != 1 ? Locale.GetString("Words") : Locale.GetString("Word");

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        Bindings?.Update();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Bindings?.StopTracking();
    }

    private static void OnIsSidePaneOpenChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not StatusBar statusBar || statusBar.suppressSidePaneEvent)
        {
            return;
        }

        statusBar.SidePaneOpenChanged?.Invoke(statusBar, (bool)e.NewValue);
    }
}
