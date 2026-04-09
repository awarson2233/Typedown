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
        public sealed partial class AddExportConfigDialog : AppContentDialog
    {
        public static readonly StyledProperty<string> ConfigNameProperty = AvaloniaProperty.Register<AddExportConfigDialog, string>(nameof(ConfigName), "");
        public string ConfigName { get => GetValue(ConfigNameProperty); set => SetValue(ConfigNameProperty, value); }

        public static readonly StyledProperty<ExportType> ExportTypeProperty = AvaloniaProperty.Register<AddExportConfigDialog, ExportType>(nameof(ExportType), Enums.Enumerable.AvailableExportTypes.First());
        public ExportType ExportType { get => GetValue(ExportTypeProperty); set => SetValue(ExportTypeProperty, value); }

        public static readonly StyledProperty<string> ErrMsgProperty = AvaloniaProperty.Register<AddExportConfigDialog, string>(nameof(ErrMsg), "");
        public string ErrMsg { get => GetValue(ErrMsgProperty); set => SetValue(ErrMsgProperty, value); }

        public AddExportConfigDialog()
        {
            this.InitializeComponent();
        }

[DoNotNotify]
            public class Result
        {
            public string ConfigName { get; set; }

            public ExportType ExportType { get; set; }
        }

        public static async Task<Result> OpenAddExportConfigDialog(object xamlRoot)
        {
            var dialog = new AddExportConfigDialog();
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
                return new Result() { ConfigName = dialog.ConfigName, ExportType = dialog.ExportType };
            return null;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
