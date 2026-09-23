using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Typedown.Core.ViewModels;

namespace Typedown.WinUI.Controls;

public sealed partial class ContextFormatItem : MenuFlyoutItem
{
    public static DependencyProperty FormatCommandProperty { get; } = DependencyProperty.Register(nameof(FormatCommand), typeof(ICommand), typeof(ContextFormatItem), null);

    // Resolved from the AppViewModel DataContext so the template can use TemplateBinding instead of a reflection {Binding}.
    public ICommand? FormatCommand { get => (ICommand?)GetValue(FormatCommandProperty); set => SetValue(FormatCommandProperty, value); }

    public ContextFormatItem()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        FormatCommand = (args.NewValue as AppViewModel)?.FormatViewModel.SetFormatCommand;
    }
}
