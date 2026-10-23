using Bit.Blex.Query;
using Microsoft.Extensions.Time.Testing;

namespace Bit.Blex.Tests.Query;

/// <summary>
/// A server whose every request parks until the test answers it, so a test decides exactly when -- and in
/// which order -- responses land.
/// </summary>
public sealed class FakeServer<T>
{
    private readonly List<(TaskCompletionSource<T> Response, CancellationToken Token)> _requests = [];

    public int Calls => _requests.Count;

    /// <summary>Whether request <paramref name="index"/>'s token has been cancelled.</summary>
    public bool WasCancelled(int index) => _requests[index].Token.IsCancellationRequested;

    /// <summary>A fetch that parks until answered; it honours its token unless <paramref name="ignoreToken"/>.</summary>
    public Func<CancellationToken, Task<T>> Fetch(bool ignoreToken = false) => ct =>
    {
        var response = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _requests.Add((response, ct));
        return ignoreToken ? response.Task : response.Task.WaitAsync(ct);
    };

    public void Respond(int index, T value) => _requests[index].Response.SetResult(value);

    public void Fail(int index, Exception error) => _requests[index].Response.SetException(error);
}

/// <summary>A fake clock that counts the timers created on it, so a test advances only once a wait has begun.</summary>
public sealed class CountingClock : TimeProvider
{
    private readonly FakeTimeProvider _inner = new();
    private int _timers;
    private int _live;

    public int Timers => Volatile.Read(ref _timers);

    /// <summary>The timers created on it and not disposed yet.</summary>
    public int LiveTimers => Volatile.Read(ref _live);

    public void Advance(TimeSpan by) => _inner.Advance(by);

    public override DateTimeOffset GetUtcNow() => _inner.GetUtcNow();

    public override long GetTimestamp() => _inner.GetTimestamp();

    public override long TimestampFrequency => _inner.TimestampFrequency;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = _inner.CreateTimer(callback, state, dueTime, period);
        Interlocked.Increment(ref _timers);
        Interlocked.Increment(ref _live);
        return new TrackedTimer(timer, this);
    }

    private sealed class TrackedTimer(ITimer inner, CountingClock clock) : ITimer
    {
        private int _disposed;

        public bool Change(TimeSpan dueTime, TimeSpan period) => inner.Change(dueTime, period);

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                Interlocked.Decrement(ref clock._live);

            inner.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

internal static class QueryTest
{
    /// <summary>Waits (in real time, with a deadline) for a condition a continuation will make true.</summary>
    public static async Task Until(Func<bool> condition, string? because = null)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                Assert.Fail("Timed out waiting" + (because is null ? "." : ": " + because));

            await Task.Delay(1);
        }
    }

    /// <summary>A client on a fake clock, with retries off unless a test turns them on.</summary>
    public static (BlexQueryClient Client, CountingClock Clock, List<BlexError> Errors) Client(Action<BlexQueryClientOptions>? configure = null)
    {
        var clock = new CountingClock();
        var errors = new List<BlexError>();
        var manager = new BlexManager { TimeProvider = clock, OnError = errors.Add };
        var options = new BlexQueryClientOptions { RetryCount = 0 };
        configure?.Invoke(options);
        return (new BlexQueryClient(manager, options), clock, errors);
    }
}
