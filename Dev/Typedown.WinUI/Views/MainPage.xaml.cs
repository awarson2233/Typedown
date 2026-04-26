using System;
using System.Linq;
using Microsoft.UI.Xaml.Navigation;
using Typedown.WinUI.Services;

namespace Typedown.WinUI.Views
{
    public partial class MainPage : Page
    {
        public string ContractsProbeSummary { get; private set; }

        public string[] ServiceItems { get; private set; }

        public string[] ValidatedItems { get; } =
        {
            "WinUI3 shell now provides minimal platform service implementations behind Typedown.Core.Contracts.",
            "The shell still builds without referencing the legacy app core or the legacy XAML host project.",
            "Phase 10b only proves service wiring, path resolution, dispatcher plumbing, dialogs, pickers, and activation stubs."
        };

        public string[] DeferredItems { get; } =
        {
            "Real editor hosting and page-control migration remain outside Phase 10b.",
            "No legacy data migration or real Typedown.Core integration is included yet.",
            "WebView2 editor host parity belongs to Phase 11.",
            "ARM64 validation remains out of scope for this phase."
        };

        public MainPage()
        {
            this.InitializeComponent();
            ServiceItems = Array.Empty<string>();
            ContractsProbeSummary = "Phase 10b services are waiting for shell initialization.";
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var services = e.Parameter as WinUIPlatformServices ?? ((App)Application.Current).PlatformServices;
            ContractsProbeSummary = Phase10ContractsProbe.Describe(services);
            ServiceItems = services.ServiceNames.Select(name => $"Validated service: {name}").ToArray();
            Bindings.Update();
        }
    }
}
