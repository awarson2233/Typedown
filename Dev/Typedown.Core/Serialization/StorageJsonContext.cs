using System.Text.Json.Serialization;
using Typedown.Core.Enums;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using ExportConfigModels = Typedown.Core.Models.ExportConfigModels;
using UploadConfigModels = Typedown.Core.Models.UploadConfigModels;

namespace Typedown.Core.Serialization
{
    /// <summary>
    /// Source-generated metadata for everything Typedown persists as JSON: setting values in settings.json and the
    /// export/upload config models in the database's Config columns. Public fields are included because the window
    /// placement setting is a struct of fields; get-only nested objects (page size, margins) are populated in place.
    /// </summary>
    [JsonSourceGenerationOptions(
        IncludeFields = true,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate)]
    // Setting value types (SettingsViewModel properties).
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(int?))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(AppTheme))]
    [JsonSerializable(typeof(FileStartupAction))]
    [JsonSerializable(typeof(FolderStartupAction))]
    [JsonSerializable(typeof(InsertImageAction))]
    [JsonSerializable(typeof(ShortcutKey))]
    [JsonSerializable(typeof(PInvoke.WINDOWPLACEMENT?))]
    // Export config models (ExportConfig.Config).
    [JsonSerializable(typeof(ExportConfigModels.ConfigModel), TypeInfoPropertyName = "ExportConfigModel")]
    [JsonSerializable(typeof(ExportConfigModels.PDFConfigModel))]
    [JsonSerializable(typeof(ExportConfigModels.HTMLConfigModel))]
    [JsonSerializable(typeof(ExportConfigModels.ImageConfigModel))]
    // Image upload config models (ImageUploadConfig.Config).
    [JsonSerializable(typeof(UploadConfigModels.ConfigModel), TypeInfoPropertyName = "UploadConfigModel")]
    [JsonSerializable(typeof(UploadConfigModels.FTPConfigModel))]
    [JsonSerializable(typeof(UploadConfigModels.GitConfigModel))]
    [JsonSerializable(typeof(UploadConfigModels.OSSConfigModel))]
    [JsonSerializable(typeof(UploadConfigModels.SCPConfigModel))]
    [JsonSerializable(typeof(UploadConfigModels.PowerShellModel))]
    public sealed partial class StorageJsonContext : JsonSerializerContext
    {
    }
}
