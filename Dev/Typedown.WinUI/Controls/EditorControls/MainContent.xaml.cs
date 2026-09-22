using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Typedown.WinUI.Utilities;

namespace Typedown.WinUI.Controls;

public sealed partial class MainContent : UserControl
{
    public static readonly DependencyProperty IsLeftPaneLoadProperty =
        DependencyProperty.Register(nameof(IsLeftPaneLoad), typeof(bool), typeof(MainContent), new PropertyMetadata(true));

    public bool IsLeftPaneLoad
    {
        get => (bool)GetValue(IsLeftPaneLoadProperty);
        set => SetValue(IsLeftPaneLoadProperty, value);
    }

    public bool IsSidePaneOpen => IsLeftPaneLoad;

    private bool animationEnabled = true;

    public MainContent()
    {
        using (StartupTrace.Phase("MainContent.InitializeComponent"))
        {
            InitializeComponent();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, IsLeftPaneLoad ? "SidePaneExpand" : "SidePaneCollapse", false);
    }

    public void SetSidePaneOpen(bool isOpen)
    {
        IsLeftPaneLoad = isOpen;
        if (IsLoaded)
        {
            VisualStateManager.GoToState(this, isOpen ? "SidePaneExpand" : "SidePaneCollapse", animationEnabled);
        }
    }

    /// <summary>
    /// 编辑区宽度下限，对应 Muya 正文区与 body 的 <c>min-width: 400px</c>：页面视口再窄，正文就只能横向溢出。
    /// </summary>
    private const double EditorMinWidth = 400;

    /// <summary>
    /// 下限取 400 与当前实际可分给编辑区的宽度中的较小者。拖分隔条压不穿它（GridSplitter 会校验相邻星号列的 MinWidth）；
    /// 但窗口本身太窄时不硬撑——否则 Grid 会裁掉编辑区右侧连同竖向滚动条，那种情况下宁可出现一条真能滚动的横条。
    /// 窗口缩放由 Grid 的 SizeChanged 覆盖，侧栏展开/收起与拖动分隔条由编辑区的 SizeChanged 覆盖。
    /// </summary>
    private void OnLayoutSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var available = MainContentGrid.ActualWidth - LeftPaneColumn.ActualWidth - SplitterColumn.ActualWidth;
        var minWidth = Math.Clamp(available, 0, EditorMinWidth);
        if (EditorColumn.MinWidth != minWidth)
        {
            EditorColumn.MinWidth = minWidth;
        }
    }

    public static double GetColumnWidthNegative(GridLength length) => -length.Value;

    public void SetAnimationEnabled(bool isEnabled)
    {
        animationEnabled = isEnabled;
    }
}
