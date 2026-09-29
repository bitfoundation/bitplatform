using System.Collections.Concurrent;

namespace Boilerplate.Tests.Infrastructure.Services;

/// <summary>
/// The system clock, except that the timers it created are stopped when the test server is disposed.
/// <para>
/// A running timer is a GC root, and whatever its callback reaches stays in memory with it. Service discovery starts a
/// periodic timer for the HttpClient handlers it serves and never stops it (https://github.com/dotnet/extensions/issues/7670),
/// and that timer reaches the app's services, so every test server that ever sent a request through an HttpClient would
/// otherwise stay in memory until the test process exits.
/// </para>
/// </summary>
public sealed class TestTimeProvider : TimeProvider, IDisposable
{
    private readonly ConcurrentDictionary<TrackedTimer, byte> timers = [];

    public override DateTimeOffset GetUtcNow() => TimeProvider.System.GetUtcNow();

    public override long GetTimestamp() => TimeProvider.System.GetTimestamp();

    public override long TimestampFrequency => TimeProvider.System.TimestampFrequency;

    public override TimeZoneInfo LocalTimeZone => TimeProvider.System.LocalTimeZone;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new TrackedTimer(this, TimeProvider.System.CreateTimer(callback, state, dueTime, period));
        timers.TryAdd(timer, 0);
        return timer;
    }

    public void Dispose()
    {
        foreach (var timer in timers.Keys)
        {
            timer.Dispose();
        }
    }

    private sealed class TrackedTimer(TestTimeProvider owner, ITimer timer) : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => timer.Change(dueTime, period);

        public void Dispose()
        {
            owner.timers.TryRemove(this, out _);
            timer.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            owner.timers.TryRemove(this, out _);
            return timer.DisposeAsync();
        }
    }
}
