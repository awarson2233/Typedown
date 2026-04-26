using Typedown.UI.Mvvm;

namespace Typedown.UI.ViewModels;

public sealed class MainPageViewModel : ObservableObject
{
    private string contractsProbeSummary = "Phase 12 MVVM shell is waiting for WinUI platform service initialization.";
    private IReadOnlyList<MigrationBoundaryItem> serviceItems = Array.Empty<MigrationBoundaryItem>();

    public string Title { get; } = "Typedown WinUI3 Platform Services";

    public string Subtitle { get; } =
        "Phase 12 introduces the Typedown.UI MVVM boundary while preserving the Phase 11 WebView2 editor host layout.";

    public string ContractsProbeSummary
    {
        get => contractsProbeSummary;
        private set => SetProperty(ref contractsProbeSummary, value);
    }

    public IReadOnlyList<MigrationBoundaryItem> ValidatedItems { get; } =
    [
        new("WinUI3 shell keeps platform service implementations and activation behind Typedown.WinUI."),
        new("Typedown.UI owns page-level MVVM state and composition, not process startup or package deployment."),
        new("The Phase 11 editor host remains in Typedown.WinUI while the UI boundary is introduced."),
        new("The same Resources\\Statics editor bundle remains the runtime editor asset source.")
    ];

    public IReadOnlyList<MigrationBoundaryItem> ServiceItems
    {
        get => serviceItems;
        private set => SetProperty(ref serviceItems, value);
    }

    public IReadOnlyList<MigrationBoundaryItem> DeferredItems { get; } =
    [
        new("Real legacy page/control migration is deferred to Phase 13."),
        new("Real Typedown.Core document workflow integration remains after the MVVM skeleton."),
        new("WinUI platform services stay in Typedown.WinUI and are not registered by Typedown.UI."),
        new("ARM64 validation remains out of scope until after WinUI3 cutover.")
    ];

    public void ApplyPlatformServiceSummary(string summary, IEnumerable<string> serviceNames)
    {
        ContractsProbeSummary = summary;
        ServiceItems = serviceNames.Select(name => new MigrationBoundaryItem($"Validated service: {name}")).ToArray();
    }
}
