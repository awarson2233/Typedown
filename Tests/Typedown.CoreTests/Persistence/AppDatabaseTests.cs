using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using System.Text.RegularExpressions;
using Typedown.Core.Enums;
using Typedown.Core.Models;
using Typedown.Core.Models.ExportConfigModels;
using Typedown.Core.Models.UploadConfigModels;
using Typedown.Core.Services;
using Typedown.Core.Utilities;

namespace Typedown.CoreTests.Persistence;

[TestClass]
public sealed class AppDatabaseTests
{
    // Config column values exactly as the EF Core + Newtonsoft build wrote them.
    private const string PdfConfigJson = """
        {
          "PDF": {
            "ExtraHead": "<style>.markdown-body { min-width: unset; max-width: unset; margin: unset; padding: 0; }</style>",
            "Orientation": 1,
            "PageSize": {
              "Width": {
                "Unit": {
                  "Name": "in",
                  "Scale": 0.3937007874,
                  "Shift": 0.0
                },
                "Value": 8.5
              },
              "Height": {
                "Unit": {
                  "Name": "cm",
                  "Scale": 1.0,
                  "Shift": 0.0
                },
                "Value": 29.7
              }
            },
            "Margins": {
              "Left": { "Unit": { "Name": "cm", "Scale": 1.0, "Shift": 0.0 }, "Value": 3.18 },
              "Top": { "Unit": { "Name": "cm", "Scale": 1.0, "Shift": 0.0 }, "Value": 2.54 },
              "Right": { "Unit": { "Name": "cm", "Scale": 1.0, "Shift": 0.0 }, "Value": 3.18 },
              "Bottom": { "Unit": { "Name": "cm", "Scale": 1.0, "Shift": 0.0 }, "Value": 2.54 }
            },
            "ShouldPrintHeaderAndFooter": false,
            "Header": "<h>",
            "Footer": "",
            "Author": "我",
            "Addition": {},
            "ScriptAfter": ""
          }
        }
        """;

    private const string PowerShellConfigJson = """
        {
          "PowerShell": {
            "Script": "a\nb",
            "Addition": {},
            "UploadPath": "p",
            "ExternalURL": ""
          }
        }
        """;

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
    public async Task EfMigratedDatabase_OpensWithAllDataAndUnchangedSchema()
    {
        var path = CreateDatabase("ef.db", ReadFixture("ef-initial-create.sql"));
        SeedLegacyRows(path);
        var schemaBefore = DumpSchema(path);

        var database = new AppDatabase(path);
        await AssertSeededDataAsync(database);

        Assert.AreEqual(1, ReadUserVersion(path));
        CollectionAssert.AreEqual(schemaBefore, DumpSchema(path), "Opening must not alter an existing schema.");
        Assert.AreEqual("9.0.8", ReadScalar(path, "SELECT \"ProductVersion\" FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20230226122314_InitialCreate';"));
    }

    [TestMethod]
    public async Task LegacyNotNullDatabaseWithBootstrappedHistory_OpensWithAllData()
    {
        var path = CreateDatabase("legacy.db", ReadFixture("legacy-schema.sql") + """
            CREATE TABLE "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            INSERT INTO "__EFMigrationsHistory" VALUES ('20230226122314_InitialCreate', '3.1.30');
            """);
        SeedLegacyRows(path);
        var schemaBefore = DumpSchema(path);

        var database = new AppDatabase(path);
        await AssertSeededDataAsync(database);

        Assert.AreEqual(1, ReadUserVersion(path));
        CollectionAssert.AreEqual(schemaBefore, DumpSchema(path));
    }

    [TestMethod]
    public async Task LegacyDatabaseWithoutMigrationHistory_IsTreatedAsVersion1()
    {
        var path = CreateDatabase("legacy-nohistory.db", ReadFixture("legacy-schema.sql"));
        SeedLegacyRows(path);

        var database = new AppDatabase(path);
        await AssertSeededDataAsync(database);

        Assert.AreEqual(1, ReadUserVersion(path));
        Assert.AreEqual(4L, ReadScalar(path, "SELECT COUNT(*) FROM \"FileAccessHistory\";"));
    }

    [TestMethod]
    public async Task FreshDatabase_HasSameSchemaAsEfMigratedDatabase()
    {
        var efPath = CreateDatabase("ef.db", ReadFixture("ef-initial-create.sql"));
        var freshPath = Path.Combine(tempDirectory, "nested", "fresh.db");

        await new AppDatabase(freshPath).EnsureMigratedAsync();

        // __EFMigrationsLock is EF's own lock table, created on demand by EF itself.
        var expected = DumpSchema(efPath).Where(x => !x.Contains("__EFMigrationsLock")).ToList();
        CollectionAssert.AreEqual(expected, DumpSchema(freshPath));
        foreach (var table in AppDatabaseSchema.Tables)
            CollectionAssert.AreEqual(DumpTableInfo(efPath, table), DumpTableInfo(freshPath, table), table);

        Assert.AreEqual(1, ReadUserVersion(freshPath));
        Assert.AreEqual("wal", ReadScalar(freshPath, "PRAGMA journal_mode;"));
        Assert.AreEqual("9.0.8", ReadScalar(freshPath, "SELECT \"ProductVersion\" FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20230226122314_InitialCreate';"));
    }

    [TestMethod]
    public async Task FreshDatabase_SupportsAllOperationsInTheLegacyTextFormats()
    {
        var path = Path.Combine(tempDirectory, "fresh.db");
        var database = new AppDatabase(path);

        await database.AddFileAccessHistoryAsync(@"C:\a.md", new DateTime(2024, 5, 6, 7, 8, 9, 120));
        await database.AddFileAccessHistoryAsync(@"C:\b.md", new DateTime(2024, 5, 6, 7, 8, 10));
        await database.AddFileAccessHistoryAsync(@"C:\a.md", new DateTime(2024, 5, 6, 7, 8, 11).AddTicks(1));
        CollectionAssert.AreEqual(new[] { @"C:\a.md", @"C:\b.md", @"C:\a.md" }, await database.GetRecentFilePathsAsync(10));
        CollectionAssert.AreEqual(new[] { @"C:\a.md" }, await database.GetRecentFilePathsAsync(1));
        Assert.AreEqual("2024-05-06 07:08:09.12", ReadScalar(path, "SELECT \"AccessTime\" FROM \"FileAccessHistory\" WHERE \"Id\" = 1;"));
        Assert.AreEqual("2024-05-06 07:08:10", ReadScalar(path, "SELECT \"AccessTime\" FROM \"FileAccessHistory\" WHERE \"Id\" = 2;"));
        Assert.AreEqual("2024-05-06 07:08:11.0000001", ReadScalar(path, "SELECT \"AccessTime\" FROM \"FileAccessHistory\" WHERE \"Id\" = 3;"));

        await database.RemoveFileAccessHistoryAsync(@"C:\a.md");
        CollectionAssert.AreEqual(new[] { @"C:\b.md" }, await database.GetRecentFilePathsAsync(10));
        await database.ClearFileAccessHistoryAsync();
        Assert.AreEqual(0, (await database.GetRecentFilePathsAsync(10)).Length);

        await database.AddFolderAccessHistoryAsync(@"C:\docs", DateTime.Now);
        CollectionAssert.AreEqual(new[] { @"C:\docs" }, await database.GetRecentFolderPathsAsync(10));
        await database.RemoveFolderAccessHistoryAsync(@"C:\docs");
        Assert.AreEqual(0, (await database.GetRecentFolderPathsAsync(10)).Length);

        Assert.IsTrue(await database.EnsureExportConfigAsync("PDF", ExportType.PDF));
        Assert.IsFalse(await database.EnsureExportConfigAsync("PDF", ExportType.PDF));
        var html = new ExportConfig { Name = "HTML", Type = ExportType.HTML };
        await database.AddExportConfigAsync(html);
        Assert.AreEqual(2, html.Id);
        Assert.AreEqual("{}", ReadScalar(path, "SELECT \"Config\" FROM \"ExportConfig\" WHERE \"Id\" = 2;"));

        html.Notes = "notes";
        html.StoreExportConfig(new HTMLConfigModel { ExtraHead = "<meta>" });
        Assert.IsTrue(await database.UpdateExportConfigAsync(html));
        var reloaded = await database.GetExportConfigAsync(2);
        Assert.IsNotNull(reloaded);
        Assert.AreEqual("notes", reloaded.Notes);
        Assert.AreEqual("<meta>", ((HTMLConfigModel)reloaded.LoadExportConfig()).ExtraHead);
        Assert.IsFalse(await database.UpdateExportConfigAsync(new ExportConfig { Id = 99, Type = ExportType.PDF }));

        await database.RemoveExportConfigAsync(1);
        Assert.AreEqual(1, (await database.GetExportConfigsAsync()).Count);
        Assert.IsNull(await database.GetExportConfigAsync(1));

        var upload = new ImageUploadConfig { Name = "PS", Method = ImageUploadMethod.PowerShell, IsEnable = true };
        await database.AddImageUploadConfigAsync(upload);
        upload.StoreUploadConfig(new PowerShellModel { Script = "return 1" });
        Assert.IsTrue(await database.UpdateImageUploadConfigAsync(upload));
        var uploads = await database.GetImageUploadConfigsAsync();
        Assert.AreEqual(1, uploads.Count);
        Assert.IsTrue(uploads[0].IsEnable);
        Assert.AreEqual("return 1", ((PowerShellModel)uploads[0].LoadUploadConfig()).Script);
        await database.RemoveImageUploadConfigAsync(upload.Id);
        await database.AddImageUploadConfigAsync(new ImageUploadConfig { Method = ImageUploadMethod.FTP });
        await database.RemoveAllImageUploadConfigsAsync();
        Assert.AreEqual(0, (await database.GetImageUploadConfigsAsync()).Count);
    }

    [TestMethod]
    public async Task LegacyNotNullDatabase_AcceptsWritesFromNewCode()
    {
        var path = CreateDatabase("legacy.db", ReadFixture("legacy-schema.sql"));
        var database = new AppDatabase(path);

        var config = new ExportConfig { Type = ExportType.PDF };
        await database.AddExportConfigAsync(config);
        await database.AddImageUploadConfigAsync(new ImageUploadConfig { Method = ImageUploadMethod.Git });
        await database.AddFileAccessHistoryAsync(@"C:\x.md", DateTime.Now);
        await database.AddFolderAccessHistoryAsync(@"C:\x", DateTime.Now);

        Assert.AreEqual(string.Empty, ReadScalar(path, "SELECT \"Name\" FROM \"ExportConfig\";"));
        Assert.AreEqual(1L, ReadScalar(path, "SELECT COUNT(*) FROM \"ImageUploadConfig\";"));
        Assert.IsTrue(Regex.IsMatch((string)ReadScalar(path, "SELECT \"AccessTime\" FROM \"FolderAccessHistory\";")!, @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(\.\d{1,7})?$"));
    }

    [TestMethod]
    public void NullStringColumns_ReadAsEmpty()
    {
        var path = CreateDatabase("ef.db", ReadFixture("ef-initial-create.sql") + """
            INSERT INTO "ExportConfig" ("Name", "Notes", "Type", "Config") VALUES (NULL, NULL, 2, NULL);
            INSERT INTO "FileAccessHistory" ("AccessTime", "FilePath") VALUES ('2024-01-01 00:00:00', NULL);
            """);
        var database = new AppDatabase(path);

        var config = database.GetExportConfigsAsync().GetAwaiter().GetResult().Single();
        Assert.AreEqual(string.Empty, config.Name);
        Assert.AreEqual(string.Empty, config.Notes);
        Assert.IsInstanceOfType(config.LoadExportConfig(), typeof(HTMLConfigModel));
        Assert.AreEqual(0, database.GetRecentFilePathsAsync(10).GetAwaiter().GetResult().Length);
    }

    [TestMethod]
    public async Task NewerUserVersion_IsLeftUntouched()
    {
        var path = CreateDatabase("future.db", ReadFixture("ef-initial-create.sql") + "PRAGMA user_version = 7;");
        await new AppDatabase(path).EnsureMigratedAsync();
        Assert.AreEqual(7, ReadUserVersion(path));
    }

    [TestMethod]
    public async Task LegacyDocumentsDatabase_IsCopiedWithWriteAheadLog()
    {
        var legacyPath = CreateDatabase("Storage.db", ReadFixture("ef-initial-create.sql"));
        using (var connection = OpenRaw(legacyPath))
        {
            Execute(connection, "PRAGMA journal_mode = 'wal'; PRAGMA wal_autocheckpoint = 0;");
            Execute(connection, "INSERT INTO \"FileAccessHistory\" (\"AccessTime\", \"FilePath\") VALUES ('2024-01-01 00:00:00', 'C:\\wal-only.md');");
            File.Copy(legacyPath, legacyPath + ".snapshot");
            File.Copy(legacyPath + "-wal", legacyPath + ".snapshot-wal");
        }

        var target = Path.Combine(tempDirectory, "target", "typedown.db");
        var database = new AppDatabase(target, legacyPath + ".snapshot");
        CollectionAssert.AreEqual(new[] { @"C:\wal-only.md" }, await database.GetRecentFilePathsAsync(10));
    }

    private static async Task AssertSeededDataAsync(AppDatabase database)
    {
        var exportConfigs = await database.GetExportConfigsAsync();
        Assert.AreEqual(2, exportConfigs.Count);
        Assert.AreEqual(1, exportConfigs[0].Id);
        Assert.AreEqual("PDF", exportConfigs[0].Name);
        Assert.AreEqual("n", exportConfigs[0].Notes);
        Assert.AreEqual(ExportType.PDF, exportConfigs[0].Type);
        Assert.AreEqual(PdfConfigJson, exportConfigs[0].Config);
        var pdf = (PDFConfigModel)exportConfigs[0].LoadExportConfig();
        Assert.AreEqual(PrintOrientation.Landscape, pdf.Orientation);
        Assert.AreEqual("我", pdf.Author);
        Assert.AreEqual("<h>", pdf.Header);
        Assert.AreEqual(Units.Inch, pdf.PageSize.Width.Unit);
        Assert.AreEqual(8.5, pdf.PageSize.Width.Value);
        Assert.AreEqual(Units.Centimeter, pdf.Margins.Top.Unit);
        Assert.AreEqual(2.54, pdf.Margins.Top.Value);
        Assert.AreEqual(ExportType.HTML, exportConfigs[1].Type);
        Assert.IsInstanceOfType(exportConfigs[1].LoadExportConfig(), typeof(HTMLConfigModel));

        var single = await database.GetExportConfigAsync(1);
        Assert.AreEqual(PdfConfigJson, single?.Config);

        var uploads = await database.GetImageUploadConfigsAsync();
        Assert.AreEqual(1, uploads.Count);
        Assert.AreEqual(7, uploads[0].Id);
        Assert.IsTrue(uploads[0].IsEnable);
        Assert.AreEqual(ImageUploadMethod.PowerShell, uploads[0].Method);
        var powerShell = (PowerShellModel)uploads[0].LoadUploadConfig();
        Assert.AreEqual("a\nb", powerShell.Script);
        Assert.AreEqual("p", powerShell.UploadPath);

        CollectionAssert.AreEqual(
            new[] { @"C:\docs\a.md", @"C:\docs\b.md", @"C:\docs\a.md", @"C:\docs\old.md" },
            await database.GetRecentFilePathsAsync(10));
        CollectionAssert.AreEqual(new[] { @"D:\notes", @"C:\docs" }, await database.GetRecentFolderPathsAsync(10));
    }

    private static void SeedLegacyRows(string path)
    {
        using var connection = OpenRaw(path);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "ExportConfig" ("Id", "Name", "Notes", "Type", "Config") VALUES (1, 'PDF', 'n', 1, $pdf);
            INSERT INTO "ExportConfig" ("Id", "Name", "Notes", "Type", "Config") VALUES (2, 'HTML', '', 2, '{}');
            INSERT INTO "ImageUploadConfig" ("Id", "Name", "Notes", "IsEnable", "Method", "Config") VALUES (7, 'PS', '', 1, 1024, $ps);
            INSERT INTO "FileAccessHistory" ("AccessTime", "FilePath") VALUES ('2024-01-02 03:04:05.6780009', 'C:\docs\a.md');
            INSERT INTO "FileAccessHistory" ("AccessTime", "FilePath") VALUES ('2024-01-02 03:04:06', 'C:\docs\b.md');
            INSERT INTO "FileAccessHistory" ("AccessTime", "FilePath") VALUES ('2023-12-31 23:59:59.1', 'C:\docs\old.md');
            INSERT INTO "FileAccessHistory" ("AccessTime", "FilePath") VALUES ('2024-01-02 03:04:06.5', 'C:\docs\a.md');
            INSERT INTO "FolderAccessHistory" ("AccessTime", "FolderPath") VALUES ('2024-01-02 03:04:07.1', 'C:\docs');
            INSERT INTO "FolderAccessHistory" ("AccessTime", "FolderPath") VALUES ('2024-02-01 00:00:00', 'D:\notes');
            """;
        command.Parameters.AddWithValue("$pdf", PdfConfigJson);
        command.Parameters.AddWithValue("$ps", PowerShellConfigJson);
        command.ExecuteNonQuery();
    }

    private string CreateDatabase(string fileName, string sql)
    {
        var path = Path.Combine(tempDirectory, fileName);
        using var connection = OpenRaw(path);
        Execute(connection, sql);
        return path;
    }

    private static string ReadFixture(string name)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Persistence", "Fixtures", name));
    }

    private static SqliteConnection OpenRaw(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ConnectionString);
        connection.Open();
        return connection;
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static object? ReadScalar(string path, string sql)
    {
        using var connection = OpenRaw(path);
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    private static int ReadUserVersion(string path) => Convert.ToInt32(ReadScalar(path, "PRAGMA user_version;"));

    /// <summary>sqlite_master rows with whitespace-normalized SQL, excluding SQLite's internal sqlite_sequence.</summary>
    private static List<string> DumpSchema(string path)
    {
        using var connection = OpenRaw(path);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT type, name, tbl_name, sql FROM sqlite_master WHERE name <> 'sqlite_sequence' ORDER BY name;";
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
        {
            var sql = reader.IsDBNull(3) ? "<null>" : Regex.Replace(reader.GetString(3), @"\s+", " ");
            result.Add($"{reader.GetString(0)}|{reader.GetString(1)}|{reader.GetString(2)}|{sql}");
        }
        return result;
    }

    private static List<string> DumpTableInfo(string path, string table)
    {
        using var connection = OpenRaw(path);
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{table}\");";
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
            result.Add(string.Join("|", System.Linq.Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "<null>" : Convert.ToString(reader.GetValue(i)))));
        return result;
    }
}
