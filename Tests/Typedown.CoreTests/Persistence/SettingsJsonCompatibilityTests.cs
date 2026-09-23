using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Typedown.Core;
using Typedown.Core.Interfaces;
using Typedown.Core.Serialization;
using Typedown.Core.ViewModels;

namespace Typedown.CoreTests.Persistence;

/// <summary>
/// settings.json must read the same under System.Text.Json as under the Newtonsoft implementation it replaced, and
/// what the new code writes must still be readable by Newtonsoft (older Typedown builds).
/// </summary>
[TestClass]
public sealed class SettingsJsonCompatibilityTests
{
    // Synthesized from the shape of real settings files; covers every value kind plus the loose inputs Newtonsoft
    // accepted (numbers as strings and vice versa, enum names, fractional ints, comments, trailing commas).
    private const string SampleSettings = """
        {
          // hand-edited files may contain comments
          "SidePaneOpen": true,
          "SidePaneWidth": 270.8868520259857,
          "StatusBarOpen": "false",
          "FindReplaceDialogWidth": 600,
          "SourceCode": 0,
          "Typewriter": 1,
          "FontSize": 11.8,
          "LineHeight": "1.75",
          "SidePaneIndex": 2,
          "TabSize": "2",
          "WordCountMethod": 1.0,
          "EditorAreaWidth": "900px",
          "AppTheme": 2,
          "FileStartupAction": "OpenLast",
          "FolderStartupAction": "OpenFolder",
          "InsertClipboardImageAction": 1,
          "InsertLocalImageAction": "upload",
          "Language": "zh-CN",
          "SpellcheckLang": 42,
          "StartupOpenFolder": "D:\\笔记\\2024",
          "LastFilePath": null,
          "LastFolderPath": "C:\\Users\\someone\\Documents",
          "InsertClipboardImageUseUploadConfigId": 3,
          "InsertLocalImageUseUploadConfigId": null,
          "InsertWebImageUseUploadConfigId": "5",
          "DefaultImageBasePath": "C:\\Pictures\\\"quoted\" <tag> & 'x'",
          "ShortcutNewFile": { "Modifiers": 5, "Key": 78 },
          "ShortcutSave": { "modifiers": "Control", "key": "S" },
          "ShortcutPrint": { "Modifiers": "Menu, Shift", "Key": 80, "Unknown": true },
          "ShortcutOpenFolder": { "Modifiers": 1, "Key": 79 },
          "ShortcutClearRecentFiles": null,
          "ShortcutUndo": null,
          "StartupPlacement": {
            "length": 44,
            "flags": 2,
            "showCmd": 3,
            "ptMinPosition": { "X": -1, "Y": -1 },
            "ptMaxPosition": { "X": -1, "Y": -1 },
            "rcNormalPosition": { "left": 1002, "top": -1192, "right": 1838, "bottom": -497 },
            "rcDevice": { "left": 0, "top": 0, "right": 0, "bottom": 0 }
          },
          "UnknownFutureSetting": { "nested": [1, 2.50, "three"] },
          "KeepRun": true,
          "UseMicaEffect": false,
        }
        """;

    private IAppDataPathProvider? previousProvider;

    private string tempDirectory = string.Empty;

    private string SettingsFile => Path.Combine(tempDirectory, "settings.json");

    [TestInitialize]
    public void Initialize()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), "TypedownCoreTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        previousProvider = Config.GetAppDataPathProvider();
        Config.SetAppDataPathProvider(new TestPathProvider(tempDirectory));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (previousProvider is not null)
            Config.SetAppDataPathProvider(previousProvider);
        Directory.Delete(tempDirectory, recursive: true);
    }

    [TestMethod]
    public void EverySettingType_IsRegisteredForSourceGeneration()
    {
        foreach (var property in SettableProperties())
            Assert.IsNotNull(StorageJson.Context.GetTypeInfo(property.PropertyType), property.Name);
    }

    [TestMethod]
    public void SampleSettings_ReadIdenticallyToNewtonsoft()
    {
        File.WriteAllText(SettingsFile, SampleSettings);
        var legacy = ParseWithNewtonsoft(SampleSettings);
        var settings = CreateSettings();
        var defaults = CreateSettings(Path.Combine(tempDirectory, "missing.json"));

        // Expected values are read the way the old code did: JToken.ToObject<T>() without a serializer.
        var compared = 0;
        foreach (var property in SettableProperties())
        {
            var token = legacy[property.Name];
            var expected = token is null || token.Type == JTokenType.Null
                ? property.GetValue(defaults)
                : token.ToObject(property.PropertyType);
            NewtonsoftReference.AssertSameValue(expected, property.GetValue(settings), property.Name);
            compared++;
        }

        Assert.IsTrue(compared > 100, "Expected to compare all settings, including shortcuts.");
        Assert.AreEqual(2, settings.TabSize);
        Assert.AreEqual(5, settings.InsertWebImageUseUploadConfigId);
        Assert.AreEqual("42", settings.SpellcheckLang);
        Assert.AreEqual(Typedown.Core.Enums.InsertImageAction.Upload, settings.InsertLocalImageAction);
        Assert.AreEqual(1002, settings.StartupPlacement?.rcNormalPosition.left);
    }

    [TestMethod]
    public void WrittenSettings_AreReadBackByNewtonsoft()
    {
        File.WriteAllText(SettingsFile, SampleSettings);
        var settings = CreateSettings();

        settings.FontSize = 14;
        settings.LineHeight = 1.5;
        settings.TabSize = 8;
        settings.SidePaneOpen = false;
        settings.AppTheme = Typedown.Core.Enums.AppTheme.Light;
        settings.FolderStartupAction = Typedown.Core.Enums.FolderStartupAction.FollowOpenedFileFolder;
        settings.Language = "日本語 \"ja\" <x>";
        settings.LastFilePath = @"C:\notes\新建.md";
        settings.InsertLocalImageUseUploadConfigId = 12;
        settings.InsertClipboardImageUseUploadConfigId = null;
        settings.ShortcutSave = new(Typedown.Core.Models.KeyboardModifiers.Control | Typedown.Core.Models.KeyboardModifiers.Shift, Typedown.Core.Models.KeyboardKey.S);
        settings.ShortcutOpenFolder = null;
        var placement = settings.StartupPlacement!.Value;
        placement.rcNormalPosition.right = 1900;
        settings.StartupPlacement = placement;

        var written = File.ReadAllText(SettingsFile);
        StringAssert.StartsWith(written, "{" + Environment.NewLine + "  \"");
        StringAssert.Contains(written, "\"FontSize\": 14.0");
        StringAssert.Contains(written, "新建.md");

        var legacy = ParseWithNewtonsoft(written);
        foreach (var property in SettableProperties())
        {
            var token = legacy[property.Name];
            if (token is null || token.Type == JTokenType.Null)
                continue;
            NewtonsoftReference.AssertSameValue(property.GetValue(settings), token.ToObject(property.PropertyType), property.Name);
        }

        Assert.AreEqual(JTokenType.Null, legacy["ShortcutOpenFolder"]?.Type);
        Assert.AreEqual(JTokenType.Null, legacy["InsertClipboardImageUseUploadConfigId"]?.Type);
        Assert.IsTrue(JToken.DeepEquals(ParseWithNewtonsoft(SampleSettings)["UnknownFutureSetting"], legacy["UnknownFutureSetting"]), "Unknown settings must survive a save.");
        Assert.AreEqual(1900, legacy["StartupPlacement"]?["rcNormalPosition"]?["right"]?.Value<int>());

        var reloaded = CreateSettings();
        foreach (var property in SettableProperties())
            NewtonsoftReference.AssertSameValue(property.GetValue(settings), property.GetValue(reloaded), property.Name);
    }

    [TestMethod]
    public void SettingEffectiveDefault_DoesNotWriteFile()
    {
        var settings = CreateSettings();
        settings.FontSize = 16;
        settings.AutoSave = false;
        settings.LastFilePath = null;
        Assert.IsFalse(File.Exists(SettingsFile));

        settings.FontSize = 17;
        Assert.IsTrue(File.Exists(SettingsFile));
    }

    [TestMethod]
    public void EveryValueKind_RoundTripsThroughNewtonsoftBothWays()
    {
        var settings = CreateSettings();
        foreach (var property in SettableProperties())
        {
            // Newtonsoft writes the current value; the new code must read it back unchanged.
            var value = property.GetValue(settings);
            var legacyJson = new JObject { [property.Name] = value is null ? JValue.CreateNull() : JToken.FromObject(value) }.ToString();
            File.WriteAllText(SettingsFile, legacyJson);
            NewtonsoftReference.AssertSameValue(value, property.GetValue(CreateSettings()), property.Name);
        }
    }

    [TestMethod]
    public void InvalidOrNonObjectFiles_FallBackToDefaults()
    {
        foreach (var content in new[] { "", "   ", "[1,2]", "not json", "{\"FontSize\": " })
        {
            File.WriteAllText(SettingsFile, content);
            Assert.AreEqual(16d, CreateSettings().FontSize, content);
        }

        File.WriteAllText(SettingsFile, "{\"FontSize\": 12, \"FontSize\": 13}");
        Assert.AreEqual(13d, CreateSettings().FontSize, "Duplicate keys keep the last value, as JObject.Parse does.");
    }

    private SettingsViewModel CreateSettings(string? settingsFile = null)
    {
        if (settingsFile is not null)
            Config.SetAppDataPathProvider(new TestPathProvider(tempDirectory, settingsFile));
        try
        {
            var services = new ServiceCollection()
                .AddSingleton<IEditorSettingsNotifier, NullEditorSettingsNotifier>()
                .BuildServiceProvider();
            return new SettingsViewModel(services);
        }
        finally
        {
            Config.SetAppDataPathProvider(new TestPathProvider(tempDirectory));
        }
    }

    private static IEnumerable<PropertyInfo> SettableProperties()
    {
        return typeof(SettingsViewModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.GetSetMethod() is not null && x.GetGetMethod() is not null);
    }

    private static JObject ParseWithNewtonsoft(string json)
    {
        return JToken.Parse(json) as JObject ?? new JObject();
    }


    private sealed class NullEditorSettingsNotifier : IEditorSettingsNotifier
    {
        public void NotifySettingsChanged(IReadOnlyDictionary<string, object> settings)
        {
        }
    }

    internal sealed class TestPathProvider(string root, string? settingsFile = null) : IAppDataPathProvider
    {
        public string GetLocalFolderPath() => root;

        public string GetSettingsFilePath() => settingsFile ?? Path.Combine(root, "settings.json");

        public string GetDatabaseFilePath() => Path.Combine(root, "typedown.db");

        public string GetBackupFolderPath() => Path.Combine(root, "backup");
    }
}
