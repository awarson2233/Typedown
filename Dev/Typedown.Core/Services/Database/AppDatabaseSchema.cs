using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Typedown.Core.Services
{
    /// <summary>
    /// Schema migrations keyed by <c>PRAGMA user_version</c>. Version 1 is the schema the retired EF Core
    /// migration <c>20230226122314_InitialCreate</c> produced, so databases written by older releases and
    /// databases created by this code are interchangeable in both directions.
    /// </summary>
    public static class AppDatabaseSchema
    {
        public const int CurrentVersion = 1;

        public const string EfMigrationsHistoryTable = "__EFMigrationsHistory";

        public const string EfInitialCreateMigrationId = "20230226122314_InitialCreate";

        // The EF Core version that shipped last; written into the history row for fresh databases so that an
        // older Typedown build opening this file sees the initial migration as applied and does not recreate tables.
        public const string EfProductVersion = "9.0.8";

        public static IReadOnlyList<string> Tables { get; } = ["ExportConfig", "FileAccessHistory", "FolderAccessHistory", "ImageUploadConfig"];

        // EF migration id → schema version it corresponds to.
        private static readonly IReadOnlyDictionary<string, int> efMigrationVersions = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [EfInitialCreateMigrationId] = 1,
        };

        // Index i holds the script that upgrades from version i to i + 1. Scripts are idempotent.
        private static readonly string[] migrations =
        [
            """
            CREATE TABLE IF NOT EXISTS "ExportConfig" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ExportConfig" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NULL,
                "Notes" TEXT NULL,
                "Type" INTEGER NOT NULL,
                "Config" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "FileAccessHistory" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_FileAccessHistory" PRIMARY KEY AUTOINCREMENT,
                "AccessTime" TEXT NOT NULL,
                "FilePath" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "FolderAccessHistory" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_FolderAccessHistory" PRIMARY KEY AUTOINCREMENT,
                "AccessTime" TEXT NOT NULL,
                "FolderPath" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "ImageUploadConfig" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ImageUploadConfig" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NULL,
                "Notes" TEXT NULL,
                "IsEnable" INTEGER NOT NULL,
                "Method" INTEGER NOT NULL,
                "Config" TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );

            INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20230226122314_InitialCreate', '9.0.8');
            """,
        ];

        /// <summary>
        /// Brings the database on <paramref name="connection"/> to <see cref="CurrentVersion"/>.
        /// A database without <c>user_version</c> gets its version inferred from the EF migration history.
        /// </summary>
        public static void Migrate(SqliteConnection connection)
        {
            var created = !HasAnyTable(connection);

            using (var transaction = connection.BeginTransaction(deferred: false))
            {
                var version = GetUserVersion(connection, transaction);
                if (version == 0)
                    version = InferVersionFromLegacySchema(connection, transaction);

                for (var i = version; i < CurrentVersion; i++)
                    Execute(connection, transaction, migrations[i]);

                var target = Math.Max(version, CurrentVersion);
                if (GetUserVersion(connection, transaction) != target)
                    Execute(connection, transaction, $"PRAGMA user_version = {target};");

                transaction.Commit();
            }

            if (created)
            {
                // EF Core switched new databases to WAL; keep that for files created here.
                Execute(connection, null, "PRAGMA journal_mode = 'wal';");
            }
        }

        public static int GetUserVersion(SqliteConnection connection, SqliteTransaction? transaction = null)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "PRAGMA user_version;";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// Version of a database written before <c>user_version</c> was used: the highest EF migration recorded in
        /// <c>__EFMigrationsHistory</c>, or version 1 when all tables exist without history (databases from releases
        /// that predate EF migrations).
        /// </summary>
        public static int InferVersionFromLegacySchema(SqliteConnection connection, SqliteTransaction? transaction = null)
        {
            var version = 0;
            if (TableExists(connection, transaction, EfMigrationsHistoryTable))
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\";";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0) && efMigrationVersions.TryGetValue(reader.GetString(0), out var migrationVersion))
                        version = Math.Max(version, migrationVersion);
                }
            }

            if (version == 0 && Tables.All(x => TableExists(connection, transaction, x)))
                version = 1;

            return version;
        }

        public static bool TableExists(SqliteConnection connection, SqliteTransaction? transaction, string tableName)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
            command.Parameters.AddWithValue("$name", tableName);
            return Convert.ToInt64(command.ExecuteScalar()) > 0;
        }

        private static bool HasAnyTable(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table';";
            return Convert.ToInt64(command.ExecuteScalar()) > 0;
        }

        private static void Execute(SqliteConnection connection, SqliteTransaction? transaction, string sql)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}
