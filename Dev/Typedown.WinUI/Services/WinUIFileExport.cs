using System.Diagnostics;
using System.Collections.ObjectModel;
using Typedown.Core.Enums;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Services;
using Typedown.Core.Utilities;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIFileExport : IFileExport
    {
        private readonly IAppDataPathProvider appDataPathProvider;
        private readonly IFileConverter fileConverter;
        private readonly object exportConfigsLoadLock = new();
        private Task? exportConfigsLoadTask;

        public ObservableCollection<ExportConfig> ExportConfigs { get; } = new();

        public WinUIFileExport(IAppDataPathProvider appDataPathProvider, IFileConverter fileConverter)
        {
            this.appDataPathProvider = appDataPathProvider ?? throw new ArgumentNullException(nameof(appDataPathProvider));
            this.fileConverter = fileConverter ?? throw new ArgumentNullException(nameof(fileConverter));
        }

        public Task EnsureExportConfigsLoaded()
        {
            lock (exportConfigsLoadLock)
            {
                exportConfigsLoadTask ??= EnsureExportConfigsInitialized();
                return exportConfigsLoadTask;
            }
        }

        public async Task<ExportConfig> AddExportConfig(string? name = null, ExportType type = 0)
        {
            await EnsureExportConfigsLoaded();

            var config = new ExportConfig() { Name = name ?? string.Empty, Type = type };

            await CreateDatabase().AddExportConfigAsync(config);
            await UpdateExportConfigs();

            return config;
        }

        public async Task<ExportConfig> GetExportConfig(int id)
        {
            await EnsureExportConfigsLoaded();

            var config = await CreateDatabase().GetExportConfigAsync(id);
            return config ?? throw new InvalidOperationException($"Export config '{id}' was not found.");
        }

        public async Task Print(string basePath, string html, string? documentName = null)
        {
            using var stream = await fileConverter.HtmlToPdf(html);
            var pdfPath = CreateTemporaryPdfPath(documentName);

            await File.WriteAllBytesAsync(pdfPath, stream.ToArray());
            if (!TryShellExecute(pdfPath, "print"))
            {
                TryShellExecute(pdfPath, null);
            }
        }

        public async Task RemoveExportConfig(int id)
        {
            await CreateDatabase().RemoveExportConfigAsync(id);
            await UpdateExportConfigs();
        }

        public async Task<bool> SaveExportConfig(ExportConfig config)
        {
            if (config is null)
                return false;

            try
            {
                if (!await CreateDatabase().UpdateExportConfigAsync(config))
                    return false;
                await UpdateExportConfigs();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task UpdateExportConfigs()
        {
            var configs = await CreateDatabase().GetExportConfigsAsync();
            ExportConfigs.UpdateCollection(configs, (a, b) => a.Id == b.Id);
        }

        private async Task EnsureExportConfigsInitialized()
        {
            var database = CreateDatabase();
            await database.EnsureExportConfigAsync("PDF", ExportType.PDF);
            await database.EnsureExportConfigAsync("HTML", ExportType.HTML);

            await UpdateExportConfigs();
        }

        private AppDatabase CreateDatabase()
        {
            return new AppDatabase(appDataPathProvider);
        }

        private static string CreateTemporaryPdfPath(string? documentName)
        {
            var safeName = string.Join(
                "_",
                (string.IsNullOrWhiteSpace(documentName) ? "Typedown" : Path.GetFileNameWithoutExtension(documentName))
                    .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

            var path = Path.Combine(Path.GetTempPath(), $"{safeName}-{Guid.NewGuid():N}.pdf");
            return path;
        }

        private static bool TryShellExecute(string filePath, string? verb)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    Verb = verb ?? string.Empty
                });

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
