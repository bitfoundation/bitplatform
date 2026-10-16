namespace Bit.Bmotion.Tests.TestInfra;

/// <summary>
/// A synchronization context that queues whatever is posted to it until the test calls
/// <see cref="Pump"/> - a stand-in for a Blazor Server circuit's dispatcher that makes both "was
/// the continuation sent here" and "has it run yet" observable without any timing.
/// </summary>
internal sealed class ManualSynchronizationContext : SynchronizationContext
{
    private readonly Queue<(SendOrPostCallback Callback, object? State)> _queue = new();

    /// <summary>How many callbacks have been posted so far.</summary>
    public int Posted { get; private set; }

    public override void Post(SendOrPostCallback d, object? state)
    {
        lock (_queue)
        {
            Posted++;
            _queue.Enqueue((d, state));
        }
    }

    /// <summary>Runs every queued callback - and any they post in turn - with this context current.</summary>
    public void Pump()
    {
        while (true)
        {
            (SendOrPostCallback Callback, object? State) next;
            lock (_queue)
            {
                if (_queue.Count == 0) return;
                next = _queue.Dequeue();
            }
            var previous = Current;
            SetSynchronizationContext(this);
            try { next.Callback(next.State); }
            finally { SetSynchronizationContext(previous); }
        }
    }

    /// <summary>
    /// Runs <paramref name="start"/> with <paramref name="context"/> as the current context (null for
    /// none) and restores the previous one: whatever it schedules "on the caller's context" is
    /// scheduled on that one.
    /// </summary>
    public static T RunUnder<T>(SynchronizationContext? context, Func<T> start)
    {
        var previous = Current;
        SetSynchronizationContext(context);
        try { return start(); }
        finally { SetSynchronizationContext(previous); }
    }
}
