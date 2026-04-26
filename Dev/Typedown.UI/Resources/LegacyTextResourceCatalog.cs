using System.Collections.ObjectModel;

namespace Typedown.UI.Resources;

public sealed class LegacyTextResourceCatalog
{
    private const string FallbackCulture = "en";

    private readonly IReadOnlyDictionary<(string Culture, LegacyTextResourceGroup Group), IReadOnlyDictionary<string, string>> resources;

    public LegacyTextResourceCatalog(
        IReadOnlyDictionary<(string Culture, LegacyTextResourceGroup Group), IReadOnlyDictionary<string, string>> resources)
    {
        this.resources = resources;
    }

    public IReadOnlyDictionary<string, string> GetGroup(string? cultureName, LegacyTextResourceGroup group)
    {
        var culture = string.IsNullOrWhiteSpace(cultureName) ? FallbackCulture : cultureName;
        var fallback = GetRawGroup(FallbackCulture, group);

        if (StringComparer.OrdinalIgnoreCase.Equals(culture, FallbackCulture))
        {
            return fallback;
        }

        var localized = GetRawGroup(culture, group);
        if (localized.Count == 0)
        {
            return fallback;
        }

        var merged = new Dictionary<string, string>(fallback, StringComparer.Ordinal);
        foreach (var pair in localized)
        {
            merged[pair.Key] = pair.Value;
        }

        return new ReadOnlyDictionary<string, string>(merged);
    }

    public string? GetString(string? cultureName, LegacyTextResourceGroup group, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var groupResources = GetGroup(cultureName, group);
        return groupResources.TryGetValue(key, out var value) ? value : null;
    }

    private IReadOnlyDictionary<string, string> GetRawGroup(string cultureName, LegacyTextResourceGroup group)
    {
        return resources.TryGetValue((cultureName, group), out var groupResources)
            ? groupResources
            : ReadOnlyDictionary<string, string>.Empty;
    }
}
