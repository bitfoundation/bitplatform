using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// BitDebouncer and BitThrottler are public: a consumer hands them a callback that renders, so the callback has to run
/// on the context the call came from (a component's renderer), and a callback that throws must neither vanish nor leave
/// the throttle shut for good.
/// </summary>
[TestClass]
public sealed class BitRateLimitTests
{
    [TestMethod]
    public async Task BitDebouncerShouldRunOnlyTheLastCallOfABurst()
    {
        var debouncer = new BitDebouncer();
        var runs = 0;
        var last = 0;

        // The first call waits far longer than the test thread could take to make the second one, so the second always
        // lands inside the first one's window: with the same short wait for both, a runner that stalled the test
        // thread between the two calls ran the first callback before the second call could cancel it.
        var first = debouncer.Do(10_000, () => { runs++; last = 1; return Task.CompletedTask; });
        var second = debouncer.Do(50, () => { runs++; last = 2; return Task.CompletedTask; });

        await Task.WhenAll(first, second);

        Assert.AreEqual(1, runs);
        Assert.AreEqual(2, last);
    }

    [TestMethod]
    public async Task BitDebouncerShouldHandWhatTheCallbackThrowsToTheCaller()
    {
        var debouncer = new BitDebouncer();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => debouncer.Do(1, () => throw new InvalidOperationException()));
    }

    [TestMethod]
    public async Task BitDebouncerShouldRunTheCallbackOnTheContextOfTheCall()
    {
        var debouncer = new BitDebouncer();

        var ranOnCallersContext = await RunOnMarkedContext(async () =>
        {
            var marked = false;
            await debouncer.Do(1, () => { marked = SynchronizationContext.Current is MarkedContext; return Task.CompletedTask; });
            return marked;
        });

        Assert.IsTrue(ranOnCallersContext);
    }

    [TestMethod]
    public async Task BitDebouncerShouldNotRunACancelledCall()
    {
        var debouncer = new BitDebouncer();
        var runs = 0;

        // A wait the test thread cannot outlast before it cancels, which ends the pending call at once either way.
        var pending = debouncer.Do(10_000, () => { runs++; return Task.CompletedTask; });
        debouncer.Cancel();
        await pending;

        Assert.AreEqual(0, runs);
    }

    [TestMethod]
    public async Task BitThrottlerShouldDropTheCallsMadeWhileItIsShut()
    {
        var throttler = new BitThrottler();
        var runs = 0;

        var release = new TaskCompletionSource();

        // The throttle stays shut until the callback of the call that shut it has finished, which is when the test lets
        // it: the second call is made while it is shut however long the test thread takes to make it. With a plain 50 ms
        // window, a runner that stalled the test thread between the two calls found the throttle open again.
        var first = throttler.Do(1, async () => { runs++; await release.Task; });
        var second = throttler.Do(1, () => { runs++; return Task.CompletedTask; });

        release.SetResult();

        await Task.WhenAll(first, second);

        Assert.AreEqual(1, runs);
    }

    [TestMethod]
    public async Task BitThrottlerShouldOpenAgainAfterACallbackThatThrew()
    {
        var throttler = new BitThrottler();
        var runs = 0;

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => throttler.Do(1, () => throw new InvalidOperationException()));

        await throttler.Do(1, () => { runs++; return Task.CompletedTask; });

        Assert.AreEqual(1, runs);
    }

    [TestMethod]
    public async Task BitThrottlerShouldRunTheCallbackOnTheContextOfTheCall()
    {
        var throttler = new BitThrottler();

        var ranOnCallersContext = await RunOnMarkedContext(async () =>
        {
            var marked = false;
            await throttler.Do(1, () => { marked = SynchronizationContext.Current is MarkedContext; return Task.CompletedTask; });
            return marked;
        });

        Assert.IsTrue(ranOnCallersContext);
    }

    [TestMethod]
    public async Task BitThrottlerShouldOpenAgainOnReset()
    {
        var throttler = new BitThrottler();
        var runs = 0;

        var pending = throttler.Do(1_000, () => { runs++; return Task.CompletedTask; });
        throttler.Reset();
        await pending;

        await throttler.Do(1, () => { runs++; return Task.CompletedTask; });

        Assert.AreEqual(1, runs);
    }

    // Runs the function with a context of its own installed, the way the renderer's dispatcher is around a component's
    // handlers, and hands back what it returned.
    private static Task<bool> RunOnMarkedContext(Func<Task<bool>> func)
    {
        var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = new MarkedContext();

        context.Post(async _ =>
        {
            try
            {
                result.SetResult(await func());
            }
            catch (Exception ex)
            {
                result.SetException(ex);
            }
        }, null);

        return result.Task;
    }

    // A context that installs itself around everything posted to it, so a continuation that resumes on it can tell.
    private sealed class MarkedContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var previous = Current;
                SetSynchronizationContext(this);
                try
                {
                    d(state);
                }
                finally
                {
                    SetSynchronizationContext(previous);
                }
            });
        }
    }
}
