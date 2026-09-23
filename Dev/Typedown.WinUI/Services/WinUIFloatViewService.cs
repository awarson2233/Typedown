using Typedown.Core.Editor;
using Typedown.Core.Interfaces;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Typedown.WinUI.Controls;
using Windows.Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIFloatViewService : IFloatViewService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IWindowContext windowContext;
        private Flyout? openedToolTip;

        public WinUIFloatViewService(IServiceProvider serviceProvider, IWindowContext windowContext)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.windowContext = windowContext ?? throw new ArgumentNullException(nameof(windowContext));
        }

        private IEditorSession EditorSession => serviceProvider.GetRequiredService<IEditorSession>();

        public void OpenFrontMenu(BlockMenuRequested request)
        {
            var viewModel = serviceProvider.GetRequiredService<AppViewModel>();
            var flyout = new MenuFlyout
            {
                Placement = FlyoutPlacementMode.Bottom
            };

            flyout.Items.Add(CreateCommandItem("Duplicate", viewModel.ParagraphViewModel.DuplicateCommand));
            flyout.Items.Add(CreateTurnIntoSubMenu(viewModel));
            flyout.Items.Add(CreateCommandItem("InsertParagraphBefore", viewModel.ParagraphViewModel.InsertParagraphCommand, "before"));
            flyout.Items.Add(CreateCommandItem("InsertParagraphAfter", viewModel.ParagraphViewModel.InsertParagraphCommand, "after"));
            flyout.Items.Add(CreateCommandItem("Delete", viewModel.ParagraphViewModel.DeleteParagraphCommand));
            flyout.Closed += (_, _) => EditorSession.Post(new BlockMenuClosed());

            ShowFlyoutAt(flyout, request.Anchor, FlyoutPlacementMode.Bottom);
        }

        public void OpenFormatPicker(FormatPickerRequested request)
        {
            var flyout = new MenuFlyout
            {
                Placement = FlyoutPlacementMode.Bottom
            };

            flyout.Items.Add(new ContextFormatItem
            {
                DataContext = serviceProvider.GetRequiredService<AppViewModel>()
            });

            ShowFlyoutAt(flyout, request.Anchor, FlyoutPlacementMode.Bottom);
        }

        public void OpenImageSelector(ImageEditorRequested request)
        {
            var selector = new ImageSelector(
                serviceProvider.GetRequiredService<AppViewModel>(),
                EditorSession,
                serviceProvider.GetRequiredService<IFilePickerService>());

            if (TryResolveEditorRectAnchor(request.Anchor, out var rectAnchor))
            {
                selector.Open(rectAnchor, default, request.Image);
                return;
            }

            selector.Open(ResolveEditorAnchor(), ToRect(request.Anchor), request.Image);
        }

        public void OpenImageToolbar(ImageToolbarRequested request)
        {
            var imageToolbar = new ImageToolbar(
                serviceProvider.GetRequiredService<AppViewModel>(),
                EditorSession,
                serviceProvider.GetRequiredService<IKeyboardAccelerator>());

            if (TryResolveEditorRectAnchor(request.Anchor, out var rectAnchor))
            {
                imageToolbar.Open(rectAnchor, default, ResolveOverlayInputPassThroughElement());
                return;
            }

            imageToolbar.Open(ResolveEditorAnchor(), ToRect(request.Anchor), ResolveOverlayInputPassThroughElement());
        }

        public void OpenTableTools(TableToolsRequested request)
        {
            var flyout = new MenuFlyout
            {
                Placement = FlyoutPlacementMode.RightEdgeAlignedTop
            };

            if (request.Axis == TableAxis.Row)
            {
                AddTableToolItem(flyout, "InsertRowAbove", TableEdit.InsertRowAbove);
                AddTableToolItem(flyout, "InsertRowBelow", TableEdit.InsertRowBelow);
                AddTableToolItem(flyout, "RemoveRow", TableEdit.RemoveRow);
            }
            else
            {
                AddTableToolItem(flyout, "InsertColumnLeft", TableEdit.InsertColumnLeft);
                AddTableToolItem(flyout, "InsertColumnRight", TableEdit.InsertColumnRight);
                AddTableToolItem(flyout, "RemoveColumn", TableEdit.RemoveColumn);
            }

            ShowFlyoutAt(flyout, request.Anchor, FlyoutPlacementMode.RightEdgeAlignedTop);
        }

        public void OpenToolTip(TooltipRequested request)
        {
            CloseToolTip();

            openedToolTip = new Flyout
            {
                Content = new TextBlock
                {
                    Text = Locale.GetString(GetTooltipResourceKey(request.Kind)),
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 320
                },
                Placement = FlyoutPlacementMode.Top,
                ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway,
                AreOpenCloseAnimationsEnabled = false
            };

            ShowFlyoutAt(openedToolTip, request.Anchor, FlyoutPlacementMode.Top);
        }

        public void CloseToolTip()
        {
            openedToolTip?.Hide();
            openedToolTip = null;
        }

        private static string GetTooltipResourceKey(TooltipKind kind) => kind switch
        {
            TooltipKind.CopyContent => "CopyContent",
            TooltipKind.CtrlClickToOpenLink => "CtrlAndClickOpenLink",
            TooltipKind.ResizeTable => "ResizeTable",
            TooltipKind.AlignLeft => "AlignLeft",
            TooltipKind.AlignCenter => "AlignCenter",
            TooltipKind.AlignRight => "AlignRight",
            TooltipKind.DeleteTable => "DeleteTable",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        private MenuFlyoutSubItem CreateTurnIntoSubMenu(AppViewModel viewModel)
        {
            var subMenu = new MenuFlyoutSubItem
            {
                Text = Locale.GetString("TurnInto")
            };

            subMenu.Items.Add(CreateCommandItem("Paragraph", viewModel.ParagraphViewModel.UpdateParagraphCommand, "paragraph"));
            subMenu.Items.Add(new MenuFlyoutSeparator());
            subMenu.Items.Add(CreateCommandItem("Heading1", viewModel.ParagraphViewModel.UpdateParagraphCommand, "heading 1"));
            subMenu.Items.Add(CreateCommandItem("Heading2", viewModel.ParagraphViewModel.UpdateParagraphCommand, "heading 2"));
            subMenu.Items.Add(CreateCommandItem("Heading3", viewModel.ParagraphViewModel.UpdateParagraphCommand, "heading 3"));
            subMenu.Items.Add(CreateCommandItem("Heading4", viewModel.ParagraphViewModel.UpdateParagraphCommand, "heading 4"));
            subMenu.Items.Add(CreateCommandItem("Heading5", viewModel.ParagraphViewModel.UpdateParagraphCommand, "heading 5"));
            subMenu.Items.Add(CreateCommandItem("Heading6", viewModel.ParagraphViewModel.UpdateParagraphCommand, "heading 6"));
            subMenu.Items.Add(new MenuFlyoutSeparator());
            subMenu.Items.Add(CreateCommandItem("OrderedList", viewModel.ParagraphViewModel.UpdateParagraphCommand, "ol-order"));
            subMenu.Items.Add(CreateCommandItem("UnorderedList", viewModel.ParagraphViewModel.UpdateParagraphCommand, "ul-bullet"));
            subMenu.Items.Add(CreateCommandItem("TaskList", viewModel.ParagraphViewModel.UpdateParagraphCommand, "ul-task"));

            return subMenu;
        }

        private static MenuFlyoutItem CreateCommandItem(string textKey, System.Windows.Input.ICommand command, object? parameter = null)
        {
            return new MenuFlyoutItem
            {
                Text = Locale.GetString(textKey),
                Command = command,
                CommandParameter = parameter
            };
        }

        private void AddTableToolItem(MenuFlyout flyout, string textKey, TableEdit edit)
        {
            var item = new MenuFlyoutItem
            {
                Text = Locale.GetString(textKey)
            };

            item.Click += (_, _) => EditorSession.Post(new EditTable(edit));

            flyout.Items.Add(item);
        }

        private void ShowFlyoutAt(FlyoutBase flyout, EditorRect? anchorRect, FlyoutPlacementMode placement)
        {
            flyout.Placement = placement;

            if (TryResolveEditorRectAnchor(anchorRect, out var rectAnchor))
            {
                flyout.ShowAt(rectAnchor, new FlyoutShowOptions
                {
                    ShowMode = FlyoutShowMode.Transient
                });
                return;
            }

            if (anchorRect is not null)
            {
                var rect = ToRect(anchorRect);
                var anchor = ResolveEditorAnchor();
                flyout.ShowAt(anchor, new FlyoutShowOptions
                {
                    Position = new Point(rect.X, rect.Y + rect.Height),
                    ShowMode = FlyoutShowMode.Transient
                });
                return;
            }

            flyout.ShowAt(ResolveAnchor());
        }

        private bool TryResolveEditorRectAnchor(EditorRect? anchorRect, out FrameworkElement anchor)
        {
            anchor = null!;
            if (anchorRect is null)
            {
                return false;
            }

            var container = ResolveEditorContainer();
            if (container is null)
            {
                return false;
            }

            anchor = container.GetFloatAnchor(ToRect(anchorRect));
            return true;
        }

        private static Rect ToRect(EditorRect? rect) =>
            rect is { } r ? new Rect(r.X, r.Y, r.Width, r.Height) : default;

        private FrameworkElement ResolveEditorAnchor()
        {
            var root = ResolveAnchor();
            return FindDescendantByName(root, "MarkdownEditorPresenter") ?? root;
        }

        private EditorContainer? ResolveEditorContainer()
        {
            return FindDescendant<EditorContainer>(ResolveAnchor());
        }

        private FrameworkElement ResolveAnchor()
        {
            if (windowContext.ViewRoot is FrameworkElement element)
            {
                return element;
            }

            if (windowContext.ViewRoot is XamlRoot xamlRoot && xamlRoot.Content is FrameworkElement root)
            {
                return root;
            }

            throw new InvalidOperationException("WinUI float view service requires a FrameworkElement-backed view root.");
        }

        private UIElement? ResolveOverlayInputPassThroughElement()
        {
            if (windowContext.ViewRoot is XamlRoot xamlRoot)
            {
                return xamlRoot.Content;
            }

            if (windowContext.ViewRoot is FrameworkElement element)
            {
                return element.XamlRoot?.Content;
            }

            return null;
        }

        private static FrameworkElement? FindDescendantByName(DependencyObject root, string name)
        {
            if (root is FrameworkElement element && string.Equals(element.Name, name, StringComparison.Ordinal))
            {
                return element;
            }

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var match = FindDescendantByName(child, name);
                if (match is not null)
                {
                    return match;
                }
            }

            return null;
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root is T match)
            {
                return match;
            }

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var descendant = FindDescendant<T>(child);
                if (descendant is not null)
                {
                    return descendant;
                }
            }

            return null;
        }
    }
}
