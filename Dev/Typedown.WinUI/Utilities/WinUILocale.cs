using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using Typedown.Core;
using Typedown.Core.Utilities;
using Windows.ApplicationModel.Resources.Core;
using PresentationLocale = Typedown.Presentation.Utilities.Locale;

namespace Typedown.WinUI.Utilities;

internal static class WinUILocale
{
    private static readonly PresentationLocale.ResourceSource[] OrderedSources =
    [
        PresentationLocale.ResourceSource.CommonResources,
        PresentationLocale.ResourceSource.DialogResources,
        PresentationLocale.ResourceSource.SettingsResources,
        PresentationLocale.ResourceSource.Resources
    ];

    private static readonly IReadOnlyDictionary<PresentationLocale.ResourceSource, ResourceMap> ResourceMaps = CreateResourceMaps();

    private const string DefaultLanguageKey = "default";

    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> ReswCache = new();

    private static readonly CultureInfo SystemUICulture = CultureInfo.CurrentUICulture;

    public static void Initialize()
    {
        PresentationLocale.StringResolver = GetString;
        LocaleAttribute.StringResolver = key => PresentationLocale.GetString(key);
    }

    public static void ApplyPersistedLanguageOverride()
    {
        ApplyLanguageOverride(ReadPersistedLanguage());
    }

    public static void ApplyLanguageOverride(string? language)
    {
        if (string.IsNullOrWhiteSpace(language) || StringComparer.OrdinalIgnoreCase.Equals(language, DefaultLanguageKey))
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride))
                {
                    Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = string.Empty;
                }
            }
            catch
            {
                // Keep using the system culture when WinRT rejects clearing the language override.
            }

            CultureInfo.CurrentUICulture = SystemUICulture;
            CultureInfo.CurrentCulture = SystemUICulture;
            return;
        }

        if (!PresentationLocale.SupportedLangs.ContainsKey(language))
        {
            return;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(language);
            Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = language;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
        }
        catch
        {
            // Keep the existing culture if the persisted language tag is not supported by the OS.
        }
    }

    public static bool IsRestartRequiredForLanguage(string? language)
    {
        var normalizedLanguage = string.IsNullOrWhiteSpace(language) ? DefaultLanguageKey : language;
        var currentLanguage = Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;

        if (StringComparer.OrdinalIgnoreCase.Equals(normalizedLanguage, DefaultLanguageKey))
        {
            return !string.IsNullOrWhiteSpace(currentLanguage);
        }

        return !StringComparer.OrdinalIgnoreCase.Equals(normalizedLanguage, currentLanguage);
    }

    private static string ReadPersistedLanguage()
    {
        try
        {
            var settingsFile = Config.GetSettingsFilePath();
            if (!File.Exists(settingsFile))
            {
                return DefaultLanguageKey;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(settingsFile));
            if (document.RootElement.TryGetProperty("Language", out var language)
                && language.ValueKind == JsonValueKind.String
                && language.GetString() is { Length: > 0 } value)
            {
                return value;
            }
        }
        catch
        {
            // Fall back to system language when settings are unavailable or invalid.
        }

        return DefaultLanguageKey;
    }

    private static IReadOnlyDictionary<PresentationLocale.ResourceSource, ResourceMap> CreateResourceMaps()
    {
        var result = new Dictionary<PresentationLocale.ResourceSource, ResourceMap>();

        foreach (var source in OrderedSources)
        {
            var map = TryGetResourceMap(source);
            if (map is not null)
            {
                result[source] = map;
            }
        }

        return result;
    }

    private static ResourceMap? TryGetResourceMap(PresentationLocale.ResourceSource source)
    {
        if (!Config.IsPackaged)
        {
            return null;
        }

        foreach (var subtree in GetResourceMapSubtrees(source))
        {
            try
            {
                var map = ResourceManager.Current.MainResourceMap.GetSubtree(subtree);
                if (map is not null)
                {
                    return map;
                }
            }
            catch
            {
                // Resource maps can be unavailable in unpackaged or test contexts; file fallback handles those cases.
            }
        }

        return null;
    }

    private static IEnumerable<string> GetResourceMapSubtrees(PresentationLocale.ResourceSource source)
    {
        yield return $"Typedown.WinUI/{source}";
        yield return $"Typedown/{source}";
        yield return $"{source}";
    }

    private static string GetString(string key, PresentationLocale.ResourceSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        foreach (var resourceSource in GetResourceSources(source))
        {
            if (ResourceMaps.TryGetValue(resourceSource, out var map))
            {
                var value = GetResourceMapString(map, key);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
        }

        return GetReswString(key, source) ?? key;
    }

    private static string? GetResourceMapString(ResourceMap map, string key)
    {
        foreach (var candidate in GetKeyCandidates(key))
        {
            try
            {
                var resourceContext = TryCreateResourceContext();
                if (resourceContext is null)
                {
                    return null;
                }

                var value = map.GetValue(candidate, resourceContext)?.ValueAsString;
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            catch
            {
                // Some key forms are valid in .resw but invalid in resource maps; keep trying fallbacks.
            }
        }

        return null;
    }

    private static ResourceContext? TryCreateResourceContext()
    {
        try
        {
            return new ResourceContext();
        }
        catch
        {
            return null;
        }
    }

    private static string? GetReswString(string key, PresentationLocale.ResourceSource source)
    {
        foreach (var culture in GetCultureFallbacks())
        {
            foreach (var resourceSource in GetResourceSources(source))
            {
                var resources = ReswCache.GetOrAdd($"{culture}/{resourceSource}", _ => LoadResw(culture, resourceSource));
                foreach (var candidate in GetKeyCandidates(key))
                {
                    if (resources.TryGetValue(candidate, out var value) && !string.IsNullOrEmpty(value))
                    {
                        return value;
                    }
                }
            }
        }

        return null;
    }

    private static IEnumerable<PresentationLocale.ResourceSource> GetResourceSources(PresentationLocale.ResourceSource source)
    {
        if (source != PresentationLocale.ResourceSource.All)
        {
            yield return source;
            yield break;
        }

        foreach (var resourceSource in OrderedSources)
        {
            yield return resourceSource;
        }
    }

    private static IEnumerable<string> GetKeyCandidates(string key)
    {
        yield return key;

        var resourceMapKey = key.Replace('.', '/');
        if (!StringComparer.Ordinal.Equals(resourceMapKey, key))
        {
            yield return resourceMapKey;
        }
    }

    private static IEnumerable<string> GetCultureFallbacks()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var culture in ExpandCulture(CultureInfo.CurrentUICulture))
        {
            if (seen.Add(culture))
            {
                yield return culture;
            }
        }

        foreach (var culture in new[] { "en", "zh-Hans" })
        {
            if (seen.Add(culture))
            {
                yield return culture;
            }
        }
    }

    private static IEnumerable<string> ExpandCulture(CultureInfo culture)
    {
        while (!string.IsNullOrEmpty(culture.Name))
        {
            yield return culture.Name;
            culture = culture.Parent;
        }
    }

    private static IReadOnlyDictionary<string, string> LoadResw(string culture, PresentationLocale.ResourceSource source)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "Strings", culture, $"{source}.resw");
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return XDocument.Load(path)
            .Descendants("data")
            .Where(element => element.Attribute("name")?.Value is { Length: > 0 })
            .Select(element => new
            {
                Key = element.Attribute("name")!.Value,
                Value = element.Element("value")?.Value ?? string.Empty
            })
            .SelectMany(item => GetKeyCandidates(item.Key).Select(candidate => new { Key = candidate, item.Value }))
            .GroupBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.Ordinal);
    }
}
