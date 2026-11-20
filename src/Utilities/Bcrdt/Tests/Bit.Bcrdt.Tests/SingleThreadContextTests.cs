namespace Bit.Bcrdt.Tests;

/// <summary>The context the replica tests run on fails loudly, never by ending the test host or by hanging.</summary>
[TestClass]
public class SingleThreadContextTests
{
    [TestMethod]
    public async Task Work_that_throws_is_kept_as_a_fault_and_the_thread_goes_on()
    {
        var context = new SingleThreadContext();
        context.Post(_ => throw new InvalidOperationException("posted"), null);

        int ranOn = await context.InvokeAsync(() => Environment.CurrentManagedThreadId);
        context.Dispose();

        Assert.AreEqual(context.ThreadId, ranOn);
        Assert.AreEqual("posted", context.Faults.Single().Message);
    }

    [TestMethod]
    public async Task Disposing_runs_the_work_already_queued_before_it_returns()
    {
        var context = new SingleThreadContext();
        using var gate = new ManualResetEventSlim();
        int ran = 0;
        context.Post(_ => gate.Wait(), null);
        context.Post(_ => Interlocked.Increment(ref ran), null);

        Task disposing = Task.Run(context.Dispose);
        await Task.Delay(50);
        Assert.IsFalse(disposing.IsCompleted, "Dispose should wait for the queue");

        gate.Set();
        await disposing.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.AreEqual(1, Volatile.Read(ref ran));
        Assert.IsEmpty(context.Faults);
    }

    [TestMethod]
    public async Task Work_posted_after_shutdown_still_runs_and_is_reported()
    {
        var context = new SingleThreadContext();
        context.Dispose();

        var ran = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        context.Post(_ => ran.SetResult(), null);

        await ran.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.HasCount(1, context.Faults);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => context.InvokeAsync(() => 0));
    }

    [SingleThreadTestMethod]
    public async Task A_single_thread_test_runs_on_its_context_across_awaits()
    {
        var context = (SingleThreadContext)SynchronizationContext.Current!;

        await Task.Delay(1);
        Assert.AreSame(context, SynchronizationContext.Current);
        Assert.AreEqual(context.ThreadId, Environment.CurrentManagedThreadId);

        await Task.Run(() => { });
        Assert.AreEqual(context.ThreadId, Environment.CurrentManagedThreadId);
    }
}
