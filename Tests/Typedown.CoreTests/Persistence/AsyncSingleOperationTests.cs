using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Services;

namespace Typedown.CoreTests.Persistence;

[TestClass]
public class AsyncSingleOperationTests
{
    [TestMethod]
    public async Task TryRunAsync_ExecutesOperationSuccessfully_WhenIdle()
    {
        var operation = new AsyncSingleOperation();
        Assert.IsFalse(operation.IsRunning);

        var executed = false;
        var result = await operation.TryRunAsync(() =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        Assert.IsTrue(result);
        Assert.IsTrue(executed);
        Assert.IsFalse(operation.IsRunning);
    }

    [TestMethod]
    public async Task TryRunAsync_SkipsOverlappingConcurrentExecutions()
    {
        var operation = new AsyncSingleOperation();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var persistenceOperations = 0;

        var first = operation.TryRunAsync(async () =>
        {
            Interlocked.Increment(ref persistenceOperations);
            entered.SetResult();
            await release.Task;
        });
        await entered.Task;

        Assert.IsTrue(operation.IsRunning);

        var overlapping = await operation.TryRunAsync(() =>
        {
            Interlocked.Increment(ref persistenceOperations);
            return Task.CompletedTask;
        });

        release.SetResult();

        Assert.IsFalse(overlapping);
        Assert.IsTrue(await first);
        Assert.AreEqual(1, persistenceOperations);
        Assert.IsFalse(operation.IsRunning);
    }

    [TestMethod]
    public async Task TryRunAsync_ResetsRunningState_AfterException()
    {
        var operation = new AsyncSingleOperation();

        try
        {
            await operation.TryRunAsync(() => throw new InvalidOperationException("Test exception"));
            Assert.Fail("Expected exception");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        Assert.IsFalse(operation.IsRunning);

        // Can run subsequent operations after error
        var subsequentRun = await operation.TryRunAsync(() => Task.CompletedTask);
        Assert.IsTrue(subsequentRun);
    }
}
