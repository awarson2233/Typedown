using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json.Nodes;
using Typedown.Core;
using Typedown.Core.Interfaces;
using Typedown.Core.Serialization;
using Typedown.Core.ViewModels;

namespace Typedown.CoreTests.Persistence;

/// <summary>
/// settings.json round-trips every setting through System.Text.Json, keeps unknown keys, and falls back to defaults
/// for values it cannot read.
/// </summary>
[TestClass]
public sealed class SettingsJsonCompatibilityTests
{
    private const string SampleSettings = """
        {
          "SidePaneOpen": true,
          "SidePaneWidth": 270.8868520259857,
          "StatusBarOpen": false,
          "FontSize": 11.8,
          "LineHeight": 1.75,
          "TabSize": 2,
          "AppTheme": 2,
          "FileStartupAction": 1,
          "InsertLocalImageAction": 2,
          "Language": "zh-CN",
          "StartupOpenFolder": "D:\\笔记\\2024",
          "LastFilePath": null,
          "InsertWebImageUseUploadConfigId": 5,
          "ShortcutNewFile": { "Modifiers": 5, "Key": 78 },
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
          "UnknownFutureSetting": { "nested": [1, 2.5, "three"] }
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
    public void SampleSettings_AreRead()
    {
        File.WriteAllText(SettingsFile, SampleSettings);
        var settings = CreateSettings();

        Assert.AreEqual(270.8868520259857, settings.SidePaneWidth);
        Assert.IsFalse(settings.StatusBarOpen);
        Assert.AreEqual(2, settings.TabSize);
        Assert.AreEqual(Typedown.Core.Enums.AppTheme.Dark, settings.AppTheme);
        Assert.AreEqual(Typedown.Core.Enums.InsertImageAction.Upload, settings.InsertLocalImageAction);
        Assert.AreEqual("D:\\笔记\\2024", settings.StartupOpenFolder);
        Assert.AreEqual(5, settings.InsertWebImageUseUploadConfigId);
        Assert.AreEqual(1002, settings.StartupPlacement?.rcNormalPosition.left);
    }

    [TestMethod]
    public void WrittenSettings_ReloadUnchanged_AndKeepUnknownKeys()
    {
        File.WriteAllText(SettingsFile, SampleSettings);
        var settings = CreateSettings();

        settings.FontSize = 14;
        settings.TabSize = 8;
        settings.SidePaneOpen = false;
        settings.AppTheme = Typedown.Core.Enums.AppTheme.Light;
        settings.Language = "日本語 \"ja\" <x>";
        settings.LastFilePath = @"C:\notes\新建.md";
        settings.InsertClipboardImageUseUploadConfigId = null;
        settings.ShortcutSave = new(Typedown.Core.Models.KeyboardModifiers.Control | Typedown.Core.Models.KeyboardModifiers.Shift, Typedown.Core.Models.KeyboardKey.S);
        settings.ShortcutOpenFolder = null;
        var placement = settings.StartupPlacement!.Value;
        placement.rcNormalPosition.right = 1900;
        settings.StartupPlacement = placement;

        var written = File.ReadAllText(SettingsFile);
        StringAssert.Contains(written, "新建.md");
        var stored = JsonNode.Parse(written)!.AsObject();
        Assert.IsTrue(JsonNode.DeepEquals(JsonNode.Parse(SampleSettings)!["UnknownFutureSetting"], stored["UnknownFutureSetting"]), "Unknown settings must survive a save.");

        var reloaded = CreateSettings();
        foreach (var property in SettableProperties())
            AssertSameValue(property.GetValue(settings), property.GetValue(reloaded), property.Name);
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
    public void InvalidFilesAndValues_FallBackToDefaults()
    {
        foreach (var content in new[] { "", "   ", "[1,2]", "not json", "{\"FontSize\": " })
        {
            File.WriteAllText(SettingsFile, content);
            Assert.AreEqual(16d, CreateSettings().FontSize, content);
        }

        File.WriteAllText(SettingsFile, "{\"FontSize\": \"large\", \"TabSize\": 8}");
        var settings = CreateSettings();
        Assert.AreEqual(16d, settings.FontSize, "A value of the wrong type falls back to the default.");
        Assert.AreEqual(8, settings.TabSize, "Other values in the same file still load.");
    }

    private static void AssertSameValue(object? expected, object? actual, string name)
    {
        var expectedNode = expected is null ? null : StorageJson.SerializeToNode(expected);
        var actualNode = actual is null ? null : StorageJson.SerializeToNode(actual);
        Assert.IsTrue(JsonNode.DeepEquals(expectedNode, actualNode), $"{name}: expected {expectedNode?.ToJsonString()}, actual {actualNode?.ToJsonString()}");
    }

    private SettingsViewModel CreateSettings()
    {
        var services = new ServiceCollection()
            .AddSingleton<IEditorSettingsNotifier, NullEditorSettingsNotifier>()
            .BuildServiceProvider();
        return new SettingsViewModel(services);
    }

    private static IEnumerable<PropertyInfo> SettableProperties()
    {
        return typeof(SettingsViewModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.GetSetMethod() is not null && x.GetGetMethod() is not null);
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
