using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Models;

namespace Typedown.CoreTests.Editor;

/// <summary>适配器侧撤销历史的语义：快照、延迟提交、光标换行提交、撤销重做与通知。</summary>
[TestClass]
public sealed class ContentHistoryTests
{
    private static CursorState At(int line, int ch = 0) => new(new(line, ch), new(line, ch));

    [TestMethod]
    public void InitHistory_IsNotUndoable()
    {
        using var history = new ContentHistory(new ManualTimeProvider());
        history.InitHistory("a\n");
        Assert.IsFalse(history.Undoable);
        Assert.IsFalse(history.Redoable);
        Assert.IsNull(history.Undo());
    }

    [TestMethod]
    public void PendingEdit_BecomesUndoableOnceItHasACursor()
    {
        using var history = new ContentHistory(new ManualTimeProvider());
        history.InitHistory("a");
        history.ContentChange("ab");
        Assert.IsFalse(history.Undoable);

        history.CursorChange(At(0, 2));
        Assert.IsTrue(history.Undoable);
        Assert.IsTrue(history.IsPending);

        Assert.AreEqual("a", history.Undo()?.Text);
        Assert.IsTrue(history.Redoable);
        Assert.IsFalse(history.Undoable);

        Assert.AreEqual("ab", history.Redo()?.Text);
        Assert.IsFalse(history.Redoable);
        Assert.IsTrue(history.Undoable);
    }

    [TestMethod]
    public void PendingEdit_CommitsAfterTheDelay()
    {
        var clock = new ManualTimeProvider();
        using var history = new ContentHistory(clock);
        history.InitHistory("a");
        history.ContentChange("ab");
        history.CursorChange(At(0, 2));

        clock.Advance(ContentHistory.CommitDelay - TimeSpan.FromMilliseconds(1));
        Assert.IsTrue(history.IsPending);

        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.IsFalse(history.IsPending);
        Assert.IsTrue(history.Undoable);
    }

    [TestMethod]
    public void EachEdit_RestartsTheCommitDelay()
    {
        var clock = new ManualTimeProvider();
        using var history = new ContentHistory(clock);
        history.InitHistory("a");
        history.ContentChange("ab");
        history.CursorChange(At(0, 2));
        clock.Advance(TimeSpan.FromSeconds(2));
        history.ContentChange("abc");
        clock.Advance(TimeSpan.FromSeconds(2));
        Assert.IsTrue(history.IsPending);

        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.IsFalse(history.IsPending);
        Assert.AreEqual("a", history.Undo()?.Text, "both edits were merged into one step");
    }

    [TestMethod]
    public void CursorMovingToAnotherLine_CommitsThePendingEdit()
    {
        using var history = new ContentHistory(new ManualTimeProvider());
        history.InitHistory("a");
        history.ContentChange("a\nb");
        history.CursorChange(At(1, 1));
        Assert.IsTrue(history.IsPending);

        history.CursorChange(At(0, 1));
        Assert.IsFalse(history.IsPending);

        history.ContentChange("xa\nb");
        history.CursorChange(At(0, 2));
        Assert.AreEqual("a\nb", history.Undo()?.Text);
        Assert.AreEqual("a", history.Undo()?.Text);
    }

    [TestMethod]
    public void EchoOfTheCurrentText_IsIgnored()
    {
        using var history = new ContentHistory(new ManualTimeProvider());
        history.InitHistory("a");
        history.ContentChange("a\n");
        Assert.IsFalse(history.IsPending);
        Assert.IsFalse(history.Undoable);
    }

    [TestMethod]
    public void NewEditAfterUndo_DropsTheRedoBranch()
    {
        using var history = new ContentHistory(new ManualTimeProvider());
        history.InitHistory("a");
        history.ContentChange("ab");
        history.CursorChange(At(0, 2));
        history.CommitPending();
        history.Undo();
        Assert.IsTrue(history.Redoable);

        history.ContentChange("ax");
        history.CursorChange(At(0, 2));
        history.CommitPending();
        Assert.IsFalse(history.Redoable);
        Assert.IsNull(history.Redo());
    }

    [TestMethod]
    public void History_KeepsAtMostOneHundredSteps()
    {
        using var history = new ContentHistory(new ManualTimeProvider());
        history.InitHistory("0");
        for (var i = 1; i <= 150; i++)
        {
            history.ContentChange(i.ToString());
            history.CursorChange(At(0));
            history.CommitPending();
        }

        var undone = 0;
        while (history.Undo() is not null)
        {
            undone++;
        }

        Assert.AreEqual(99, undone);
    }

    [TestMethod]
    public void Changes_RaisePropertyChanged()
    {
        using var history = new ContentHistory(new ManualTimeProvider());
        var raised = new List<string?>();
        history.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        history.InitHistory("a");
        history.ContentChange("ab");
        history.CursorChange(At(0, 2));
        history.Undo();

        CollectionAssert.AreEqual(new[] { nameof(ContentHistory.Undoable), nameof(ContentHistory.Redoable), nameof(ContentHistory.Undoable) }, raised);
    }

    [TestMethod]
    public void ClearHistory_ResetsEverything()
    {
        using var history = new ContentHistory(new ManualTimeProvider());
        history.InitHistory("a");
        history.CursorChange(At(0));
        history.ContentChange("ab");
        history.ClearHistory();
        Assert.IsFalse(history.Undoable);
        Assert.IsFalse(history.IsPending);
        Assert.IsNull(history.Undo());
    }
}
