using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Typedown.Core.Services
{
    public sealed partial class AppDatabase
    {
        public Task AddFileAccessHistoryAsync(string filePath, DateTime accessTime) => AddAccessHistory("FileAccessHistory", "FilePath", filePath, accessTime);

        public Task RemoveFileAccessHistoryAsync(string filePath) => RemoveAccessHistory("FileAccessHistory", "FilePath", filePath);

        public Task ClearFileAccessHistoryAsync() => ClearAccessHistory("FileAccessHistory");

        /// <summary>Most recent file paths first, one entry per history row (not de-duplicated).</summary>
        public Task<string[]> GetRecentFilePathsAsync(int maxCount) => GetRecentPaths("FileAccessHistory", "FilePath", maxCount);

        public Task AddFolderAccessHistoryAsync(string folderPath, DateTime accessTime) => AddAccessHistory("FolderAccessHistory", "FolderPath", folderPath, accessTime);

        public Task RemoveFolderAccessHistoryAsync(string folderPath) => RemoveAccessHistory("FolderAccessHistory", "FolderPath", folderPath);

        public Task ClearFolderAccessHistoryAsync() => ClearAccessHistory("FolderAccessHistory");

        /// <summary>Most recent folder paths first, one entry per history row (not de-duplicated).</summary>
        public Task<string[]> GetRecentFolderPathsAsync(int maxCount) => GetRecentPaths("FolderAccessHistory", "FolderPath", maxCount);

        private Task AddAccessHistory(string table, string pathColumn, string path, DateTime accessTime)
        {
            return RunAsync(connection =>
            {
                using var command = CreateCommand(connection, $"INSERT INTO \"{table}\" (\"AccessTime\", \"{pathColumn}\") VALUES ($time, $path);");
                command.Parameters.AddWithValue("$time", FormatDateTime(accessTime));
                command.Parameters.AddWithValue("$path", path ?? string.Empty);
                command.ExecuteNonQuery();
            });
        }

        private Task RemoveAccessHistory(string table, string pathColumn, string path)
        {
            return RunAsync(connection =>
            {
                using var command = CreateCommand(connection, $"DELETE FROM \"{table}\" WHERE \"{pathColumn}\" = $path;");
                command.Parameters.AddWithValue("$path", path ?? string.Empty);
                command.ExecuteNonQuery();
            });
        }

        private Task ClearAccessHistory(string table)
        {
            return RunAsync(connection =>
            {
                using var command = CreateCommand(connection, $"DELETE FROM \"{table}\";");
                command.ExecuteNonQuery();
            });
        }

        private Task<string[]> GetRecentPaths(string table, string pathColumn, int maxCount)
        {
            return RunAsync(connection =>
            {
                using var command = CreateCommand(connection,
                    $"SELECT \"{pathColumn}\" FROM \"{table}\" WHERE \"{pathColumn}\" IS NOT NULL ORDER BY \"AccessTime\" DESC LIMIT $count;");
                command.Parameters.AddWithValue("$count", maxCount);
                using var reader = command.ExecuteReader();
                var result = new List<string>();
                while (reader.Read())
                    result.Add(reader.GetString(0));
                return result.ToArray();
            });
        }
    }
}
