using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Typedown.Core.Models;

namespace Typedown.Core.Services
{
    public class AccessHistory
    {
        public ObservableCollection<string> FileRecentlyOpened { get; } = new();

        public ObservableCollection<string> FolderRecentlyOpened { get; } = new();

        private readonly TaskCompletionSource<bool> initializedTask = new();

        public AccessHistory()
        {
            _ = UpdateRecentlyOpened();
        }

        public async Task RecordFileHistory(string filePath)
        {
            using var ctx = await AppDbContext.Create();
            var model = ctx.FileAccessHistories;
            var item = new FileAccessHistory() { FilePath = filePath, AccessTime = DateTime.Now };
            await model.AddAsync(item);
            await ctx.SaveChangesAsync();
            await UpdateFileRecentlyOpened(filePath, CollectionChangeAction.Add);
        }

        public async Task RemoveFileHistory(string filePath)
        {
            using var ctx = await AppDbContext.Create();
            var model = ctx.FileAccessHistories;
            model.RemoveRange(model.Where(x => x.FilePath == filePath));
            await ctx.SaveChangesAsync();
            await UpdateFileRecentlyOpened(filePath, CollectionChangeAction.Remove);
        }

        public async Task ClearFileHistory()
        {
            using var ctx = await AppDbContext.Create();
            var model = ctx.FileAccessHistories;
            model.RemoveRange(model);
            await ctx.SaveChangesAsync();
            await UpdateFileRecentlyOpened(string.Empty, CollectionChangeAction.Refresh);
        }

        private async Task UpdateFileRecentlyOpened(string filePath, CollectionChangeAction action)
        {
            var maxCount = 10;
            switch (action)
            {
                case CollectionChangeAction.Add:
                    FileRecentlyOpened.Remove(filePath);
                    FileRecentlyOpened.Insert(0, filePath);
                    break;
                case CollectionChangeAction.Remove:
                    FileRecentlyOpened.Remove(filePath);
                    break;
                case CollectionChangeAction.Refresh:
                    FileRecentlyOpened.Clear();
                    break;
            }
            while (FileRecentlyOpened.Count > maxCount)
            {
                FileRecentlyOpened.RemoveAt(FileRecentlyOpened.Count - 1);
            }
            if (FileRecentlyOpened.Count < maxCount)
            {
                foreach (var path in await LoadRecentlyOpenedPathsAsync("FileAccessHistory", "FilePath", maxCount))
                {
                    if (!FileRecentlyOpened.Contains(path))
                        FileRecentlyOpened.Add(path);
                }
            }
        }

        public async Task RecordFolderHistory(string folderPath)
        {
            using var ctx = await AppDbContext.Create();
            var model = ctx.FolderAccessHistories;
            var item = new FolderAccessHistory() { FolderPath = folderPath, AccessTime = DateTime.Now };
            await model.AddAsync(item);
            await ctx.SaveChangesAsync();
            await UpdateFolderRecentlyOpened(folderPath, CollectionChangeAction.Add);
        }

        public async Task RemoveFolderHistory(string folderPath)
        {
            using var ctx = await AppDbContext.Create();
            var model = ctx.FolderAccessHistories;
            model.RemoveRange(model.Where(x => x.FolderPath == folderPath));
            await ctx.SaveChangesAsync();
            await UpdateFolderRecentlyOpened(folderPath, CollectionChangeAction.Remove);
        }

        public async Task ClearFolderHistory()
        {
            using var ctx = await AppDbContext.Create();
            var model = ctx.FolderAccessHistories;
            model.RemoveRange(model);
            await ctx.SaveChangesAsync();
            await UpdateFolderRecentlyOpened(string.Empty, CollectionChangeAction.Refresh);
        }

        private async Task UpdateFolderRecentlyOpened(string folderPath, CollectionChangeAction action)
        {
            var maxCount = 10;
            switch (action)
            {
                case CollectionChangeAction.Add:
                    FolderRecentlyOpened.Remove(folderPath);
                    FolderRecentlyOpened.Insert(0, folderPath);
                    break;
                case CollectionChangeAction.Remove:
                    FolderRecentlyOpened.Remove(folderPath);
                    break;
                case CollectionChangeAction.Refresh:
                    FolderRecentlyOpened.Clear();
                    break;
            }
            while (FolderRecentlyOpened.Count > maxCount)
            {
                FolderRecentlyOpened.RemoveAt(FileRecentlyOpened.Count - 1);
            }
            if (FolderRecentlyOpened.Count < maxCount)
            {
                foreach (var path in await LoadRecentlyOpenedPathsAsync("FolderAccessHistory", "FolderPath", maxCount))
                {
                    if (!FolderRecentlyOpened.Contains(path))
                        FolderRecentlyOpened.Add(path);
                }
            }
        }

        private static async Task<string[]> LoadRecentlyOpenedPathsAsync(string tableName, string pathColumnName, int maxCount)
        {
            using var ctx = await AppDbContext.Create();
            var connectionString = ctx.Database.GetDbConnection().ConnectionString;

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT \"{pathColumnName}\" FROM \"{tableName}\" ORDER BY \"AccessTime\" DESC LIMIT $maxCount;";
            command.Parameters.AddWithValue("$maxCount", maxCount);

            var paths = new string[maxCount];
            var count = 0;
            await using var reader = await command.ExecuteReaderAsync();
            while (count < paths.Length && await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                    paths[count++] = reader.GetString(0);
            }

            if (count == paths.Length)
                return paths;

            Array.Resize(ref paths, count);
            return paths;
        }

        private async Task UpdateRecentlyOpened()
        {
            var updateFileTask = UpdateFileRecentlyOpened(string.Empty, CollectionChangeAction.Refresh);
            var updateFolderTask = UpdateFolderRecentlyOpened(string.Empty, CollectionChangeAction.Refresh);
            await Task.WhenAll(updateFileTask, updateFolderTask);
            if (!initializedTask.Task.IsCompleted)
                initializedTask.SetResult(true);
        }

        public async Task EnsureInitialized()
        {
            await initializedTask.Task;
        }

        public async Task ClearHistory()
        {
            await ClearFileHistory();
            await ClearFolderHistory();
        }
    }
}
