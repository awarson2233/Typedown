using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Typedown.Core.Editor;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.ViewModels;
using Windows.Foundation;

namespace Typedown.WinUI.Controls
{
    public sealed partial class ImageToolbar : Flyout
    {
        public AppViewModel ViewModel { get; }

        public IEditorSession EditorSession { get; }

        public IKeyboardAccelerator KeyboardAccelerator { get; }

        private CompositeDisposable? disposables;

        public ImageToolbar(AppViewModel viewModel, IEditorSession editorSession, IKeyboardAccelerator keyboardAccelerator)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            EditorSession = editorSession ?? throw new ArgumentNullException(nameof(editorSession));
            KeyboardAccelerator = keyboardAccelerator ?? throw new ArgumentNullException(nameof(keyboardAccelerator));
            InitializeComponent();
        }

        public void Open(FrameworkElement anchor, Rect rect, UIElement? overlayInputPassThroughElement)
        {
            AreOpenCloseAnimationsEnabled = ViewModel.SettingsViewModel.AnimationEnable;
            OverlayInputPassThroughElement = overlayInputPassThroughElement;
            if (rect == default)
            {
                ShowAt(anchor);
                return;
            }

            ShowAt(anchor, new FlyoutShowOptions
            {
                Position = new Point(rect.X, rect.Y + rect.Height),
                ShowMode = FlyoutShowMode.Transient
            });
        }

        private void EditClick(object sender, RoutedEventArgs e)
        {
            PostEditImageMessage(new ApplyImageToolbarAction(ImageToolbarAction.Edit));
        }

        private void InlineClick(object sender, RoutedEventArgs e)
        {
            PostEditImageMessage(new ApplyImageToolbarAction(ImageToolbarAction.Inline));
        }

        private void LeftClick(object sender, RoutedEventArgs e)
        {
            PostEditImageMessage(new ApplyImageToolbarAction(ImageToolbarAction.AlignLeft));
        }

        private void CenterClick(object sender, RoutedEventArgs e)
        {
            PostEditImageMessage(new ApplyImageToolbarAction(ImageToolbarAction.AlignCenter));
        }

        private void RightClick(object sender, RoutedEventArgs e)
        {
            PostEditImageMessage(new ApplyImageToolbarAction(ImageToolbarAction.AlignRight));
        }

        private void DeleteClick(object sender, RoutedEventArgs e)
        {
            PostEditImageMessage(new ApplyImageToolbarAction(ImageToolbarAction.Delete));
        }

        private void ZoomClick(object sender, RoutedEventArgs e)
        {
            // 菜单项的 Tag 是缩放百分比，如 "25%"。
            if (sender is not MenuFlyoutItem { Tag: string zoom } || !int.TryParse(zoom.TrimEnd('%'), out var percent))
            {
                return;
            }

            PostEditImageMessage(new SetImageZoom(percent));
        }

        private void PostEditImageMessage(EditorCommand command)
        {
            EditorSession.Post(command);
            Hide();
        }

        private void OnOpened(object sender, object e)
        {
            disposables?.Dispose();
            disposables = new CompositeDisposable();
            disposables.Add(KeyboardAccelerator.GetObservable().Where(e => e.Key == KeyboardKey.Back || e.Key == KeyboardKey.Delete).Subscribe(e =>
            {
                DispatcherQueue.TryEnqueue(() => DeleteClick(this, new RoutedEventArgs()));
                e.Handled = true;
            }));
        }

        private void OnClosed(object sender, object e)
        {
            disposables?.Dispose();
            disposables = null;
        }
    }
}
