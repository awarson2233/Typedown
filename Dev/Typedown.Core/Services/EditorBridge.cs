using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using Typedown.Core.Interfaces;
using Typedown.Services;

namespace Typedown.Core.Services
{
    public class EditorBridge : IEditorBridge
    {
        private readonly RemoteInvoke remoteInvoke;

        private readonly Transport transport;

        private readonly Func<string, bool> sender;

        public EditorBridge(RemoteInvoke remoteInvoke, Transport transport, Func<string, bool> sender)
        {
            this.remoteInvoke = remoteInvoke ?? throw new ArgumentNullException(nameof(remoteInvoke));
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }

        public bool Send(string name, object args)
        {
            var payload = JsonConvert.SerializeObject(new { name, args }, Core.Config.EditorJsonSerializerSettings);
            return sender(payload);
        }

        public async Task ReceiveAsync(string json)
        {
            EditorMessage? msg;
            try
            {
                msg = JsonConvert.DeserializeObject<EditorMessage>(json, Core.Config.EditorJsonSerializerSettings);
            }
            catch
            {
                return;
            }

            if (msg == null || string.IsNullOrWhiteSpace(msg.Type))
                return;

            try
            {
                switch (msg.Type)
                {
                    case "invoke":
                        await HandleInvokeAsync(msg);
                        break;
                    case "message":
                        transport.EmitMessage(msg.Name, msg.Args);
                        break;
                    case "diffmsg":
                        transport.EmitDiffMessage(msg.Name, msg.Args, msg.Diff, msg.Start, msg.End);
                        break;
                }
            }
            catch
            {
                // Malformed editor payloads must not bubble to WebView event handlers.
            }
        }

        private async Task HandleInvokeAsync(EditorMessage msg)
        {
            try
            {
                var ret = await remoteInvoke.Invoke(msg.Name, msg.Args);
                Send(msg.Id, new { code = 0, data = ret });
            }
            catch (Exception ex)
            {
                Send(msg.Id, new { code = 1, msg = ex.Message });
            }
        }

        private sealed class EditorMessage
        {
            public string Id { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;

            public JToken Args { get; set; } = JValue.CreateNull();

            public string Type { get; set; } = string.Empty;

            public bool Diff { get; set; }

            public int Start { get; set; }

            public int End { get; set; }
        }
    }
}
