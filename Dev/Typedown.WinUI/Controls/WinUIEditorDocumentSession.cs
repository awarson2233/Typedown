using System;
using System.Collections.Generic;
using System.Text.Json;
using Typedown.Core.Contracts.Editor;

namespace Typedown.WinUI.Controls
{
    internal sealed class WinUIEditorDocumentSession : IEditorDocumentSession
    {
        public WinUIEditorDocumentSession(string? initialMarkdown = null, string? basePath = null, string? filePath = null)
        {
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

        public void HandleEditorEvent(EditorEventMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            switch (message.Name)
            {
                case "FileLoaded":
                    ApplyLoadedState(message.Args);
                    break;
                case "MarkdownChange":
                    ApplyMarkdownChange(message.Args);
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

        public object? HandleRemoteInvoke(string name, JsonElement? args)
        {
            return name switch
            {
                "GetCurrentTheme" => CreateThemePayload(),
                "ContentLoaded" => HandleContentLoaded(),
                "ExportCallback" => HandleStubInvoke(name),
                "PrintHTML" => HandleStubInvoke(name),
                "ResizeTable" => CreateResizeTablePayload(args),
                "LoadImage" => CreateLoadImagePayload(args),
                "GetSettings" => SettingsSnapshot.Payload,
                "SetClipboard" => HandleStubInvoke(name),
                "GetStringResources" => CreateStringResources(args),
                "OpenNewWindow" => HandleStubInvoke(name),
                "UnhandledException" => HandleUnhandledException(),
                _ => throw new InvalidOperationException($"function [{name}] does not exist")
            };
        }

        public EditorHostMessage CreateLoadFileMessage()
        {
            return new EditorHostMessage("LoadFile", new
            {
                text = State.Text,
                basePath = State.BasePath
            });
        }

        private string HandleContentLoaded()
        {
            State = State with { LastEventName = "ContentLoaded" };
            return "WinUI editor content loaded.";
        }

        private bool HandleStubInvoke(string name)
        {
            State = State with { LastEventName = name };
            return true;
        }

        private object? HandleUnhandledException()
        {
            State = State with { LastEventName = "UnhandledException" };
            return null;
        }

        private void ApplyLoadedState(JsonElement? args)
        {
            var text = ReadStringProperty(args, "text") ?? string.Empty;
            var filePath = ReadStringProperty(args, "filePath");
            var basePath = ReadStringProperty(args, "basePath")
                ?? InferBasePath(filePath)
                ?? State.BasePath;
            var hash = ComputeHash(text);

            State = State with
            {
                Text = text,
                FilePath = filePath ?? State.FilePath,
                BasePath = basePath,
                FileHash = hash,
                CurrentHash = hash,
                IsLoaded = true,
                IsSaved = true,
                LastEventName = "FileLoaded"
            };

            SettingsSnapshot = SettingsSnapshot with
            {
                Payload = SettingsSnapshot.Payload with
                {
                    Markdown = text,
                    BasePath = basePath
                }
            };
        }

        private void ApplyMarkdownChange(JsonElement? args)
        {
            var text = ReadStringProperty(args, "text") ?? State.Text;
            var currentHash = ComputeHash(text);

            State = State with
            {
                Text = text,
                CurrentHash = currentHash,
                IsSaved = string.Equals(currentHash, State.FileHash, StringComparison.Ordinal),
                LastEventName = "MarkdownChange"
            };

            SettingsSnapshot = SettingsSnapshot with
            {
                Payload = SettingsSnapshot.Payload with
                {
                    Markdown = text,
                    BasePath = State.BasePath
                }
            };
        }

        private static object CreateThemePayload()
        {
            return new
            {
                theme = "Light",
                accentColor = new { r = 27, g = 102, b = 107, a = 1 },
                background = new { R = 249, G = 249, B = 249, A = 1 }
            };
        }

        private static Dictionary<string, string> CreateStringResources(JsonElement? args)
        {
            var resources = new Dictionary<string, string>(StringComparer.Ordinal);

            if (args is not JsonElement element || element.ValueKind != JsonValueKind.Object)
            {
                return resources;
            }

            if (!element.TryGetProperty("names", out var namesElement) || namesElement.ValueKind != JsonValueKind.Array)
            {
                return resources;
            }

            foreach (var entry in namesElement.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var key = entry.GetString();
                if (!string.IsNullOrEmpty(key))
                {
                    resources[key] = key;
                }
            }

            return resources;
        }

        private static object CreateResizeTablePayload(JsonElement? args)
        {
            var row = ReadIntProperty(args, "row");
            var column = ReadIntProperty(args, "column");
            var rows = ReadIntProperty(args, "rows");
            var columns = ReadIntProperty(args, "columns");

            return new
            {
                row = row ?? rows ?? 0,
                column = column ?? columns ?? 0,
                rows = rows ?? row ?? 0,
                columns = columns ?? column ?? 0
            };
        }

        private static object CreateLoadImagePayload(JsonElement? args)
        {
            return new
            {
                url = ReadStringProperty(args, "url") ?? string.Empty
            };
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

        private static string? InferBasePath(string? filePath)
        {
            return string.IsNullOrWhiteSpace(filePath) ? null : System.IO.Path.GetDirectoryName(filePath);
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
            return "# Typedown WinUI Phase 11\n\n"
                + "This smoke document proves the WinUI editor host can open and edit markdown.\n\n"
                + "- Bridge invoke handlers respond through a contract-backed local session.\n"
                + "- `LoadFile` comes from the session document state after navigation.\n"
                + "- Real `Typedown.Core` document/save/export integration remains later work.\n";
        }
    }
}
