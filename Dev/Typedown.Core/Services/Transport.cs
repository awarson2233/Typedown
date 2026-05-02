using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Typedown.Core.Models;
using Typedown.Core.Services;

namespace Typedown.Services
{
    public class Transport
    {
        private readonly Dictionary<string, string> prevDic = new();

        public EventCenter EventCenter { get; }

        public Transport(EventCenter eventCenter)
        {
            EventCenter = eventCenter;
        }

        public void EmitMessage(string name, JToken args)
        {
            EventCenter.EmitEvent(name, new EditorEventArgs(name, args));
        }

        public void EmitDiffMessage(string name, JToken args, bool diff, int start, int end)
        {
            if (diff && prevDic.TryGetValue(name, out var prev))
                prevDic[name] = prev.Substring(0, start) + args + prev.Substring(end);
            else
                prevDic[name] = args?.ToString() ?? "null";
            EventCenter.EmitEvent(name, new EditorEventArgs(name, JToken.Parse(prevDic[name])));
        }
    }
}
