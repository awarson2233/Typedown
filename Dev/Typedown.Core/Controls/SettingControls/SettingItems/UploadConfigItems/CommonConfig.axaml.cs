using PropertyChanged;
﻿using System;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Data.Converters;
using System.Linq;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Pages.SettingPages;
using Typedown.Core.Utilities;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;

namespace Typedown.Core.Controls.SettingControls.SettingItems.UploadConfigItems
{
    [DoNotNotify]
        public sealed partial class CommonConfig : UserControl
    {
        public static StyledProperty<ImageUploadConfig> ImageUploadConfigProperty { get; } = AvaloniaProperty.Register<CommonConfig, ImageUploadConfig>(nameof(ImageUploadConfig), null);
        public ImageUploadConfig ImageUploadConfig { get => (ImageUploadConfig)GetValue(ImageUploadConfigProperty); set => SetValue(ImageUploadConfigProperty, value); }

        public static StyledProperty<Control> DetailProperty { get; } = AvaloniaProperty.Register<CommonConfig, Control>(nameof(Detail), null);
        public Control Detail { get => (Control)GetValue(DetailProperty); set => SetValue(DetailProperty, value); }

        public CommonConfig()
        {
            InitializeComponent();
        }

        private void OnDeleteButtonClick(object sender, RoutedEventArgs e)
        {
            this.GetAncestor<UploadConfigPage>()?.DeleteConfigAsync();
        }

        private async void OnTestUploadButtonClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            try
            {
                button.IsEnabled = false;
                // TODO: Re-implement file picker with Avalonia StorageProvider API
                // FileOpenPicker doesn't exist in Avalonia
                // var filePicker = new FileOpenPicker();
                // FileTypeHelper.Image.ToList().ForEach(filePicker.FileTypeFilter.Add);
                // filePicker.SetOwnerWindow(this.GetService<IWindowService>().GetWindow(this));
                // var file = await filePicker.PickSingleFileAsync();
                // if (file == null)
                //     return;
                // var res = await ImageUploadConfig.LoadUploadConfig().Upload(this.GetService<IServiceProvider>(), file.Path);
                // await AppContentDialog.Create(Locale.GetDialogString("UploadSuccessfulTitle"), res, "Ok").ShowAsync(this);
            }
            catch (Exception ex)
            {
                await AppContentDialog.Create(Locale.GetDialogString("UploadFailedTitle"), ex.Message, "Ok").ShowAsync(this);
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
