using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Typedown.Core.Interfaces;

namespace Typedown.Core.Services
{
    /// <summary>
    /// Application database (export configs, image upload configs, file and folder access history) on
    /// Microsoft.Data.Sqlite with hand-written SQL. Every operation runs on the thread pool; the schema is
    /// migrated once per database file and process before the first operation.
    /// </summary>
    public sealed partial class AppDatabase
    {
        // Same text format EF Core used for DateTime columns; lexical order equals chronological order.
        private const string DateTimeFormat = @"yyyy\-MM\-dd HH\:mm\:ss.FFFFFFF";

        private static readonly object migrateLock = new();

        private static readonly HashSet<string> migratedDatabases = new(StringComparer.OrdinalIgnoreCase);

        private readonly string dbPath;

        private readonly string connectionString;

        private readonly string? legacyDbPath;

        /// <summary>
        /// Database at the provider's path. When that file does not exist yet, the database of releases that kept
        /// it in Documents\Typedown\Storage.db is copied over first.
        /// </summary>
        public AppDatabase(IAppDataPathProvider? appDataPathProvider = null)
            : this(
                (appDataPathProvider ?? Config.GetAppDataPathProvider()).GetDatabaseFilePath(),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Config.AppName, "Storage.db"))
        {
        }

        public AppDatabase(string databaseFilePath, string? legacyDatabaseFilePath = null)
        {
            dbPath = Path.GetFullPath(databaseFilePath);
            legacyDbPath = string.IsNullOrEmpty(legacyDatabaseFilePath) ? null : Path.GetFullPath(legacyDatabaseFilePath);
            connectionString = new SqliteConnectionStringBuilder() { DataSource = dbPath }.ConnectionString;
        }

        public string DatabaseFilePath => dbPath;

        public Task EnsureMigratedAsync() => Task.Run(EnsureMigrated);

        private Task RunAsync(Action<SqliteConnection> action)
        {
            return Task.Run(() =>
            {
                using var connection = Open();
                action(connection);
            });
        }

        private Task<T> RunAsync<T>(Func<SqliteConnection, T> func)
        {
            return Task.Run(() =>
            {
                using var connection = Open();
                return func(connection);
            });
        }

        private SqliteConnection Open()
        {
            EnsureMigrated();
            return OpenConnection();
        }

        private SqliteConnection OpenConnection()
        {
            SqliteRuntime.EnsureInitialized();
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            return connection;
        }

        private void EnsureMigrated()
        {
            lock (migrateLock)
            {
                if (migratedDatabases.Contains(dbPath))
                    return;

                EnsureDatabaseDirectory();
                MigrateLegacyDatabaseFileIfNeeded();

                using (var connection = OpenConnection())
                    AppDatabaseSchema.Migrate(connection);

                migratedDatabases.Add(dbPath);
            }
        }

        private void EnsureDatabaseDirectory()
        {
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
        }

        private void MigrateLegacyDatabaseFileIfNeeded()
        {
            if (legacyDbPath is null || File.Exists(dbPath))
                return;

            if (!File.Exists(legacyDbPath) || string.Equals(legacyDbPath, dbPath, StringComparison.OrdinalIgnoreCase))
                return;

            // Copy the write-ahead log too: legacy databases are in WAL mode and recent changes may live only there.
            File.Copy(legacyDbPath, dbPath);
            if (File.Exists(legacyDbPath + "-wal"))
                File.Copy(legacyDbPath + "-wal", dbPath + "-wal", overwrite: true);
        }

        private static SqliteCommand CreateCommand(SqliteConnection connection, string sql, SqliteTransaction? transaction = null)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            return command;
        }

        private static string FormatDateTime(DateTime value) => value.ToString(DateTimeFormat, CultureInfo.InvariantCulture);

        private static string GetStringOrEmpty(SqliteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);

        private static long LastInsertRowId(SqliteConnection connection, SqliteTransaction? transaction)
        {
            using var command = CreateCommand(connection, "SELECT last_insert_rowid();", transaction);
            return Convert.ToInt64(command.ExecuteScalar());
        }
    }
}
