namespace Bit.Belemetry;

/// <summary>
/// A token bucket: it starts full with <c>capacity</c> tokens and refills at <c>rate</c> per <c>period</c>,
/// so it allows a burst of <c>capacity</c> and then <c>rate</c> per <c>period</c> on average.
/// </summary>
/// <param name="capacity">The burst size.</param>
/// <param name="rate">The number of tokens refilled per <paramref name="period"/> (at least 1).</param>
/// <param name="period">The time <paramref name="rate"/> is measured over.</param>
/// <param name="clock">Milliseconds from any fixed point; settable for tests. Defaults to <see cref="Environment.TickCount64"/>.</param>
internal sealed class BelemetryRateLimiter(int capacity, int rate, TimeSpan period, Func<long>? clock = null)
{
    private readonly object _lock = new();
    private readonly Func<long> _clock = clock ?? (static () => Environment.TickCount64);
    private readonly double _capacity = Math.Max(1, capacity);
    private readonly double _perMillisecond = Math.Max(1, rate) / Math.Max(1, period.TotalMilliseconds);
    private double _tokens = Math.Max(1, capacity);
    private long? _last;

    /// <summary>A bucket whose burst is also its rate: <paramref name="capacity"/> at once, then that many per <paramref name="period"/>.</summary>
    public BelemetryRateLimiter(int capacity, TimeSpan period, Func<long>? clock = null) : this(capacity, capacity, period, clock)
    {
    }

    /// <summary>Takes up to <paramref name="count"/> tokens and returns how many it got (0 when the bucket is empty).</summary>
    public int Take(int count)
    {
        if (count <= 0) return 0;

        lock (_lock)
        {
            var now = _clock();
            if (_last is { } last && now > last) _tokens = Math.Min(_capacity, _tokens + ((now - last) * _perMillisecond));
            _last = now;

            var granted = (int)Math.Min(count, Math.Floor(_tokens));
            _tokens -= granted;
            return granted;
        }
    }
}
