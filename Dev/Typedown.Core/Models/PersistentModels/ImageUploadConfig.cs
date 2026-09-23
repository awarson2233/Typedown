using System;
using System.ComponentModel;
using System.Text.Json.Nodes;
using Typedown.Core.Enums;
using Typedown.Core.Serialization;
using Typedown.Core.Models.UploadConfigModels;

namespace Typedown.Core.Models
{
    public partial class ImageUploadConfig : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public bool IsEnable { get; set; }

        public ImageUploadMethod Method { get; set; }

        public string Config { get; internal set; } = "{}";

        public ConfigModel LoadUploadConfig()
        {
            try
            {
                var allConfig = ParseConfig();
                if (allConfig.TryGetPropertyValue(GetConfigModelKey(), out var value) && value is not null && StorageJson.Deserialize(value, GetConfigModelType()) is ConfigModel config)
                    return config;
            }
            catch
            {
                // Return default value;
            }
            var defaultConfig = GetDefaultConfigModel();
            StoreUploadConfig(defaultConfig);
            return defaultConfig;
        }

        public void StoreUploadConfig(ConfigModel uploadConfig)
        {
            var config = ParseConfig();
            config[GetConfigModelKey()] = StorageJson.SerializeToNode((object)uploadConfig);
            Config = StorageJson.Write(config);
        }

        private Type GetConfigModelType()
        {
            return Method switch
            {
                ImageUploadMethod.FTP => typeof(FTPConfigModel),
                ImageUploadMethod.Git => typeof(GitConfigModel),
                ImageUploadMethod.OSS => typeof(OSSConfigModel),
                ImageUploadMethod.SCP => typeof(SCPConfigModel),
                ImageUploadMethod.PowerShell => typeof(PowerShellModel),
                _ => typeof(ConfigModel)
            };
        }

        private string GetConfigModelKey()
        {
            return Method switch
            {
                ImageUploadMethod.FTP => "FTP",
                ImageUploadMethod.Git => "Git",
                ImageUploadMethod.OSS => "OSS",
                ImageUploadMethod.SCP => "SCP",
                ImageUploadMethod.PowerShell => "PowerShell",
                _ => throw new InvalidOperationException($"Unsupported image upload method: {Method}")
            };
        }

        private ConfigModel GetDefaultConfigModel()
        {
            return Method switch
            {
                ImageUploadMethod.FTP => new FTPConfigModel(),
                ImageUploadMethod.Git => new GitConfigModel(),
                ImageUploadMethod.OSS => new OSSConfigModel(),
                ImageUploadMethod.SCP => new SCPConfigModel(),
                ImageUploadMethod.PowerShell => new PowerShellModel(),
                _ => throw new InvalidOperationException($"Unsupported image upload method: {Method}")
            };
        }

        private JsonObject ParseConfig()
        {
            try
            {
                return StorageJson.ParseObject(Config);
            }
            catch
            {
                return new();
            }
        }
    }
}
