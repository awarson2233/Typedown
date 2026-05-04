using Typedown.Presentation.ViewModels;
using System.ComponentModel;

namespace Typedown.WinUI.Controls;

public sealed partial class EditorContainer : UserControl
{
    private WinUIEditorHost? editorHost;
    private FindReplace? findReplaceDialog;
    private AppViewModel? viewModel;

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
        DataContextChanged += OnDataContextChanged;
        SizeChanged += OnSizeChanged;
        FindReplacePopup.Opened += OnFindReplacePopupOpened;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(DataContext as AppViewModel);

        if (editorHost is null)
        {
            var serviceProvider = (DataContext as AppViewModel)?.ServiceProvider;
            editorHost = new WinUIEditorHost(serviceProvider);
            editorHost.ContextMenuRequested += OnEditorContextMenuRequested;
        }

        MarkdownEditorPresenter.Content ??= editorHost;
        UpdateFindReplaceState(viewModel?.FloatViewModel.FindReplaceDialogOpen ?? FloatViewModel.FindReplaceDialogState.None, false);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(null);

        if (editorHost is not null)
        {
            editorHost.ContextMenuRequested -= OnEditorContextMenuRequested;
            editorHost = null;
        }

        MarkdownEditorPresenter.Content = null;
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        AttachViewModel(args.NewValue as AppViewModel);
    }

    private void AttachViewModel(AppViewModel? nextViewModel)
    {
        if (ReferenceEquals(viewModel, nextViewModel))
        {
            return;
        }

        if (viewModel is not null)
        {
            viewModel.FloatViewModel.PropertyChanged -= OnFloatViewModelPropertyChanged;
        }

        viewModel = nextViewModel;

        if (viewModel is not null)
        {
            viewModel.FloatViewModel.PropertyChanged += OnFloatViewModelPropertyChanged;
            if (findReplaceDialog is not null)
            {
                findReplaceDialog.DataContext = viewModel;
            }

            UpdateFindReplaceState(viewModel.FloatViewModel.FindReplaceDialogOpen, false);
        }
        else
        {
            if (findReplaceDialog is not null)
            {
                findReplaceDialog.DataContext = null;
            }
        }
    }

    private void OnFloatViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FloatViewModel.FindReplaceDialogOpen) && sender is FloatViewModel floatViewModel)
        {
            UpdateFindReplaceState(floatViewModel.FindReplaceDialogOpen);
        }
    }

    private void UpdateFindReplaceState(FloatViewModel.FindReplaceDialogState findReplaceOpen, bool useTransitions = true)
    {
        var isOpen = findReplaceOpen != FloatViewModel.FindReplaceDialogState.None;
        IsFindReplaceLoad = isOpen;

        if (isOpen)
        {
            EnsureFindReplaceDialog();
            UpdateFindReplacePopupPlacement();
        }

        FindReplacePopup.IsOpen = isOpen;

        var state = findReplaceOpen == FloatViewModel.FindReplaceDialogState.None
            ? "FindReplaceCollapsed"
            : "FindReplaceVisible";

        VisualStateManager.GoToState(this, state, useTransitions && (viewModel?.SettingsViewModel.AnimationEnable ?? true));
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (findReplaceDialog is not null)
        {
            findReplaceDialog.Width = ActualWidth;
            findReplaceDialog.Height = ActualHeight;
        }

        UpdateFindReplacePopupPlacement();
    }

    private void OnFindReplaceDialogSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateFindReplacePopupPlacement();
    }

    private void OnFindReplacePopupOpened(object? sender, object e)
    {
        UpdateFindReplacePopupPlacement();
    }

    private void UpdateFindReplacePopupPlacement()
    {
        FindReplacePopup.HorizontalOffset = 0;
        FindReplacePopup.VerticalOffset = 0;
    }

    private void EnsureFindReplaceDialog()
    {
        if (findReplaceDialog is not null)
        {
            return;
        }

        findReplaceDialog = new FindReplace
        {
            DataContext = viewModel,
            RenderTransform = FindReplaceTransform,
            Width = ActualWidth,
            Height = ActualHeight
        };
        findReplaceDialog.SizeChanged += OnFindReplaceDialogSizeChanged;
        FindReplacePopup.Child = findReplaceDialog;
    }

    private void OnDragEnter(object sender, DragEventArgs e) { }

    private void OnDrop(object sender, DragEventArgs e) { }

    private void OnScroll(object sender, Microsoft.UI.Xaml.Controls.Primitives.ScrollEventArgs e) { }

    private void OnEditorContextMenuRequested(object? sender, WinUIEditorContextMenuRequestedEventArgs e)
    {
        Flyout.ShowAt(MarkdownEditorPresenter, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
        {
            Position = e.Position
        });
    }
}
