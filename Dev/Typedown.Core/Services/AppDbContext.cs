using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;

namespace Typedown.Core.Services
{
    public class AppDbContext : DbContext
    {
        public DbSet<ExportConfig> ExportConfigs { get; set; }

        public DbSet<FileAccessHistory> FileAccessHistories { get; set; }

        public DbSet<FolderAccessHistory> FolderAccessHistories { get; set; }

        public DbSet<ImageUploadConfig> ImageUploadConfigs { get; set; }

        private readonly string dbPath;

        private readonly string migrateTaskKey;

        private const string InitialMigrationId = "20230226122314_InitialCreate";

        private static readonly string[] InitialTables = ["ExportConfig", "FileAccessHistory", "FolderAccessHistory", "ImageUploadConfig"];

        private static readonly object lockMigrateTask = new();

        private static readonly Dictionary<string, Task> migrateTasks = new(StringComparer.OrdinalIgnoreCase);

        [RequiresDynamicCode("EF Core database access is not fully compatible with NativeAOT.")]
        [RequiresUnreferencedCode("EF Core database access is not fully compatible with trimming.")]
        public AppDbContext()
            : this(null)
        {
        }

        [RequiresDynamicCode("EF Core database access is not fully compatible with NativeAOT.")]
        [RequiresUnreferencedCode("EF Core database access is not fully compatible with trimming.")]
        public AppDbContext(IAppDataPathProvider? appDataPathProvider)
        {
            dbPath = (appDataPathProvider ?? Config.GetAppDataPathProvider()).GetDatabaseFilePath();
            migrateTaskKey = Path.GetFullPath(dbPath);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var builder = new SqliteConnectionStringBuilder() { DataSource = dbPath };
            options
                .UseSqlite(builder.ConnectionString)
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        [RequiresDynamicCode("EF Core migrations are not supported with NativeAOT.")]
        [RequiresUnreferencedCode("EF Core migrations are not fully compatible with trimming.")]
        public async Task EnsureMigrateAsync()
        {
            Task? migrateTask;
            lock (lockMigrateTask)
            {
                if (!migrateTasks.TryGetValue(migrateTaskKey, out migrateTask) || migrateTask.IsFaulted || migrateTask.IsCanceled)
                {
                    migrateTask = EnsureMigrateCoreAsync();
                    migrateTasks[migrateTaskKey] = migrateTask;
                }
            }

            await migrateTask;
        }

        [RequiresDynamicCode("EF Core migrations are not supported with NativeAOT.")]
        [RequiresUnreferencedCode("EF Core migrations are not fully compatible with trimming.")]
        private async Task EnsureMigrateCoreAsync()
        {
            EnsureDatabaseDirectory();
            MigrateLegacyDatabaseFileIfNeeded();

            if (await IsInitialMigrationAppliedAsync())
                return;

            await BootstrapLegacyMigrationHistoryAsync();

            if (await IsInitialMigrationAppliedAsync())
                return;

            await Database.MigrateAsync();
        }

        private void EnsureDatabaseDirectory()
        {
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
        }

        private void MigrateLegacyDatabaseFileIfNeeded()
        {
            if (File.Exists(dbPath))
                return;

            var legacyDbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Config.AppName,
                "Storage.db");

            if (!File.Exists(legacyDbPath))
                return;

            File.Copy(legacyDbPath, dbPath);
        }

        private async Task BootstrapLegacyMigrationHistoryAsync()
        {
            if (!CanProbeLegacySqliteMetadata())
                return;

            if (!File.Exists(dbPath))
                return;

            try
            {
                var builder = new SqliteConnectionStringBuilder() { DataSource = dbPath };
                await using var connection = new SqliteConnection(builder.ConnectionString);
                await connection.OpenAsync();

                foreach (var tableName in InitialTables)
                {
                    if (!await TableExistsAsync(connection, tableName))
                        return;
                }

                await using var createHistory = connection.CreateCommand();
                createHistory.CommandText =
                    "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (" +
                    "\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, " +
                    "\"ProductVersion\" TEXT NOT NULL);";
                await createHistory.ExecuteNonQueryAsync();

                await using var insertInitialMigration = connection.CreateCommand();
                insertInitialMigration.CommandText =
                    "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") " +
                    $"VALUES ('{InitialMigrationId}', '3.1.30');";
                await insertInitialMigration.ExecuteNonQueryAsync();
            }
            catch (Exception ex) when (ex is InvalidOperationException or TargetInvocationException)
            {
                // Microsoft.Data.Sqlite internally probes ApplicationData.Current via reflection
                // to resolve the native SQLite library. In unpackaged WinUI 3 apps this throws
                // InvalidOperationException (HRESULT 0x80073D54: APPMODEL_ERROR_NO_PACKAGE).
                // When the probe is wrapped in reflection, the inner exception surfaces as
                // TargetInvocationException. In either case the legacy bootstrap must be skipped.
            }
        }

        private async Task<bool> IsInitialMigrationAppliedAsync()
        {
            if (!CanProbeLegacySqliteMetadata())
                return false;

            if (!File.Exists(dbPath))
                return false;

            try
            {
                var builder = new SqliteConnectionStringBuilder() { DataSource = dbPath };
                await using var connection = new SqliteConnection(builder.ConnectionString);
                await connection.OpenAsync();

                if (!await TableExistsAsync(connection, "__EFMigrationsHistory"))
                    return false;

                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = $migrationId;";
                command.Parameters.AddWithValue("$migrationId", InitialMigrationId);

                var result = await command.ExecuteScalarAsync();
                return result is long count && count > 0;
            }
            catch (Exception ex) when (ex is InvalidOperationException or TargetInvocationException or SqliteException)
            {
                return false;
            }
        }

        private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $tableName;";
            command.Parameters.AddWithValue("$tableName", tableName);

            var result = await command.ExecuteScalarAsync();
            return result is long count && count > 0;
        }

        private static bool CanProbeLegacySqliteMetadata()
        {
            if (!OperatingSystem.IsWindows())
                return true;

            var length = 0;
            return GetCurrentPackageFullName(ref length, IntPtr.Zero) == ErrorInsufficientBuffer;
        }

        [RequiresDynamicCode("EF Core database access is not fully compatible with NativeAOT.")]
        [RequiresUnreferencedCode("EF Core database access is not fully compatible with trimming.")]
        public static Task<AppDbContext> Create(IAppDataPathProvider? appDataPathProvider = null)
        {
            return Task.Run(async () =>
            {
                var ctx = new AppDbContext(appDataPathProvider);
                await ctx.EnsureMigrateAsync();
                return ctx;
            });
        }

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, IntPtr packageFullName);

        private const int ErrorInsufficientBuffer = 122;
    }
}
