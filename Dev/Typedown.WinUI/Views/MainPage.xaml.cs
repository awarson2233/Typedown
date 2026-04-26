namespace Typedown.WinUI.Views
{
    public partial class MainPage : Page
    {
        public string[] ValidatedItems { get; } =
        {
            "WinUI3 project builds independently from the legacy XamlUI app project.",
            "A Windows App SDK window can be created and activated on x64 Debug.",
            "The shell has a dedicated namespace and solution entry for future integration."
        };

        public string[] DeferredItems { get; } =
        {
            "Core service registration is deferred because Typedown.Core still references Dev\\Typedown.XamlUI.",
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
