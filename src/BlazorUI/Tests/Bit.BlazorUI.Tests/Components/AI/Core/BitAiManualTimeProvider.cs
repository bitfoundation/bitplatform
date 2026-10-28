using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

/// <summary>
/// A clock the tests move on by hand: it reads the time it was moved to, and the timers it creates fire when it is moved
/// past their due time - on the thread that moved it, and never on their own.
/// </summary>
/// <remarks>
/// What a held tool result is timed with, so a test can say "ten seconds later" without waiting ten seconds, and can tell
/// what the processor's timer did from what an event did: <see cref="Skip"/> moves the clock without firing anything,
/// the way a timer that has not run yet on a busy thread pool leaves it.
/// </remarks>
internal sealed class BitAiManualTimeProvider : TimeProvider
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly object _gate = new();
    private readonly List<ManualTimer> _timers = [];
    private long _now;

    /// <summary>One tick of the clock is one <see cref="TimeSpan"/> tick, so an elapsed time reads back exactly.</summary>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp()
    {
        lock (_gate) return _now;
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate) return Start.AddTicks(_now);
    }

    /// <summary>The timers created and not disposed of yet.</summary>
    public int ActiveTimers
    {
        get { lock (_gate) return _timers.Count; }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        lock (_gate) _timers.Add(timer);
        timer.Change(dueTime, period);
        return timer;
    }

    /// <summary>
    /// Moves the clock on, firing every timer that falls due on the way, in the order they fall due and with the clock
    /// reading the time each one was due at.
    /// </summary>
    public void Advance(TimeSpan by)
    {
        long target;
        lock (_gate) target = _now + by.Ticks;

        while (true)
        {
            ManualTimer? due;
            lock (_gate)
            {
                due = _timers.Where(t => t.DueAt <= target).OrderBy(t => t.DueAt).FirstOrDefault();
                if (due is null)
                {
                    _now = target;
                    return;
                }

                _now = Math.Max(_now, due.DueAt!.Value);
                due.DueAt = due.Period > 0 ? _now + due.Period : null;
            }

            // Outside the lock, as a real timer's callback runs: it may change or dispose of its timer, or read the clock.
            due.Fire();
        }
    }

    /// <summary>Moves the clock on without firing a timer: the time is up, but no timer has run yet.</summary>
    public void Skip(TimeSpan by)
    {
        lock (_gate) _now += by.Ticks;
    }

    private sealed class ManualTimer(BitAiManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        // Guarded by the lock of the owner.
        public long? DueAt { get; set; }

        public long Period { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (owner._gate)
            {
                if (owner._timers.Contains(this) is false) return false;

                DueAt = dueTime == Timeout.InfiniteTimeSpan ? null : owner._now + dueTime.Ticks;
                Period = period == Timeout.InfiniteTimeSpan ? 0 : period.Ticks;
                return true;
            }
        }

        public void Fire() => callback(state);

        public void Dispose()
        {
            lock (owner._gate) owner._timers.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
