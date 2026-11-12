using System.Collections.Concurrent;

namespace Bit.Blex.Tests;

/// <summary>One thread, one queue: continuations run in the order they were posted, as on a renderer's dispatcher.</summary>
internal sealed class SingleThreadContext : SynchronizationContext, IDisposable
{
    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = [];
    private readonly Thread _thread;

    public SingleThreadContext()
    {
        _thread = new Thread(Run) { IsBackground = true };
        _thread.Start();
    }

    public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));

    public Task RunAsync(Func<Task> work)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(async _ =>
        {
            try
            {
                await work();
                done.SetResult();
            }
            catch (Exception ex)
            {
                done.SetException(ex);
            }
        }, null);

        return done.Task;
    }

    private void Run()
    {
        SetSynchronizationContext(this);
        foreach (var (callback, state) in _queue.GetConsumingEnumerable())
            callback(state);
    }

    public void Dispose() => _queue.CompleteAdding();
}
