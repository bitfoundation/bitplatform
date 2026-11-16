using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Tests for <see cref="BmotionContinuations"/>: the work that follows an animation's completion
/// runs on the context the animation was started from - the circuit's dispatcher on Blazor
/// Server, where a consumer's <c>OnAnimationComplete</c> re-rendering from a thread-pool thread
/// would throw - and on the default scheduler where there is none (WebAssembly).
/// </summary>
[TestClass]
public class ContinuationsTests
{
    [TestMethod]
    public void OnCallerContext_PostsToTheCurrentSynchronizationContext()
    {
        var context = new ManualSynchronizationContext();
        var source = new TaskCompletionSource<bool>();
        SynchronizationContext? ranOn = null;
        var continuation = ManualSynchronizationContext.RunUnder(context, () => source.Task.OnCallerContext(_ =>
        {
            ranOn = SynchronizationContext.Current;
            return Task.CompletedTask;
        }));

        // Completed from somewhere else entirely (a JS callback, a timer): it must not run inline.
        source.SetResult(true);
        Assert.AreEqual(1, context.Posted, "the continuation must be posted to the starting context");
        Assert.IsFalse(continuation.IsCompleted, "and run only when that context gets to it");

        context.Pump();

        Assert.AreSame(context, ranOn);
        Assert.IsTrue(continuation.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void OnCallerContext_NonGeneric_PostsToTheCurrentSynchronizationContext()
    {
        var context = new ManualSynchronizationContext();
        var source = new TaskCompletionSource();
        bool ran = false;
        var continuation = ManualSynchronizationContext.RunUnder(context, () => source.Task.OnCallerContext(_ => ran = true));

        source.SetResult();
        Assert.AreEqual(1, context.Posted);
        Assert.IsFalse(ran);

        context.Pump();

        Assert.IsTrue(ran);
        Assert.IsTrue(continuation.IsCompletedSuccessfully);
    }

    [TestMethod]
    public async Task OnCallerContext_WithoutASynchronizationContext_UsesTheDefaultScheduler()
    {
        var source = new TaskCompletionSource<bool>();
        TaskScheduler? ranOn = null;
        var continuation = ManualSynchronizationContext.RunUnder(null, () => source.Task.OnCallerContext(_ =>
        {
            ranOn = TaskScheduler.Current;
            return Task.CompletedTask;
        }));

        source.SetResult(true);
        await continuation.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreSame(TaskScheduler.Default, ranOn);
    }

    [TestMethod]
    public async Task OnCallerContext_SettlesWithTheTaskTheContinuationReturns()
    {
        // Unwrapped: the returned task is the handler's own work, so a handler's failure is
        // observable through it rather than vanishing with a fire-and-forget task.
        var source = new TaskCompletionSource<bool>();
        var handler = new TaskCompletionSource();
        var continuation = ManualSynchronizationContext.RunUnder(null, () => source.Task.OnCallerContext(_ => handler.Task));

        source.SetResult(true);
        Assert.IsFalse(continuation.IsCompleted, "it settles with the handler, not before");

        handler.SetException(new InvalidOperationException("handler failed"));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => continuation.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [TestMethod]
    public void RegisterOnCallerContext_DisposedBeforeThePostedStopRuns_DropsIt()
    {
        // CancelAfter's timer cancels from the thread pool, so the stop is posted to the caller's context. The
        // animation settling first (which disposes the registration) must keep that stop from reaching a later one.
        var context = new ManualSynchronizationContext();
        using var cts = new CancellationTokenSource();
        int stops = 0;
        var registration = ManualSynchronizationContext.RunUnder(context, () => cts.Token.RegisterOnCallerContext(() => stops++));

        Task.Run(cts.Cancel).Wait();
        Assert.AreEqual(1, context.Posted, "cancelled off the caller's context, the stop is posted to it");

        registration.Dispose();
        context.Pump();

        Assert.AreEqual(0, stops);
    }

    [TestMethod]
    public void RegisterOnCallerContext_CancelledOffTheCallersContext_StopsOnIt()
    {
        var context = new ManualSynchronizationContext();
        using var cts = new CancellationTokenSource();
        SynchronizationContext? ranOn = null;
        using var registration = ManualSynchronizationContext.RunUnder(context,
            () => cts.Token.RegisterOnCallerContext(() => ranOn = SynchronizationContext.Current));

        Task.Run(cts.Cancel).Wait();
        Assert.IsNull(ranOn, "not on the thread that cancelled");
        context.Pump();

        Assert.AreSame(context, ranOn);
    }
}
