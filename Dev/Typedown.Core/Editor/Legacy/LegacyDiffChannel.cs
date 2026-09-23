using System.Collections.Generic;
using System.Text.Json;

namespace Typedown.Core.Editor.Legacy
{
    /// <summary>
    /// 页面 diffmsg 的重组：页面对同名消息只发与上一次 <c>JSON.stringify</c> 结果的差异片段，
    /// 宿主按消息名保存上一份全文，把片段拼回去。页面重载后第一条总是全量（diff = false），状态随之重置。
    /// </summary>
    public sealed class LegacyDiffChannel
    {
        private readonly Dictionary<string, string> previous = new();

        /// <summary>拼出这条消息的完整 JSON 文本；片段不是字符串或区间越界时返回 <c>false</c>。</summary>
        public bool TryApply(string name, JsonElement args, bool diff, int start, int end, out string text)
        {
            text = string.Empty;
            var fragment = args.ValueKind switch
            {
                JsonValueKind.String => args.GetString(),
                JsonValueKind.Undefined or JsonValueKind.Null => null,
                _ => args.GetRawText(),
            };
            if (fragment is null)
            {
                return false;
            }

            if (diff && previous.TryGetValue(name, out var prev))
            {
                if (start < 0 || end < start || end > prev.Length)
                {
                    return false;
                }

                text = string.Concat(prev.AsSpan(0, start), fragment, prev.AsSpan(end));
            }
            else
            {
                text = fragment;
            }

            previous[name] = text;
            return true;
        }

        public void Reset() => previous.Clear();
    }
}
