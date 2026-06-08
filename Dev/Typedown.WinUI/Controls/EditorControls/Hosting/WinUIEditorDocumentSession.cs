using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Typedown.Core.Models;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Presentation.Utilities;
using Typedown.Presentation.ViewModels;

namespace Typedown.WinUI.Controls
{
    internal sealed class WinUIEditorDocumentSession : IEditorDocumentSession
    {
        private readonly Func<EditorThemePayload> themeProvider;
        private readonly RemoteInvoke? remoteInvoke;
        private readonly EventCenter? eventCenter;
        private readonly AppViewModel? appViewModel;

        public WinUIEditorDocumentSession(
            string? initialMarkdown = null,
            string? basePath = null,
            string? filePath = null,
            Func<EditorThemePayload>? themeProvider = null,
            IServiceProvider? serviceProvider = null)
        {
            this.themeProvider = themeProvider ?? CreateDefaultThemePayload;
            remoteInvoke = serviceProvider?.GetService<RemoteInvoke>();
            eventCenter = serviceProvider?.GetService<EventCenter>();
            appViewModel = serviceProvider?.GetService<AppViewModel>();
            EnsurePresentationHandlers(serviceProvider);
            var seedMarkdown = string.IsNullOrWhiteSpace(initialMarkdown) ? GetDefaultSmokeMarkdown() : initialMarkdown;
            var seedBasePath = string.IsNullOrWhiteSpace(basePath) ? AppContext.BaseDirectory : basePath;
            var seedHash = ComputeHash(seedMarkdown);

            State = new EditorDocumentState
            {
                Text = seedMarkdown,
                FilePath = filePath,
                BasePath = seedBasePath,
                FileHash = seedHash,
                CurrentHash = seedHash,
                IsLoaded = false,
                IsSaved = true,
                LastEventName = "Waiting"
            };

            SettingsSnapshot = new EditorSettingsSnapshot(new EditorSettingsPayload
            {
                Markdown = seedMarkdown,
                BasePath = seedBasePath
            });
        }

        public EditorDocumentState State { get; private set; }

        public EditorSettingsSnapshot SettingsSnapshot { get; private set; }

        // Persistence boundary: LoadFile(), ReplaceFileText(), Save(), SaveAs().
        // Expected IO/path failures are collapsed through catch () filters into EditorPersistenceResult.
        public EditorPersistenceResult LoadFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new EditorPersistenceResult(false, State, "A file path is required to load markdown.");
            }

            try
            {
                var text = File.ReadAllText(filePath);
                var basePath = InferBasePath(filePath) ?? AppContext.BaseDirectory;
                var hash = ComputeHash(text);

                UpdateState(new EditorDocumentState
                {
                    Text = text,
                    FilePath = filePath,
                    BasePath = basePath,
                    FileHash = hash,
                    CurrentHash = hash,
                    IsLoaded = true,
                    IsSaved = true,
                    LastEventName = "LoadFile"
                });

                return new EditorPersistenceResult(true, State, PersistedFilePath: filePath);
            }
            catch (Exception ex) when (IsExpectedIoFailure(ex))
            {
                return new EditorPersistenceResult(false, State, ex.Message, PersistedFilePath: filePath);
            }
        }

        public EditorPersistenceResult ReplaceFileText(string text, string? filePath = null, string? basePath = null)
        {
            text ??= string.Empty;
            var nextFilePath = filePath ?? State.FilePath;
            var nextBasePath = basePath
                ?? InferBasePath(nextFilePath)
                ?? State.BasePath;
            var currentHash = ComputeHash(text);

            UpdateState(State with
            {
                Text = text,
                FilePath = nextFilePath,
                BasePath = nextBasePath,
                CurrentHash = currentHash,
                IsLoaded = !string.IsNullOrWhiteSpace(nextFilePath) || State.IsLoaded,
                IsSaved = string.Equals(currentHash, State.FileHash, StringComparison.Ordinal),
                LastEventName = "ReplaceFileText"
            });

            return new EditorPersistenceResult(true, State, PersistedFilePath: nextFilePath);
        }

        public EditorPersistenceResult Save()
        {
            if (string.IsNullOrWhiteSpace(State.FilePath))
            {
                return new EditorPersistenceResult(false, State, "Cannot save a smoke document without a file path.");
            }

            try
            {
                File.WriteAllText(State.FilePath, State.Text);
                var hash = ComputeHash(State.Text);

                UpdateState(State with
                {
                    FileHash = hash,
                    CurrentHash = hash,
                    IsLoaded = true,
                    IsSaved = true,
                    LastEventName = "Save"
                });

                return new EditorPersistenceResult(true, State, PersistedFilePath: State.FilePath);
            }
            catch (Exception ex) when (IsExpectedIoFailure(ex))
            {
                return new EditorPersistenceResult(false, State, ex.Message, PersistedFilePath: State.FilePath);
            }
        }

        public EditorPersistenceResult SaveAs(string filePath, bool saveCopy = false)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new EditorPersistenceResult(false, State, "A file path is required to save markdown.");
            }

            try
            {
                File.WriteAllText(filePath, State.Text);
                var savedHash = ComputeHash(State.Text);
                var savedBasePath = InferBasePath(filePath) ?? AppContext.BaseDirectory;

                if (!saveCopy)
                {
                    UpdateState(State with
                    {
                        FilePath = filePath,
                        BasePath = savedBasePath,
                        FileHash = savedHash,
                        CurrentHash = savedHash,
                        IsLoaded = true,
                        IsSaved = true,
                        LastEventName = "SaveAs"
                    });
                }

                return new EditorPersistenceResult(true, State, PersistedFilePath: filePath, IsCopy: saveCopy);
            }
            catch (Exception ex) when (IsExpectedIoFailure(ex))
            {
                return new EditorPersistenceResult(false, State, ex.Message, PersistedFilePath: filePath, IsCopy: saveCopy);
            }
        }

        public void HandleEditorEvent(EditorEventMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            ForwardPresentationEvent(message);

            switch (message.Name)
            {
                case "FileLoaded":
                    ApplyLoadedState(message.Args);
                    break;
                case "MarkdownChange":
                    ApplyMarkdownChange(message.Args);
                    break;
                case "OpenURI":
                    HandleOpenUri(message.Args);
                    break;
                case "CursorChange":
                case "StateChange":
                case "ContentLoaded":
                    State = State with { LastEventName = message.Name };
                    break;
                default:
                    State = State with { LastEventName = message.Name };
                    break;
            }
        }

        public Task<object?> HandleRemoteInvokeAsync(string name, JsonElement? args)
        {
            return name switch
            {
                "GetCurrentTheme" => Task.FromResult<object?>(themeProvider()),
                "ContentLoaded" => Task.FromResult<object?>(HandleContentLoaded()),
                "ExportCallback" => InvokePresentationAsync(name, args),
                "PrintHTML" => InvokePresentationAsync(name, args),
                "ResizeTable" => HandleResizeTableAsync(args),
                "LoadImage" => Task.FromResult<object?>(CreateLoadImagePayload(args)),
                "GetSettings" => InvokePresentationAsync(name, args),
                "SetClipboard" => InvokePresentationAsync(name, args),
                "GetStringResources" => HandleGetStringResourcesAsync(args),
                "OpenNewWindow" => Task.FromResult<object?>(HandleOpenNewWindow(args)),
                "OpenURI" => Task.FromResult<object?>(HandleOpenUri(args)),
                "UnhandledException" => Task.FromResult<object?>(HandleUnhandledException()),
                _ => throw new InvalidOperationException($"function [{name}] does not exist")
            };
        }

        private string HandleContentLoaded()
        {
            State = State with { LastEventName = "ContentLoaded" };
            return "WinUI editor content loaded.";
        }

        private bool HandleOpenNewWindow(JsonElement? args)
        {
            State = State with { LastEventName = "OpenNewWindow" };
            return OpenUri(ReadUriArgument(args));
        }

        private bool HandleOpenUri(JsonElement? args)
        {
            State = State with { LastEventName = "OpenURI" };
            return OpenUri(ReadUriArgument(args));
        }

        private bool OpenUri(string? uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                return false;
            }

            if (!UriHelper.IsWebUrl(uri) && UriHelper.TryGetLocalPath(uri, out var localPath))
            {
                var currentFilePath = State.FilePath;
                var currentFolder = string.IsNullOrWhiteSpace(currentFilePath) ? null : Path.GetDirectoryName(currentFilePath);
                if (!string.IsNullOrWhiteSpace(currentFolder))
                {
                    var fullPath = Path.GetFullPath(Path.Combine(currentFolder, localPath));
                    if (File.Exists(fullPath))
                    {
                        if (FileTypeHelper.IsMarkdownFile(fullPath) && appViewModel is not null)
                        {
                            appViewModel.FileViewModel.NewWindowCommand.Execute(fullPath);
                        }
                        else
                        {
                            Common.OpenUrl(fullPath);
                        }

                        return true;
                    }
                }
            }

            Common.OpenUrl(uri);
            return true;
        }

        private object? HandleUnhandledException()
        {
            State = State with { LastEventName = "UnhandledException" };
            return null;
        }

        private async Task<object?> InvokePresentationAsync(string name, JsonElement? args)
        {
            if (remoteInvoke is null)
            {
                throw new InvalidOperationException($"Presentation RemoteInvoke is not available for editor invoke [{name}].");
            }

            State = State with { LastEventName = name };
            return await remoteInvoke.Invoke(name, ToJToken(args));
        }

        private Task<object?> HandleGetStringResourcesAsync(JsonElement? args)
        {
            if (remoteInvoke is null)
            {
                State = State with { LastEventName = "GetStringResources" };
                return Task.FromResult<object?>(CreateStringResources(args));
            }

            return InvokePresentationAsync("GetStringResources", args);
        }

        private Task<object?> HandleResizeTableAsync(JsonElement? args)
        {
            if (remoteInvoke is not null)
            {
                return InvokePresentationAsync("ResizeTable", args);
            }

            if (TryCreateResizeTablePayload(args, out var payload))
            {
                State = State with { LastEventName = "ResizeTable" };
                return Task.FromResult<object?>(payload);
            }

            return InvokePresentationAsync("ResizeTable", args);
        }

        private void ForwardPresentationEvent(EditorEventMessage message)
        {
            if (eventCenter is null || !IsPresentationEvent(message.Name))
            {
                return;
            }

            eventCenter.EmitEvent(message.Name, new EditorEventArgs(message.Name, ToJToken(message.Args)));
        }

        private static bool IsPresentationEvent(string name)
        {
            return name is "MarkdownChange"
                or "FileLoaded"
                or "CursorChange"
                or "SelectionChange"
                or "CodeMirrorSelectionChange"
                or "StateChange"
                or "SelectionFormats"
                or "OnScroll"
                or "OpenFindReplace"
                or "Save"
                or "SaveAs"
                or "Close"
                or "OpenFrontMenu"
                or "OpenFormatPicker"
                or "OpenImageSelector"
                or "OpenTableTools"
                or "OpenImageToolbar"
                or "OpenToolTip";
        }

        private static JToken ToJToken(JsonElement? args)
        {
            return args is JsonElement element
                ? JToken.Parse(element.GetRawText())
                : JValue.CreateNull();
        }

        private static void EnsurePresentationHandlers(IServiceProvider? serviceProvider)
        {
            if (serviceProvider is null)
            {
                return;
            }

            var appViewModel = serviceProvider.GetService<AppViewModel>();
            if (appViewModel is null)
            {
                return;
            }

            _ = appViewModel.EditorViewModel;
            _ = appViewModel.FileViewModel;
            _ = appViewModel.FloatViewModel;
            _ = appViewModel.FormatViewModel;
            _ = appViewModel.ParagraphViewModel;
            _ = appViewModel.UIViewModel;
        }

        private void ApplyLoadedState(JsonElement? args)
        {
            var text = ReadStringProperty(args, "text") ?? string.Empty;
            var filePath = ReadStringProperty(args, "filePath");
            var basePath = ReadStringProperty(args, "basePath")
                ?? InferBasePath(filePath)
                ?? State.BasePath;
            var hash = ComputeHash(text);

            UpdateState(State with
            {
                Text = text,
                FilePath = filePath ?? State.FilePath,
                BasePath = basePath,
                FileHash = hash,
                CurrentHash = hash,
                IsLoaded = true,
                IsSaved = true,
                LastEventName = "FileLoaded"
            });
        }

        private void ApplyMarkdownChange(JsonElement? args)
        {
            var text = ReadStringProperty(args, "text") ?? State.Text;
            var currentHash = ComputeHash(text);

            UpdateState(State with
            {
                Text = text,
                CurrentHash = currentHash,
                IsSaved = string.Equals(currentHash, State.FileHash, StringComparison.Ordinal),
                LastEventName = "MarkdownChange"
            });
        }

        private void UpdateState(EditorDocumentState state)
        {
            State = state;
            SettingsSnapshot = SettingsSnapshot with
            {
                Payload = SettingsSnapshot.Payload with
                {
                    Markdown = state.Text,
                    BasePath = state.BasePath
                }
            };
        }

        private static EditorThemePayload CreateDefaultThemePayload()
        {
            return new EditorThemePayload
            {
                Theme = "Light",
                AccentColor = new EditorColorPayload(27, 102, 107, 1),
                Background = new EditorColorPayload(249, 249, 249, 1)
            };
        }

        private static object CreateLoadImagePayload(JsonElement? args)
        {
            return new
            {
                url = ReadStringProperty(args, "url") ?? string.Empty
            };
        }

        private static Dictionary<string, string> CreateStringResources(JsonElement? args)
        {
            var resources = new Dictionary<string, string>(StringComparer.Ordinal);
            if (args is not JsonElement element
                || element.ValueKind != JsonValueKind.Object
                || !element.TryGetProperty("names", out var names)
                || names.ValueKind != JsonValueKind.Array)
            {
                return resources;
            }

            foreach (var nameElement in names.EnumerateArray())
            {
                if (nameElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var name = nameElement.GetString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                resources[name] = GetStringResource(name);
            }

            return resources;
        }

        private static string GetStringResource(string name)
        {
            try
            {
                var value = Locale.GetString(name);
                return string.IsNullOrWhiteSpace(value) ? name : value;
            }
            catch
            {
                return name;
            }
        }

        private static bool TryCreateResizeTablePayload(JsonElement? args, out object? payload)
        {
            payload = null;
            var row = ReadIntProperty(args, "row") ?? ReadIntProperty(args, "rows");
            var column = ReadIntProperty(args, "column") ?? ReadIntProperty(args, "columns");
            if (row is null || column is null)
            {
                return false;
            }

            payload = new
            {
                row = row.Value,
                column = column.Value,
                rows = row.Value,
                columns = column.Value
            };
            return true;
        }

        private static string? ReadStringProperty(JsonElement? args, string propertyName)
        {
            if (args is not JsonElement element || element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        }

        private static string? ReadStringArgument(JsonElement? args)
        {
            return args is JsonElement element && element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : null;
        }

        private static string? ReadUriArgument(JsonElement? args)
        {
            return ReadStringArgument(args)
                ?? ReadStringProperty(args, "uri")
                ?? ReadStringProperty(args, "href");
        }

        private static int? ReadIntProperty(JsonElement? args, string propertyName)
        {
            if (args is not JsonElement element || element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
                ? value
                : null;
        }

        private static string? InferBasePath(string? filePath)
        {
            return string.IsNullOrWhiteSpace(filePath) ? null : System.IO.Path.GetDirectoryName(filePath);
        }

        private static bool IsExpectedIoFailure(Exception ex)
        {
            return ex is IOException
                or UnauthorizedAccessException
                or NotSupportedException
                or ArgumentException;
        }

        private static string ComputeHash(string text)
        {
            unchecked
            {
                const uint offset = 2166136261;
                const uint prime = 16777619;
                var hash = offset;
                foreach (var ch in text)
                {
                    hash ^= ch;
                    hash *= prime;
                }

                return hash.ToString("x8", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private static string GetDefaultSmokeMarkdown()
        {
            return "# Typedown WinUI Editor Host\n\n"
                + "This smoke document proves the WinUI editor host can open and edit markdown.\n\n"
                + "- Bridge invoke handlers respond through a contract-backed local session.\n"
                + "- `LoadFile` comes from the session document state after navigation.\n"
                + "- Real `Typedown.Core` document/save/export integration remains later work.\n";
        }
    }
}
