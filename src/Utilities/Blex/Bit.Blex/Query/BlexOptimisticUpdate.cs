namespace Bit.Blex;

/// <summary>
/// An optimistic update of one cached query (<see cref="BlexQueryClient.Optimistic{T}"/>), and the way
/// back from it.
/// </summary>
/// <typeparam name="T">The query's data.</typeparam>
public sealed class BlexOptimisticUpdate<T> : IBlexRollback
{
    private readonly BlexQueryClient _client;
    private readonly BlexQueryEntry<T> _entry;
    private readonly bool _hadData;
    private readonly T? _previous;
    private readonly DateTimeOffset? _previousUpdatedAt;
    private readonly BlexQueryStatus _previousStatus;
    private readonly Exception? _previousError;
    private readonly bool _previousInvalidated;
    private int _done;

    internal BlexOptimisticUpdate(
        BlexQueryClient client,
        BlexQueryEntry<T> entry,
        bool hadData,
        T? previous,
        DateTimeOffset? previousUpdatedAt,
        BlexQueryStatus previousStatus,
        Exception? previousError,
        bool previousInvalidated,
        T value)
    {
        _client = client;
        _entry = entry;
        _hadData = hadData;
        _previous = previous;
        _previousUpdatedAt = previousUpdatedAt;
        _previousStatus = previousStatus;
        _previousError = previousError;
        _previousInvalidated = previousInvalidated;
        Value = value;
    }

    /// <summary>The key of the updated query.</summary>
    public BlexQueryKey Key => _entry.Key;

    /// <summary>The optimistic value the update wrote.</summary>
    public T Value { get; }

    /// <summary>
    /// Restores the data from before the update -- if the cache still holds this update's value. When
    /// something newer has landed since, the key is invalidated instead, so it is refetched rather than
    /// overwritten with a snapshot older than what replaced it. Called at most once; later calls do nothing.
    /// </summary>
    public void Rollback()
    {
        if (Interlocked.Exchange(ref _done, 1) != 0 || _client.IsDisposed)
            return;

        // Read under the gate (inside TryGetData), compared outside it: Equals is user code.
        var current = _entry.TryGetData(out var data, out _) && EqualityComparer<T>.Default.Equals(data, Value);

        if (current)
        {
            _entry.Restore(_hadData, _previous, _previousUpdatedAt, _previousStatus, _previousError, _previousInvalidated);
            if (!_hadData)
                _entry.Invalidate();
        }
        else
        {
            _ = _client.InvalidateAsync(_entry.Key, exact: true);
        }
    }
}
