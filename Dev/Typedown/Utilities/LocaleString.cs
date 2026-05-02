using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using Typedown.Core.Utilities;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
using Windows.UI.Xaml.Markup;
using System.Xml.Linq;
using PresentationLocale = Typedown.Presentation.Utilities.Locale;

namespace Typedown.Core.Utilities
{
    public static class AppLocale
    {
        private static readonly IReadOnlyDictionary<PresentationLocale.ResourceSource, ResourceMap> ResourcesDictionary = CreateResourcesDictionary();

        private static ResourceContext ResourceContext { get; } = new();

        private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> ReswCache = new();

        public static void Initialize()
        {
            PresentationLocale.StringResolver = GetString;
            LocaleAttribute.StringResolver = key => PresentationLocale.GetString(key);
        }

        private static IReadOnlyDictionary<PresentationLocale.ResourceSource, ResourceMap> CreateResourcesDictionary()
        {
            var result = new Dictionary<PresentationLocale.ResourceSource, ResourceMap>();
            foreach (var source in new[] { PresentationLocale.ResourceSource.CommonResources, PresentationLocale.ResourceSource.DialogResources, PresentationLocale.ResourceSource.SettingsResources, PresentationLocale.ResourceSource.Resources })
            {
                var map = ResourceManager.Current.MainResourceMap.GetSubtree($"Typedown.Presentation/{source}")
                    ?? ResourceManager.Current.MainResourceMap.GetSubtree($"Typedown/{source}");
                if (map != null)
                    result[source] = map;
            }
            return result;
        }

        private static string GetString(string key, PresentationLocale.ResourceSource source)
        {
            key = key.Replace('.', '/');
            if (source == 0 || !ResourcesDictionary.ContainsKey(source))
                return ResourcesDictionary.Values.Select(x => x.GetValue(key, ResourceContext)?.ValueAsString).Where(x => !string.IsNullOrEmpty(x)).FirstOrDefault()
                    ?? GetReswString(key, source)
                    ?? key;
            return ResourcesDictionary[source].GetValue(key, ResourceContext)?.ValueAsString
                ?? GetReswString(key, source)
                ?? key;
        }

        private static string GetReswString(string key, PresentationLocale.ResourceSource source)
        {
            foreach (var culture in GetCultureFallbacks())
            {
                foreach (var resourceSource in GetResourceSources(source))
                {
                    var resources = ReswCache.GetOrAdd($"{culture}/{resourceSource}", _ => LoadResw(culture, resourceSource));
                    if (resources.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                        return value;
                }
            }

            return null;
        }

        private static IEnumerable<string> GetCultureFallbacks()
        {
            var culture = CultureInfo.CurrentUICulture;
            while (!string.IsNullOrEmpty(culture.Name))
            {
                yield return culture.Name;
                culture = culture.Parent;
            }

            yield return "en";
            yield return "zh-Hans";
        }

        private static IEnumerable<PresentationLocale.ResourceSource> GetResourceSources(PresentationLocale.ResourceSource source)
        {
            if (source != PresentationLocale.ResourceSource.All)
            {
                yield return source;
                yield break;
            }

            yield return PresentationLocale.ResourceSource.CommonResources;
            yield return PresentationLocale.ResourceSource.DialogResources;
            yield return PresentationLocale.ResourceSource.SettingsResources;
            yield return PresentationLocale.ResourceSource.Resources;
        }

        private static IReadOnlyDictionary<string, string> LoadResw(string culture, PresentationLocale.ResourceSource source)
        {
            var path = Path.Combine(global::System.AppContext.BaseDirectory, "Resources", "Strings", culture, $"{source}.resw");
            if (!File.Exists(path))
                return new Dictionary<string, string>();

            return XDocument.Load(path)
                .Descendants("data")
                .Where(x => x.Attribute("name")?.Value is { Length: > 0 })
                .ToDictionary(
                    x => x.Attribute("name").Value.Replace('.', '/'),
                    x => x.Element("value")?.Value ?? string.Empty);
        }
    }

    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    public class LocaleString : MarkupExtension
    {
        public string Key { get; set; }

        public PresentationLocale.ResourceSource Source { get; set; }

        protected override object ProvideValue()
        {
            return PresentationLocale.GetString(Key, Source);
        }
    }

    public static class Locale
    {
        public static IReadOnlyDictionary<string, string> SupportedLangs => PresentationLocale.SupportedLangs;

        public static IReadOnlyDictionary<string, string> LangsOptions => PresentationLocale.LangsOptions;

        public static string GetLangOptionDisplayName(string key) => PresentationLocale.GetLangOptionDisplayName(key);

        public static string GetString(string key, PresentationLocale.ResourceSource source = PresentationLocale.ResourceSource.All)
        {
            return PresentationLocale.GetString(key, source);
        }

        public static string GetDialogString(string key)
        {
            return PresentationLocale.GetDialogString(key);
        }

        public static string GetTypeString(Type type)
        {
            return PresentationLocale.GetTypeString(type);
        }
    }
}
