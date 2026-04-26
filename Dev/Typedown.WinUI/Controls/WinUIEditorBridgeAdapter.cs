using System;
using System.Collections.Generic;
using System.Text.Json;
using Typedown.Core.Contracts.Editor;

namespace Typedown.WinUI.Controls
{
    internal sealed class WinUIEditorBridgeAdapter
    {
        private static readonly HashSet<string> SessionBackedEvents = new(StringComparer.Ordinal)
        {
            "FileLoaded",
            "MarkdownChange",
            "CursorChange",
            "StateChange"
        };

        private readonly Dictionary<string, string> diffCache = new(StringComparer.Ordinal);
        private readonly IEditorDocumentSession documentSession;

        public WinUIEditorBridgeAdapter(IEditorDocumentSession documentSession)
        {
            this.documentSession = documentSession ?? throw new ArgumentNullException(nameof(documentSession));
            LastEventName = "Waiting";
            LastRawMessage = "No editor message received yet.";
        }

        public bool IsContentLoaded { get; private set; }

        public bool IsFileLoaded => documentSession.State.IsLoaded;

        public int CurrentMarkdownLength => documentSession.State.Text.Length;

        public string LastEventName { get; private set; }

        public string LastRawMessage { get; private set; }

        public string StatusText =>
            $"Loaded={IsContentLoaded}; FileLoaded={IsFileLoaded}; MarkdownLength={CurrentMarkdownLength}; LastEvent={LastEventName}";

        public void ResetForNavigation()
        {
            IsContentLoaded = false;
            LastEventName = "Waiting";
            LastRawMessage = "No editor message received yet.";
            diffCache.Clear();
        }

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
                var data = documentSession.HandleRemoteInvoke(name, args);
                if (string.Equals(name, "ContentLoaded", StringComparison.Ordinal))
                {
                    IsContentLoaded = true;
                }

                Send(sender, id, new { code = 0, data });
            }
            catch (Exception ex)
            {
                Send(sender, id, new { code = 1, msg = ex.Message });
            }
        }

        private void HandleMessage(JsonElement root)
        {
            var message = TryCreateEventMessage(root, "MalformedMessage");
            if (message is null)
            {
                return;
            }

            HandleEditorEvent(message);
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
                HandleEditorEvent(new EditorEventMessage(name, argsDocument.RootElement.Clone()));
            }
            catch
            {
                LastEventName = name;
            }
        }

        private EditorEventMessage? TryCreateEventMessage(JsonElement root, string malformedName)
        {
            var name = root.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                ? nameElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(name))
            {
                LastEventName = malformedName;
                return null;
            }

            var args = root.TryGetProperty("args", out var argsElement) ? argsElement.Clone() : (JsonElement?)null;
            return new EditorEventMessage(name, args);
        }

        private void HandleEditorEvent(EditorEventMessage message)
        {
            if (SessionBackedEvents.Contains(message.Name))
            {
                documentSession.HandleEditorEvent(message);
            }

            LastEventName = message.Name;
        }

        private static void Send(Func<string, bool> sender, string name, object args)
        {
            var payload = JsonSerializer.Serialize(new { name, args });
            _ = sender(payload);
        }
    }
}
