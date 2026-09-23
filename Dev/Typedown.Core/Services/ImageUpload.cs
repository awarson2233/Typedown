using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Typedown.Core.Enums;
using Typedown.Core.Models;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;

namespace Typedown.Core.Services
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
            await CreateDatabase().RemoveAllImageUploadConfigsAsync();
            await UpdateImageUploadConfigs();
        }

        public async Task<ImageUploadConfig> AddImageUploadConfig(string? name = null, ImageUploadMethod method = 0)
        {
            var res = new ImageUploadConfig() { Name = name ?? string.Empty, Method = method };
            await CreateDatabase().AddImageUploadConfigAsync(res);
            await UpdateImageUploadConfigs();
            return res;
        }

        public async Task RemoveImageUploadConfig(int id)
        {
            await CreateDatabase().RemoveImageUploadConfigAsync(id);
            await UpdateImageUploadConfigs();
        }

        public async Task<bool> SaveImageUploadConfig(ImageUploadConfig config)
        {
            try
            {
                if (!await CreateDatabase().UpdateImageUploadConfigAsync(config))
                    return false;
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
            return await CreateDatabase().GetImageUploadConfigAsync(id);
        }

        public async Task UpdateImageUploadConfigs()
        {
            var newItems = await CreateDatabase().GetImageUploadConfigsAsync();
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

        private static AppDatabase CreateDatabase()
        {
            return new AppDatabase();
        }
    }
}
