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

        private readonly object initializationLock = new();

        private Task? initializationTask;

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
                using var ctx = await AppDbContext.Create();
                var paths = await ctx.FileAccessHistories
                    .OrderByDescending(x => x.AccessTime)
                    .Select(x => x.FilePath)
                    .Take(maxCount)
                    .ToListAsync();

                foreach (var path in paths)
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
                FolderRecentlyOpened.RemoveAt(FolderRecentlyOpened.Count - 1);
            }
            if (FolderRecentlyOpened.Count < maxCount)
            {
                using var ctx = await AppDbContext.Create();
                var paths = await ctx.FolderAccessHistories
                    .OrderByDescending(x => x.AccessTime)
                    .Select(x => x.FolderPath)
                    .Take(maxCount)
                    .ToListAsync();

                foreach (var path in paths)
                {
                    if (!FolderRecentlyOpened.Contains(path))
                        FolderRecentlyOpened.Add(path);
                }
            }
        }

        private async Task UpdateRecentlyOpened()
        {
            var updateFileTask = UpdateFileRecentlyOpened(string.Empty, CollectionChangeAction.Refresh);
            var updateFolderTask = UpdateFolderRecentlyOpened(string.Empty, CollectionChangeAction.Refresh);
            await Task.WhenAll(updateFileTask, updateFolderTask);
        }

        public async Task EnsureInitialized()
        {
            Task task;
            lock (initializationLock)
            {
                initializationTask ??= UpdateRecentlyOpened();
                task = initializationTask;
            }

            await task;
        }

        public async Task ClearHistory()
        {
            await ClearFileHistory();
            await ClearFolderHistory();
        }
    }
}
