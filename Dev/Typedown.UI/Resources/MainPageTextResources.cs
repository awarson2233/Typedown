namespace Typedown.UI.Resources;

public static class MainPageTextResources
{
    public static string Title => "Typedown WinUI3 Platform Services";

    public static string Subtitle =>
        "Typedown.UI keeps the MVVM boundary while preserving the WinUI WebView2 editor host layout.";

    public static string PendingContractsProbeSummary =>
        "UI resources are waiting for WinUI platform service initialization.";

    public static IReadOnlyList<string> ValidatedItems { get; } =
    [
        "WinUI3 shell keeps platform service implementations and activation behind Typedown.WinUI.",
        "Typedown.UI owns page-level MVVM state and composition, not process startup or package deployment.",
        "The editor host remains in Typedown.WinUI while the UI boundary is introduced.",
        "The same Resources\\Statics editor bundle remains the runtime editor asset source."
    ];

    public static IReadOnlyList<string> DeferredItems { get; } =
    [
        "Real legacy page/control migration stays deferred until the inventory can be split into safe batches.",
        "Real Typedown.Core document workflow integration remains after the MVVM boundary.",
        "WinUI platform services stay in Typedown.WinUI and are not registered by Typedown.UI.",
        "ARM64 validation remains out of scope until after WinUI3 cutover."
    ];
}
