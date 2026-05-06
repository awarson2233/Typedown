using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using Typedown.Core.Models;
using Typedown.Core.Models.ExportConfigModels;
using Typedown.Core.Utilities;
using Typedown.WinUI.Controls;
using Typedown.Presentation.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Typedown.WinUI.Pages.SettingPages.ExportConfigPageParts
{
    public sealed partial class PDFConfig : UserControl, INotifyPropertyChanged
    {
        public static DependencyProperty ExportConfigProperty { get; } = DependencyProperty.Register(nameof(ExportConfig), typeof(ExportConfig), typeof(PDFConfig), null);
        public ExportConfig? ExportConfig { get => (ExportConfig?)GetValue(ExportConfigProperty); set => SetValue(ExportConfigProperty, value); }

        public static DependencyProperty PDFConfigModelProperty { get; } = DependencyProperty.Register(nameof(PDFConfigModel), typeof(PDFConfigModel), typeof(PDFConfig), null);
        public PDFConfigModel? PDFConfigModel { get => (PDFConfigModel?)GetValue(PDFConfigModelProperty); set => SetValue(PDFConfigModelProperty, value); }

        private ObservableCollection<PDFConfigPageSizeItem> pageSizeComboxItems = new();
        public ObservableCollection<PDFConfigPageSizeItem> PageSizeComboxItems
        {
            get => pageSizeComboxItems;
            set => SetProperty(ref pageSizeComboxItems, value);
        }

        private PDFConfigPageSizeItem? pageSizeComboxSelectedItem;
        public PDFConfigPageSizeItem? PageSizeComboxSelectedItem
        {
            get => pageSizeComboxSelectedItem;
            set => SetProperty(ref pageSizeComboxSelectedItem, value);
        }

        private ObservableCollection<PDFConfigPageMarginItem> pageMarginComboxItems = new();
        public ObservableCollection<PDFConfigPageMarginItem> PageMarginComboxItems
        {
            get => pageMarginComboxItems;
            set => SetProperty(ref pageMarginComboxItems, value);
        }

        private PDFConfigPageMarginItem? pageMarginComboxSelectedItem;
        public PDFConfigPageMarginItem? PageMarginComboxSelectedItem
        {
            get => pageMarginComboxSelectedItem;
            set => SetProperty(ref pageMarginComboxSelectedItem, value);
        }

        private readonly CompositeDisposable disposables = new();
        private Typedown.Core.Enums.ExportType? loadedExportType;

        public event PropertyChangedEventHandler? PropertyChanged;

        public PDFConfig()
        {
            this.InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            loadedExportType = ExportConfig?.Type;
            PDFConfigModel = ExportConfig?.LoadExportConfig() as PDFConfigModel ?? new PDFConfigModel();
            PageSizeComboxItems = new(PageSize.StandardPageSizes.Select(x => new PDFConfigPageSizeItem() { Name = x.Name, PageSize = x.PageSize }));
            PageMarginComboxItems = new(PageMargin.StandardPageMargin.Select(x => new PDFConfigPageMarginItem() { Name = x.Name, PageMargin = x.PageMargin }));
            Bindings.Update();
            disposables.Add(PDFConfigModel.GetPropertyObservable().Subscribe(_ => StoreCurrentConfig()));
            disposables.Add(PDFConfigModel.PageSize.GetPropertyObservable().Subscribe(_ =>
            {
                OnPageSizeChanged();
                StoreCurrentConfig();
            }));
            disposables.Add(PDFConfigModel.Margins.GetPropertyObservable().Subscribe(_ =>
            {
                OnPageMarginChanged();
                StoreCurrentConfig();
            }));
            OnPageSizeChanged();
            OnPageMarginChanged();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StoreCurrentConfig();
            disposables.Clear();
        }

        private void OnPageSizeComboxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox { SelectedItem: PDFConfigPageSizeItem selected } || selected.PageSize == null || PDFConfigModel == null)
                return;

            PDFConfigModel.PageSize.Width = selected.PageSize.Width;
            PDFConfigModel.PageSize.Height = selected.PageSize.Height;
        }

        private void OnPageMarginComboxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox { SelectedItem: PDFConfigPageMarginItem selected } || selected.PageMargin == null || PDFConfigModel == null)
                return;

            PDFConfigModel.Margins.Left = selected.PageMargin.Left;
            PDFConfigModel.Margins.Top = selected.PageMargin.Top;
            PDFConfigModel.Margins.Right = selected.PageMargin.Right;
            PDFConfigModel.Margins.Bottom = selected.PageMargin.Bottom;
        }

        private void OnPageSizeChanged()
        {
            if (PDFConfigModel == null)
                return;

            var customItem = PageSizeComboxItems.Where(x => x.PageSize == null).FirstOrDefault();
            var selectItem = PageSizeComboxItems.Where(x => x.PageSize != null && x.PageSize.ApproxEquals(PDFConfigModel.PageSize)).FirstOrDefault();
            if (selectItem == null)
            {
                if (customItem == null)
                    PageSizeComboxItems.Add(selectItem = new() { Name = Locale.GetString("Custom") });
                else
                    selectItem = customItem;
            }
            else
            {
                if (customItem != null)
                    PageSizeComboxItems.Remove(customItem);
            }
            PageSizeComboxSelectedItem = selectItem;
        }

        private void OnPageMarginChanged()
        {
            if (PDFConfigModel == null)
                return;

            var customItem = PageMarginComboxItems.Where(x => x.PageMargin == null).FirstOrDefault();
            var selectItem = PageMarginComboxItems.Where(x => x.PageMargin != null && x.PageMargin.ApproxEquals(PDFConfigModel.Margins)).FirstOrDefault();
            if (selectItem == null)
            {
                if (customItem == null)
                    PageMarginComboxItems.Add(selectItem = new() { Name = Locale.GetString("Custom") });
                else
                    selectItem = customItem;
            }
            else
            {
                if (customItem != null)
                    PageMarginComboxItems.Remove(customItem);
            }
            PageMarginComboxSelectedItem = selectItem;
        }

        private void StoreCurrentConfig()
        {
            if (ExportConfig == null || PDFConfigModel == null || ExportConfig.Type != loadedExportType)
                return;

            ExportConfig.StoreExportConfig(PDFConfigModel);
        }

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }

    public class PDFConfigPageSizeItem
    {
        public string Name { get; set; } = string.Empty;

        public PageSize? PageSize { get; set; }
    }

    public class PDFConfigPageMarginItem
    {
        public string Name { get; set; } = string.Empty;

        public PageMargin? PageMargin { get; set; }
    }
}
