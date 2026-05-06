using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Typedown.WinUI.Pages.SettingPages.ExportConfigPageParts;
using Typedown.Core.Enums;
using Typedown.Core.Interfaces;
using Typedown.Presentation.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.WinUI.Controls;
using Typedown.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Typedown.WinUI.Pages.SettingPages
{
    [Locale("ExportConfig.Title")]
    public sealed partial class ExportConfigPage : Page
    {
        private static DependencyProperty ExportConfigProperty { get; } = DependencyProperty.Register(nameof(ExportConfig), typeof(ExportConfig), typeof(ExportConfigPage), null);
        private ExportConfig? ExportConfig { get => (ExportConfig?)GetValue(ExportConfigProperty); set => SetValue(ExportConfigProperty, value); }

        public AppViewModel? ViewModel => DataContext as AppViewModel;

        public Lazy<IFileExport> ExportService { get; }

        private int configId;

        private readonly CompositeDisposable disposables = new();

        public ExportConfigPage()
        {
            ExportService = new(() => this.GetService<IFileExport>());
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is SettingsNavigationParameter parameter)
            {
                DataContext = parameter.AppViewModel;
                int.TryParse(parameter.Query, out configId);
                return;
            }

            int.TryParse(e.Parameter?.ToString(), out configId);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            ExportConfig = await ExportService.Value.GetExportConfig(configId);
            if (ExportConfig != null)
            {
                Bindings.Update();
                disposables.Add(ExportConfig.WhenPropertyChanged(nameof(ExportConfig.Name)).Cast<string>().StartWith(ExportConfig.Name).Subscribe(UpdateTitle));
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _ = Dispatcher.RunIdleAsync(async _ =>
            {
                if (ExportConfig != null)
                    await ExportService.Value.SaveExportConfig(ExportConfig);
            });
            disposables.Clear();
        }

        private void UpdateTitle(string title)
        {
            this.GetAncestor<SettingsPage>()?.SetPageTitle(this, title);
        }

        private void OnDeleteButtonClick(object sender, RoutedEventArgs e)
        {
            _ = DeleteConfigAsync();
        }

        public FrameworkElement? GetExportConfigItem(ExportType type)
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
                Frame.GoBack();
            }
        }
    }
}
