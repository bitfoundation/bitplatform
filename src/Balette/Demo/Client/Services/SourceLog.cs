using System.Diagnostics;

namespace Bit.Balette.Demo.Client.Services;

/// <summary>
/// Records every call the palette makes into the demo's async sources and how it ended, so the modes page can
/// show debouncing and cancellation happening rather than describe them. A keystroke the debounce swallowed never
/// reaches a source at all; a search still in flight when the next one starts ends as cancelled.
/// </summary>
/// <remarks>
/// It is a singleton because the sources are: they are created once, with the options. The sources only run in
/// the browser - the palette is never opened during prerendering - so on WebAssembly that is one log per visitor.
/// </remarks>
public sealed class SourceLog
{
    private const int Capacity = 40;

    private readonly Lock _gate = new();
    private readonly List<SourceCall> _calls = [];

    public event Action? Changed;

    /// <summary>Newest first.</summary>
    public IReadOnlyList<SourceCall> Calls
    {
        get
        {
            lock (_gate)
            {
                return [.. _calls];
            }
        }
    }

    public async Task<IReadOnlyList<BaletteCommand>> TrackAsync(string source, string query, Func<CancellationToken, Task<IReadOnlyList<BaletteCommand>>> search, CancellationToken cancellationToken)
    {
        var call = new SourceCall(source, query, DateTime.Now);
        lock (_gate)
        {
            _calls.Insert(0, call);
            if (_calls.Count > Capacity)
            {
                _calls.RemoveAt(_calls.Count - 1);
            }
        }

        Changed?.Invoke();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var results = await search(cancellationToken);
            call.Complete(results.Count, stopwatch.Elapsed);
            return results;
        }
        catch (OperationCanceledException)
        {
            call.Cancel(stopwatch.Elapsed);
            throw;
        }
        finally
        {
            Changed?.Invoke();
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _calls.Clear();
        }

        Changed?.Invoke();
    }
}

public enum SourceCallOutcome
{
    Running,
    Completed,
    Cancelled,
}

public sealed class SourceCall(string source, string query, DateTime startedAt)
{
    public string Source { get; } = source;

    public string Query { get; } = query;

    public DateTime StartedAt { get; } = startedAt;

    public SourceCallOutcome Outcome { get; private set; }

    public int ResultCount { get; private set; }

    public TimeSpan Duration { get; private set; }

    internal void Complete(int count, TimeSpan duration)
    {
        Outcome = SourceCallOutcome.Completed;
        ResultCount = count;
        Duration = duration;
    }

    internal void Cancel(TimeSpan duration)
    {
        Outcome = SourceCallOutcome.Cancelled;
        Duration = duration;
    }
}
