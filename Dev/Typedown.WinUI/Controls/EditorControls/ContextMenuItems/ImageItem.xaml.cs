using System.Collections.Specialized;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using Typedown.Core.Models;
using Typedown.Core.Models.RuntimeModels;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Services;
using Typedown.Presentation.Utilities;
using Typedown.Presentation.ViewModels;

namespace Typedown.WinUI.Controls;

public sealed partial class ImageItem : MenuItemCollection
{
    private AppViewModel? viewModel;

    private ImageUpload? ImageUpload => viewModel?.ServiceProvider.GetService<ImageUpload>();

    private ImageAction? ImageAction => viewModel?.ServiceProvider.GetService<ImageAction>();

    private IFileOperation? FileOperation => viewModel?.ServiceProvider.GetService<IFileOperation>();

    private IFilePickerService? FilePickerService => viewModel?.ServiceProvider.GetService<IFilePickerService>();

    private IEditorCommandSink? EditorCommandSink => viewModel?.ServiceProvider.GetService<IEditorCommandSink>();

    private JToken? SelectedImage { get; set; }

    private string ImageSrc => SelectedImage?["token"]?["src"]?.ToString() ?? string.Empty;

    private string ImageAlt => SelectedImage?["token"]?["alt"]?.ToString() ?? string.Empty;

    private string ImageTitle => SelectedImage?["token"]?["title"]?.ToString() ?? string.Empty;

    public ImageItem()
    {
        InitializeComponent();
    }

    private void OnOpenImageLocationItemLoaded(object sender, RoutedEventArgs e)
    {
        viewModel = ResolveViewModel(sender);
        RefreshSelectedImage();
        UpdateImageUploadConfigItem();
    }

    private void OnOpenImageLocationClick(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshSelectedImage();
            if (viewModel is null || !UriHelper.TryGetLocalPath(ImageSrc, out var path))
            {
                return;
            }

            var fullPath = viewModel.GetImageAbsolutePath(path);
            if (!string.IsNullOrWhiteSpace(fullPath))
            {
                Common.OpenFileLocation(fullPath);
            }
        }
        catch
        {
        }
    }

    private async void OnCopyImageToClick(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshSelectedImage();
            var bytes = await GetImageBytes();
            if (bytes is null)
            {
                return;
            }

            var file = await PickImageSavePath(bytes);
            if (string.IsNullOrWhiteSpace(file))
            {
                return;
            }

            await File.WriteAllBytesAsync(file, bytes);
            ReplaceImage(new HtmlImgTag(ConvertImagePath(file), ImageAlt, ImageTitle));
        }
        catch
        {
        }
    }

    private async void OnMoveImageToClick(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshSelectedImage();
            var bytes = await GetImageBytes();
            if (bytes is null)
            {
                return;
            }

            var file = await PickImageSavePath(bytes);
            if (string.IsNullOrWhiteSpace(file))
            {
                return;
            }

            await File.WriteAllBytesAsync(file, bytes);
            ReplaceImage(new HtmlImgTag(ConvertImagePath(file), ImageAlt, ImageTitle));
            DeleteOriginalLocalImageFile();
        }
        catch
        {
        }
    }

    private async void OnUploadImageClick(ImageUploadConfig config)
    {
        try
        {
            RefreshSelectedImage();
            if (viewModel is null || ImageUpload is null || !UriHelper.TryGetLocalPath(ImageSrc, out var path))
            {
                return;
            }

            var fullPath = viewModel.GetImageAbsolutePath(path);
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return;
            }

            var uri = await ImageUpload.Upload(config, fullPath);
            ReplaceImage(new HtmlImgTag(uri, ImageAlt, ImageTitle));
        }
        catch (Exception ex)
        {
            await ShowError(ex.Message);
        }
    }

    private void OnImageUploadSettingsClick(object sender, RoutedEventArgs e)
    {
        viewModel?.NavigateCommand.Execute("Settings/Image");
    }

    private async void OnSaveImageClick(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshSelectedImage();
            var bytes = await GetImageBytes();
            if (bytes is null)
            {
                return;
            }

            var file = await PickImageSavePath(bytes);
            if (!string.IsNullOrWhiteSpace(file))
            {
                await File.WriteAllBytesAsync(file, bytes);
            }
        }
        catch
        {
        }
    }

    private void OnDeleteImageFileClick(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshSelectedImage();
            if (DeleteOriginalLocalImageFile())
            {
                viewModel?.EditorViewModel.DeleteSelectionCommand.Execute(default);
            }
        }
        catch
        {
        }
    }

    private void RefreshSelectedImage()
    {
        SelectedImage = viewModel?.EditorViewModel.Selection?["selectedImage"];
        UpdateMenuItemState();
    }

    private bool DeleteOriginalLocalImageFile()
    {
        if (viewModel is null || FileOperation is null || !UriHelper.TryGetLocalPath(ImageSrc, out var path))
        {
            return false;
        }

        var fullPath = viewModel.GetImageAbsolutePath(path);
        return !string.IsNullOrWhiteSpace(fullPath) && FileOperation.Delete(new StringCollection { fullPath });
    }

    private void UpdateMenuItemState()
    {
        var isLocalImage = UriHelper.TryGetLocalPath(ImageSrc, out _);
        OpenImageLocationItem.IsEnabled = isLocalImage;
        MoveImageItem.IsEnabled = isLocalImage;
        DeleteImageItem.IsEnabled = isLocalImage;
    }

    private void UpdateImageUploadConfigItem()
    {
        var configs = ImageUpload?.ImageUploadConfigs.Where(x => x.IsEnable).ToList() ?? new List<ImageUploadConfig>();
        while (UploadSubMenu.Items.Count > 1 && UploadSubMenu.Items[1] is not MenuFlyoutSeparator)
        {
            UploadSubMenu.Items.RemoveAt(1);
        }

        foreach (var config in configs.AsEnumerable().Reverse())
        {
            var item = new MenuFlyoutItem
            {
                Text = config.Name,
                Tag = config
            };
            item.Click += (_, _) => OnUploadImageClick(config);
            UploadSubMenu.Items.Insert(1, item);
        }

        NoUploadConfigItem.Visibility = configs.Any() ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ReplaceImage(HtmlImgTag htmlImgTag)
    {
        EditorCommandSink?.Send("ReplaceImage", new
        {
            src = htmlImgTag.Src,
            alt = htmlImgTag.Alt,
            title = htmlImgTag.Title,
            isReplaceSelected = true
        });
    }

    private async Task<string?> PickImageSavePath(byte[] bytes)
    {
        if (FilePickerService is null)
        {
            return null;
        }

        var type = ImageAction.GetImageType(bytes, "png");
        return await FilePickerService.PickSaveFileAsync(new SaveFileRequest
        {
            SuggestedFileName = ImageAlt,
            FileTypeChoices = new List<SaveFileTypeChoice>
            {
                new(type, new[] { $".{type}" })
            }
        });
    }

    private async Task<byte[]?> GetImageBytes()
    {
        if (ImageAction is null || viewModel is null)
        {
            return null;
        }

        if (UriHelper.IsWebUrl(ImageSrc))
        {
            return await ImageAction.GetWebImage(new Uri(ImageSrc));
        }

        if (UriHelper.TryGetLocalPath(ImageSrc, out var path))
        {
            var fullPath = viewModel.GetImageAbsolutePath(path);
            return string.IsNullOrWhiteSpace(fullPath) ? null : await File.ReadAllBytesAsync(fullPath);
        }

        return null;
    }

    private string ConvertImagePath(string file)
    {
        return ImageAction?.ConvertImagePath(file) ?? file;
    }

    private async Task ShowError(string message)
    {
        var dialogService = viewModel?.ServiceProvider.GetService<IDialogService>();
        if (dialogService is null)
        {
            return;
        }

        await dialogService.ShowAsync(new DialogRequest
        {
            Title = Locale.GetString("Error"),
            Content = message,
            CloseButtonText = Locale.GetString("Ok"),
            DefaultButton = DialogDefaultButton.Close
        });
    }

    private static AppViewModel? ResolveViewModel(object sender)
    {
        if (sender is FrameworkElement { DataContext: AppViewModel appViewModel })
        {
            return appViewModel;
        }

        return AppViewModel.GetInstances().LastOrDefault();
    }
}
