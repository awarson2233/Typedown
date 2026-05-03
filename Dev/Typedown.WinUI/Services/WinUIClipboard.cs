using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using Typedown.Presentation.Interfaces;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIClipboard : IClipboard
    {
        public bool ContainsText(TextDataFormat format)
        {
            var view = Clipboard.GetContent();
            return format switch
            {
                TextDataFormat.Text or TextDataFormat.UnicodeText => view.Contains(StandardDataFormats.Text),
                TextDataFormat.Html => view.Contains(StandardDataFormats.Html),
                TextDataFormat.Rtf => view.Contains(StandardDataFormats.Rtf),
                _ => false
            };
        }

        public async Task<string> GetTextAsync(TextDataFormat format)
        {
            if (!ContainsText(format))
            {
                return string.Empty;
            }

            var view = Clipboard.GetContent();
            return format switch
            {
                TextDataFormat.Text or TextDataFormat.UnicodeText => await view.GetTextAsync(),
                TextDataFormat.Html => await view.GetHtmlFormatAsync(),
                TextDataFormat.Rtf => await view.GetRtfAsync(),
                _ => throw new NotSupportedException($"Clipboard text format '{format}' is not supported by the WinUI adapter.")
            };
        }

        public void SetText(string text)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
        }

        public void SetText(string text, TextDataFormat format)
        {
            var dataPackage = new DataPackage();
            switch (format)
            {
                case TextDataFormat.Text:
                case TextDataFormat.UnicodeText:
                    dataPackage.SetText(text);
                    break;
                case TextDataFormat.Html:
                    dataPackage.SetHtmlFormat(text);
                    break;
                case TextDataFormat.Rtf:
                    dataPackage.SetRtf(text);
                    break;
                default:
                    throw new NotSupportedException($"Clipboard text format '{format}' is not supported by the WinUI adapter.");
            }

            Clipboard.SetContent(dataPackage);
        }

        public async Task<StringCollection> GetFileDropListAsync()
        {
            var result = new StringCollection();
            var view = Clipboard.GetContent();
            if (!view.Contains(StandardDataFormats.StorageItems))
            {
                return result;
            }

            var items = await view.GetStorageItemsAsync();
            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.Path))
                {
                    result.Add(item.Path);
                }
            }

            return result;
        }

        public async Task SetFileDropListAsync(StringCollection fileDropList)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetStorageItems(await ResolveStorageItemsAsync(fileDropList));
            Clipboard.SetContent(dataPackage);
        }

        public async Task<IClipboardImage> GetImageAsync()
        {
            var view = Clipboard.GetContent();
            if (!view.Contains(StandardDataFormats.Bitmap))
            {
                return null!;
            }

            return new WinUIClipboardImage(await view.GetBitmapAsync());
        }

        private static async Task<IReadOnlyList<IStorageItem>> ResolveStorageItemsAsync(StringCollection fileDropList)
        {
            var storageItems = new List<IStorageItem>();
            foreach (var item in fileDropList)
            {
                if (item is not string filePath)
                {
                    continue;
                }

                storageItems.Add(await StorageFile.GetFileFromPathAsync(filePath));
            }

            return storageItems;
        }

        private sealed class WinUIClipboardImage : IClipboardImage
        {
            private readonly RandomAccessStreamReference bitmapStreamRef;

            public WinUIClipboardImage(RandomAccessStreamReference bitmapStreamRef)
            {
                this.bitmapStreamRef = bitmapStreamRef;
            }

            public byte[] GetBytes()
            {
                return GetBytesAsync().GetAwaiter().GetResult();
            }

            public void SaveAsPng(string path)
            {
                File.WriteAllBytes(path, GetBytes());
            }

            private async Task<byte[]> GetBytesAsync()
            {
                using var stream = await bitmapStreamRef.OpenReadAsync();
                var decoder = await BitmapDecoder.CreateAsync(stream);
                var pixelData = await decoder.GetPixelDataAsync();
                var bytes = pixelData.DetachPixelData();

                using var memoryStream = new InMemoryRandomAccessStream();
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, memoryStream);
                encoder.SetPixelData(
                    decoder.BitmapPixelFormat,
                    decoder.BitmapAlphaMode,
                    decoder.PixelWidth,
                    decoder.PixelHeight,
                    decoder.DpiX,
                    decoder.DpiY,
                    bytes);
                await encoder.FlushAsync();

                var result = new byte[memoryStream.Size];
                memoryStream.Seek(0);
                await memoryStream.AsStreamForRead().ReadExactlyAsync(result);
                return result;
            }
        }
    }
}
