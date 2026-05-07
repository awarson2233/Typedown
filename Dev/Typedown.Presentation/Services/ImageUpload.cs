using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Typedown.Core.Enums;
using Typedown.Core.Models;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Presentation.ViewModels;

namespace Typedown.Presentation.Services
{
    public class ImageUpload
    {
        public ObservableCollection<ImageUploadConfig> ImageUploadConfigs { get; } = new();

        private readonly IServiceProvider serviceProvider;

        public ImageUpload(IServiceProvider serviceProvider, SettingsViewModel settings)
        {
            this.serviceProvider = serviceProvider;
            settings.ResetSettingsCommand.OnExecute.Subscribe(async _ => await ResetDefaultConfigs());
            Initialize(settings);
        }

        private async void Initialize(SettingsViewModel settings)
        {
            if (!settings.ImageUploadDatabaseInitialized)
            {
                settings.ImageUploadDatabaseInitialized = true;
                await ResetDefaultConfigs();
            }
            else
            {
                await UpdateImageUploadConfigs();
            }
        }

        private async Task ResetDefaultConfigs()
        {
            using var ctx = await CreateDbContextAsync();
            ctx.ImageUploadConfigs.RemoveRange(ctx.ImageUploadConfigs);
            await ctx.SaveChangesAsync();
            await UpdateImageUploadConfigs();
        }

        public async Task<ImageUploadConfig> AddImageUploadConfig(string? name = null, ImageUploadMethod method = 0)
        {
            using var ctx = await CreateDbContextAsync();
            var model = ctx.ImageUploadConfigs;
            var res = new ImageUploadConfig() { Name = name ?? string.Empty, Method = method };
            await model.AddAsync(res);
            await ctx.SaveChangesAsync();
            await UpdateImageUploadConfigs();
            return res;
        }

        public async Task RemoveImageUploadConfig(int id)
        {
            using var ctx = await CreateDbContextAsync();
            var model = ctx.ImageUploadConfigs;
            model.RemoveRange(model.Where(x => x.Id == id));
            await ctx.SaveChangesAsync();
            await UpdateImageUploadConfigs();
        }

        public async Task<bool> SaveImageUploadConfig(ImageUploadConfig config)
        {
            try
            {
                using var ctx = await CreateDbContextAsync();
                var model = ctx.ImageUploadConfigs;
                model.Update(config);
                await ctx.SaveChangesAsync();
                await UpdateImageUploadConfigs();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<ImageUploadConfig?> GetImageUploadConfig(int id)
        {
            using var ctx = await CreateDbContextAsync();
            var model = ctx.ImageUploadConfigs;
            return await model.Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateImageUploadConfigs()
        {
            using var ctx = await CreateDbContextAsync();
            var newItems = await ctx.ImageUploadConfigs.ToListAsync();
            ImageUploadConfigs.UpdateCollection(newItems, (a, b) => a.Id == b.Id);
        }

        public async Task<string> Upload(ImageAction.InsertImageSource source, string filePath)
        {
            var settings = serviceProvider.GetRequiredService<SettingsViewModel>();
            var configId = source switch
            {
                ImageAction.InsertImageSource.Clipboard => settings.InsertClipboardImageUseUploadConfigId,
                ImageAction.InsertImageSource.Local => settings.InsertLocalImageUseUploadConfigId,
                ImageAction.InsertImageSource.Web => settings.InsertWebImageUseUploadConfigId,
                _ => throw new NotImplementedException()
            };
            if (!configId.HasValue || ImageUploadConfigs.Where(x => x.IsEnable && x.Id == configId.Value).FirstOrDefault() is not ImageUploadConfig config)
            {
                throw new InvalidOperationException("Failed to load upload configuration.");
            }
            return await config.LoadUploadConfig().Upload(serviceProvider, filePath);
        }

        public async Task<string> Upload(ImageUploadConfig config, string filePath)
        {
            return await config.LoadUploadConfig().Upload(serviceProvider, filePath);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Image-upload config persistence uses EF Core by design and is isolated to this Presentation service.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Image-upload config persistence uses EF Core by design and is isolated to this Presentation service.")]
        private static Task<AppDbContext> CreateDbContextAsync()
        {
            return AppDbContext.Create();
        }
    }
}
