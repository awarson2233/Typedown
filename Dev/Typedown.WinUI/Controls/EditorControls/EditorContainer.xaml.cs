using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Utilities;
using Typedown.Presentation.ViewModels;
using Typedown.WinUI.Utilities;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;

namespace Typedown.WinUI.Controls;

public sealed partial class EditorContainer : UserControl
{
    private WinUIEditorHost? editorHost;
    private FindReplace? findReplaceDialog;
    private AppViewModel? viewModel;
    private FloatViewModel? floatViewModel;
    private EditorViewModel? editorViewModel;
    private FormatViewModel? formatViewModel;
    private SettingsViewModel? settingsViewModel;
    private IDisposable? scrollSubscription;
    private bool hasFloatAnchor;
    private MenuFlyout? editorContextFlyout;
    private ContextFormatItem menuFormatItem = null!;
    private MenuFlyoutSeparator menuImageItemSeparator = null!;
    private MenuFlyoutSubItem menuImageItem = null!;
    private MenuFlyoutItem UndoItem = null!;
    private MenuFlyoutItem CutItem = null!;
    private MenuFlyoutItem CopyItem = null!;
    private MenuFlyoutItem PasteItem = null!;
    private MenuFlyoutItem CopyAsPlainTextItem = null!;
    private MenuFlyoutItem CopyAsMarkdownItem = null!;
    private MenuFlyoutItem CopyAsHTMLCodeItem = null!;
    private MenuFlyoutItem PasteAsPlainTextItem = null!;
    private MenuFlyoutItem DeleteItem = null!;
    private MenuFlyoutItem SelectAllItem = null!;

    public static readonly DependencyProperty IsFindReplaceLoadProperty =
        DependencyProperty.Register(nameof(IsFindReplaceLoad), typeof(bool), typeof(EditorContainer), new PropertyMetadata(false));

    public bool IsFindReplaceLoad
    {
        get => (bool)GetValue(IsFindReplaceLoadProperty);
        set => SetValue(IsFindReplaceLoadProperty, value);
    }

    public static readonly DependencyProperty ScrollStateProperty =
        DependencyProperty.Register(nameof(ScrollState), typeof(ScrollState), typeof(EditorContainer), new PropertyMetadata(new ScrollState(), OnScrollStateChanged));

    private ScrollState ScrollState
    {
        get => (ScrollState)GetValue(ScrollStateProperty);
        set => SetValue(ScrollStateProperty, value);
    }

    public EditorViewModel? Editor => editorViewModel;

    public FormatViewModel? Format => formatViewModel;

    public EditorContainer()
    {
        using (StartupTrace.Phase("EditorContainer.InitializeComponent"))
        {
            InitializeComponent();
        }
        DataContextChanged += OnDataContextChanged;
        SizeChanged += OnSizeChanged;
        FindReplacePopup.Opened += OnFindReplacePopupOpened;
        AddHandler(PointerMovedEvent, new PointerEventHandler(OnPointerPointerMoved), true);
        AddHandler(PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChanged), true);
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
        UpdateFindReplaceState(floatViewModel?.FindReplaceDialogOpen ?? FloatViewModel.FindReplaceDialogState.None, false);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(null);
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        AttachViewModel(args.NewValue as AppViewModel);
        if (editorContextFlyout is not null)
        {
            ConfigureContextMenuCommands();
        }
    }

    private void AttachViewModel(AppViewModel? nextViewModel)
    {
        if (ReferenceEquals(viewModel, nextViewModel))
        {
            return;
        }

        if (floatViewModel is not null)
        {
            floatViewModel.PropertyChanged -= OnFloatViewModelPropertyChanged;
        }

        scrollSubscription?.Dispose();
        scrollSubscription = null;
        viewModel = nextViewModel;
        floatViewModel = null;
        editorViewModel = null;
        formatViewModel = null;
        settingsViewModel = null;

        if (viewModel is not null)
        {
            floatViewModel = viewModel.FloatViewModel;
            editorViewModel = viewModel.EditorViewModel;
            formatViewModel = viewModel.FormatViewModel;
            settingsViewModel = viewModel.SettingsViewModel;

            floatViewModel.PropertyChanged += OnFloatViewModelPropertyChanged;
            scrollSubscription = editorViewModel.EventCenter
                .GetObservable<EditorEventArgs>("OnScroll")
                .Subscribe(OnEditorScrollStateChanged);
            if (findReplaceDialog is not null)
            {
                findReplaceDialog.DataContext = viewModel;
            }

            UpdateFindReplaceState(floatViewModel.FindReplaceDialogOpen, false);
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

        VisualStateManager.GoToState(this, state, useTransitions && (settingsViewModel?.AnimationEnable ?? true));
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

    private void OnFlyoutOpening(object? sender, object e)
    {
        ConfigureContextMenuCommands();
    }

    private void ConfigureContextMenuCommands()
    {
        if (editorContextFlyout is null)
        {
            return;
        }

        var editor = Editor;
        var hasSelection = editor?.Selected == true;

        menuFormatItem.DataContext = viewModel;
        menuImageItem.DataContext = viewModel;
        var hasImageMenu = IsLoadImageMenu(Format?.FormatState.Image ?? false, Editor?.Selection);
        menuImageItem.Visibility = hasImageMenu ? Visibility.Visible : Visibility.Collapsed;
        menuImageItemSeparator.Visibility = hasImageMenu ? Visibility.Visible : Visibility.Collapsed;

        SetCommand(UndoItem, editor?.UndoCommand, editor?.History.Undoable == true);
        SetCommand(CutItem, editor?.CutCommand, hasSelection);
        SetCommand(CopyItem, editor?.CopyCommand, hasSelection);
        SetCommand(PasteItem, editor?.PasteCommand, editor is not null);
        SetCommand(CopyAsPlainTextItem, editor?.CopyCommand, hasSelection);
        SetCommand(CopyAsMarkdownItem, editor?.CopyCommand, hasSelection);
        SetCommand(CopyAsHTMLCodeItem, editor?.CopyCommand, hasSelection);
        SetCommand(PasteAsPlainTextItem, editor?.PasteCommand, editor is not null);
        SetCommand(DeleteItem, editor?.DeleteSelectionCommand, hasSelection);
        SetCommand(SelectAllItem, editor?.SelectAllCommand, editor is not null);
    }

    private static void SetCommand(MenuFlyoutItem item, ICommand? command, bool isAvailable)
    {
        item.Command = command;
        item.IsEnabled = command is not null && isAvailable;
    }

    private MenuFlyout EnsureEditorContextFlyout()
    {
        if (editorContextFlyout is not null)
        {
            return editorContextFlyout;
        }

        using (StartupTrace.Phase("Editor context menu create"))
        {
            menuFormatItem = new ContextFormatItem();
            menuImageItem = new MenuFlyoutSubItem
            {
                Text = Locale.GetString("Image"),
                Icon = new FontIcon { Glyph = "\uE91B" }
            };
            MenuItemCollection.SetValue(menuImageItem, new ImageItem());
            menuImageItemSeparator = new MenuFlyoutSeparator();

            UndoItem = CreateMenuItem("Undo", "normal", new SymbolIcon(Symbol.Undo));
            CutItem = CreateMenuItem("Cut", "normal", new SymbolIcon(Symbol.Cut));
            CopyItem = CreateMenuItem("Copy", "normal", new SymbolIcon(Symbol.Copy));
            PasteItem = CreateMenuItem("Paste", "normal", new SymbolIcon(Symbol.Paste));
            CopyAsPlainTextItem = CreateMenuItem("CopyAsPlainText", "copyAsPlainText");
            CopyAsMarkdownItem = CreateMenuItem("CopyAsMarkdown", "copyAsMarkdown");
            CopyAsHTMLCodeItem = CreateMenuItem("CopyAsHTMLCode", "copyAsHtml");
            PasteAsPlainTextItem = CreateMenuItem("PasteAsPlainText", "pasteAsPlainText");
            DeleteItem = CreateMenuItem("Delete", null, new SymbolIcon(Symbol.Delete));
            SelectAllItem = CreateMenuItem("SelectAll", null, new SymbolIcon(Symbol.SelectAll));

            editorContextFlyout = new MenuFlyout();
            editorContextFlyout.Opening += OnFlyoutOpening;
            editorContextFlyout.Items.Add(menuFormatItem);
            editorContextFlyout.Items.Add(new MenuFlyoutSeparator());
            editorContextFlyout.Items.Add(menuImageItem);
            editorContextFlyout.Items.Add(menuImageItemSeparator);
            editorContextFlyout.Items.Add(UndoItem);
            editorContextFlyout.Items.Add(new MenuFlyoutSeparator());
            editorContextFlyout.Items.Add(CutItem);
            editorContextFlyout.Items.Add(CopyItem);
            editorContextFlyout.Items.Add(PasteItem);
            editorContextFlyout.Items.Add(CreateCopyPasteAsSubMenu());
            editorContextFlyout.Items.Add(new MenuFlyoutSeparator());
            editorContextFlyout.Items.Add(DeleteItem);
            editorContextFlyout.Items.Add(new MenuFlyoutSeparator());
            editorContextFlyout.Items.Add(SelectAllItem);
            ConfigureContextMenuCommands();
        }

        return editorContextFlyout;
    }

    private static MenuFlyoutItem CreateMenuItem(string localeKey, object? commandParameter = null, IconElement? icon = null)
    {
        var item = new MenuFlyoutItem
        {
            Text = Locale.GetString(localeKey),
            Icon = icon
        };

        if (commandParameter is not null)
        {
            item.CommandParameter = commandParameter;
        }

        return item;
    }

    private MenuFlyoutSubItem CreateCopyPasteAsSubMenu()
    {
        var item = new MenuFlyoutSubItem
        {
            Text = Locale.GetString("CopyPasteAs")
        };

        item.Items.Add(CopyAsPlainTextItem);
        item.Items.Add(CopyAsMarkdownItem);
        item.Items.Add(CopyAsHTMLCodeItem);
        item.Items.Add(PasteAsPlainTextItem);
        return item;
    }

    private async void OnDragEnter(object sender, DragEventArgs e)
    {
        var deferral = e.GetDeferral();
        try
        {
            e.AcceptedOperation = DataPackageOperation.None;
            if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                return;
            }

            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count != 1)
            {
                return;
            }

            switch (FileTypeHelper.GetFileType(items[0].Path))
            {
                case FileTypeHelper.FileType.Markdown:
                    e.AcceptedOperation = DataPackageOperation.Link;
                    e.DragUIOverride.Caption = "Open";
                    break;
                case FileTypeHelper.FileType.Image:
                    e.AcceptedOperation = DataPackageOperation.Link;
                    e.DragUIOverride.Caption = "InsertImage";
                    break;
            }
        }
        catch
        {
            e.AcceptedOperation = DataPackageOperation.None;
        }
        finally
        {
            deferral.Complete();
        }
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (viewModel is null || !e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        if (items.Count != 1)
        {
            return;
        }

        var path = items[0].Path;
        if (FileTypeHelper.IsMarkdownFile(path))
        {
            viewModel.FileViewModel.OpenFileCommand.Execute(path);
        }
        else if (FileTypeHelper.IsImageFile(path))
        {
            viewModel.ServiceProvider.GetService<IEditorCommandSink>()?.Send("InsertImage", new { src = path });
        }
    }

    private void OnScroll(object sender, Microsoft.UI.Xaml.Controls.Primitives.ScrollEventArgs e)
    {
        viewModel?.ServiceProvider.GetService<IEditorCommandSink>()?.Send("OnScroll", new
        {
            scrollX = HorizontalScrollBar.Value,
            scrollY = VerticalScrollBar.Value
        });
    }

    private void OnPointerPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch)
        {
            HorizontalScrollBar.IndicatorMode = Microsoft.UI.Xaml.Controls.Primitives.ScrollingIndicatorMode.TouchIndicator;
            VerticalScrollBar.IndicatorMode = Microsoft.UI.Xaml.Controls.Primitives.ScrollingIndicatorMode.TouchIndicator;
        }
        else
        {
            HorizontalScrollBar.IndicatorMode = Microsoft.UI.Xaml.Controls.Primitives.ScrollingIndicatorMode.MouseIndicator;
            VerticalScrollBar.IndicatorMode = Microsoft.UI.Xaml.Controls.Primitives.ScrollingIndicatorMode.MouseIndicator;
        }
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var settings = settingsViewModel;
        if (settings is null || !e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
        {
            return;
        }

        var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        settings.FontSize = Math.Max(8, Math.Min(48, Math.Round(settings.FontSize * (1 + delta / 1200d), 1)));
        e.Handled = true;
    }

    private void OnEditorScrollStateChanged(EditorEventArgs args)
    {
        if (args.Args is null || args.Args.Type == JTokenType.Null)
        {
            return;
        }

        ScrollState = args.Args.ToObject<ScrollState>() ?? new ScrollState();
    }

    private static void OnScrollStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not EditorContainer container || e.NewValue is not ScrollState scrollState)
        {
            return;
        }

        if (e.OldValue is ScrollState previous)
        {
            container.MoveFloatAnchor(previous.ScrollX - scrollState.ScrollX, previous.ScrollY - scrollState.ScrollY);
        }

        container.HorizontalScrollBar.Maximum = Math.Max(0, scrollState.MaximumX);
        container.HorizontalScrollBar.ViewportSize = Math.Max(0, scrollState.ViewportWidth);
        container.HorizontalScrollBar.Value = Math.Max(0, Math.Min(container.HorizontalScrollBar.Maximum, scrollState.ScrollX));
        container.HorizontalScrollBar.Visibility = scrollState.MaximumX <= 0 ? Visibility.Collapsed : Visibility.Visible;

        container.VerticalScrollBar.Maximum = Math.Max(0, scrollState.MaximumY);
        container.VerticalScrollBar.ViewportSize = Math.Max(0, scrollState.ViewportHeight);
        container.VerticalScrollBar.Value = Math.Max(0, Math.Min(container.VerticalScrollBar.Maximum, scrollState.ScrollY));
        container.VerticalScrollBar.Visibility = scrollState.MaximumY <= 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    public static bool IsLoadImageMenu(bool isImageFormat, JToken? selection)
    {
        return isImageFormat && (selection?["selectedImage"]?.HasValues ?? false);
    }

    private void OnEditorContextMenuRequested(object? sender, WinUIEditorContextMenuRequestedEventArgs e)
    {
        EnsureEditorContextFlyout().ShowAt(MarkdownEditorPresenter, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
        {
            Position = e.Position
        });
    }

    public FrameworkElement GetFloatAnchor(Rect rect)
    {
        hasFloatAnchor = true;
        FloatAnchorElement.Visibility = Visibility.Visible;
        FloatAnchorElement.Width = Math.Max(0, rect.Width);
        FloatAnchorElement.Height = Math.Max(0, rect.Height);
        Canvas.SetLeft(FloatAnchorElement, rect.X);
        Canvas.SetTop(FloatAnchorElement, rect.Y);
        return FloatAnchorElement;
    }

    public void MoveFloatAnchor(double offsetX, double offsetY)
    {
        if (!hasFloatAnchor)
        {
            return;
        }

        Canvas.SetLeft(FloatAnchorElement, Canvas.GetLeft(FloatAnchorElement) + offsetX);
        Canvas.SetTop(FloatAnchorElement, Canvas.GetTop(FloatAnchorElement) + offsetY);
    }
}
