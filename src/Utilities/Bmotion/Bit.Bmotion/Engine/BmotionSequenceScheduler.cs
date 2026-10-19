namespace Bit.Bmotion;

/// <summary>
/// Fires a set of timed callbacks off the engine's own animation clock rather than the wall clock -
/// the timeline playhead behind <see cref="BmSequence"/>.
/// <para>
/// This is what makes a sequence's playback controls mean anything. A wall-clock timer would start
/// every later segment on schedule no matter what the controls said, so pausing a timeline would
/// still burn through its gaps and doubling its speed would compress the segments but not the
/// silence between them. Advancing by <c>frameDelta × Rate</c> instead keeps the gaps and the
/// animations on the same playhead: at rate 0 the timeline holds, at rate 2 the whole thing -
/// segments and gaps alike - runs twice as fast.
/// </para>
/// </summary>
internal sealed class BmotionSequenceScheduler
{
    // Entries sorted by start time, fired in order; _next is the index of the first unfired entry.
    private readonly (double StartSeconds, Action Fire)[] _entries;
    private int _next;
    private double _elapsedSeconds;
    private double _lastTimestamp = -1;
    private bool _cancelled;

    /// <param name="entries">The callbacks and their timeline positions in seconds.</param>
    public BmotionSequenceScheduler(IEnumerable<(double StartSeconds, Action Fire)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _entries = entries.OrderBy(e => e.StartSeconds).ToArray();
    }

    /// <summary>
    /// Playback rate of the timeline: 1 = realtime, 0 = held, 2 = twice as fast. Negative and
    /// non-finite rates are coerced to 0, matching the per-element playback rate.
    /// </summary>
    public double Rate
    {
        get => _rate;
        set => _rate = double.IsFinite(value) && value >= 0 ? value : 0;
    }
    private double _rate = 1;

    /// <summary>True once every entry has fired or the scheduler was cancelled.</summary>
    public bool IsFinished => _cancelled || _next >= _entries.Length;

    /// <summary>
    /// Advances the playhead and fires everything now due. Returns <c>true</c> when there is
    /// nothing left to fire, so the engine can drop the scheduler.
    /// </summary>
    public bool Tick(double timestamp)
    {
        if (IsFinished) return true;

        // The first tick only establishes the clock origin; a scheduler created mid-frame must not
        // inherit the whole elapsed time since the page loaded.
        if (_lastTimestamp < 0)
        {
            _lastTimestamp = timestamp;
        }
        else
        {
            // Cap the step the way the drivers do, so a backgrounded tab returning after seconds
            // doesn't fire the entire remaining timeline in one frame.
            double deltaSeconds = Math.Min((timestamp - _lastTimestamp) / 1000.0, 0.064);
            _lastTimestamp = timestamp;
            if (deltaSeconds > 0) _elapsedSeconds += deltaSeconds * _rate;
        }

        // Entries are sorted, so everything due this frame sits at the front of the remainder.
        while (_next < _entries.Length && _entries[_next].StartSeconds <= _elapsedSeconds)
        {
            var fire = _entries[_next].Fire;
            _next++;
            // One faulted segment must not strand the rest of the timeline (or, since this runs
            // inside the rAF tick, take down the whole frame loop).
            try { fire(); } catch { /* the segment's own completion task carries the failure */ }
        }

        return IsFinished;
    }

    /// <summary>Abandons every entry that hasn't fired yet (a stopped or completed timeline).</summary>
    public void Cancel() => _cancelled = true;
}
