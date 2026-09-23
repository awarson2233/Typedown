using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

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
            await CreateDatabase().AddFileAccessHistoryAsync(filePath, DateTime.Now);
            await UpdateFileRecentlyOpened(filePath, CollectionChangeAction.Add);
        }

        public async Task RemoveFileHistory(string filePath)
        {
            await CreateDatabase().RemoveFileAccessHistoryAsync(filePath);
            await UpdateFileRecentlyOpened(filePath, CollectionChangeAction.Remove);
        }

        public async Task ClearFileHistory()
        {
            await CreateDatabase().ClearFileAccessHistoryAsync();
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
                    foreach (var path in await LoadFileRecentlyOpenedAsync(maxCount))
                    {
                        if (!FileRecentlyOpened.Contains(path))
                            FileRecentlyOpened.Add(path);
                    }
                    break;
            }
            while (FileRecentlyOpened.Count > maxCount)
            {
                FileRecentlyOpened.RemoveAt(FileRecentlyOpened.Count - 1);
            }
        }

        public async Task RecordFolderHistory(string folderPath)
        {
            await CreateDatabase().AddFolderAccessHistoryAsync(folderPath, DateTime.Now);
            await UpdateFolderRecentlyOpened(folderPath, CollectionChangeAction.Add);
        }

        public async Task RemoveFolderHistory(string folderPath)
        {
            await CreateDatabase().RemoveFolderAccessHistoryAsync(folderPath);
            await UpdateFolderRecentlyOpened(folderPath, CollectionChangeAction.Remove);
        }

        public async Task ClearFolderHistory()
        {
            await CreateDatabase().ClearFolderAccessHistoryAsync();
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
                    foreach (var path in await LoadFolderRecentlyOpenedAsync(maxCount))
                    {
                        if (!FolderRecentlyOpened.Contains(path))
                            FolderRecentlyOpened.Add(path);
                    }
                    break;
            }
            while (FolderRecentlyOpened.Count > maxCount)
            {
                FolderRecentlyOpened.RemoveAt(FolderRecentlyOpened.Count - 1);
            }
        }

        private async Task UpdateRecentlyOpened()
        {
            var updateFileTask = UpdateFileRecentlyOpened(string.Empty, CollectionChangeAction.Refresh);
            var updateFolderTask = UpdateFolderRecentlyOpened(string.Empty, CollectionChangeAction.Refresh);
            await Task.WhenAll(updateFileTask, updateFolderTask);
        }

        private static Task<string[]> LoadFileRecentlyOpenedAsync(int maxCount)
        {
            return CreateDatabase().GetRecentFilePathsAsync(maxCount);
        }

        private static Task<string[]> LoadFolderRecentlyOpenedAsync(int maxCount)
        {
            return CreateDatabase().GetRecentFolderPathsAsync(maxCount);
        }

        private static AppDatabase CreateDatabase()
        {
            return new AppDatabase();
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
