using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Typedown.Presentation.Interfaces;
using Typedown.Core.Models;
using Typedown.Presentation.ViewModels;
using Windows.Foundation;

namespace Typedown.WinUI.Controls
{
    public sealed partial class ImageToolbar : Flyout
    {
        public AppViewModel ViewModel { get; }

        public IEditorCommandSink EditorCommandSink { get; }

        public IKeyboardAccelerator KeyboardAccelerator { get; }

        private readonly CompositeDisposable disposables = new();

        private JToken attrs;

        public ImageToolbar(AppViewModel viewModel, IEditorCommandSink editorCommandSink, IKeyboardAccelerator keyboardAccelerator)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            EditorCommandSink = editorCommandSink ?? throw new ArgumentNullException(nameof(editorCommandSink));
            KeyboardAccelerator = keyboardAccelerator ?? throw new ArgumentNullException(nameof(keyboardAccelerator));
            InitializeComponent();
        }

        public void Open(FrameworkElement anchor, Rect rect, JToken attrs, UIElement? overlayInputPassThroughElement)
        {
            this.attrs = attrs;
            AreOpenCloseAnimationsEnabled = ViewModel.SettingsViewModel.AnimationEnable;
            OverlayInputPassThroughElement = overlayInputPassThroughElement;
            ShowAt(anchor, new FlyoutShowOptions
            {
                Position = new Point(rect.X, rect.Y + rect.Height),
                ShowMode = FlyoutShowMode.Transient
            });
        }

        private void EditClick(object sender, RoutedEventArgs e)
        {
            PostEditImageMessage(new { type = "edit" });
        }

        private void InlineClick(object sender, RoutedEventArgs e)
        {
            PostEditImageMessage(new { type = "inline" });
        }

        private void LeftClick(object sender, RoutedEventArgs e)
        {
            PostEditImageMessage(new { type = "left" });
        }

        private void CenterClick(object sender, RoutedEventArgs e)
        {
            PostEditImageMessage(new { type = "center" });
        }

        private void RightClick(object sender, RoutedEventArgs e)
        {
            PostEditImageMessage(new { type = "right" });
        }

        private void DeleteClick(object sender, RoutedEventArgs e)
        {
            PostEditImageMessage(new { type = "delete" });
        }

        private void ZoomClick(object sender, RoutedEventArgs e)
        {
            var zoom = (sender as MenuFlyoutItem).Tag as string;
            var style = (attrs["style"]?.ToString() ?? "").Split(';').Where(x => !x.StartsWith("zoom:") && !string.IsNullOrWhiteSpace(x)).ToList();
            style.Add($"zoom:{zoom}");
            PostEditImageMessage(new { type = "updateImage", attrName = "style", attrValue = $"{string.Join(';', style)};" });
        }

        private void PostEditImageMessage(object args)
        {
            EditorCommandSink.Send("ImageEditToolbarClick", args);
            Hide();
        }

        private void OnOpened(object sender, object e)
        {
            disposables.Add(KeyboardAccelerator.GetObservable().Where(e => e.Key == KeyboardKey.Back || e.Key == KeyboardKey.Delete).Subscribe(e =>
            {
                DispatcherQueue.TryEnqueue(() => DeleteClick(this, new RoutedEventArgs()));
                e.Handled = true;
            }));
        }

        private void OnClosed(object sender, object e)
        {
            disposables.Dispose();
        }
    }
}
