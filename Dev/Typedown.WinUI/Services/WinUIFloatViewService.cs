using Newtonsoft.Json.Linq;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Utilities;
using Typedown.Presentation.ViewModels;
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

        public void OpenFrontMenu(JToken args)
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
            flyout.Closed += (_, _) => serviceProvider.GetRequiredService<IEditorCommandSink>().Send("FrontMenuClosed", null);

            ShowFlyoutAt(flyout, args, FlyoutPlacementMode.Bottom);
        }

        public void OpenFormatPicker(JToken args)
        {
            var flyout = new MenuFlyout
            {
                Placement = FlyoutPlacementMode.Bottom
            };

            flyout.Items.Add(new ContextFormatItem
            {
                DataContext = serviceProvider.GetRequiredService<AppViewModel>()
            });

            ShowFlyoutAt(flyout, args, FlyoutPlacementMode.Bottom);
        }

        public void OpenImageSelector(JToken args)
        {
            var selector = new ImageSelector(
                serviceProvider.GetRequiredService<AppViewModel>(),
                serviceProvider.GetRequiredService<IEditorCommandSink>(),
                serviceProvider.GetRequiredService<IFilePickerService>());
            var rect = args["boundingClientRect"]?.ToObject<Rect>() ?? default;
            var info = args["imageInfo"] ?? new JObject();

            if (TryResolveEditorRectAnchor(args, out var rectAnchor))
            {
                selector.Open(rectAnchor, default, info);
                return;
            }

            selector.Open(ResolveEditorAnchor(), rect, info);
        }

        public void OpenImageToolbar(JToken args)
        {
            var imageToolbar = new ImageToolbar(
                serviceProvider.GetRequiredService<AppViewModel>(),
                serviceProvider.GetRequiredService<IEditorCommandSink>(),
                serviceProvider.GetRequiredService<IKeyboardAccelerator>());
            var rect = args["boundingClientRect"]?.ToObject<Rect>() ?? default;
            var attrs = args["attrs"] ?? new JObject();

            if (TryResolveEditorRectAnchor(args, out var rectAnchor))
            {
                imageToolbar.Open(rectAnchor, default, attrs, ResolveOverlayInputPassThroughElement());
                return;
            }

            imageToolbar.Open(ResolveEditorAnchor(), rect, attrs, ResolveOverlayInputPassThroughElement());
        }

        public void OpenTableTools(JToken args)
        {
            var type = args["tableInfo"]?["barType"]?.ToString();
            var isRow = type != "bottom";
            var flyout = new MenuFlyout
            {
                Placement = FlyoutPlacementMode.RightEdgeAlignedTop
            };

            if (isRow)
            {
                AddTableToolItem(flyout, "InsertRowAbove", "insert", "previous", "row");
                AddTableToolItem(flyout, "InsertRowBelow", "insert", "next", "row");
                AddTableToolItem(flyout, "RemoveRow", "remove", "current", "row");
            }
            else
            {
                AddTableToolItem(flyout, "InsertColumnLeft", "insert", "left", "column");
                AddTableToolItem(flyout, "InsertColumnRight", "insert", "right", "column");
                AddTableToolItem(flyout, "RemoveColumn", "remove", "current", "column");
            }

            ShowFlyoutAt(flyout, args, FlyoutPlacementMode.RightEdgeAlignedTop);
        }

        public void OpenToolTip(JToken args)
        {
            openedToolTip?.Hide();
            openedToolTip = null;

            if (args["open"]?.ToObject<bool>() != true)
            {
                return;
            }

            var tooltipName = args["tooltip"]?.ToString();
            if (string.IsNullOrWhiteSpace(tooltipName))
            {
                return;
            }

            openedToolTip = new Flyout
            {
                Content = new TextBlock
                {
                    Text = Locale.GetString(tooltipName),
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 320
                },
                Placement = FlyoutPlacementMode.Top,
                ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway,
                AreOpenCloseAnimationsEnabled = false
            };

            ShowFlyoutAt(openedToolTip, args, FlyoutPlacementMode.Top);
        }

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

        private void AddTableToolItem(MenuFlyout flyout, string textKey, string action, string location, string target)
        {
            var item = new MenuFlyoutItem
            {
                Text = Locale.GetString(textKey)
            };

            item.Click += (_, _) => serviceProvider.GetRequiredService<IEditorCommandSink>()
                .Send("EditTable", new { action, location, target });

            flyout.Items.Add(item);
        }

        private void ShowFlyoutAt(FlyoutBase flyout, JToken args, FlyoutPlacementMode placement)
        {
            flyout.Placement = placement;

            if (TryResolveEditorRectAnchor(args, out var rectAnchor))
            {
                flyout.ShowAt(rectAnchor, new FlyoutShowOptions
                {
                    ShowMode = FlyoutShowMode.Transient
                });
                return;
            }

            if (TryGetBoundingClientRect(args, out var rect))
            {
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

        private bool TryResolveEditorRectAnchor(JToken args, out FrameworkElement anchor)
        {
            anchor = null!;
            if (!TryGetBoundingClientRect(args, out var rect))
            {
                return false;
            }

            var container = ResolveEditorContainer();
            if (container is null)
            {
                return false;
            }

            anchor = container.GetFloatAnchor(rect);
            return true;
        }

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

        private static bool TryGetBoundingClientRect(JToken args, out Rect rect)
        {
            rect = default;
            var token = args["boundingClientRect"];
            if (token is null)
            {
                return false;
            }

            rect = token.ToObject<Rect>();
            return true;
        }
    }
}
