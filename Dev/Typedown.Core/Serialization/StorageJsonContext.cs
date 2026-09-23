using System.Text.Json;
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
    /// export/upload config models in the database's Config columns. The options reproduce Newtonsoft.Json's
    /// defaults: PascalCase names matched case-insensitively, public fields included, get-only object members
    /// populated in place, comments and trailing commas tolerated, lenient primitive conversions.
    /// </summary>
    [JsonSourceGenerationOptions(
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        Converters =
        [
            typeof(NewtonsoftBooleanConverter),
            typeof(NewtonsoftDoubleConverter),
            typeof(NewtonsoftInt32Converter),
            typeof(NewtonsoftStringConverter),
            typeof(NewtonsoftEnumConverter<AppTheme>),
            typeof(NewtonsoftEnumConverter<FileStartupAction>),
            typeof(NewtonsoftEnumConverter<FolderStartupAction>),
            typeof(NewtonsoftEnumConverter<InsertImageAction>),
            typeof(NewtonsoftEnumConverter<PrintOrientation>),
            typeof(NewtonsoftEnumConverter<KeyboardModifiers>),
            typeof(NewtonsoftEnumConverter<KeyboardKey>),
            typeof(NewtonsoftEnumConverter<PInvoke.WindowPlacementFlags>),
            typeof(NewtonsoftEnumConverter<PInvoke.ShowWindowCommand>),
        ])]
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
