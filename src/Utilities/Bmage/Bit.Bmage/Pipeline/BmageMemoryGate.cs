namespace Bit.Bmage;

/// <summary>
/// Admits work by its memory: a request waits until what it needs fits beside what is already admitted, in the order
/// requests arrive. A request larger than the whole capacity is admitted when nothing else is, so it runs alone instead
/// of never.
/// </summary>
internal sealed class BmageMemoryGate(long capacity)
{
    private readonly object _lock = new();
    private readonly LinkedList<Waiter> _queue = new();
    private long _used;
    private int _admitted;

    private sealed class Waiter(long bytes)
    {
        public long Bytes { get; } = bytes;
        public TaskCompletionSource<long> Admission { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationTokenRegistration Registration { get; set; }
    }

    /// <summary>Bytes admitted and not yet released.</summary>
    public long Used { get { lock (_lock) return _used; } }

    /// <summary>Waits until <paramref name="bytes"/> can be admitted, and returns them to pass to <see cref="Release"/>.</summary>
    public Task<long> AcquireAsync(long bytes, CancellationToken cancellationToken)
    {
        bytes = Math.Max(1, bytes);
        lock (_lock)
        {
            if (_queue.Count == 0 && Fits(bytes))
            {
                Admit(bytes);
                return Task.FromResult(bytes);
            }
            var waiter = new Waiter(bytes);
            var node = _queue.AddLast(waiter);
            if (cancellationToken.CanBeCanceled)
            {
                waiter.Registration = cancellationToken.Register(() =>
                {
                    lock (_lock)
                    {
                        if (node.List is null) return;
                        _queue.Remove(node);
                        waiter.Admission.TrySetCanceled(cancellationToken);
                        // The head may have been what held back those behind it.
                        Drain();
                    }
                });
            }
            return waiter.Admission.Task;
        }
    }

    /// <summary>Returns bytes an earlier <see cref="AcquireAsync"/> admitted.</summary>
    public void Release(long bytes)
    {
        lock (_lock)
        {
            _used -= bytes;
            _admitted--;
            Drain();
        }
    }

    private bool Fits(long bytes) => _admitted == 0 || _used + bytes <= capacity;

    private void Admit(long bytes)
    {
        _used += bytes;
        _admitted++;
    }

    private void Drain()
    {
        while (_queue.First is { } head && Fits(head.Value.Bytes))
        {
            _queue.RemoveFirst();
            var waiter = head.Value;
            Admit(waiter.Bytes);
            // Unregister rather than dispose: disposing waits for a callback that may be blocked on this lock.
            waiter.Registration.Unregister();
            waiter.Admission.TrySetResult(waiter.Bytes);
        }
    }
}
