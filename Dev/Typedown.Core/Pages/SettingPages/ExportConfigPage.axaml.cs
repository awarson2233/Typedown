using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Typedown.Core.Controls.SettingControls.SettingItems.ExportConfigItems;
using Typedown.Core.Enums;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;

namespace Typedown.Core.Pages.SettingPages
{
    [Locale("ExportConfig.Title")]
    public sealed partial class ExportConfigPage : UserControl
    {
        public static readonly StyledProperty<ExportConfig> ExportConfigProperty =
            AvaloniaProperty.Register<ExportConfigPage, ExportConfig>(nameof(ExportConfig), null);
        private ExportConfig ExportConfig { get => GetValue(ExportConfigProperty); set => SetValue(ExportConfigProperty, value); }

        public AppViewModel ViewModel => DataContext as AppViewModel;

        public Lazy<IFileExport> ExportService { get; }

        private int configId;

        private readonly CompositeDisposable disposables = new();

        public ExportConfigPage()
        {
            ExportService = new(() => this.GetService<IFileExport>());
            InitializeComponent();
        }

        /// <summary>
        /// Call this to initialize the page with a config ID parameter.
        /// Replaces WinUI OnNavigatedTo(NavigationEventArgs e).
        /// </summary>
        public void Initialize(string parameter)
        {
            int.TryParse(parameter, out configId);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            ExportConfig = await ExportService.Value.GetExportConfig(configId);
            if (ExportConfig != null)
                disposables.Add(ExportConfig.WhenPropertyChanged(nameof(ExportConfig.Name)).Cast<string>().StartWith(ExportConfig.Name).Subscribe(UpdateTitle));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (ExportConfig != null)
            {
                var config = ExportConfig;
                var service = ExportService.Value;
                _ = Task.Run(async () => await service.SaveExportConfig(config));
            }
            disposables.Clear();
        }

        private void UpdateTitle(string title)
        {
            this.GetAncestor<SettingsPage>()?.SetPageTitle(this, title);
        }

        public Control GetExportConfigItem(ExportType type)
        {
            return type switch
            {
                ExportType.PDF => new PDFConfig() { ExportConfig = ExportConfig },
                ExportType.HTML => new HTMLConfig() { ExportConfig = ExportConfig },
                ExportType.Image => new ImageConfig() { ExportConfig = ExportConfig },
                _ => null
            };
        }

        public async Task DeleteConfigAsync()
        {
            if (ExportConfig != null)
            {
                var service = this.GetService<IFileExport>();
                await service.RemoveExportConfig(ExportConfig.Id);
                ExportConfig = null;
                // TODO: Navigate back in Avalonia
            }
        }
    }
}
