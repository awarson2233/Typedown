namespace Typedown.WinUI.Views
{
    public partial class MainPage : Page
    {
        public string ContractsProbeSummary { get; } = Phase10ContractsProbe.Describe();

        public string[] ValidatedItems { get; } =
        {
            "WinUI3 project builds independently from the legacy XamlUI app project.",
            "A Windows App SDK window can be created and activated on x64 Debug.",
            "The shell can compile against Typedown.Core.Contracts without importing Typedown.Core or Typedown.XamlUI."
        };

        public string[] DeferredItems { get; } =
        {
            "Platform service implementations stay in later phases; Phase 10a only opens the contracts reference path.",
            "WebView2 editor host parity belongs to Phase 11.",
            "Typedown.UI project extraction belongs to Phase 12.",
            "ARM64 validation belongs to Phase 16 after WinUI3 shell cutover."
        };

        public MainPage()
        {
            this.InitializeComponent();
        }
    }
}
