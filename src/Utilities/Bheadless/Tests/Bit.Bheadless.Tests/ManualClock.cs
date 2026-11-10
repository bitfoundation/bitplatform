namespace Bit.Bheadless.Tests;

/// <summary>
/// A clock that stands still until <see cref="Advance"/> moves it on, firing the timers then due on the calling
/// thread. Registered as the <see cref="TimeProvider"/>, it runs the components' delays on the test's schedule:
/// nothing opens or closes behind the test's back, and no timer thread races the test for the renderer.
/// </summary>
internal sealed class ManualClock : TimeProvider
{
    private readonly List<Timer> _timers = [];
    private TimeSpan _now;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new Timer(this, callback, state);
        timer.Change(dueTime, period);
        return timer;
    }

    public void Advance(int milliseconds)
    {
        Timer[] due;
        lock (_timers)
        {
            _now += TimeSpan.FromMilliseconds(milliseconds);
            due = [.. _timers.Where(t => t.Due <= _now)];
            foreach (var timer in due) _timers.Remove(timer);
        }
        foreach (var timer in due) timer.Fire();
    }

    // One-shot: what the delays need.
    private sealed class Timer(ManualClock clock, TimerCallback callback, object? state) : ITimer
    {
        public TimeSpan Due { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (clock._timers)
            {
                clock._timers.Remove(this);
                if (dueTime == Timeout.InfiniteTimeSpan) return true;
                Due = clock._now + dueTime;
                clock._timers.Add(this);
            }
            return true;
        }

        public void Fire() => callback(state);

        public void Dispose()
        {
            lock (clock._timers) clock._timers.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
