using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Typedown.Core;
using Typedown.Core.Enums;
using Typedown.Core.Serialization;
using Typedown.Core.Utilities;
using Typedown.Core.Interfaces;

namespace Typedown.Core.ViewModels
{
    public sealed partial class SettingsViewModel : INotifyPropertyChanged, IDisposable
    {
        public PInvoke.WINDOWPLACEMENT? StartupPlacement { get => GetSettingValue<PInvoke.WINDOWPLACEMENT?>(null); set => SetSettingValue(value); }
        public bool SidePaneOpen { get => GetSettingValue(false); set => SetSettingValue(value); }
        public double SidePaneWidth { get => GetSettingValue(300d); set => SetSettingValue(value); }
        public bool StatusBarOpen { get => GetSettingValue(true); set => SetSettingValue(value); }
        public double FindReplaceDialogWidth { get => GetSettingValue(600d); set => SetSettingValue(value); }
        public bool SourceCode { get => GetSettingValue(false); set => SetSettingValue(value); }
        public bool Typewriter { get => GetSettingValue(false); set => SetSettingValue(value); }
        public bool FocusMode { get => GetSettingValue(false); set => SetSettingValue(value); }
        public bool SearchIsCaseSensitive { get => GetSettingValue(false); set => SetSettingValue(value); }
        public bool SearchIsRegexp { get => GetSettingValue(false); set => SetSettingValue(value); }
        public bool SearchIsWholeWord { get => GetSettingValue(false); set => SetSettingValue(value); }
        public int SidePaneIndex { get => GetSettingValue(0); set => SetSettingValue(value); }
        public double FontSize { get => GetSettingValue(16d); set => SetSettingValue(value); }
        public double LineHeight { get => GetSettingValue(1.6d); set => SetSettingValue(value); }
        public bool AutoPairBracket { get => GetSettingValue(true); set => SetSettingValue(value); }
        public bool AutoPairQuote { get => GetSettingValue(true); set => SetSettingValue(value); }
        public bool TrimUnnecessaryCodeBlockEmptyLines { get => GetSettingValue(false); set => SetSettingValue(value); }
        public bool PreferLooseListItem { get => GetSettingValue(true); set => SetSettingValue(value); }
        public bool AutoPairMarkdownSyntax { get => GetSettingValue(true); set => SetSettingValue(value); }
        public string EditorAreaWidth { get => GetSettingValue("1200px"); set => SetSettingValue(value); }
        public bool AutoSave { get => GetSettingValue(false); set => SetSettingValue(value); }
        public AppTheme AppTheme { get => GetSettingValue(AppTheme.Default); set => SetSettingValue(value); }
        public string Language { get => GetSettingValue("default"); set => SetSettingValue(value); }
        public int WordCountMethod { get => GetSettingValue(0); set => SetSettingValue(value); }
        public int TabSize { get => GetSettingValue(4); set => SetSettingValue(value); }
        public bool SpellcheckEnabled { get => GetSettingValue(false); set => SetSettingValue(value); }
        public string SpellcheckLang { get => GetSettingValue(""); set => SetSettingValue(value); }
        public bool KeepRun { get => GetSettingValue(Config.IsPackaged); set => SetSettingValue(value); }
        public bool AnimationEnable { get => GetSettingValue(true); set => SetSettingValue(value); }
        public bool UseMicaEffect { get => GetSettingValue(Config.IsMicaSupported); set => SetSettingValue(value); }
        public bool UseEditorMicaEffect { get => GetSettingValue(false); set => SetSettingValue(value); }
        public bool Topmost { get => GetSettingValue(false); set => SetSettingValue(value); }
        public FileStartupAction FileStartupAction { get => GetSettingValue(FileStartupAction.None); set => SetSettingValue(value); }
        public FolderStartupAction FolderStartupAction { get => GetSettingValue(FolderStartupAction.OpenLast); set => SetSettingValue(value); }
        public string StartupOpenFolder { get => GetSettingValue(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)); set => SetSettingValue(value); }
        public string? LastFilePath { get => GetSettingValue<string?>(null); set => SetSettingValue(value); }
        public string? LastFolderPath { get => GetSettingValue<string?>(null); set => SetSettingValue(value); }
        public bool AppCompactMode { get => GetSettingValue(false); set => SetSettingValue(value); }
        public InsertImageAction InsertClipboardImageAction { get => GetSettingValue(InsertImageAction.None); set => SetSettingValue(value); }
        public string InsertClipboardImageCopyPath { get => GetSettingValue("./images"); set => SetSettingValue(value); }
        public int? InsertClipboardImageUseUploadConfigId { get => GetSettingValue<int?>(null); set => SetSettingValue(value); }
        public InsertImageAction InsertLocalImageAction { get => GetSettingValue(InsertImageAction.None); set => SetSettingValue(value); }
        public string InsertLocalImageCopyPath { get => GetSettingValue("./images"); set => SetSettingValue(value); }
        public int? InsertLocalImageUseUploadConfigId { get => GetSettingValue<int?>(null); set => SetSettingValue(value); }
        public InsertImageAction InsertWebImageAction { get => GetSettingValue(InsertImageAction.None); set => SetSettingValue(value); }
        public string InsertWebImageCopyPath { get => GetSettingValue("./images"); set => SetSettingValue(value); }
        public int? InsertWebImageUseUploadConfigId { get => GetSettingValue<int?>(null); set => SetSettingValue(value); }
        public IDialogService DialogService => ServiceProvider.GetRequiredService<IDialogService>();
        public IEditorSettingsNotifier EditorSettingsNotifier => ServiceProvider.GetRequiredService<IEditorSettingsNotifier>();
        public string DefaultImageBasePath { get => GetSettingValue(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), Config.AppName)); set => SetSettingValue(value); }
        public bool AutoCopyRelativePathImage { get => GetSettingValue(true); set => SetSettingValue(value); }
        public bool PreferRelativeImagePaths { get => GetSettingValue(false); set => SetSettingValue(value); }
        public bool AddSymbolBeforeRelativePath { get => GetSettingValue(false); set => SetSettingValue(value); }
        public bool AutoEncodeImageURL { get => GetSettingValue(true); set => SetSettingValue(value); }
        public bool OpenFolderAfterExport { get => GetSettingValue(false); set => SetSettingValue(value); }
        public bool FileExportDatabaseInitialized { get => GetSettingValue(false); set => SetSettingValue(value); }
        public bool ImageUploadDatabaseInitialized { get => GetSettingValue(false); set => SetSettingValue(value); }
        public IServiceProvider ServiceProvider { get; }

        public Command<Unit> ResetSettingsCommand { get; } = new();

        private readonly CompositeDisposable disposables = new();

        private readonly string settingsFile = Config.GetSettingsFilePath();

        private JsonObject store = new();

        private readonly IReadOnlyDictionary<string, string> editorSettingNameMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [nameof(SourceCode)] = "sourceCode",
            [nameof(Typewriter)] = "typewriter",
            [nameof(FocusMode)] = "focusMode",
            [nameof(SearchIsCaseSensitive)] = "searchIsCaseSensitive",
            [nameof(SearchIsRegexp)] = "searchIsRegexp",
            [nameof(SearchIsWholeWord)] = "searchIsWholeWord",
            [nameof(FontSize)] = "fontSize",
            [nameof(LineHeight)] = "lineHeight",
            [nameof(AutoPairBracket)] = "autoPairBracket",
            [nameof(AutoPairQuote)] = "autoPairQuote",
            [nameof(TrimUnnecessaryCodeBlockEmptyLines)] = "trimUnnecessaryCodeBlockEmptyLines",
            [nameof(PreferLooseListItem)] = "preferLooseListItem",
            [nameof(AutoPairMarkdownSyntax)] = "autoPairMarkdownSyntax",
            [nameof(EditorAreaWidth)] = "editorAreaWidth",
            [nameof(TabSize)] = "tabSize",
            [nameof(SpellcheckEnabled)] = "spellcheckEnabled"
        };

        public SettingsViewModel(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            ResetSettingsCommand.OnExecute.Subscribe(_ => ResetSetting());
            LoadAllSettings();
        }

        private void LoadAllSettings()
        {
            try
            {
                if (!File.Exists(settingsFile))
                {
                    store = new JsonObject();
                    return;
                }

                var json = File.ReadAllText(settingsFile);
                store = string.IsNullOrWhiteSpace(json)
                    ? new JsonObject()
                    : StorageJson.ParseObject(json);
            }
            catch
            {
                store = new JsonObject();
            }
        }

        private void SaveAllSettings()
        {
            try
            {
                var settingsDirectory = Path.GetDirectoryName(settingsFile);
                if (!string.IsNullOrEmpty(settingsDirectory))
                    Directory.CreateDirectory(settingsDirectory);

                File.WriteAllText(settingsFile, StorageJson.Write(store));
            }
            catch
            {
                // Ignore
            }
        }

        public T GetSettingValue<T>(T defaultValue = default!, [CallerMemberName] string propertyName = "")
        {
            var value = store[propertyName];
            if (value is null)
                return defaultValue;

            try
            {
                return StorageJson.Deserialize<T>(value)!;
            }
            catch (JsonException)
            {
                // A value that no longer matches the setting's type falls back to the default instead of failing startup.
                return defaultValue;
            }
        }

        public void SetSettingValue<T>(T value, [CallerMemberName] string propertyName = "")
        {
            var updatedValue = StorageJson.SerializeToNode(value);
            var currentValue = store[propertyName];
            if (currentValue is null
                && TryGetEffectiveSettingValue(propertyName, out var effectiveValue))
            {
                currentValue = effectiveValue is T effective ? StorageJson.SerializeToNode(effective) : null;
            }

            if (JsonNode.DeepEquals(currentValue, updatedValue))
            {
                return;
            }

            store[propertyName] = updatedValue;
            SaveAllSettings();
        }

        private bool TryGetEffectiveSettingValue(string propertyName, out object? value)
        {
            var property = typeof(SettingsViewModel).GetProperty(propertyName);
            if (property?.GetSetMethod() is null || property.GetMethod is null)
            {
                value = null;
                return false;
            }

            value = property.GetValue(this);
            return true;
        }

        public IReadOnlyDictionary<string, object> GetEditorSettings()
        {
            return new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [editorSettingNameMap[nameof(SourceCode)]] = SourceCode,
                [editorSettingNameMap[nameof(Typewriter)]] = Typewriter,
                [editorSettingNameMap[nameof(FocusMode)]] = FocusMode,
                [editorSettingNameMap[nameof(SearchIsCaseSensitive)]] = SearchIsCaseSensitive,
                [editorSettingNameMap[nameof(SearchIsRegexp)]] = SearchIsRegexp,
                [editorSettingNameMap[nameof(SearchIsWholeWord)]] = SearchIsWholeWord,
                [editorSettingNameMap[nameof(FontSize)]] = FontSize,
                [editorSettingNameMap[nameof(LineHeight)]] = LineHeight,
                [editorSettingNameMap[nameof(AutoPairBracket)]] = AutoPairBracket,
                [editorSettingNameMap[nameof(AutoPairQuote)]] = AutoPairQuote,
                [editorSettingNameMap[nameof(TrimUnnecessaryCodeBlockEmptyLines)]] = TrimUnnecessaryCodeBlockEmptyLines,
                [editorSettingNameMap[nameof(PreferLooseListItem)]] = PreferLooseListItem,
                [editorSettingNameMap[nameof(AutoPairMarkdownSyntax)]] = AutoPairMarkdownSyntax,
                [editorSettingNameMap[nameof(EditorAreaWidth)]] = EditorAreaWidth,
                [editorSettingNameMap[nameof(TabSize)]] = TabSize,
                [editorSettingNameMap[nameof(SpellcheckEnabled)]] = SpellcheckEnabled
            };
        }

        private bool TryGetEditorSettingChange(string propertyName, object value, out KeyValuePair<string, object> change)
        {
            if (editorSettingNameMap.TryGetValue(propertyName, out var editorSettingName))
            {
                change = new KeyValuePair<string, object>(editorSettingName, value);
                return true;
            }

            change = default;
            return false;
        }

        public void OnPropertyChanged(string propertyName, object before, object after)
        {
            PropertyChanged?.Invoke(this, new(propertyName));
            if (TryGetEditorSettingChange(propertyName, after, out var change))
                EditorSettingsNotifier?.NotifySettingsChanged(new Dictionary<string, object>(StringComparer.Ordinal) { [change.Key] = change.Value });
        }

        public async void ResetSetting()
        {
            var result = await DialogService.ShowAsync(new DialogRequest
            {
                Title = Locale.GetString("General.RestoreDefault.Title"),
                Content = Locale.GetDialogString("RestoreSettingsContent"),
                CloseButtonText = Locale.GetString("Cancel"),
                PrimaryButtonText = Locale.GetString("Ok"),
                DefaultButton = DialogDefaultButton.Close,
            });
            if (result != DialogButton.Primary)
                return;
            store = new JsonObject();
            SaveAllSettings();
            foreach (var item in typeof(SettingsViewModel).GetProperties().Where(x => x.GetSetMethod() != null).Select(x => x.Name))
                OnPropertyChanged(item);
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
