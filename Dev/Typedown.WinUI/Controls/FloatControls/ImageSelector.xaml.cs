using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Typedown.Core.Models.RuntimeModels;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Services;
using Typedown.Presentation.Utilities;
using Typedown.Presentation.ViewModels;
using Windows.Foundation;

namespace Typedown.WinUI.Controls
{
    public sealed partial class ImageSelector : UserControl
    {
        public AppViewModel ViewModel { get; }

        private IEditorCommandSink EditorCommandSink { get; }

        private IFilePickerService FilePickerService { get; }

        private readonly Flyout flyout = new() { Placement = FlyoutPlacementMode.Bottom };

        private static string? currentSrc;

        private Rect rect;

        private FrameworkElement? anchor;

        private bool isPicking;

        private bool isSaving;

        public ImageSelector(AppViewModel viewModel, IEditorCommandSink editorCommandSink, IFilePickerService filePickerService)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            EditorCommandSink = editorCommandSink ?? throw new ArgumentNullException(nameof(editorCommandSink));
            FilePickerService = filePickerService ?? throw new ArgumentNullException(nameof(filePickerService));
            flyout.AreOpenCloseAnimationsEnabled = ViewModel.SettingsViewModel.AnimationEnable;
            flyout.Closing += OnFlyoutClosing;
            InitializeComponent();
            ApplyLocalizedText();
        }

        public void Open(FrameworkElement anchor, Rect rect, JToken imageInfo)
        {
            this.anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
            this.rect = rect;
            TextBoxSrc.Text = WebUtility.UrlDecode(imageInfo?["src"]?.ToString() ?? "");
            TextBoxAlt.Text = imageInfo?["alt"]?.ToString() ?? "";
            TextBoxTitle.Text = imageInfo?["title"]?.ToString() ?? "";
            flyout.Content = this;
            ShowFlyout();
            currentSrc = TextBoxSrc.Text;
        }

        private void OnFlyoutClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
        {
            args.Cancel = isPicking || isSaving;
        }

        private async void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            SetSaving(true);
            try
            {
                await SaveImageSrc(TextBoxSrc.Text, TextBoxTitle.Text, TextBoxAlt.Text);
                flyout.Hide();
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = Locale.GetString("Error"),
                    Content = ex.Message,
                    CloseButtonText = Locale.GetDialogString("Ok"),
                    XamlRoot = XamlRoot
                }.ShowAsync();
            }
            finally
            {
                SetSaving(false);
            }
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            flyout.Hide();
        }

        private async Task SaveImageSrc(string src, string? title = null, string? alt = null)
        {
            if (src != currentSrc)
            {
                var imageAction = ViewModel.ServiceProvider.GetRequiredService<ImageAction>();
                if (UriHelper.IsWebUrl(src))
                {
                    src = await imageAction.DoWebFileAction(src);
                }
                else if (UriHelper.TryGetLocalPath(src, out _))
                {
                    src = await imageAction.DoLocalFileAction(src);
                    src = src.Replace('\\', '/');
                }
            }
            if (flyout.IsOpen)
            {
                if (ViewModel.SettingsViewModel.AutoEncodeImageURL)
                {
                    // TODO
                }
                EditorCommandSink.Send("ReplaceImage", new HtmlImgTag(src, alt, title));
            }
        }

        private async void OnImagePickerButtonClick(object sender, RoutedEventArgs e)
        {
            isPicking = true;
            try
            {
                var path = await FilePickerService.PickOpenFileAsync(new OpenFileRequest
                {
                    FileTypeFilter = FileTypeHelper.Image.ToList()
                });

                if (!string.IsNullOrEmpty(path))
                {
                    TextBoxSrc.Text = path;
                }
            }
            finally
            {
                isPicking = false;
                if (!flyout.IsOpen && anchor is not null)
                {
                    ShowFlyout();
                }
            }
        }

        private void ShowFlyout()
        {
            if (anchor is null)
            {
                return;
            }

            flyout.ShowAt(anchor, new FlyoutShowOptions
            {
                Position = new Point(rect.X, rect.Y + rect.Height),
                ShowMode = FlyoutShowMode.Transient
            });
        }

        private void SetSaving(bool value)
        {
            isSaving = value;
            SaveButton.IsEnabled = !value;
        }

        private void ApplyLocalizedText()
        {
            TitleTextBlock.Text = Locale.GetString("EditImage");
            PathTextBlock.Text = Locale.GetString("Path");
            ImageAltTextBlock.Text = Locale.GetString("ImageAlt");
            ImageTitleTextBlock.Text = Locale.GetString("Title");
            ImagePickerButton.Content = Locale.GetString("Browse");
            SaveButton.Content = Locale.GetDialogString("Ok");
            CancelButton.Content = Locale.GetDialogString("Cancel");
            ToolTipService.SetToolTip(SettingsButton, Locale.GetString("Settings"));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Bindings?.StopTracking();
        }
    }
}
