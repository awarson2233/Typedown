using PropertyChanged;
﻿using System.Threading.Tasks;
using Typedown.Core.Utilities;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Typedown.Core.Controls
{
[DoNotNotify]
        public sealed partial class InsertTableDialog : UserControl
    {
        public InsertTableDialog()
        {
            InitializeComponent();
        }

[DoNotNotify]
            public class Result
        {
            public int Rows { get; set; }

            public int Columns { get; set; }
        }

        public static async Task<Result> OpenInsertTableDialog(Control xamlRoot)
        {
            var (dialog, content) = CreateContentDialog(Locale.GetDialogString("InsertTableTitle"));
            var result = await dialog.ShowAsync(xamlRoot);
            var rowsControl = content.FindControl<NumericUpDown>("rows");
            var columnsControl = content.FindControl<NumericUpDown>("columns");
            if (result == ContentDialogResult.Primary)
                return new() { Rows = (int)(rowsControl?.Value ?? 0), Columns = (int)(columnsControl?.Value ?? 0) };
            return null;
        }

        public static async Task<Result> OpenResizeTableDialog(Control xamlRoot)
        {
            var (dialog, content) = CreateContentDialog(Locale.GetDialogString("ResizeTableTitle"));
            var result = await dialog.ShowAsync(xamlRoot);
            var rowsControl = content.FindControl<NumericUpDown>("rows");
            var columnsControl = content.FindControl<NumericUpDown>("columns");
            if (result == ContentDialogResult.Primary)
                return new() { Rows = (int)(rowsControl?.Value ?? 0), Columns = (int)(columnsControl?.Value ?? 0) };
            return null;
        }

        private static (AppContentDialog, InsertTableDialog) CreateContentDialog(string title)
        {
            var content = new InsertTableDialog();
            var dialog = AppContentDialog.Create(title, content, Locale.GetDialogString("Cancel"), Locale.GetDialogString("Ok"));
            return (dialog, content);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
