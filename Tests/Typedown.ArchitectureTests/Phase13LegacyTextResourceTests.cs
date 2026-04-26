using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.UI.Resources;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase13LegacyTextResourceTests
{
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
    public void LegacyTextResources_ReadsChineseValuesThatDifferFromEnglish()
    {
        Assert.AreEqual(
            "取消",
            LegacyTextResourceReader.GetString("zh-Hans", LegacyTextResourceGroup.CommonResources, "Cancel"));
    }

    [TestMethod]
    public void LegacyTextResources_ExcludesReswTemplateKeys()
    {
        var resources = LegacyTextResourceReader.GetGroup("en", LegacyTextResourceGroup.Resources);

        CollectionAssert.DoesNotContain(resources.Keys.ToArray(), "Name1");
        CollectionAssert.DoesNotContain(resources.Keys.ToArray(), "Color1");
        CollectionAssert.DoesNotContain(resources.Keys.ToArray(), "Bitmap1");
        CollectionAssert.DoesNotContain(resources.Keys.ToArray(), "Icon1");
    }

    [TestMethod]
    public void LegacyTextResources_FallsBackToEnglishForUnknownCulture()
    {
        Assert.AreEqual(
            "Typedown",
            LegacyTextResourceReader.GetString("fr-FR", LegacyTextResourceGroup.Resources, "AppName"));
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
}
