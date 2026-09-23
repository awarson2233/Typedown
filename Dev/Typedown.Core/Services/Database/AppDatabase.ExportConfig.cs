using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;
using Typedown.Core.Enums;
using Typedown.Core.Models;

namespace Typedown.Core.Services
{
    public sealed partial class AppDatabase
    {
        private const string ExportConfigColumns = "\"Id\", \"Name\", \"Notes\", \"Type\", \"Config\"";

        public Task<List<ExportConfig>> GetExportConfigsAsync()
        {
            return RunAsync(connection =>
            {
                using var command = CreateCommand(connection, $"SELECT {ExportConfigColumns} FROM \"ExportConfig\" ORDER BY \"Id\";");
                using var reader = command.ExecuteReader();
                var result = new List<ExportConfig>();
                while (reader.Read())
                    result.Add(ReadExportConfig(reader));
                return result;
            });
        }

        public Task<ExportConfig?> GetExportConfigAsync(int id)
        {
            return RunAsync(connection =>
            {
                using var command = CreateCommand(connection, $"SELECT {ExportConfigColumns} FROM \"ExportConfig\" WHERE \"Id\" = $id;");
                command.Parameters.AddWithValue("$id", id);
                using var reader = command.ExecuteReader();
                return reader.Read() ? ReadExportConfig(reader) : null;
            });
        }

        /// <summary>Inserts <paramref name="config"/> and assigns its generated <see cref="ExportConfig.Id"/>.</summary>
        public Task AddExportConfigAsync(ExportConfig config)
        {
            return RunAsync(connection => InsertExportConfig(connection, null, config));
        }

        /// <summary>Returns false when no row with the config's id exists.</summary>
        public Task<bool> UpdateExportConfigAsync(ExportConfig config)
        {
            return RunAsync(connection =>
            {
                using var command = CreateCommand(connection,
                    "UPDATE \"ExportConfig\" SET \"Name\" = $name, \"Notes\" = $notes, \"Type\" = $type, \"Config\" = $config WHERE \"Id\" = $id;");
                BindExportConfig(command, config);
                command.Parameters.AddWithValue("$id", config.Id);
                return command.ExecuteNonQuery() > 0;
            });
        }

        public Task RemoveExportConfigAsync(int id)
        {
            return RunAsync(connection =>
            {
                using var command = CreateCommand(connection, "DELETE FROM \"ExportConfig\" WHERE \"Id\" = $id;");
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            });
        }

        /// <summary>Adds a config with the given name and type unless one already exists. Returns whether it was added.</summary>
        public Task<bool> EnsureExportConfigAsync(string name, ExportType type)
        {
            return RunAsync(connection =>
            {
                using var transaction = connection.BeginTransaction(deferred: false);
                using var exists = CreateCommand(connection,
                    "SELECT EXISTS (SELECT 1 FROM \"ExportConfig\" WHERE \"Type\" = $type AND \"Name\" = $name);", transaction);
                exists.Parameters.AddWithValue("$type", (int)type);
                exists.Parameters.AddWithValue("$name", name);
                if (System.Convert.ToInt64(exists.ExecuteScalar()) != 0)
                    return false;

                InsertExportConfig(connection, transaction, new ExportConfig { Name = name, Type = type });
                transaction.Commit();
                return true;
            });
        }

        private static void InsertExportConfig(SqliteConnection connection, SqliteTransaction? transaction, ExportConfig config)
        {
            using var command = CreateCommand(connection,
                "INSERT INTO \"ExportConfig\" (\"Name\", \"Notes\", \"Type\", \"Config\") VALUES ($name, $notes, $type, $config);", transaction);
            BindExportConfig(command, config);
            command.ExecuteNonQuery();
            config.Id = (int)LastInsertRowId(connection, transaction);
        }

        private static void BindExportConfig(SqliteCommand command, ExportConfig config)
        {
            command.Parameters.AddWithValue("$name", config.Name ?? string.Empty);
            command.Parameters.AddWithValue("$notes", config.Notes ?? string.Empty);
            command.Parameters.AddWithValue("$type", (int)config.Type);
            command.Parameters.AddWithValue("$config", config.Config ?? string.Empty);
        }

        private static ExportConfig ReadExportConfig(SqliteDataReader reader)
        {
            return new ExportConfig
            {
                Id = reader.GetInt32(0),
                Name = GetStringOrEmpty(reader, 1),
                Notes = GetStringOrEmpty(reader, 2),
                Type = (ExportType)reader.GetInt32(3),
                Config = GetStringOrEmpty(reader, 4),
            };
        }
    }
}
