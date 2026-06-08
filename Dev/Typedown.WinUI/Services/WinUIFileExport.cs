using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Typedown.Core.Enums;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;

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

            using var ctx = await CreateDbContext();
            var config = new ExportConfig() { Name = name ?? string.Empty, Type = type };

            await ctx.ExportConfigs.AddAsync(config);
            await ctx.SaveChangesAsync();
            await UpdateExportConfigs();

            return config;
        }

        public async Task<ExportConfig> GetExportConfig(int id)
        {
            await EnsureExportConfigsLoaded();

            using var ctx = await CreateDbContext();
            var config = await ctx.ExportConfigs.Where(x => x.Id == id).FirstOrDefaultAsync();
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
            using var ctx = await CreateDbContext();
            ctx.ExportConfigs.RemoveRange(ctx.ExportConfigs.Where(x => x.Id == id));
            await ctx.SaveChangesAsync();
            await UpdateExportConfigs();
        }

        public async Task<bool> SaveExportConfig(ExportConfig config)
        {
            if (config is null)
                return false;

            try
            {
                using var ctx = await CreateDbContext();
                ctx.ExportConfigs.Update(config);
                await ctx.SaveChangesAsync();
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
            using var ctx = await CreateDbContext();
            var configs = await ctx.ExportConfigs.ToListAsync();
            ExportConfigs.UpdateCollection(configs, (a, b) => a.Id == b.Id);
        }

        private async Task EnsureExportConfigsInitialized()
        {
            using var ctx = await CreateDbContext();
            var changed = false;

            changed |= await EnsureDefaultExportConfig(ctx, "PDF", ExportType.PDF);
            changed |= await EnsureDefaultExportConfig(ctx, "HTML", ExportType.HTML);

            if (changed)
                await ctx.SaveChangesAsync();

            await UpdateExportConfigs();
        }

        private static async Task<bool> EnsureDefaultExportConfig(AppDbContext ctx, string name, ExportType type)
        {
            if (await ctx.ExportConfigs.AnyAsync(x => x.Type == type && x.Name == name))
                return false;

            ctx.ExportConfigs.Add(new ExportConfig { Name = name, Type = type });
            return true;
        }

        private Task<AppDbContext> CreateDbContext()
        {
            return AppDbContext.Create(appDataPathProvider);
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
