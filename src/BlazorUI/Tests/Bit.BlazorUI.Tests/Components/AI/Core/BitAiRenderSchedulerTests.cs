using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

[TestClass]
public class BitAiRenderSchedulerTests
{
    [TestMethod]
    public async Task ManyRequestsInsideOneIntervalProduceOneRender()
    {
        var renders = 0;
        await using var scheduler = new BitAiRenderScheduler(
            () => { Interlocked.Increment(ref renders); return Task.CompletedTask; },
            TimeSpan.FromMilliseconds(40));

        for (var i = 0; i < 200; i++) scheduler.Request();

        // Waits for the first render rather than for a fixed time a busy machine can overrun, then for three more
        // intervals, in which a request that was not coalesced would have rendered again.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (Volatile.Read(ref renders) == 0 && DateTime.UtcNow < deadline) await Task.Delay(5);
        await Task.Delay(120);

        Assert.IsInRange(1, 3, Volatile.Read(ref renders));
        Assert.IsTrue(scheduler.CoalescedCount > 100);
    }

    [TestMethod]
    public async Task FlushRendersImmediatelyWithoutWaitingForTheInterval()
    {
        var renders = 0;
        await using var scheduler = new BitAiRenderScheduler(
            () => { Interlocked.Increment(ref renders); return Task.CompletedTask; },
            TimeSpan.FromSeconds(10));

        scheduler.Request();
        await scheduler.FlushAsync();

        Assert.AreEqual(1, renders);
    }

    [TestMethod]
    public async Task RequestsAfterDisposalAreIgnored()
    {
        var renders = 0;
        var scheduler = new BitAiRenderScheduler(
            () => { Interlocked.Increment(ref renders); return Task.CompletedTask; },
            TimeSpan.FromMilliseconds(10));

        await scheduler.DisposeAsync();
        scheduler.Request();
        await Task.Delay(40);

        Assert.AreEqual(0, renders);
    }

    [TestMethod]
    public async Task AFlushRequestedWhileAnotherIsRunningIsNotDropped()
    {
        var renders = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var scheduler = new BitAiRenderScheduler(
            async () =>
            {
                if (Interlocked.Increment(ref renders) == 1) await gate.Task;
            },
            TimeSpan.FromMilliseconds(10));

        var first = scheduler.FlushAsync();
        await scheduler.FlushAsync();
        gate.SetResult();
        await first;

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (Volatile.Read(ref renders) < 2 && DateTime.UtcNow < deadline) await Task.Delay(5);

        Assert.AreEqual(2, Volatile.Read(ref renders));
    }

    [TestMethod]
    public async Task AFlushThatThrowsDoesNotStopTheNextRender()
    {
        var flushes = 0;
        await using var scheduler = new BitAiRenderScheduler(
            () => Interlocked.Increment(ref flushes) == 1 ? throw new ObjectDisposedException("circuit") : Task.CompletedTask,
            TimeSpan.FromMilliseconds(1));

        scheduler.Request();
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (Volatile.Read(ref flushes) < 1 && DateTime.UtcNow < deadline) await Task.Delay(5);

        scheduler.Request();
        while (Volatile.Read(ref flushes) < 2 && DateTime.UtcNow < deadline) await Task.Delay(5);

        Assert.AreEqual(2, Volatile.Read(ref flushes));
    }

    [TestMethod]
    public async Task RequestsRacingDisposalNeverThrow()
    {
        for (var i = 0; i < 200; i++)
        {
            var scheduler = new BitAiRenderScheduler(() => Task.CompletedTask, TimeSpan.FromMilliseconds(1));
            var requests = Task.Run(() =>
            {
                for (var j = 0; j < 50; j++) scheduler.Request();
            });

            await scheduler.DisposeAsync();
            await requests;
        }
    }
}
