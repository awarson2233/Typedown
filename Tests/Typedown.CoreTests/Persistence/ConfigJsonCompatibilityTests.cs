using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using Typedown.Core.Enums;
using Typedown.Core.Models;
using Typedown.Core.Services;
using ExportModels = Typedown.Core.Models.ExportConfigModels;
using UploadModels = Typedown.Core.Models.UploadConfigModels;

namespace Typedown.CoreTests.Persistence;

/// <summary>
/// The Config columns of ExportConfig and ImageUploadConfig hold a JSON object keyed by config kind. Values written
/// by Newtonsoft must load identically, and values written by the new code must load in Newtonsoft.
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
    public async Task NewtonsoftWrittenDefaults_LoadIdentically()
    {
        foreach (var (type, key, modelType) in exportKinds)
        {
            var json = new JObject { [key] = NewtonsoftReference.FromObject(Activator.CreateInstance(modelType)!) }.ToString();
            await AssertExportLoadsLikeNewtonsoft(type, key, modelType, json);
        }

        foreach (var (method, key, modelType) in uploadKinds)
        {
            var json = new JObject { [key] = NewtonsoftReference.FromObject(Activator.CreateInstance(modelType)!) }.ToString();
            await AssertUploadLoadsLikeNewtonsoft(method, key, modelType, json);
        }
    }

    [TestMethod]
    public async Task HandEditedAndPartialConfigs_LoadIdentically()
    {
        await AssertExportLoadsLikeNewtonsoft(ExportType.PDF, "PDF", typeof(ExportModels.PDFConfigModel), """
            {
              "HTML": { "ExtraHead": "kept for the HTML kind" },
              /* comment */
              "PDF": {
                "orientation": "Landscape",
                "PageSize": { "Width": { "Value": "10" }, "Height": { "Unit": { "Name": "in", "Scale": 0.3937007874, "Shift": 0 }, "Value": 11 } },
                "Margins": { "Left": { "Unit": { "Name": "cm", "Scale": 1, "Shift": 0 }, "Value": 1.27 } },
                "ShouldPrintHeaderAndFooter": "true",
                "Header": 123,
                "Footer": null,
                "Addition": { "custom": { "a": [1, true, null] }, "n": 1.50 },
                "ScriptAfter": "ignored: get-only",
                "FutureField": 1,
              }
            }
            """);

        await AssertExportLoadsLikeNewtonsoft(ExportType.Image, "Image", typeof(ExportModels.ImageConfigModel), """{ "Image": { "DPI": "300.5" } }""");

        await AssertUploadLoadsLikeNewtonsoft(ImageUploadMethod.FTP, "FTP", typeof(UploadModels.FTPConfigModel), """
            { "FTP": { "Host": "ftp.example.com", "Port": "2121", "Username": "u", "Password": "p😀", "UploadPath": "/img", "ExternalURL": "https://x/{0}" } }
            """);

        await AssertUploadLoadsLikeNewtonsoft(ImageUploadMethod.SCP, "SCP", typeof(UploadModels.SCPConfigModel), """
            { "SCP": { "host": "h", "port": 2222.0, "PubKeyAuthentication": 1, "IdentityFile": "C:\\keys\\id" }, "Git": { "URL": "kept" } }
            """);

        await AssertUploadLoadsLikeNewtonsoft(ImageUploadMethod.PowerShell, "PowerShell", typeof(UploadModels.PowerShellModel), """
            { "PowerShell": { "Script": "function Upload-Image($FilePath)\r\n{\r\n    return \"<$FilePath>\"\r\n}" } }
            """);
    }

    [TestMethod]
    public async Task MissingOrInvalidConfig_FallsBackToDefaults()
    {
        foreach (var json in new[] { "", "{}", "not json", "[]", """{ "PDF": null }""", """{ "PDF": { "Orientation": {} } }""" })
        {
            var config = await InsertAndLoadExport(ExportType.PDF, json);
            var model = config.LoadExportConfig();
            Assert.IsInstanceOfType(model, typeof(ExportModels.PDFConfigModel), json);
            NewtonsoftReference.AssertSameValue(new ExportModels.PDFConfigModel(), model, json);
        }
    }

    private async Task AssertExportLoadsLikeNewtonsoft(ExportType type, string key, Type modelType, string json)
    {
        var expected = NewtonsoftReference.ToObject(JObject.Parse(json)[key]!, modelType);
        var config = await InsertAndLoadExport(type, json);

        var loaded = config.LoadExportConfig();
        Assert.IsInstanceOfType(loaded, modelType);
        NewtonsoftReference.AssertSameValue(expected, loaded, $"{key} load");

        config.StoreExportConfig(loaded);
        AssertStoredReadableByNewtonsoft(json, config.Config, key, modelType, loaded);
    }

    private async Task AssertUploadLoadsLikeNewtonsoft(ImageUploadMethod method, string key, Type modelType, string json)
    {
        var expected = NewtonsoftReference.ToObject(JObject.Parse(json)[key]!, modelType);
        var config = await InsertAndLoadUpload(method, json);

        var loaded = config.LoadUploadConfig();
        Assert.IsInstanceOfType(loaded, modelType);
        NewtonsoftReference.AssertSameValue(expected, loaded, $"{key} load");

        config.StoreUploadConfig(loaded);
        AssertStoredReadableByNewtonsoft(json, config.Config, key, modelType, loaded);
    }

    private static void AssertStoredReadableByNewtonsoft(string original, string stored, string key, Type modelType, object model)
    {
        var storedObject = JObject.Parse(stored);
        NewtonsoftReference.AssertSameValue(model, NewtonsoftReference.ToObject(storedObject[key]!, modelType), $"{key} write");
        Assert.IsTrue(JToken.DeepEquals(NewtonsoftReference.FromObject(model), storedObject[key]), $"{key}: stored JSON differs from Newtonsoft's serialization.\n{storedObject[key]}");

        foreach (var property in JObject.Parse(original).Properties().Where(x => x.Name != key))
            Assert.IsTrue(JToken.DeepEquals(property.Value, storedObject[property.Name]), $"Entry '{property.Name}' of another kind must be preserved.");
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
