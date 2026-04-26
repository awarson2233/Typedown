using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.UI.Resources;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase13LegacyTextResourceTests
{
    private static readonly string[] Cultures = ["en", "zh-Hans", "zh-Hant"];

    private static readonly LegacyTextResourceGroup[] Groups =
    [
        LegacyTextResourceGroup.CommonResources,
        LegacyTextResourceGroup.DialogResources,
        LegacyTextResourceGroup.Resources,
        LegacyTextResourceGroup.SettingsResources
    ];

    private static readonly string[] ReswTemplateKeys = ["Name1", "Color1", "Bitmap1", "Icon1"];

    [TestMethod]
    public void LegacyTextResources_ReadsEnglishAppName()
    {
        Assert.AreEqual(
            "Typedown",
            LegacyTextResourceReader.GetString("en", LegacyTextResourceGroup.Resources, "AppName"));
    }

    [TestMethod]
    public void LegacyTextResources_ReadsCommonCommandText()
    {
        var common = LegacyTextResourceReader.GetGroup("en", LegacyTextResourceGroup.CommonResources);

        Assert.AreEqual("Cancel", common["Cancel"]);
        Assert.AreEqual("OK", common["Ok"]);
        Assert.AreEqual("Settings", common["Settings"]);
    }

    [TestMethod]
    public void LegacyTextResources_ReadsSettingsThemeText()
    {
        var settings = LegacyTextResourceReader.GetGroup("en", LegacyTextResourceGroup.SettingsResources);

        Assert.AreEqual("Light", settings["View.AppTheme.Light"]);
        Assert.AreEqual("Dark", settings["View.AppTheme.Dark"]);
    }

    [TestMethod]
    public void LegacyTextResources_ReadsDialogResourceText()
    {
        Assert.AreEqual(
            "You have unsaved changes",
            LegacyTextResourceReader.GetString("en", LegacyTextResourceGroup.DialogResources, "AsKToSaveContent"));
    }

    [TestMethod]
    public void LegacyTextResources_ReadsChineseSimplifiedValuesThatDifferFromEnglish()
    {
        Assert.AreEqual(
            "取消",
            LegacyTextResourceReader.GetString("zh-Hans", LegacyTextResourceGroup.CommonResources, "Cancel"));
    }

    [TestMethod]
    public void LegacyTextResources_ReadsChineseTraditionalValuesThatDifferFromEnglish()
    {
        var english = LegacyTextResourceReader.GetString("en", LegacyTextResourceGroup.DialogResources, "AsKToSaveContent");
        var traditionalChinese = LegacyTextResourceReader.GetString("zh-Hant", LegacyTextResourceGroup.DialogResources, "AsKToSaveContent");

        Assert.AreEqual("您有未儲存的變更", traditionalChinese);
        Assert.AreNotEqual(english, traditionalChinese);
    }

    [TestMethod]
    public void LegacyTextResources_AllSupportedCulturesAndGroupsHaveExpectedKeyCountsAndExcludeTemplateKeys()
    {
        foreach (var culture in Cultures)
        {
            foreach (var group in Groups)
            {
                var resources = LegacyTextResourceReader.GetGroup(culture, group);

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
    public void LegacyTextResources_LocalizedGroupsHaveSameKeyShapeAsEnglish()
    {
        foreach (var group in Groups)
        {
            var englishKeys = LegacyTextResourceReader.GetGroup("en", group).Keys.ToArray();

            foreach (var culture in Cultures.Where(culture => culture != "en"))
            {
                var localizedKeys = LegacyTextResourceReader.GetGroup(culture, group).Keys.ToArray();

                CollectionAssert.AreEquivalent(englishKeys, localizedKeys, $"{culture}/{group} key shape differs from en.");
            }
        }
    }

    [TestMethod]
    public void LegacyTextResources_FallsBackToEnglishForUnknownCulture()
    {
        Assert.AreEqual(
            "Typedown",
            LegacyTextResourceReader.GetString("fr-FR", LegacyTextResourceGroup.Resources, "AppName"));
    }

    [TestMethod]
    public void LegacyTextResources_ReturnsNullForUnknownKey()
    {
        Assert.IsNull(
            LegacyTextResourceReader.GetString("en", LegacyTextResourceGroup.Resources, "Missing.Key"));
    }

    [TestMethod]
    public void LegacyTextResources_FallsBackToEnglishForMissingLocalizedKey()
    {
        var catalog = new LegacyTextResourceCatalog(new Dictionary<(string Culture, LegacyTextResourceGroup Group), IReadOnlyDictionary<string, string>>
        {
            [("en", LegacyTextResourceGroup.CommonResources)] = new Dictionary<string, string>
            {
                ["Cancel"] = "Cancel",
                ["Ok"] = "OK"
            },
            [("zh-Hans", LegacyTextResourceGroup.CommonResources)] = new Dictionary<string, string>
            {
                ["Cancel"] = "取消"
            }
        });

        Assert.AreEqual("取消", catalog.GetString("zh-Hans", LegacyTextResourceGroup.CommonResources, "Cancel"));
        Assert.AreEqual("OK", catalog.GetString("zh-Hans", LegacyTextResourceGroup.CommonResources, "Ok"));
    }

    private static int GetExpectedKeyCount(LegacyTextResourceGroup group)
    {
        return group switch
        {
            LegacyTextResourceGroup.CommonResources => 204,
            LegacyTextResourceGroup.DialogResources => 33,
            LegacyTextResourceGroup.Resources => 1,
            LegacyTextResourceGroup.SettingsResources => 141,
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
        };
    }
}
