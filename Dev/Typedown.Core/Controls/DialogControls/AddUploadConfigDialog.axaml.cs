using PropertyChanged;
﻿using System.Linq;
using System.Threading.Tasks;
using Typedown.Core.Enums;
using Typedown.Core.Utilities;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Typedown.Core.Controls.DialogControls
{
[DoNotNotify]
        public sealed partial class AddUploadConfigDialog : AppContentDialog
    {
        public static readonly StyledProperty<string> ConfigNameProperty = AvaloniaProperty.Register<AddUploadConfigDialog, string>(nameof(ConfigName), "");
        public string ConfigName { get => GetValue(ConfigNameProperty); set => SetValue(ConfigNameProperty, value); }

        public static readonly StyledProperty<ImageUploadMethod> UploadMethodProperty = AvaloniaProperty.Register<AddUploadConfigDialog, ImageUploadMethod>(nameof(UploadMethod), Enums.Enumerable.AvailableImageUploadMethods.First());
        public ImageUploadMethod UploadMethod { get => GetValue(UploadMethodProperty); set => SetValue(UploadMethodProperty, value); }

        public static readonly StyledProperty<string> ErrMsgProperty = AvaloniaProperty.Register<AddUploadConfigDialog, string>(nameof(ErrMsg), "");
        public string ErrMsg { get => GetValue(ErrMsgProperty); set => SetValue(ErrMsgProperty, value); }

        public AddUploadConfigDialog()
        {
            this.InitializeComponent();
        }

[DoNotNotify]
            public class Result
        {
            public string ConfigName { get; set; }

            public ImageUploadMethod UploadMethod { get; set; }
        }

        public static async Task<Result> OpenAddUploadConfigDialog(object xamlRoot)
        {
            var dialog = new AddUploadConfigDialog();
            dialog.PrimaryButtonClick += (s, e) =>
            {
                if (string.IsNullOrEmpty(dialog.ConfigName))
                {
                    e.Cancel = true;
                    dialog.ErrMsg = Locale.GetString("NameCannotBeEmpty");
                }
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
                return new Result() { ConfigName = dialog.ConfigName, UploadMethod = dialog.UploadMethod };
            return null;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
