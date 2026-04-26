using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace Typedown.UI.Resources;

public static class LegacyTextResourceReader
{
    private const string FallbackCulture = "en";

    private static readonly ISet<string> SupportedCultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "en",
        "zh-Hans",
        "zh-Hant"
    };

    private static readonly ISet<string> TemplateKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        "Name1",
        "Color1",
        "Bitmap1",
        "Icon1"
    };

    private static readonly ConcurrentDictionary<(string Culture, LegacyTextResourceGroup Group), IReadOnlyDictionary<string, string>> Cache = new();

    public static IReadOnlyDictionary<string, string> GetGroup(string? cultureName, LegacyTextResourceGroup group)
    {
        var culture = NormalizeCulture(cultureName);
        var localized = LoadGroup(culture, group);

        if (StringComparer.OrdinalIgnoreCase.Equals(culture, FallbackCulture))
        {
            return localized;
        }

        var fallback = LoadGroup(FallbackCulture, group);
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

    public static string? GetString(string? cultureName, LegacyTextResourceGroup group, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var resources = GetGroup(cultureName, group);
        return resources.TryGetValue(key, out var value) ? value : null;
    }

    private static IReadOnlyDictionary<string, string> LoadGroup(string cultureName, LegacyTextResourceGroup group)
    {
        return Cache.GetOrAdd((cultureName, group), static entry =>
        {
            var path = ResolveResourcePath(entry.Culture, entry.Group);
            if (path is null)
            {
                return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));
            }

            var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
            var resources = document
                .Root?
                .Elements("data")
                .Select(element => new
                {
                    Key = element.Attribute("name")?.Value,
                    Value = element.Element("value")?.Value
                })
                .Where(item => item.Key is not null && item.Value is not null && !TemplateKeys.Contains(item.Key))
                .ToDictionary(item => item.Key!, item => item.Value!, StringComparer.Ordinal)
                ?? new Dictionary<string, string>(StringComparer.Ordinal);

            return new ReadOnlyDictionary<string, string>(resources);
        });
    }

    private static string NormalizeCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return FallbackCulture;
        }

        return SupportedCultures.Contains(cultureName) ? cultureName : FallbackCulture;
    }

    private static string? ResolveResourcePath(string cultureName, LegacyTextResourceGroup group)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "Dev",
                "Typedown.Core",
                "Resources",
                "Strings",
                cultureName,
                $"{group}.resw");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }
}
