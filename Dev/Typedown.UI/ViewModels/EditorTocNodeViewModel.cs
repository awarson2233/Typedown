using Typedown.Core.Contracts.EditorRuntime;

namespace Typedown.UI.ViewModels;

public sealed record EditorTocNodeViewModel(
    EditorTocItem Item,
    int Depth,
    IReadOnlyList<EditorTocNodeViewModel> Children)
{
    public string Slug => Item.Slug;

    public int Level => Item.Level;

    public string Content => Item.Content;

    public bool IsSelected => Item.IsSelected;

    public static IReadOnlyList<EditorTocNodeViewModel> CreateTree(IEnumerable<EditorTocItem>? items)
    {
        return CreateTree(items?.ToArray() ?? Array.Empty<EditorTocItem>(), depth: 1);
    }

    private static IReadOnlyList<EditorTocNodeViewModel> CreateTree(
        IReadOnlyList<EditorTocItem> items,
        int depth)
    {
        if (items.Count == 0)
        {
            return Array.Empty<EditorTocNodeViewModel>();
        }

        var nodes = new List<EditorTocNodeViewModel>();
        var index = 0;

        while (index < items.Count)
        {
            var parent = items[index++];
            var descendants = new List<EditorTocItem>();

            while (index < items.Count && items[index].Level > parent.Level)
            {
                descendants.Add(items[index++]);
            }

            nodes.Add(new EditorTocNodeViewModel(
                parent,
                depth,
                CreateTree(descendants, depth + 1)));
        }

        return nodes;
    }
}
