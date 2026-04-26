using Typedown.UI.Mvvm;
using Typedown.UI.Resources;

namespace Typedown.UI.ViewModels;

public sealed class MainPageViewModel : ObservableObject
{
    private string contractsProbeSummary = MainPageTextResources.PendingContractsProbeSummary;
    private IReadOnlyList<MigrationBoundaryItem> serviceItems = Array.Empty<MigrationBoundaryItem>();

    public MainPageViewModel()
        : this(new Phase14ShellViewModel())
    {
    }

    public MainPageViewModel(Phase14ShellViewModel shell)
    {
        Shell = shell;
    }

    public string Title { get; } = MainPageTextResources.Title;

    public string Subtitle { get; } = MainPageTextResources.Subtitle;

    public Phase14ShellViewModel Shell { get; }

    public string ContractsProbeSummary
    {
        get => contractsProbeSummary;
        private set => SetProperty(ref contractsProbeSummary, value);
    }

    public IReadOnlyList<MigrationBoundaryItem> ValidatedItems { get; } =
        MainPageTextResources.ValidatedItems.Select(text => new MigrationBoundaryItem(text)).ToArray();

    public IReadOnlyList<MigrationBoundaryItem> ServiceItems
    {
        get => serviceItems;
        private set => SetProperty(ref serviceItems, value);
    }

    public IReadOnlyList<MigrationBoundaryItem> DeferredItems { get; } =
        MainPageTextResources.DeferredItems.Select(text => new MigrationBoundaryItem(text)).ToArray();

    public void ApplyPlatformServiceSummary(string summary, IEnumerable<string> serviceNames)
    {
        ContractsProbeSummary = summary;
        ServiceItems = serviceNames.Select(name => new MigrationBoundaryItem($"Validated service: {name}")).ToArray();
    }
}
