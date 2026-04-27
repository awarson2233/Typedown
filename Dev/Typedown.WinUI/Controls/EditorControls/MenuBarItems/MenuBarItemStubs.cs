using Microsoft.UI.Xaml.Controls;

namespace Typedown.WinUI.Controls;

public abstract class MenuBarItemBase : Microsoft.UI.Xaml.Controls.MenuBarItem
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

public sealed class FileItem : MenuBarItemBase
{
    public FileItem() : base("File")
    {
        AddPlaceholder("New");
        AddPlaceholder("Open");
        AddPlaceholder("Save");
    }
}

public sealed class EditItem : MenuBarItemBase
{
    public EditItem() : base("Edit")
    {
        AddPlaceholder("Undo");
        AddPlaceholder("Redo");
        AddPlaceholder("Find");
    }
}

public sealed class ParagraphItem : MenuBarItemBase
{
    public ParagraphItem() : base("Paragraph")
    {
        AddPlaceholder("Heading");
        AddPlaceholder("Paragraph");
        AddPlaceholder("Table");
    }
}

public sealed class FormatItem : MenuBarItemBase
{
    public FormatItem() : base("Format")
    {
        AddPlaceholder("Strong");
        AddPlaceholder("Emphasis");
        AddPlaceholder("Code");
    }
}

public sealed class ViewItem : MenuBarItemBase
{
    public ViewItem() : base("View")
    {
        AddPlaceholder("Side Pane");
        AddPlaceholder("Status Bar");
    }
}