using Typedown.Core.Models;
using Typedown.Core.Models.ExportConfigModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Typedown.Core.Enums;

namespace Typedown.WinUI.Pages.SettingPages.ExportConfigPageParts
{
    public sealed partial class HTMLConfig : UserControl
    {
        public static DependencyProperty ExportConfigProperty { get; } = DependencyProperty.Register(nameof(ExportConfig), typeof(ExportConfig), typeof(HTMLConfig), null);
        public ExportConfig? ExportConfig { get => (ExportConfig?)GetValue(ExportConfigProperty); set => SetValue(ExportConfigProperty, value); }

        public static DependencyProperty HTMLConfigModelProperty { get; } = DependencyProperty.Register(nameof(HTMLConfigModel), typeof(HTMLConfigModel), typeof(HTMLConfig), null);
        public HTMLConfigModel? HTMLConfigModel { get => (HTMLConfigModel?)GetValue(HTMLConfigModelProperty); set => SetValue(HTMLConfigModelProperty, value); }

        private ExportType? loadedExportType;

        public HTMLConfig()
        {
            this.InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            loadedExportType = ExportConfig?.Type;
            HTMLConfigModel = ExportConfig?.LoadExportConfig() as HTMLConfigModel ?? new HTMLConfigModel();
            Bindings.Update();
            HTMLConfigModel.PropertyChanged += OnHTMLConfigModelPropertyChanged;
        }

        private void OnHTMLConfigModelPropertyChanged(object? sender, global::System.ComponentModel.PropertyChangedEventArgs e)
        {
            StoreCurrentConfig();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (HTMLConfigModel != null)
                HTMLConfigModel.PropertyChanged -= OnHTMLConfigModelPropertyChanged;

            StoreCurrentConfig();
        }

        private void StoreCurrentConfig()
        {
            if (ExportConfig == null || HTMLConfigModel == null || ExportConfig.Type != loadedExportType)
                return;

            ExportConfig.StoreExportConfig(HTMLConfigModel);
        }
    }
}
