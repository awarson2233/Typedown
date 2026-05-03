using Microsoft.EntityFrameworkCore;
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

        public ObservableCollection<ExportConfig> ExportConfigs { get; } = new();

        public WinUIFileExport(IAppDataPathProvider appDataPathProvider)
        {
            this.appDataPathProvider = appDataPathProvider ?? throw new ArgumentNullException(nameof(appDataPathProvider));
            Initialize();
        }

        public async Task<ExportConfig> AddExportConfig(string? name = null, ExportType type = 0)
        {
            using var ctx = await CreateDbContext();
            var config = new ExportConfig() { Name = name ?? string.Empty, Type = type };

            await ctx.ExportConfigs.AddAsync(config);
            await ctx.SaveChangesAsync();
            await UpdateExportConfigs();

            return config;
        }

        public async Task<ExportConfig> GetExportConfig(int id)
        {
            using var ctx = await CreateDbContext();
            return (await ctx.ExportConfigs.Where(x => x.Id == id).FirstOrDefaultAsync())!;
        }

        public Task Print(string basePath, string html, string? documentName = null)
        {
            throw new NotSupportedException("WinUI print/export still requires the WebView-backed HTML-to-PDF conversion pipeline, which is not wired into the WinUI app head in this migration slice.");
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

        private Task<AppDbContext> CreateDbContext()
        {
            return AppDbContext.Create(appDataPathProvider);
        }

        private async void Initialize()
        {
            await UpdateExportConfigs();
        }
    }
}
