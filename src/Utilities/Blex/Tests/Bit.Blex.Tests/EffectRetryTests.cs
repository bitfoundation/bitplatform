using Bit.Blex.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Bit.Blex.Tests;

/// <summary>Effects that fail, hang or recover on demand, for retry and timeout.</summary>
[BlexStoreAttribute(Name = "retry")]
public partial class RetryStore
{
    [BlexStateAttribute] private string _value = "";

    public int Attempts { get; private set; }

    /// <summary>How many attempts fail before one succeeds.</summary>
    public int FailuresBeforeSuccess { get; set; }

    public List<DateTimeOffset> AttemptTimes { get; } = [];

    public TaskCompletionSource Never { get; } = new();

    // Fails synchronously (no await before the throw), so each backoff timer is created before the call returns.
    [BlexEffectAttribute(RetryCount = 2, RetryDelayMilliseconds = 100)]
    private Task OnLoad(string value)
    {
        Attempts++;
        AttemptTimes.Add(TimeProvider.GetUtcNow());
        if (Attempts <= FailuresBeforeSuccess)
            throw new InvalidOperationException("flaky " + Attempts);

        Value = value;
        return Task.CompletedTask;
    }

    [BlexEffectAttribute(TimeoutMilliseconds = 1000)]
    private async Task OnHang(CancellationToken ct)
    {
        Attempts++;
        await Never.Task.WaitAsync(ct);
        Value = "never";
    }

    [BlexEffectAttribute(RetryCount = 1, RetryDelayMilliseconds = 0, TimeoutMilliseconds = 1000)]
    private async Task OnHangThenRecover(CancellationToken ct)
    {
        Attempts++;
        if (Attempts == 1)
            await Never.Task.WaitAsync(ct);

        Value = "recovered";
    }

    [BlexEffectAttribute(RetryCount = 5, RetryDelayMilliseconds = 1000)]
    private Task OnAlwaysFail(CancellationToken ct)
    {
        Attempts++;
        throw new InvalidOperationException("always " + Attempts);
    }

    [BlexEffectAttribute(RetryCount = 2, RetryDelayMilliseconds = 0)]
    private Task OnExhaust()
    {
        Attempts++;
        throw new InvalidOperationException("exhausted " + Attempts);
    }

    [BlexEffectAttribute(RetryCount = 5, RetryDelayMilliseconds = 1000)]
    private Task OnTokenless()
    {
        Attempts++;
        throw new InvalidOperationException("tokenless " + Attempts);
    }

    // Changes state on every attempt: the attempts are still one action.
    [BlexEffectAttribute(RetryCount = 1, RetryDelayMilliseconds = 0)]
    private Task OnCountAttempts()
    {
        Attempts++;
        Value = "attempt " + Attempts;
        if (Attempts == 1)
            throw new InvalidOperationException("first");

        return Task.CompletedTask;
    }
}

/// <summary>
/// <c>[BlexEffect(RetryCount, RetryDelayMilliseconds, TimeoutMilliseconds)]</c>: a failed run is retried
/// with exponential backoff on the manager's clock, and an attempt that overruns its timeout fails with a
/// <see cref="TimeoutException"/> -- all inside one run, so loading stays on and the action records once.
/// </summary>
[TestClass]
public class EffectRetryTests
{
    /// <summary>A fake clock that signals each timer created on it, so a test advances only once a wait has begun.</summary>
    private sealed class ObservedClock : TimeProvider
    {
        private readonly FakeTimeProvider _inner = new();
        private int _timers;

        public int Timers => Volatile.Read(ref _timers);

        public void Advance(int milliseconds) => _inner.Advance(TimeSpan.FromMilliseconds(milliseconds));

        public override DateTimeOffset GetUtcNow() => _inner.GetUtcNow();

        public override long GetTimestamp() => _inner.GetTimestamp();

        public override long TimestampFrequency => _inner.TimestampFrequency;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = _inner.CreateTimer(callback, state, dueTime, period);
            Interlocked.Increment(ref _timers);
            return timer;
        }
    }

    private static async Task Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                Assert.Fail("timed out waiting for the condition");
            await Task.Delay(1);
        }
    }

    private static (BlexTestHarness<RetryStore> Harness, ObservedClock Clock) Setup()
    {
        var clock = new ObservedClock();
        var harness = BlexTestHarness.For<RetryStore>();
        harness.TimeProvider = clock;
        return (harness, clock);
    }

    [TestMethod]
    public async Task A_failed_run_is_retried_with_exponential_backoff()
    {
        var (harness, clock) = Setup();
        using var _ = harness;
        var store = harness.Store;
        store.FailuresBeforeSuccess = 2;
        var start = clock.GetUtcNow();

        var run = store.Load("ok");
        Assert.AreEqual(1, store.Attempts);
        Assert.IsTrue(store.LoadIsLoading);

        clock.Advance(99);
        Assert.AreEqual(1, store.Attempts, "the first retry waits the full RetryDelayMilliseconds");

        clock.Advance(1);
        await Until(() => clock.Timers == 2);   // the second attempt failed and began the doubled wait
        Assert.AreEqual(2, store.Attempts);
        Assert.IsTrue(store.LoadIsLoading, "loading stays on between attempts");
        Assert.IsNull(store.LoadError, "only the final failure is the run's error");

        clock.Advance(199);
        Assert.AreEqual(2, store.Attempts, "the wait doubles before each further retry");

        clock.Advance(1);
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(3, store.Attempts);
        Assert.AreEqual("ok", store.Value);
        Assert.IsNull(store.LoadError);
        Assert.IsFalse(store.LoadIsLoading);
        Assert.AreSequenceEqual([TimeSpan.Zero, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(300)], store.AttemptTimes.Select(t => t - start).ToList());
        Assert.AreSequenceEqual(["Load"], harness.Log.Names.ToList(), "the attempts are one recorded action");
        Assert.IsEmpty(harness.Failures);
    }

    [TestMethod]
    public async Task Exhausted_retries_leave_the_last_failure_in_Error_and_report_it_once()
    {
        using var harness = BlexTestHarness.For<RetryStore>();

        await harness.Store.Exhaust();

        Assert.AreEqual(3, harness.Store.Attempts);
        Assert.AreEqual("exhausted 3", harness.Store.ExhaustError?.Message);
        var failure = Assert.ContainsSingle(harness.Failures);
        Assert.AreEqual("exhausted 3", failure.Exception.Message);
    }

    [TestMethod]
    public async Task An_attempt_that_overruns_its_timeout_fails_with_a_TimeoutException()
    {
        var (harness, clock) = Setup();
        using var _ = harness;
        var store = harness.Store;

        var run = store.Hang();
        Assert.IsTrue(store.HangIsLoading);

        clock.Advance(999);
        Assert.IsTrue(store.HangIsLoading);

        clock.Advance(1);
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        var error = Assert.IsInstanceOfType<TimeoutException>(store.HangError);
        Assert.IsInstanceOfType<OperationCanceledException>(error.InnerException);
        Assert.IsFalse(store.HangIsLoading);
        Assert.AreEqual("", store.Value);
        Assert.ContainsSingle(harness.Failures);
    }

    [TestMethod]
    public async Task A_timed_out_attempt_is_retried()
    {
        var (harness, clock) = Setup();
        using var _ = harness;
        var store = harness.Store;

        var run = store.HangThenRecover();
        clock.Advance(1000);
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(2, store.Attempts);
        Assert.AreEqual("recovered", store.Value);
        Assert.IsNull(store.HangThenRecoverError);
    }

    [TestMethod]
    public async Task Cancelling_during_the_backoff_ends_the_run_without_an_error()
    {
        var (harness, clock) = Setup();
        using var _ = harness;
        var store = harness.Store;

        var run = store.AlwaysFail();
        Assert.AreEqual(1, clock.Timers);

        store.CancelAlwaysFail();
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(1, store.Attempts);
        Assert.IsNull(store.AlwaysFailError, "cancellation through the effect's own token is not a failure");
        Assert.IsFalse(store.AlwaysFailIsLoading);
        Assert.IsEmpty(harness.Failures);
    }

    [TestMethod]
    public async Task Disposing_the_store_stops_retrying_an_effect_without_a_token()
    {
        var (harness, clock) = Setup();
        using var _ = harness;
        var store = harness.Store;

        var run = store.Tokenless();
        store.Dispose();
        clock.Advance(1000);
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(1, store.Attempts, "no attempt starts once the store is disposed");
        Assert.AreEqual("tokenless 1", store.TokenlessError?.Message);
    }

    [TestMethod]
    public async Task Every_attempt_belongs_to_one_action()
    {
        using var harness = BlexTestHarness.For<RetryStore>();

        await harness.Store.CountAttempts();

        Assert.AreEqual("attempt 2", harness.Store.Value);
        Assert.AreSequenceEqual(["CountAttempts"], harness.Log.Names.ToList());
    }

    [TestMethod]
    public async Task A_replay_skips_the_backoff()
    {
        var (original, _) = Setup();
        using var o = original;
        original.Store.FailuresBeforeSuccess = 0;
        await original.Store.Load("replayed");

        var (replica, clock) = Setup();
        using var r = replica;
        replica.Store.FailuresBeforeSuccess = 2;

        // Nobody advances the clock: a replay that waited out the backoff would never finish.
        await replica.Manager.ReplayAsync(original.Log.Actions).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(3, replica.Store.Attempts);
        Assert.AreEqual("replayed", replica.Store.Value);
        Assert.AreEqual(0, clock.Timers);
    }
}
