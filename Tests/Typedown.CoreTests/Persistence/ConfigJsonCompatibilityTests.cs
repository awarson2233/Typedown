using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using System.Text.Json.Nodes;
using Typedown.Core.Enums;
using Typedown.Core.Models;
using Typedown.Core.Serialization;
using Typedown.Core.Services;
using ExportModels = Typedown.Core.Models.ExportConfigModels;
using UploadModels = Typedown.Core.Models.UploadConfigModels;

namespace Typedown.CoreTests.Persistence;

/// <summary>
/// The Config columns of ExportConfig and ImageUploadConfig hold a JSON object keyed by config kind. Every model
/// round-trips through System.Text.Json, entries of other kinds survive a store, and unreadable values fall back to
/// the model's defaults.
/// </summary>
[TestClass]
public sealed class ConfigJsonCompatibilityTests
{
    private static readonly (ExportType Type, string Key, Type ModelType)[] exportKinds =
    [
        (ExportType.PDF, "PDF", typeof(ExportModels.PDFConfigModel)),
        (ExportType.HTML, "HTML", typeof(ExportModels.HTMLConfigModel)),
        (ExportType.Image, "Image", typeof(ExportModels.ImageConfigModel)),
    ];

    private static readonly (ImageUploadMethod Method, string Key, Type ModelType)[] uploadKinds =
    [
        (ImageUploadMethod.FTP, "FTP", typeof(UploadModels.FTPConfigModel)),
        (ImageUploadMethod.Git, "Git", typeof(UploadModels.GitConfigModel)),
        (ImageUploadMethod.OSS, "OSS", typeof(UploadModels.OSSConfigModel)),
        (ImageUploadMethod.SCP, "SCP", typeof(UploadModels.SCPConfigModel)),
        (ImageUploadMethod.PowerShell, "PowerShell", typeof(UploadModels.PowerShellModel)),
    ];

    private string tempDirectory = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        SQLitePCL.Batteries_V2.Init();
        tempDirectory = Path.Combine(Path.GetTempPath(), "TypedownCoreTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [TestMethod]
    public async Task DefaultModels_RoundTrip()
    {
        foreach (var (type, key, modelType) in exportKinds)
        {
            var model = Activator.CreateInstance(modelType)!;
            var config = await InsertAndLoadExport(type, Wrap(key, model));
            var loaded = config.LoadExportConfig();
            Assert.IsInstanceOfType(loaded, modelType);
            AssertSameValue(model, loaded, key);
        }

        foreach (var (method, key, modelType) in uploadKinds)
        {
            var model = Activator.CreateInstance(modelType)!;
            var config = await InsertAndLoadUpload(method, Wrap(key, model));
            var loaded = config.LoadUploadConfig();
            Assert.IsInstanceOfType(loaded, modelType);
            AssertSameValue(model, loaded, key);
        }
    }

    [TestMethod]
    public async Task PartialConfig_LoadsWithDefaults_AndStoreKeepsOtherKinds()
    {
        var json = """
            {
              "HTML": { "ExtraHead": "kept for the HTML kind" },
              "PDF": {
                "Orientation": 1,
                "PageSize": { "Width": { "Unit": { "Name": "cm", "Scale": 1, "Shift": 0 }, "Value": 10 } },
                "ShouldPrintHeaderAndFooter": true,
                "Header": "title",
                "FutureField": 1
              }
            }
            """;
        var config = await InsertAndLoadExport(ExportType.PDF, json);

        var pdf = (ExportModels.PDFConfigModel)config.LoadExportConfig();
        Assert.AreEqual(PrintOrientation.Landscape, pdf.Orientation);
        Assert.AreEqual(10, pdf.PageSize.Width.Value);
        Assert.IsTrue(pdf.ShouldPrintHeaderAndFooter);
        Assert.AreEqual("title", pdf.Header);
        AssertSameValue(new ExportModels.PDFConfigModel().Margins, pdf.Margins, "Margins default");

        pdf.Header = "changed";
        config.StoreExportConfig(pdf);
        var stored = JsonNode.Parse(config.Config)!.AsObject();
        Assert.AreEqual("changed", stored["PDF"]?["Header"]?.GetValue<string>());
        Assert.IsTrue(JsonNode.DeepEquals(JsonNode.Parse(json)!["HTML"], stored["HTML"]), "Entries of another kind must be preserved.");
    }

    [TestMethod]
    public async Task MissingOrInvalidConfig_FallsBackToDefaults()
    {
        foreach (var json in new[] { "", "{}", "not json", "[]", """{ "PDF": null }""", """{ "PDF": { "Orientation": {} } }""", """{ "PDF": { "Orientation": "Landscape" } }""" })
        {
            var config = await InsertAndLoadExport(ExportType.PDF, json);
            var model = config.LoadExportConfig();
            Assert.IsInstanceOfType(model, typeof(ExportModels.PDFConfigModel), json);
            AssertSameValue(new ExportModels.PDFConfigModel(), model, json);
        }
    }

    private static string Wrap(string key, object model)
    {
        return new JsonObject { [key] = StorageJson.SerializeToNode(model) }.ToJsonString();
    }

    private static void AssertSameValue(object expected, object actual, string name)
    {
        var expectedNode = StorageJson.SerializeToNode(expected);
        var actualNode = StorageJson.SerializeToNode(actual);
        Assert.IsTrue(JsonNode.DeepEquals(expectedNode, actualNode), $"{name}: expected {expectedNode?.ToJsonString()}, actual {actualNode?.ToJsonString()}");
    }

    private async Task<ExportConfig> InsertAndLoadExport(ExportType type, string json)
    {
        var path = Path.Combine(tempDirectory, Guid.NewGuid().ToString("N") + ".db");
        var database = new AppDatabase(path);
        await database.EnsureMigratedAsync();
        Execute(path, "INSERT INTO \"ExportConfig\" (\"Name\", \"Notes\", \"Type\", \"Config\") VALUES ('x', '', $type, $config);", (int)type, json);
        return (await database.GetExportConfigsAsync()).Single();
    }

    private async Task<ImageUploadConfig> InsertAndLoadUpload(ImageUploadMethod method, string json)
    {
        var path = Path.Combine(tempDirectory, Guid.NewGuid().ToString("N") + ".db");
        var database = new AppDatabase(path);
        await database.EnsureMigratedAsync();
        Execute(path, "INSERT INTO \"ImageUploadConfig\" (\"Name\", \"Notes\", \"IsEnable\", \"Method\", \"Config\") VALUES ('x', '', 1, $type, $config);", (int)method, json);
        return (await database.GetImageUploadConfigsAsync()).Single();
    }

    private static void Execute(string path, string sql, int type, string config)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$type", type);
        command.Parameters.AddWithValue("$config", config);
        command.ExecuteNonQuery();
    }
}
