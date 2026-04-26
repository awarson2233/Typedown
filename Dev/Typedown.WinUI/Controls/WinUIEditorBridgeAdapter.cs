using System.Text;
using System.Text.Json;

namespace Typedown.WinUI.Controls
{
    internal sealed class WinUIEditorBridgeAdapter
    {
        private const string DefaultBasePath = "D:\\source\\repos\\Typedown";

        private readonly Dictionary<string, Func<JsonElement?, object?>> invokeHandlers;
        private readonly Dictionary<string, string> diffCache = new(StringComparer.Ordinal);
        private readonly string smokeMarkdown;
        private readonly string basePath;

        public WinUIEditorBridgeAdapter(string? smokeMarkdown = null, string? basePath = null)
        {
            this.smokeMarkdown = string.IsNullOrWhiteSpace(smokeMarkdown) ? GetDefaultSmokeMarkdown() : smokeMarkdown;
            this.basePath = string.IsNullOrWhiteSpace(basePath) ? DefaultBasePath : basePath;

            invokeHandlers = new Dictionary<string, Func<JsonElement?, object?>>(StringComparer.Ordinal)
            {
                ["GetCurrentTheme"] = _ => CreateThemePayload(),
                ["ContentLoaded"] = _ =>
                {
                    IsContentLoaded = true;
                    LastEventName = "ContentLoaded";
                    return "WinUI editor content loaded.";
                },
                ["GetStringResources"] = args => CreateStringResources(args),
                ["GetSettings"] = _ => CreateSettingsPayload()
            };

            LastEventName = "Waiting";
            LastRawMessage = "No editor message received yet.";
        }

        public bool IsContentLoaded { get; private set; }

        public bool IsFileLoaded { get; private set; }

        public int CurrentMarkdownLength { get; private set; }

        public string LastEventName { get; private set; }

        public string LastRawMessage { get; private set; }

        public string SmokeMarkdown => smokeMarkdown;

        public string BasePath => basePath;

        public string StatusText =>
            $"Loaded={IsContentLoaded}; FileLoaded={IsFileLoaded}; MarkdownLength={CurrentMarkdownLength}; LastEvent={LastEventName}";

        public void Receive(string? rawMessage, Func<string, bool> sender)
        {
            LastRawMessage = rawMessage ?? string.Empty;

            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                LastEventName = "EmptyPayload";
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(rawMessage);
                var root = document.RootElement;

                if (!root.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
                {
                    LastEventName = "UnknownPayload";
                    return;
                }

                var type = typeElement.GetString();
                switch (type)
                {
                    case "invoke":
                        HandleInvoke(root, sender);
                        break;
                    case "message":
                        HandleMessage(root);
                        break;
                    case "diffmsg":
                        HandleDiffMessage(root);
                        break;
                    default:
                        LastEventName = type ?? "UnknownPayload";
                        break;
                }
            }
            catch
            {
                LastEventName = "MalformedPayload";
            }
        }

        private void HandleInvoke(JsonElement root, Func<string, bool> sender)
        {
            var id = root.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.String
                ? idElement.GetString()
                : null;
            var name = root.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                ? nameElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            {
                LastEventName = "MalformedInvoke";
                return;
            }

            LastEventName = name;
            var args = root.TryGetProperty("args", out var argsElement) ? argsElement.Clone() : (JsonElement?)null;

            try
            {
                if (!invokeHandlers.TryGetValue(name, out var handler))
                {
                    Send(sender, id, new { code = 1, msg = $"function [{name}] does not exist" });
                    return;
                }

                var data = handler(args);
                Send(sender, id, new { code = 0, data });
            }
            catch (Exception ex)
            {
                Send(sender, id, new { code = 1, msg = ex.Message });
            }
        }

        private void HandleMessage(JsonElement root)
        {
            var name = root.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                ? nameElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(name))
            {
                LastEventName = "MalformedMessage";
                return;
            }

            var args = root.TryGetProperty("args", out var argsElement) ? argsElement.Clone() : (JsonElement?)null;
            UpdateEventState(name, args);
        }

        private void HandleDiffMessage(JsonElement root)
        {
            var name = root.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                ? nameElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(name))
            {
                LastEventName = "MalformedDiffMessage";
                return;
            }

            if (!root.TryGetProperty("args", out var argsElement) || argsElement.ValueKind != JsonValueKind.String)
            {
                LastEventName = name;
                return;
            }

            var isDiff = root.TryGetProperty("diff", out var diffElement) && diffElement.ValueKind == JsonValueKind.True;
            var serializedArgs = argsElement.GetString() ?? "null";

            if (!isDiff)
            {
                diffCache[name] = serializedArgs;
            }
            else
            {
                if (!diffCache.TryGetValue(name, out var previous))
                {
                    LastEventName = name;
                    return;
                }

                var start = root.TryGetProperty("start", out var startElement) && startElement.TryGetInt32(out var parsedStart)
                    ? parsedStart
                    : -1;
                var end = root.TryGetProperty("end", out var endElement) && endElement.TryGetInt32(out var parsedEnd)
                    ? parsedEnd
                    : -1;

                if (start < 0 || end < start || end > previous.Length)
                {
                    LastEventName = name;
                    return;
                }

                diffCache[name] = previous[..start] + serializedArgs + previous[end..];
            }

            try
            {
                using var argsDocument = JsonDocument.Parse(diffCache[name]);
                UpdateEventState(name, argsDocument.RootElement.Clone());
            }
            catch
            {
                LastEventName = name;
            }
        }

        private void UpdateEventState(string name, JsonElement? args)
        {
            LastEventName = name;

            switch (name)
            {
                case "FileLoaded":
                    IsFileLoaded = true;
                    CurrentMarkdownLength = ReadMarkdownLength(args);
                    break;
                case "MarkdownChange":
                    CurrentMarkdownLength = ReadMarkdownLength(args);
                    break;
                case "CursorChange":
                case "StateChange":
                    break;
            }
        }

        private int ReadMarkdownLength(JsonElement? args)
        {
            if (args is not JsonElement element || element.ValueKind != JsonValueKind.Object)
            {
                return CurrentMarkdownLength;
            }

            if (!element.TryGetProperty("text", out var textElement) || textElement.ValueKind != JsonValueKind.String)
            {
                return CurrentMarkdownLength;
            }

            return textElement.GetString()?.Length ?? 0;
        }

        private object CreateSettingsPayload()
        {
            return new
            {
                markdown = smokeMarkdown,
                basePath,
                sourceCode = false,
                fontSize = 16,
                lineHeight = 1.6,
                tabSize = 4,
                focusMode = false,
                typewriter = false,
                trimUnnecessaryCodeBlockEmptyLines = false,
                preferLooseListItem = true,
                autoPairBracket = true,
                autoPairMarkdownSyntax = true,
                autoPairQuote = true,
                bulletListMarker = "-",
                orderListDelimiter = ".",
                codeBlockLineNumbers = false,
                listIndentation = 1,
                frontmatterType = "-",
                sequenceTheme = "simple",
                mermaidTheme = "default",
                vegaTheme = "latimes",
                hideQuickInsertHint = false,
                hideLinkPopup = false,
                autoCheck = false,
                spellcheckEnabled = false,
                superSubScript = false,
                footnote = true,
                isGitlabCompatibilityEnabled = false,
                disableHtml = false,
                editorAreaWidth = "880px"
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

        private static void Send(Func<string, bool> sender, string name, object args)
        {
            var payload = JsonSerializer.Serialize(new { name, args });
            _ = sender(payload);
        }

        private static string GetDefaultSmokeMarkdown()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Typedown WinUI Phase 11");
            builder.AppendLine();
            builder.AppendLine("This smoke document proves the WinUI editor host can open and edit markdown.");
            builder.AppendLine();
            builder.AppendLine("- Bridge invoke handlers respond locally inside `Typedown.WinUI`.");
            builder.AppendLine("- `LoadFile` provides an explicit open-document path after navigation.");
            builder.AppendLine("- Real `Typedown.Core` document/save/export integration remains later work.");
            return builder.ToString();
        }
    }
}
