using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Services;

namespace Typedown.CoreTests.Persistence;

[TestClass]
public class PendingImportGateTests
{
    [TestMethod]
    public void BeginAndComplete_TracksPendingStateAndRevision()
    {
        var gate = new PendingImportGate();
        Assert.IsFalse(gate.IsPending);
        Assert.IsNull(gate.Revision);

        gate.Begin("rev-1");
        Assert.IsTrue(gate.IsPending);
        Assert.AreEqual("rev-1", gate.Revision);

        Assert.IsFalse(gate.Complete("wrong-rev"));
        Assert.IsTrue(gate.IsPending);

        Assert.IsTrue(gate.Complete("rev-1"));
        Assert.IsFalse(gate.IsPending);
        Assert.IsNull(gate.Revision);
    }

    [TestMethod]
    public void Cancel_ResetsPendingStateAndRevision()
    {
        var gate = new PendingImportGate();
        gate.Begin("rev-1");
        Assert.IsTrue(gate.IsPending);

        gate.Cancel();
        Assert.IsFalse(gate.IsPending);
        Assert.IsNull(gate.Revision);
    }

    [TestMethod]
    public async Task RevisionsCancelStaleWaitersAndRequireMatchingFinal()
    {
        var gate = new PendingImportGate();
        gate.Begin("r1");
        var staleWaiter = gate.AcquireAsync(TimeSpan.FromSeconds(1));

        gate.Begin("r2");

        Assert.IsNull(await staleWaiter);
        Assert.IsFalse(gate.Complete("r1"));
        Assert.IsTrue(gate.IsPending);
        Assert.IsTrue(gate.Complete("r2"));
        using var lease = await gate.AcquireAsync(TimeSpan.Zero);
        Assert.IsNotNull(lease);
        Assert.IsTrue(lease.IsValid);
    }

    [TestMethod]
    public async Task PendingImportLease_DoesNotBlockUiEventAndInvalidatesOldGeneration()
    {
        var gate = new PendingImportGate();
        using var lease = await gate.AcquireAsync(TimeSpan.Zero);
        Assert.IsNotNull(lease);

        var beginR2 = Task.Run(() => gate.Begin("r2"));
        Assert.AreSame(beginR2, await Task.WhenAny(beginR2, Task.Delay(500)));
        Assert.IsTrue(gate.IsPending);
        Assert.AreEqual("r2", gate.Revision);
        Assert.IsFalse(lease.IsValid);

        Assert.IsTrue(gate.Complete("r2"));
        lease.Dispose();
        using var r2Lease = await gate.AcquireAsync(TimeSpan.Zero);
        Assert.IsNotNull(r2Lease);
        Assert.IsTrue(r2Lease.IsValid);
    }

    [TestMethod]
    public async Task PendingImportAcquire_UsesBoundedTimeoutAndCancellation()
    {
        var gate = new PendingImportGate();
        using var held = await gate.AcquireAsync(TimeSpan.Zero);
        Assert.IsNotNull(held);

        var stopwatch = Stopwatch.StartNew();
        Assert.IsNull(await gate.AcquireAsync(TimeSpan.Zero));
        Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromMilliseconds(200));

        stopwatch.Restart();
        Assert.IsNull(await gate.AcquireAsync(TimeSpan.FromMilliseconds(75)));
        Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(1));

        using var cancellation = new CancellationTokenSource(50);
        try
        {
            await gate.AcquireAsync(TimeSpan.FromSeconds(5), cancellation.Token);
            Assert.Fail("Expected cancellation.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestMethod]
    public async Task Lease_TryCommit_ExecutesOnlyWhenGenerationMatchesAndSnapshotMatches()
    {
        var gate = new PendingImportGate();
        using var lease = await gate.AcquireAsync(TimeSpan.Zero);
        Assert.IsNotNull(lease);

        var committed = false;
        var result = lease.TryCommit(() => true, () => committed = true);
        Assert.IsTrue(result);
        Assert.IsTrue(committed);

        // TryCommit with false predicate
        committed = false;
        result = lease.TryCommit(() => false, () => committed = true);
        Assert.IsFalse(result);
        Assert.IsFalse(committed);

        // Invalidate generation by starting a new revision
        gate.Begin("r3");
        committed = false;
        result = lease.TryCommit(() => true, () => committed = true);
        Assert.IsFalse(result);
        Assert.IsFalse(committed);
    }
}
