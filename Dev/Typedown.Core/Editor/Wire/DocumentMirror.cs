using System;
using System.Collections.Generic;
using System.Text;

namespace Typedown.Core.Editor.Wire
{
    /// <summary><see cref="DocumentMirror"/> 处理一条报文后的结果，决定会话下一步做什么。</summary>
    public enum DocumentMirrorOutcome
    {
        /// <summary>镜像前进到了新版本，会话发 <see cref="DocumentChanged"/>。</summary>
        Applied,

        /// <summary>过期报文（早于镜像版本或早于最近一次装载），已丢弃。</summary>
        Stale,

        /// <summary>重同步进行中，报文已暂存，等 <c>doc.getText</c> 的应答。</summary>
        Buffered,

        /// <summary>镜像与页面失步，会话发 <c>doc.getText</c>，应答交给 <see cref="DocumentMirror.CompleteResync"/>。</summary>
        ResyncRequired,
    }

    /// <summary>
    /// 宿主侧的正文镜像：一份正文与它的版本号。页面每帧最多发一批 <c>doc.changed</c> 增量，版本连续时就地应用；
    /// 早于镜像的报文丢弃；版本跳号或区间越界说明失步，改由 <c>doc.getText</c> 整体重同步，期间到达的增量暂存，
    /// 拿到应答后只应用版本更新的那些。非线程安全，由会话在 UI 线程上调用。
    /// </summary>
    public sealed class DocumentMirror
    {
        /// <summary>
        /// 装载版本号比此前见过的最大版本号大出的量。页面在收到 <c>doc.load</c> 之前可能还有增量在途，
        /// 宿主看不到它们的版本号；留出这段空隙，在途的旧增量不可能追上新的装载版本，按版本比较就能认出并丢弃。
        /// </summary>
        public const long LoadVersionGap = 1L << 20;

        private readonly List<DocChanged> buffered = new();
        private long highestVersion;

        public EditorDocument Document { get; private set; } = EditorDocument.Empty;

        /// <summary>最近一次 <c>doc.load</c> 的版本号，<c>doc.rendered</c> 按它认出过期回声。</summary>
        public long LoadedVersion { get; private set; }

        /// <summary>是否在等 <c>doc.getText</c> 的应答。</summary>
        public bool IsResyncing { get; private set; }

        /// <summary>
        /// 整篇装载：分配严格大于此前任何版本号的新版本，覆盖镜像并放弃进行中的重同步，返回要发给页面的 <c>doc.load</c> 载荷。
        /// </summary>
        public DocLoad Load(string text, string basePath, DocSelection? selection = null, double? scrollTop = null)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(basePath);
            var version = checked(highestVersion + LoadVersionGap);
            if (version > EditorWireCodec.MaxSafeInteger)
            {
                throw new InvalidOperationException("The document version space is exhausted.");
            }

            highestVersion = version;
            LoadedVersion = version;
            Document = new EditorDocument(text, version);
            AbandonResync();
            return new DocLoad(version, text, basePath, selection, scrollTop);
        }

        /// <summary><c>doc.rendered</c> 的版本号是否对应最近一次装载；不是就是过期的回声。</summary>
        public bool IsCurrentLoad(long version) => version == LoadedVersion;

        /// <summary>处理一条 <c>doc.changed</c>。</summary>
        public DocumentMirrorOutcome Apply(DocChanged changed)
        {
            ArgumentNullException.ThrowIfNull(changed);
            if (changed.BaseVersion < LoadedVersion)
            {
                // 装载之前页面发出的增量，版本号必然小于装载版本。
                return DocumentMirrorOutcome.Stale;
            }

            Observe(changed.Version);
            if (IsResyncing)
            {
                buffered.Add(changed);
                return DocumentMirrorOutcome.Buffered;
            }

            return ApplyCore(changed);
        }

        /// <summary>
        /// 用 <c>doc.getText</c> 的应答覆盖镜像，再依次应用暂存的增量。暂存里又出现跳号时返回
        /// <see cref="DocumentMirrorOutcome.ResyncRequired"/>，剩下的继续暂存；不在重同步中或应答早于最近一次装载时返回
        /// <see cref="DocumentMirrorOutcome.Stale"/>。
        /// </summary>
        public DocumentMirrorOutcome CompleteResync(DocText snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            if (!IsResyncing || snapshot.Version < LoadedVersion)
            {
                return DocumentMirrorOutcome.Stale;
            }

            Observe(snapshot.Version);
            IsResyncing = false;
            Document = new EditorDocument(snapshot.Text, snapshot.Version);
            var pending = buffered.ToArray();
            buffered.Clear();
            for (var i = 0; i < pending.Length; i++)
            {
                if (ApplyCore(pending[i]) == DocumentMirrorOutcome.ResyncRequired)
                {
                    buffered.AddRange(pending.AsSpan(i + 1));
                    return DocumentMirrorOutcome.ResyncRequired;
                }
            }

            return DocumentMirrorOutcome.Applied;
        }

        /// <summary>重同步请求失败（超时、页面重载）：丢掉暂存，下一条对不上的增量会再次触发重同步。</summary>
        public void AbandonResync()
        {
            IsResyncing = false;
            buffered.Clear();
        }

        private DocumentMirrorOutcome ApplyCore(DocChanged changed)
        {
            var current = Document;
            if (changed.BaseVersion < current.Version)
            {
                return DocumentMirrorOutcome.Stale;
            }

            if (changed.BaseVersion > current.Version
                || changed.Version != changed.BaseVersion + 1
                || !TryApply(current.Text, changed.Changes, out var text))
            {
                IsResyncing = true;
                return DocumentMirrorOutcome.ResyncRequired;
            }

            Document = new EditorDocument(text, changed.Version);
            return DocumentMirrorOutcome.Applied;
        }

        /// <summary>按 from 升序一次拼出新正文；区间逆序、重叠或越界时返回 <c>false</c>。</summary>
        private static bool TryApply(string text, IReadOnlyList<DocChange> changes, out string result)
        {
            result = text;
            if (changes.Count == 0)
            {
                return true;
            }

            var capacity = (long)text.Length;
            var position = 0;
            foreach (var change in changes)
            {
                if (change.From < position || change.To < change.From || change.To > text.Length || change.Insert is null)
                {
                    return false;
                }

                capacity += change.Insert.Length - (change.To - change.From);
                position = change.To;
            }

            var builder = new StringBuilder((int)Math.Min(capacity, int.MaxValue));
            position = 0;
            foreach (var change in changes)
            {
                builder.Append(text, position, change.From - position).Append(change.Insert);
                position = change.To;
            }

            result = builder.Append(text, position, text.Length - position).ToString();
            return true;
        }

        private void Observe(long version) => highestVersion = Math.Max(highestVersion, version);
    }
}
