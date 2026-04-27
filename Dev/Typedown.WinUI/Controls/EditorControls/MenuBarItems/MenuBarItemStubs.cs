using Microsoft.UI.Xaml.Controls;

namespace Typedown.WinUI.Controls;

public abstract partial class MenuBarItemBase : Microsoft.UI.Xaml.Controls.MenuBarItem
{
    protected MenuBarItemBase(string title)
    {
        Title = title;
    }

    protected void AddPlaceholder(string text)
    {
        Items.Add(new MenuFlyoutItem { Text = text });
    }
}

public sealed partial class FileItem : MenuBarItemBase
{
    public FileItem() : base("File")
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }

    private void OnOpenRecentSubMenuLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }

    private void OnExportSubMenuLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }
}

public sealed partial class EditItem : MenuBarItemBase
{
    public EditItem() : base("Edit")
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }
}

public sealed partial class ParagraphItem : MenuBarItemBase
{
    public ParagraphItem() : base("Paragraph")
    {
        InitializeComponent();
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }
}

public sealed partial class FormatItem : MenuBarItemBase
{
    public FormatItem() : base("Format")
    {
        InitializeComponent();
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }
}

public sealed partial class ViewItem : MenuBarItemBase
{
    public ViewItem() : base("View")
    {
        InitializeComponent();
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }
}
