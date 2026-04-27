namespace Typedown.WinUI.Controls;

public sealed partial class CodeFencesItem : MenuItemCollection
{
    public CodeFencesItem()
    {
        InitializeComponent();
    }
}

public sealed partial class ContextFormatItem : MenuFlyoutItem
{
    public ContextFormatItem()
    {
        InitializeComponent();
    }
}

public sealed partial class ImageItem : MenuItemCollection
{
    public ImageItem()
    {
        InitializeComponent();
    }

    private void OnOpenImageLocationItemLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }

    private void OnOpenImageLocationClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }

    private void OnCopyImageToClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }

    private void OnMoveImageToClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }

    private void OnImageUploadSettingsClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }

    private void OnSaveImageClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }

    private void OnDeleteImageFileClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { }
}
