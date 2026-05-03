using System.Collections.Generic;
using System.Collections.Specialized;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIFileOperation : IFileOperation
    {
        public bool IsPasteEnabled => Clipboard.GetContent().Contains(StandardDataFormats.StorageItems);

        public bool Copy(StringCollection files, string to)
        {
            return RunShellFileOperation(PInvoke.FileFuncFlags.FO_COPY, files, to);
        }

        public async Task CopyToClipboardAsync(StringCollection files)
        {
            var storageItems = await ResolveStorageItemsAsync(files);
            if (storageItems.Count == 0)
            {
                return;
            }

            var dataPackage = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            dataPackage.SetStorageItems(storageItems);
            Clipboard.SetContent(dataPackage);
        }

        public async Task CutToClipboardAsync(StringCollection files)
        {
            var storageItems = await ResolveStorageItemsAsync(files);
            if (storageItems.Count == 0)
            {
                return;
            }

            var dataPackage = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Move
            };
            dataPackage.SetStorageItems(storageItems);
            Clipboard.SetContent(dataPackage);
        }

        public bool Delete(StringCollection files)
        {
            throw new NotSupportedException("WinUI file delete shell operation is not wired in this migration slice.");
        }

        public bool IsFilenameValid(string sourceFolder, string fileName)
        {
            var path = Path.Combine(sourceFolder, fileName);
            return !string.IsNullOrEmpty(fileName)
                && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && !File.Exists(path)
                && !Directory.Exists(path);
        }

        public bool Move(StringCollection files, string to)
        {
            return RunShellFileOperation(PInvoke.FileFuncFlags.FO_MOVE, files, to);
        }

        public void PasteFromClipboard(string to)
        {
            throw new NotSupportedException("WinUI file paste shell operation is not wired in this migration slice.");
        }

        public bool Rename(string from, string to)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            {
                return false;
            }

            var files = new StringCollection { from };
            return RunShellFileOperation(PInvoke.FileFuncFlags.FO_RENAME, files, to);
        }

        private static async Task<IReadOnlyList<IStorageItem>> ResolveStorageItemsAsync(StringCollection files)
        {
            var storageItems = new List<IStorageItem>();
            foreach (var item in files)
            {
                if (item is not string filePath)
                {
                    continue;
                }

                if (File.Exists(filePath))
                {
                    storageItems.Add(await StorageFile.GetFileFromPathAsync(filePath));
                }
                else if (Directory.Exists(filePath))
                {
                    storageItems.Add(await StorageFolder.GetFolderFromPathAsync(filePath));
                }
            }

            return storageItems;
        }

        private static bool RunShellFileOperation(PInvoke.FileFuncFlags fileFunc, StringCollection files, string? to)
        {
            var pFrom = CreateShellPathList(files);
            if (string.IsNullOrEmpty(pFrom) || string.IsNullOrEmpty(to))
            {
                return false;
            }

            var operation = new PInvoke.SHFILEOPSTRUCT
            {
                wFunc = fileFunc,
                fFlags = PInvoke.FILEOP_FLAGS.FOF_ALLOWUNDO,
                pFrom = pFrom,
                pTo = to + "\0"
            };

            return PInvoke.SHFileOperation(ref operation) == 0 && !operation.fAnyOperationsAborted;
        }

        private static string CreateShellPathList(StringCollection files)
        {
            var paths = "";
            foreach (var item in files)
            {
                if (item is not string filePath || string.IsNullOrEmpty(filePath))
                {
                    continue;
                }

                paths += filePath + "\0";
            }

            return paths;
        }
    }
}
