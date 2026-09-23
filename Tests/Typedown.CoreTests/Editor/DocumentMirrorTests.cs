using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Editor;
using Typedown.Core.Editor.Wire;

namespace Typedown.CoreTests.Editor;

/// <summary>
/// 正文镜像：版本连续时应用增量，过期报文丢弃，失步时重同步并暂存后续报文。
/// 性质测试模拟一个会乱序、重复、丢失 doc.changed 的通道和一个随时被整篇装载打断的页面，最终镜像必须与页面正文一致。
/// </summary>
[TestClass]
public sealed class DocumentMirrorTests
{
    // ── 单步规则 ─────────────────────────────────────────────────────

    [TestMethod]
    public void Load_AssignsVersionsBeyondAnythingSeen()
    {
        var mirror = new DocumentMirror();
        var first = mirror.Load("a", "base");
        Assert.AreEqual(DocumentMirror.LoadVersionGap, first.Version);
        Assert.AreEqual(new EditorDocument("a", first.Version), mirror.Document);
        Assert.IsTrue(mirror.IsCurrentLoad(first.Version));

        Assert.AreEqual(DocumentMirrorOutcome.Applied, mirror.Apply(Changed(first.Version, (1, 1, "b"))));
        var second = mirror.Load("x", "base");
        Assert.AreEqual(first.Version + 1 + DocumentMirror.LoadVersionGap, second.Version);
        Assert.IsFalse(mirror.IsCurrentLoad(first.Version));
    }

    [TestMethod]
    public void Apply_ContiguousBatchSplicesAllChangesAgainstTheBaseText()
    {
        var mirror = new DocumentMirror();
        var load = mirror.Load("hello world", "");
        var outcome = mirror.Apply(Changed(load.Version, (0, 1, "H"), (5, 6, ",\n"), (11, 11, "!")));
        Assert.AreEqual(DocumentMirrorOutcome.Applied, outcome);
        Assert.AreEqual(new EditorDocument("Hello,\nworld!", load.Version + 1), mirror.Document);
    }

    [TestMethod]
    public void Apply_DropsStaleAndPreLoadMessages()
    {
        var mirror = new DocumentMirror();
        var load = mirror.Load("abc", "");
        mirror.Apply(Changed(load.Version, (0, 0, "x")));
        Assert.AreEqual(DocumentMirrorOutcome.Stale, mirror.Apply(Changed(load.Version, (0, 0, "y"))), "duplicate");
        Assert.AreEqual(DocumentMirrorOutcome.Stale, mirror.Apply(Changed(load.Version - 5, (0, 0, "z"))), "from before the load");
        Assert.AreEqual("xabc", mirror.Document.Text);
    }

    [DataTestMethod]
    [DataRow(1, 0, 0, DisplayName = "version gap")]
    [DataRow(0, 4, 4, DisplayName = "past the end")]
    [DataRow(0, 2, 1, DisplayName = "inverted range")]
    public void Apply_RequestsResyncWhenOutOfStep(int baseOffset, int from, int to)
    {
        var mirror = new DocumentMirror();
        var load = mirror.Load("abc", "");
        Assert.AreEqual(DocumentMirrorOutcome.ResyncRequired, mirror.Apply(Changed(load.Version + baseOffset, (from, to, "x"))));
        Assert.IsTrue(mirror.IsResyncing);
        Assert.AreEqual("abc", mirror.Document.Text);
    }

    [TestMethod]
    public void Apply_RequestsResyncForOverlappingOrUnorderedChanges()
    {
        var mirror = new DocumentMirror();
        var load = mirror.Load("abcdef", "");
        Assert.AreEqual(DocumentMirrorOutcome.ResyncRequired, mirror.Apply(Changed(load.Version, (3, 4, "x"), (1, 2, "y"))));
    }

    [TestMethod]
    public void Resync_BuffersThenReplaysOnlyNewerMessages()
    {
        var mirror = new DocumentMirror();
        var v = mirror.Load("abc", "").Version;
        Assert.AreEqual(DocumentMirrorOutcome.ResyncRequired, mirror.Apply(Changed(v + 1, (0, 0, "?"))));
        Assert.AreEqual(DocumentMirrorOutcome.Buffered, mirror.Apply(Changed(v + 2, (0, 0, "old"))));
        Assert.AreEqual(DocumentMirrorOutcome.Buffered, mirror.Apply(Changed(v + 3, (4, 4, "!"))));

        Assert.AreEqual(DocumentMirrorOutcome.Applied, mirror.CompleteResync(new DocText(v + 3, "zabc")));
        Assert.AreEqual(new EditorDocument("zabc!", v + 4), mirror.Document);
        Assert.IsFalse(mirror.IsResyncing);
        Assert.AreEqual(DocumentMirrorOutcome.Stale, mirror.CompleteResync(new DocText(v + 9, "late")), "a reply when not resyncing");
    }

    [TestMethod]
    public void Resync_ReportsAnotherGapInsideTheBuffer()
    {
        var mirror = new DocumentMirror();
        var v = mirror.Load("", "").Version;
        mirror.Apply(Changed(v + 1, (0, 0, "?")));
        mirror.Apply(Changed(v + 5, (0, 0, "far")));
        mirror.Apply(Changed(v + 6, (0, 0, "after")));
        Assert.AreEqual(DocumentMirrorOutcome.ResyncRequired, mirror.CompleteResync(new DocText(v + 2, "ab")));
        Assert.IsTrue(mirror.IsResyncing);
        Assert.AreEqual(DocumentMirrorOutcome.Applied, mirror.CompleteResync(new DocText(v + 7, "afterfarab")));
        Assert.AreEqual(new EditorDocument("afterfarab", v + 7), mirror.Document, "the leftover buffered message is stale now");
    }

    [TestMethod]
    public void Load_AbandonsResyncAndIgnoresItsReply()
    {
        var mirror = new DocumentMirror();
        var v = mirror.Load("a", "").Version;
        mirror.Apply(Changed(v + 3, (0, 0, "x")));
        var reload = mirror.Load("fresh", "");
        Assert.IsFalse(mirror.IsResyncing);
        Assert.AreEqual(DocumentMirrorOutcome.Stale, mirror.CompleteResync(new DocText(v + 3, "old")));
        Assert.AreEqual(new EditorDocument("fresh", reload.Version), mirror.Document);
    }

    // ── 性质测试 ─────────────────────────────────────────────────────

    [TestMethod]
    public void RandomTraffic_MirrorConvergesToThePage()
    {
        for (var seed = 1; seed <= 300; seed++)
        {
            RunSimulation(seed, steps: 400);
        }
    }

    private static void RunSimulation(int seed, int steps)
    {
        var random = new Random(seed);
        var mirror = new DocumentMirror();
        var page = new FakePage();
        var toPage = new List<object>();
        var toHost = new List<object>();

        page.Receive(mirror.Load(RandomText(random, 20), ""), toHost);

        void DeliverToHost(object message)
        {
            switch (message)
            {
                case DocChanged changed when mirror.Apply(changed) == DocumentMirrorOutcome.ResyncRequired:
                case DocText text when mirror.CompleteResync(text) == DocumentMirrorOutcome.ResyncRequired:
                    toPage.Add(new DocGetText());
                    break;
            }
        }

        for (var step = 0; step < steps; step++)
        {
            var roll = random.Next(100);
            if (roll < 45)
            {
                toHost.Add(page.Edit(random));
            }
            else if (roll < 50)
            {
                toPage.Add(mirror.Load(RandomText(random, 30), ""));
            }
            else if (roll < 65 && toPage.Count > 0)
            {
                page.Receive(TakeFirst(toPage), toHost);
            }
            else if (toHost.Count > 0)
            {
                // 通道故障：偶尔丢掉、重复或提前递送一条 doc.changed；应答不会丢。
                var index = toHost.Count > 1 && random.Next(10) == 0 ? 1 : 0;
                var message = toHost[index];
                toHost.RemoveAt(index);
                var fault = random.Next(20);
                if (message is DocChanged && fault == 0)
                {
                    continue;
                }

                DeliverToHost(message);
                if (message is DocChanged && fault == 1)
                {
                    DeliverToHost(message);
                }
            }
        }

        // 收尾：故障停止，页面再改一次，两个方向都送空。
        toHost.Add(page.Edit(random));
        while (toHost.Count > 0 || toPage.Count > 0)
        {
            if (toPage.Count > 0)
            {
                page.Receive(TakeFirst(toPage), toHost);
            }
            else
            {
                DeliverToHost(TakeFirst(toHost));
            }
        }

        Assert.IsFalse(mirror.IsResyncing, $"seed {seed}");
        Assert.AreEqual(page.Version, mirror.Document.Version, $"seed {seed}");
        Assert.AreEqual(page.Text, mirror.Document.Text, $"seed {seed}");
    }

    private static object TakeFirst(List<object> queue)
    {
        var first = queue[0];
        queue.RemoveAt(0);
        return first;
    }

    /// <summary>页面：持有真相正文与版本号，按协议处理 doc.load 与 doc.getText，每次编辑产生一批增量。</summary>
    private sealed class FakePage
    {
        public string Text { get; private set; } = string.Empty;

        public long Version { get; private set; }

        public void Receive(object message, List<object> toHost)
        {
            switch (message)
            {
                case DocLoad load:
                    Text = load.Text;
                    Version = load.Version;
                    break;
                case DocGetText:
                    toHost.Add(new DocText(Version, Text));
                    break;
            }
        }

        public DocChanged Edit(Random random)
        {
            var count = random.Next(4);
            var cuts = Enumerable.Range(0, count * 2).Select(_ => random.Next(Text.Length + 1)).Order().ToArray();
            var changes = new List<DocChange>();
            for (var i = 0; i < count; i++)
            {
                changes.Add(new DocChange(cuts[2 * i], cuts[2 * i + 1], RandomText(random, 4)));
            }

            var builder = new StringBuilder();
            var position = 0;
            foreach (var change in changes)
            {
                builder.Append(Text, position, change.From - position).Append(change.Insert);
                position = change.To;
            }

            Text = builder.Append(Text, position, Text.Length - position).ToString();
            var baseVersion = Version++;
            return new DocChanged(baseVersion, Version, changes);
        }
    }

    private static string RandomText(Random random, int maxLength)
    {
        const string alphabet = "ab \n#*`中文😀";
        var length = random.Next(maxLength + 1);
        var builder = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            builder.Append(alphabet[random.Next(alphabet.Length)]);
        }

        return builder.ToString();
    }

    private static DocChanged Changed(long baseVersion, params (int From, int To, string Insert)[] changes) =>
        new(baseVersion, baseVersion + 1, changes.Select(x => new DocChange(x.From, x.To, x.Insert)).ToList());
}
