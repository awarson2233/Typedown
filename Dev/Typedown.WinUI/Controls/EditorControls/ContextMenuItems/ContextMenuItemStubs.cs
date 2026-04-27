namespace Typedown.WinUI.Controls;

public sealed class CodeFencesItem : MenuItemCollection
{
    public CodeFencesItem()
    {
        Items.Add(new MenuFlyoutItem { Text = "Format Code" });
    }
}

public sealed class ContextFormatItem : MenuFlyoutItem
{
    public ContextFormatItem()
    {
        Text = "Format";
    }
}

public sealed class ImageItem : MenuItemCollection
{
    public ImageItem()
    {
        Items.Add(new MenuFlyoutItem { Text = "Open Image Location" });
        Items.Add(new MenuFlyoutItem { Text = "Save Image As" });
    }
}