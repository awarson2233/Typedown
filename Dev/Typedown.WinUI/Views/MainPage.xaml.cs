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
            "WinUI3 shell keeps platform service implementations and the activation stub contract surface behind Typedown.Core.Contracts.",
            "Phase 11 editor host is implemented inside Typedown.WinUI without referencing Typedown.Core or the legacy XAML host project.",
            "The host loads the same Resources\\Statics editor bundle path used by the legacy app when the bundle exists."
        };

        public string[] DeferredItems { get; } =
        {
            "Full MarkdownEditor command parity and document workflow remain later Phase 11 work.",
            "No legacy data migration or real Typedown.Core integration is included yet.",
            "Legacy page/control migration belongs to Typedown.UI phases, not this host smoke slice.",
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
