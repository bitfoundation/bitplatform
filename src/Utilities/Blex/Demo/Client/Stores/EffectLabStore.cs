namespace Bit.Blex.Demo.Client.Stores;

/// <summary>One line in the effect lab's timeline.</summary>
/// <param name="Mode">Which concurrency mode produced it.</param>
/// <param name="Run">The run number within that mode.</param>
/// <param name="Phase"><c>started</c>, <c>finished</c>, <c>cancelled</c> or <c>dropped</c>.</param>
/// <param name="At">Milliseconds since the lab was last cleared.</param>
public sealed record JobEvent(string Mode, int Run, string Phase, long At);

/// <summary>
/// Runs the same simulated 1.2s job under all four <see cref="BlexEffectConcurrency"/> modes so the
/// difference is observable: <c>Parallel</c> overlaps, <c>Latest</c> cancels the previous run,
/// <c>Drop</c> ignores clicks while busy, and <c>Queue</c> serializes in arrival order.
/// </summary>
[BlexStoreAttribute(Name = "effectLab")]
public partial class EffectLabStore
{
    private static readonly TimeSpan JobDuration = TimeSpan.FromMilliseconds(1200);

    private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
    private int _parallelRuns;
    private int _latestRuns;
    private int _dropRuns;
    private int _queueRuns;

    [BlexStateAttribute] private IReadOnlyList<JobEvent> _events = [];

    [BlexComputedAttribute] private int ComputeEventCount() => Events.Count;

    // The token of the most recently started Parallel run while it is still going: the only run the generated
    // CancelRunParallel() can reach. Once that one ends, older overlapping runs carry on beyond its reach.
    private CancellationToken _cancellableParallelRun;

    /// <summary>Whether <c>CancelRunParallel()</c> has a run to cancel: the most recently started one is still going.</summary>
    public bool CanCancelRunParallel => _cancellableParallelRun.CanBeCanceled && !_cancellableParallelRun.IsCancellationRequested;

    /// <summary>mergeMap: every invocation runs to completion, overlapping freely.</summary>
    [BlexEffectAttribute]
    private async Task OnRunParallel(CancellationToken ct)
    {
        var run = ++_parallelRuns;
        _cancellableParallelRun = ct;
        Log("Parallel", run, "started");
        try
        {
            await Task.Delay(JobDuration, ct);
        }
        catch (OperationCanceledException)
        {
            Log("Parallel", run, "cancelled");
            throw;
        }
        finally
        {
            if (_cancellableParallelRun == ct)
                _cancellableParallelRun = default;
        }

        Log("Parallel", run, "finished");
    }

    /// <summary>switchMap: starting a new run cancels the one before it.</summary>
    [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Latest)]
    private async Task OnRunLatest(CancellationToken ct)
    {
        var run = ++_latestRuns;
        Log("Latest", run, "started");
        try
        {
            await Task.Delay(JobDuration, ct);
        }
        catch (OperationCanceledException)
        {
            Log("Latest", run, "cancelled");
            throw;
        }

        Log("Latest", run, "finished");
    }

    /// <summary>exhaustMap: invocations arriving while a run is in flight are ignored.</summary>
    [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Drop)]
    private async Task OnRunDrop()
    {
        var run = ++_dropRuns;
        Log("Drop", run, "started");
        await Task.Delay(JobDuration);
        Log("Drop", run, "finished");
    }

    /// <summary>concatMap: runs wait their turn and execute in arrival order.</summary>
    [BlexEffectAttribute(Concurrency = BlexEffectConcurrency.Queue)]
    private async Task OnRunQueue()
    {
        var run = ++_queueRuns;
        Log("Queue", run, "started");
        await Task.Delay(JobDuration);
        Log("Queue", run, "finished");
    }

    private int _flakyAttempts;

    /// <summary>How many attempts of <c>FetchFlaky</c> fail before one succeeds.</summary>
    [BlexStateAttribute] private int _failuresToSimulate = 2;

    /// <summary>Whether those attempts hang past the two-second timeout instead of failing outright.</summary>
    [BlexStateAttribute] private bool _hangInsteadOfFailing;

    [BlexStateAttribute] private IReadOnlyList<string> _attempts = [];

    /// <summary>
    /// A request that fails <see cref="FailuresToSimulate"/> times before it succeeds: three retries,
    /// 400 ms backoff doubling each time, and two seconds per attempt. A hanging attempt never answers, so the
    /// timeout cancels its token and it fails with a <see cref="TimeoutException"/> - retried like any failure.
    /// </summary>
    [BlexEffectAttribute(RetryCount = 3, RetryDelayMilliseconds = 400, TimeoutMilliseconds = 2000)]
    private async Task OnFetchFlaky(CancellationToken ct)
    {
        var attempt = ++_flakyAttempts;
        Attempts = [.. Attempts, $"{_clock.ElapsedMilliseconds,5} ms  attempt {attempt} started"];

        if (attempt <= FailuresToSimulate && HangInsteadOfFailing)
        {
            Attempts = [.. Attempts, $"{_clock.ElapsedMilliseconds,5} ms  attempt {attempt} hangs past the 2000 ms timeout"];
            await Task.Delay(TimeSpan.FromSeconds(10), ct);   // the timeout cancels ct long before this ends
        }

        await Task.Delay(300, ct);   // the "network"

        if (attempt <= FailuresToSimulate)
        {
            Attempts = [.. Attempts, $"{_clock.ElapsedMilliseconds,5} ms  attempt {attempt} failed"];
            throw new InvalidOperationException($"The server failed attempt {attempt}.");
        }

        Attempts = [.. Attempts, $"{_clock.ElapsedMilliseconds,5} ms  attempt {attempt} succeeded"];
    }

    [BlexActionAttribute]
    private void OnPrepareFlaky(int failures, bool hang = false)
    {
        FailuresToSimulate = failures;
        HangInsteadOfFailing = hang;
        Attempts = [];
        _flakyAttempts = 0;
        _clock.Restart();
    }

    /// <summary>Records a click that <c>Drop</c> mode swallowed (the wrapper returns without running).</summary>
    [BlexActionAttribute]
    private void OnNoteDropped() => Log("Drop", _dropRuns, "dropped");

    [BlexActionAttribute]
    private void OnClear()
    {
        Events = [];
        _parallelRuns = _latestRuns = _dropRuns = _queueRuns = 0;
        _clock.Restart();
    }

    private void Log(string mode, int run, string phase)
        => Events = [.. Events, new JobEvent(mode, run, phase, _clock.ElapsedMilliseconds)];
}
