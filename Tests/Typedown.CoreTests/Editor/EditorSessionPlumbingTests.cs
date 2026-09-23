using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Editor;
using Typedown.Core.Editor.Legacy;

namespace Typedown.CoreTests.Editor;

/// <summary>会话适配器里与 WinUI 无关的部件：就绪门、剪贴板合并、崩溃恢复策略。</summary>
[TestClass]
public sealed class EditorSessionPlumbingTests
{
    private static readonly EditorTheme Light = new(false, new EditorColor(1, 1, 1, 1), new EditorColor(2, 2, 2, 1));
    private static readonly EditorTheme Dark = new(true, new EditorColor(3, 3, 3, 1), new EditorColor(4, 4, 4, 1));

    // ── 就绪门 ───────────────────────────────────────────────────────

    [TestMethod]
    public void Gate_ReplaysLatestStateThenQueueInOrder()
    {
        var gate = new EditorCommandGate<string>();
        Assert.AreEqual(0, gate.Attach().Count);
        Assert.IsFalse(gate.Submit("m1"));
        Assert.IsFalse(gate.Submit(new ApplyTheme(Light)));
        Assert.IsFalse(gate.Submit(new ApplySettings(new EditorSettings { FontSize = 14, TabSize = 2 })));
        Assert.IsFalse(gate.Submit(new RefreshViewport()));
        Assert.IsFalse(gate.Submit(new SetKeymap([])));
        Assert.IsFalse(gate.Submit(new ApplyTheme(Dark)));
        Assert.IsFalse(gate.Submit(new ApplySettings(new EditorSettings { FontSize = 18 })));
        Assert.IsFalse(gate.Submit("m2"));

        var replay = gate.MarkReady();

        Assert.AreEqual(6, replay.Count);
        Assert.AreEqual(Dark, ((ApplyTheme)replay[0].Command!).Theme);
        Assert.IsInstanceOfType(replay[1].Command, typeof(SetKeymap));
        var settings = ((ApplySettings)replay[2].Command!).Changes;
        Assert.AreEqual((18d, 2), (settings.FontSize, settings.TabSize));
        Assert.IsInstanceOfType(replay[3].Command, typeof(RefreshViewport));
        CollectionAssert.AreEqual(new[] { "m1", "m2" }, replay.Skip(4).Select(x => x.Message).ToArray());
        Assert.IsTrue(gate.IsOpen);
    }

    [TestMethod]
    public void Gate_PassesThroughWhenOpenAndKeepsLatestThemeForReload()
    {
        var gate = new EditorCommandGate<string>();
        gate.Attach();
        gate.MarkReady();

        Assert.IsTrue(gate.Submit("m"));
        Assert.IsTrue(gate.Submit(new ApplySettings(new EditorSettings { FontSize = 1 })));
        Assert.IsTrue(gate.Submit(new ApplyTheme(Dark)));
        Assert.AreEqual(0, gate.QueuedCount);

        gate.MarkNotReady();
        Assert.IsFalse(gate.Submit("stale"));
        Assert.IsFalse(gate.Submit(new ApplySettings(new EditorSettings { FontSize = 2 })));
        gate.MarkNotReady();

        var replay = gate.MarkReady();
        Assert.AreEqual(1, replay.Count, "reload drops queued messages and settings but replays the theme");
        Assert.AreEqual(Dark, ((ApplyTheme)replay[0].Command!).Theme);
    }

    [TestMethod]
    public void Gate_DropsOldestWhenFull()
    {
        var gate = new EditorCommandGate<int>(capacity: 2);
        gate.Attach();
        gate.Submit(1);
        gate.Submit(2);
        gate.Submit(3);
        CollectionAssert.AreEqual(new[] { 2, 3 }, gate.MarkReady().Select(x => x.Message).ToArray());
    }

    [TestMethod]
    public void Gate_RetainsSettingsWhileDetachedOnlyWhenAsked()
    {
        var dropping = new EditorCommandGate<string>();
        var retaining = new EditorCommandGate<string>(retainWhileDetached: true);
        foreach (var gate in new[] { dropping, retaining })
        {
            gate.Attach();
            gate.MarkReady();
            gate.Detach();
            gate.Submit(new ApplySettings(new EditorSettings { SourceCode = true }));
            gate.Submit("while detached");
        }

        Assert.AreEqual(0, dropping.Attach().Count);

        var replay = retaining.Attach();
        Assert.AreEqual(true, ((ApplySettings)replay[0].Command!).Changes.SourceCode);
        Assert.AreEqual("while detached", replay[1].Message);
    }

    [TestMethod]
    public void Gate_StartupReplySupersedesPendingSettings()
    {
        var gate = new EditorCommandGate<string>();
        gate.Attach();
        gate.Submit(new ApplySettings(new EditorSettings { FontSize = 20 }));
        gate.ClearPendingSettings();
        Assert.AreEqual(0, gate.MarkReady().Count);
    }

    [TestMethod]
    public void Gate_RejectsNonRetainedCommands()
    {
        var gate = new EditorCommandGate<string>();
        Assert.ThrowsException<ArgumentException>(() => gate.Submit(new SelectAll()));
    }

    [TestMethod]
    public void EditorSettings_MergeKeepsNewerValues()
    {
        var merged = new EditorSettings { FontSize = 1, TabSize = 2 }.Merge(new EditorSettings { FontSize = 3, EditorAreaWidth = "50%" });
        Assert.AreEqual(new EditorSettings { FontSize = 3, TabSize = 2, EditorAreaWidth = "50%" }, merged);
    }

    // ── 剪贴板合并 ───────────────────────────────────────────────────

    [TestMethod]
    public void Clipboard_PairsHtmlWithFollowingPlainText()
    {
        var coalescer = new LegacyClipboardCoalescer();
        Assert.AreEqual(0, coalescer.Accept(LegacyClipboardCoalescer.HtmlType, "<b>x</b>").Count);
        Assert.IsTrue(coalescer.HasPending);

        var written = coalescer.Accept(LegacyClipboardCoalescer.PlainTextType, "x");
        CollectionAssert.AreEqual(new[] { new ClipboardContent("x", "<b>x</b>") }, written.ToArray());
        Assert.IsFalse(coalescer.HasPending);
    }

    [TestMethod]
    public void Clipboard_DropsEmptyHtmlPlaceholder()
    {
        var coalescer = new LegacyClipboardCoalescer();
        coalescer.Accept(LegacyClipboardCoalescer.HtmlType, string.Empty);
        CollectionAssert.AreEqual(new[] { new ClipboardContent("# md", null) }, coalescer.Accept(LegacyClipboardCoalescer.PlainTextType, "# md").ToArray());
    }

    [TestMethod]
    public void Clipboard_FlushesUnpairedHtml()
    {
        var coalescer = new LegacyClipboardCoalescer();
        coalescer.Accept(LegacyClipboardCoalescer.HtmlType, "<i>1</i>");
        CollectionAssert.AreEqual(new[] { new ClipboardContent(null, "<i>1</i>") }, coalescer.Accept(LegacyClipboardCoalescer.HtmlType, "<i>2</i>").ToArray());
        CollectionAssert.AreEqual(new[] { new ClipboardContent(null, "<i>2</i>") }, coalescer.Flush().ToArray());
        Assert.AreEqual(0, coalescer.Flush().Count);
        Assert.AreEqual(0, coalescer.Accept("image/png", "x").Count);
    }

    // ── 崩溃恢复 ─────────────────────────────────────────────────────

    [TestMethod]
    public void CrashRecovery_QuarantinesCursorAndStopsReloadLoops()
    {
        var clock = new ManualTimeProvider();
        var recovery = new EditorCrashRecovery(clock);

        Assert.AreEqual(new EditorCrashDecision(true, true), recovery.OnCrash());
        clock.Advance(TimeSpan.FromSeconds(5));
        Assert.AreEqual(new EditorCrashDecision(true, false), recovery.OnCrash(), "second crash soon after: reload without the cursor");
        clock.Advance(TimeSpan.FromSeconds(20));
        Assert.AreEqual(new EditorCrashDecision(true, true), recovery.OnCrash());
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.AreEqual(new EditorCrashDecision(false, false), recovery.OnCrash(), "fourth crash within a minute: stop reloading");

        clock.Advance(TimeSpan.FromMinutes(2));
        Assert.AreEqual(new EditorCrashDecision(true, true), recovery.OnCrash());
    }
}
