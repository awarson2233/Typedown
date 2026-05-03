using System.Collections.ObjectModel;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase13LegacyTextResourceTests
{
    private static readonly string[] Cultures = ["en", "zh-Hans", "zh-Hant"];

    private static readonly TextResourceGroup[] Groups =
    [
        TextResourceGroup.CommonResources,
        TextResourceGroup.DialogResources,
        TextResourceGroup.Resources,
        TextResourceGroup.SettingsResources
    ];

    private static readonly string[] ReswTemplateKeys = ["Name1", "Color1", "Bitmap1", "Icon1"];

    [TestMethod]
    public void TextResources_ReadsEnglishAppName()
    {
        Assert.AreEqual(
            "Typedown",
            TestTextResourceReader.GetString("en", TextResourceGroup.Resources, "AppName"));
    }

    [TestMethod]
    public void TextResources_ReadsCommonCommandText()
    {
        var common = TestTextResourceReader.GetGroup("en", TextResourceGroup.CommonResources);

        Assert.AreEqual("Cancel", common["Cancel"]);
        Assert.AreEqual("OK", common["Ok"]);
        Assert.AreEqual("Settings", common["Settings"]);
    }

    [TestMethod]
    public void TextResources_ReadsSettingsThemeText()
    {
        var settings = TestTextResourceReader.GetGroup("en", TextResourceGroup.SettingsResources);

        Assert.AreEqual("Light", settings["View.AppTheme.Light"]);
        Assert.AreEqual("Dark", settings["View.AppTheme.Dark"]);
    }

    [TestMethod]
    public void TextResources_ReadsDialogResourceText()
    {
        Assert.AreEqual(
            "You have unsaved changes",
            TestTextResourceReader.GetString("en", TextResourceGroup.DialogResources, "AsKToSaveContent"));
    }

    [TestMethod]
    public void TextResources_ReadsChineseSimplifiedValuesThatDifferFromEnglish()
    {
        Assert.AreEqual(
            "取消",
            TestTextResourceReader.GetString("zh-Hans", TextResourceGroup.CommonResources, "Cancel"));
    }

    [TestMethod]
    public void TextResources_ReadsChineseTraditionalValuesThatDifferFromEnglish()
    {
        var english = TestTextResourceReader.GetString("en", TextResourceGroup.DialogResources, "AsKToSaveContent");
        var traditionalChinese = TestTextResourceReader.GetString("zh-Hant", TextResourceGroup.DialogResources, "AsKToSaveContent");

        Assert.AreEqual("您有未儲存的變更", traditionalChinese);
        Assert.AreNotEqual(english, traditionalChinese);
    }

    [TestMethod]
    public void TextResources_AllSupportedCulturesAndGroupsHaveExpectedKeyCountsAndExcludeTemplateKeys()
    {
        foreach (var culture in Cultures)
        {
            foreach (var group in Groups)
            {
                var resources = TestTextResourceReader.GetGroup(culture, group);

                Assert.AreEqual(GetExpectedKeyCount(group), resources.Count, $"{culture}/{group} key count changed.");
                Assert.IsTrue(resources.Count > 0, $"{culture}/{group} should not be empty.");

                foreach (var templateKey in ReswTemplateKeys)
                {
                    CollectionAssert.DoesNotContain(resources.Keys.ToArray(), templateKey, $"{culture}/{group} includes template key {templateKey}.");
                }
            }
        }
    }

    [TestMethod]
    public void TextResources_LocalizedGroupsHaveSameKeyShapeAsEnglish()
    {
        foreach (var group in Groups)
        {
            var englishKeys = TestTextResourceReader.GetGroup("en", group).Keys.ToArray();

            foreach (var culture in Cultures.Where(culture => culture != "en"))
            {
                var localizedKeys = TestTextResourceReader.GetGroup(culture, group).Keys.ToArray();

                CollectionAssert.AreEquivalent(englishKeys, localizedKeys, $"{culture}/{group} key shape differs from en.");
            }
        }
    }

    [TestMethod]
    public void TextResources_FallsBackToEnglishForUnknownCulture()
    {
        Assert.AreEqual(
            "Typedown",
            TestTextResourceReader.GetString("fr-FR", TextResourceGroup.Resources, "AppName"));
    }

    [TestMethod]
    public void TextResources_ReturnsNullForUnknownKey()
    {
        Assert.IsNull(
            TestTextResourceReader.GetString("en", TextResourceGroup.Resources, "Missing.Key"));
    }

    [TestMethod]
    public void TextResources_FallsBackToEnglishForMissingLocalizedKey()
    {
        var catalog = new TestTextResourceCatalog(new Dictionary<(string Culture, TextResourceGroup Group), IReadOnlyDictionary<string, string>>
        {
            [("en", TextResourceGroup.CommonResources)] = new Dictionary<string, string>
            {
                ["Cancel"] = "Cancel",
                ["Ok"] = "OK"
            },
            [("zh-Hans", TextResourceGroup.CommonResources)] = new Dictionary<string, string>
            {
                ["Cancel"] = "取消"
            }
        });

        Assert.AreEqual("取消", catalog.GetString("zh-Hans", TextResourceGroup.CommonResources, "Cancel"));
        Assert.AreEqual("OK", catalog.GetString("zh-Hans", TextResourceGroup.CommonResources, "Ok"));
    }

    [TestMethod]
    public void TextResources_ProjectIncludesCopiedPresentationStringResources()
    {
        var projectSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "Typedown.Presentation.csproj"));

        StringAssert.Contains(projectSource, @"Resources\Strings\**\*.resw");
        Assert.IsFalse(projectSource.Contains("Typedown.Core.Legacy", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Presentation_DoesNotCarryLegacyTextResourceRuntimeHelpers()
    {
        var resourcesRoot = Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "Resources");

        Assert.IsFalse(File.Exists(Path.Combine(resourcesRoot, "LegacyTextResourceCatalog.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(resourcesRoot, "LegacyTextResourceGroup.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(resourcesRoot, "LegacyTextResourceReader.cs")));
    }

    private static string RepoRoot => ResolveRepoRoot();

    private static string ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Typedown.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repo root from test base directory.");
    }

    private static int GetExpectedKeyCount(TextResourceGroup group)
    {
        return group switch
        {
            TextResourceGroup.CommonResources => 204,
            TextResourceGroup.DialogResources => 33,
            TextResourceGroup.Resources => 1,
            TextResourceGroup.SettingsResources => 141,
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
        };
    }

    private enum TextResourceGroup
    {
        CommonResources,
        DialogResources,
        Resources,
        SettingsResources
    }

    private sealed class TestTextResourceCatalog
    {
        private const string FallbackCulture = "en";

        private readonly IReadOnlyDictionary<(string Culture, TextResourceGroup Group), IReadOnlyDictionary<string, string>> resources;

        public TestTextResourceCatalog(
            IReadOnlyDictionary<(string Culture, TextResourceGroup Group), IReadOnlyDictionary<string, string>> resources)
        {
            this.resources = resources;
        }

        public IReadOnlyDictionary<string, string> GetGroup(string? cultureName, TextResourceGroup group)
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

        public string? GetString(string? cultureName, TextResourceGroup group, string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            var groupResources = GetGroup(cultureName, group);
            return groupResources.TryGetValue(key, out var value) ? value : null;
        }

        private IReadOnlyDictionary<string, string> GetRawGroup(string cultureName, TextResourceGroup group)
        {
            return resources.TryGetValue((cultureName, group), out var groupResources)
                ? groupResources
                : ReadOnlyDictionary<string, string>.Empty;
        }
    }

    private static class TestTextResourceReader
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

        private static readonly Dictionary<(string Culture, TextResourceGroup Group), IReadOnlyDictionary<string, string>> Cache = [];

        public static IReadOnlyDictionary<string, string> GetGroup(string? cultureName, TextResourceGroup group)
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

        public static string? GetString(string? cultureName, TextResourceGroup group, string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            var resources = GetGroup(cultureName, group);
            return resources.TryGetValue(key, out var value) ? value : null;
        }

        private static IReadOnlyDictionary<string, string> LoadGroup(string cultureName, TextResourceGroup group)
        {
            var key = (cultureName, group);
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var path = Path.Combine(
                RepoRoot,
                "Dev",
                "Typedown.Presentation",
                "Resources",
                "Strings",
                cultureName,
                $"{group}.resw");

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

            var readOnly = new ReadOnlyDictionary<string, string>(resources);
            Cache[key] = readOnly;
            return readOnly;
        }

        private static string NormalizeCulture(string? cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
            {
                return FallbackCulture;
            }

            return SupportedCultures.Contains(cultureName) ? cultureName : FallbackCulture;
        }
    }
}
