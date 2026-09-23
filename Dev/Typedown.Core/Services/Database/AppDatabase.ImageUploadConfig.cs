using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;
using Typedown.Core.Enums;
using Typedown.Core.Models;

namespace Typedown.Core.Services
{
    public sealed partial class AppDatabase
    {
        private const string ImageUploadConfigColumns = "\"Id\", \"Name\", \"Notes\", \"IsEnable\", \"Method\", \"Config\"";

        public Task<List<ImageUploadConfig>> GetImageUploadConfigsAsync()
        {
            return RunAsync(connection =>
            {
                using var command = CreateCommand(connection, $"SELECT {ImageUploadConfigColumns} FROM \"ImageUploadConfig\" ORDER BY \"Id\";");
                using var reader = command.ExecuteReader();
                var result = new List<ImageUploadConfig>();
                while (reader.Read())
                    result.Add(ReadImageUploadConfig(reader));
                return result;
            });
        }

        public Task<ImageUploadConfig?> GetImageUploadConfigAsync(int id)
        {
            return RunAsync(connection =>
            {
                using var command = CreateCommand(connection, $"SELECT {ImageUploadConfigColumns} FROM \"ImageUploadConfig\" WHERE \"Id\" = $id;");
                command.Parameters.AddWithValue("$id", id);
                using var reader = command.ExecuteReader();
                return reader.Read() ? ReadImageUploadConfig(reader) : null;
            });
        }

        /// <summary>Inserts <paramref name="config"/> and assigns its generated <see cref="ImageUploadConfig.Id"/>.</summary>
        public Task AddImageUploadConfigAsync(ImageUploadConfig config)
        {
            return RunAsync(connection =>
            {
                using var command = CreateCommand(connection,
                    "INSERT INTO \"ImageUploadConfig\" (\"Name\", \"Notes\", \"IsEnable\", \"Method\", \"Config\") VALUES ($name, $notes, $isEnable, $method, $config);");
                BindImageUploadConfig(command, config);
                command.ExecuteNonQuery();
                config.Id = (int)LastInsertRowId(connection, null);
            });
        }

        /// <summary>Returns false when no row with the config's id exists.</summary>
        public Task<bool> UpdateImageUploadConfigAsync(ImageUploadConfig config)
        {
            return RunAsync(connection =>
            {
                using var command = CreateCommand(connection,
                    "UPDATE \"ImageUploadConfig\" SET \"Name\" = $name, \"Notes\" = $notes, \"IsEnable\" = $isEnable, \"Method\" = $method, \"Config\" = $config WHERE \"Id\" = $id;");
                BindImageUploadConfig(command, config);
                command.Parameters.AddWithValue("$id", config.Id);
                return command.ExecuteNonQuery() > 0;
            });
        }

        public Task RemoveImageUploadConfigAsync(int id)
        {
            return RunAsync(connection =>
            {
                using var command = CreateCommand(connection, "DELETE FROM \"ImageUploadConfig\" WHERE \"Id\" = $id;");
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            });
        }

        public Task RemoveAllImageUploadConfigsAsync()
        {
            return RunAsync(connection =>
            {
                using var command = CreateCommand(connection, "DELETE FROM \"ImageUploadConfig\";");
                command.ExecuteNonQuery();
            });
        }

        private static void BindImageUploadConfig(SqliteCommand command, ImageUploadConfig config)
        {
            command.Parameters.AddWithValue("$name", config.Name ?? string.Empty);
            command.Parameters.AddWithValue("$notes", config.Notes ?? string.Empty);
            command.Parameters.AddWithValue("$isEnable", config.IsEnable ? 1 : 0);
            command.Parameters.AddWithValue("$method", (int)config.Method);
            command.Parameters.AddWithValue("$config", config.Config ?? string.Empty);
        }

        private static ImageUploadConfig ReadImageUploadConfig(SqliteDataReader reader)
        {
            return new ImageUploadConfig
            {
                Id = reader.GetInt32(0),
                Name = GetStringOrEmpty(reader, 1),
                Notes = GetStringOrEmpty(reader, 2),
                IsEnable = reader.GetInt64(3) != 0,
                Method = (ImageUploadMethod)reader.GetInt32(4),
                Config = GetStringOrEmpty(reader, 5),
            };
        }
    }
}
