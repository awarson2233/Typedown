namespace Typedown.WinUI.Controls;

public sealed partial class EditorContainer : UserControl
{
    public static readonly DependencyProperty IsFindReplaceLoadProperty =
        DependencyProperty.Register(nameof(IsFindReplaceLoad), typeof(bool), typeof(EditorContainer), new PropertyMetadata(false));

    public bool IsFindReplaceLoad
    {
        get => (bool)GetValue(IsFindReplaceLoadProperty);
        set => SetValue(IsFindReplaceLoadProperty, value);
    }

    public EditorContainer()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        MarkdownEditorPresenter.Content ??= new WinUIEditorHost();
        VisualStateManager.GoToState(this, "FindReplaceCollapsed", false);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        MarkdownEditorPresenter.Content = null;
    }

    private void OnDragEnter(object sender, DragEventArgs e) { }

    private void OnDrop(object sender, DragEventArgs e) { }

    private void OnScroll(object sender, Microsoft.UI.Xaml.Controls.Primitives.ScrollEventArgs e) { }
}
