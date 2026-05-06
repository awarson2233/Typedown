using Typedown.Core.Models;
using Typedown.Core.Models.ExportConfigModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Typedown.Core.Enums;

namespace Typedown.WinUI.Pages.SettingPages.ExportConfigPageParts
{
    public sealed partial class ImageConfig : UserControl
    {
        public static DependencyProperty ExportConfigProperty { get; } = DependencyProperty.Register(nameof(ExportConfig), typeof(ExportConfig), typeof(ImageConfig), null);
        public ExportConfig? ExportConfig { get => (ExportConfig?)GetValue(ExportConfigProperty); set => SetValue(ExportConfigProperty, value); }

        public static DependencyProperty ImageConfigModelProperty { get; } = DependencyProperty.Register(nameof(ImageConfigModel), typeof(ImageConfigModel), typeof(ImageConfig), null);
        public ImageConfigModel? ImageConfigModel { get => (ImageConfigModel?)GetValue(ImageConfigModelProperty); set => SetValue(ImageConfigModelProperty, value); }

        private ExportType? loadedExportType;

        public ImageConfig()
        {
            this.InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            loadedExportType = ExportConfig?.Type;
            ImageConfigModel = ExportConfig?.LoadExportConfig() as ImageConfigModel ?? new ImageConfigModel();
            Bindings.Update();
            ImageConfigModel.PropertyChanged += OnImageConfigModelPropertyChanged;
        }

        private void OnImageConfigModelPropertyChanged(object? sender, global::System.ComponentModel.PropertyChangedEventArgs e)
        {
            StoreCurrentConfig();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (ImageConfigModel != null)
                ImageConfigModel.PropertyChanged -= OnImageConfigModelPropertyChanged;

            StoreCurrentConfig();
        }

        private void StoreCurrentConfig()
        {
            if (ExportConfig == null || ImageConfigModel == null || ExportConfig.Type != loadedExportType)
                return;

            ExportConfig.StoreExportConfig(ImageConfigModel);
        }
    }
}
