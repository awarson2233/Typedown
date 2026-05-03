using System.Collections.Generic;
using System.Collections.Specialized;
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
            throw new NotSupportedException("WinUI file copy shell operation is not wired in this migration slice.");
        }

        public async Task CopyToClipboardAsync(StringCollection files)
        {
            var dataPackage = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            dataPackage.SetStorageItems(await ResolveStorageItemsAsync(files));
            Clipboard.SetContent(dataPackage);
        }

        public async Task CutToClipboardAsync(StringCollection files)
        {
            var dataPackage = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Move
            };
            dataPackage.SetStorageItems(await ResolveStorageItemsAsync(files));
            Clipboard.SetContent(dataPackage);
        }

        public bool Delete(StringCollection files)
        {
            throw new NotSupportedException("WinUI file delete shell operation is not wired in this migration slice.");
        }

        public bool IsFilenameValid(string sourceFolder, string fileName)
        {
            var path = Path.Combine(sourceFolder, fileName);
            return !string.IsNullOrWhiteSpace(fileName)
                && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && !File.Exists(path)
                && !Directory.Exists(path);
        }

        public bool Move(StringCollection files, string to)
        {
            throw new NotSupportedException("WinUI file move shell operation is not wired in this migration slice.");
        }

        public void PasteFromClipboard(string to)
        {
            throw new NotSupportedException("WinUI file paste shell operation is not wired in this migration slice.");
        }

        public bool Rename(string from, string to)
        {
            throw new NotSupportedException("WinUI file rename shell operation is not wired in this migration slice.");
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

                storageItems.Add(await StorageFile.GetFileFromPathAsync(filePath));
            }

            return storageItems;
        }
    }
}
