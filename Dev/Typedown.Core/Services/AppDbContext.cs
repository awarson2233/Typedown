using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
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

        private static readonly object lockMigrateTask = new();

        private static readonly Dictionary<string, Task> migrateTasks = new(StringComparer.OrdinalIgnoreCase);

        public AppDbContext()
            : this(null)
        {
        }

        public AppDbContext(IAppDataPathProvider appDataPathProvider)
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

        public async Task EnsureMigrateAsync()
        {
            Task migrateTask;
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

        private async Task EnsureMigrateCoreAsync()
        {
            await BootstrapLegacyMigrationHistoryAsync();
            await Database.MigrateAsync();
        }

        private async Task BootstrapLegacyMigrationHistoryAsync()
        {
            if (!File.Exists(dbPath))
                return;

            var builder = new SqliteConnectionStringBuilder() { DataSource = dbPath };
            await using var connection = new SqliteConnection(builder.ConnectionString);
            await connection.OpenAsync();

            var initialTables = new[] { "ExportConfig", "FileAccessHistory", "FolderAccessHistory", "ImageUploadConfig" };
            foreach (var tableName in initialTables)
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
                "VALUES ('20230226122314_InitialCreate', '3.1.30');";
            await insertInitialMigration.ExecuteNonQueryAsync();
        }

        private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $tableName;";
            command.Parameters.AddWithValue("$tableName", tableName);

            var result = await command.ExecuteScalarAsync();
            return result is long count && count > 0;
        }

        public static Task<AppDbContext> Create(IAppDataPathProvider appDataPathProvider = null)
        {
            return Task.Run(async () =>
            {
                var ctx = new AppDbContext(appDataPathProvider);
                await ctx.EnsureMigrateAsync();
                return ctx;
            });
        }
    }
}
