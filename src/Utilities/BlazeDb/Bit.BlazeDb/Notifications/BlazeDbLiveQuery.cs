namespace Bit.BlazeDb;

/// <summary>
/// A query that keeps its result current: it runs once when created, and again after every commit that
/// touched its table - or, given a relevance test, only after commits that touched a row the test accepts,
/// before or after the change. <see cref="Changed"/> says when <see cref="Result"/> was replaced. Create one
/// with <see cref="BlazeDbTable{TKey, TRow}.Watch"/>, and dispose it to stop listening.
/// <para>
/// The query re-runs on the thread that committed, right after the commit - never part-way through a
/// transaction, and never for one that rolled back. In a Blazor component, hand the re-render to the
/// renderer: <c>live.Changed += (_, _) =&gt; InvokeAsync(StateHasChanged);</c>. Return a materialized result
/// (<c>.ToList()</c>, a count, a single row): a lazy sequence would be enumerated later against whatever
/// the table holds then, which is exactly what this type exists to avoid.
/// </para>
/// </summary>
public sealed class BlazeDbLiveQuery<TResult> : IDisposable
{
    private readonly Func<TResult> _evaluate;
    private Action? _unsubscribe;

    internal BlazeDbLiveQuery(Func<TResult> evaluate)
    {
        _evaluate = evaluate;
        Result = evaluate();
    }

    /// <summary>The query's result as of the last commit that concerned it.</summary>
    public TResult Result { get; private set; }

    /// <summary>How many times the query has re-run since it was created; changes with every new <see cref="Result"/>.</summary>
    public long Version { get; private set; }

    /// <summary>Raised after <see cref="Result"/> has been replaced, on the thread that made the commit.</summary>
    public event EventHandler? Changed;

    /// <summary>True once disposed; the result then stays as it last was.</summary>
    public bool IsDisposed => _unsubscribe is null;

    /// <summary>Re-runs the query now, as a commit would, and raises <see cref="Changed"/>.</summary>
    public void Refresh()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        Result = _evaluate();
        Version++;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal void Attach(Action unsubscribe) => _unsubscribe = unsubscribe;

    /// <summary>Stops listening to the table. Calling it again does nothing.</summary>
    public void Dispose()
    {
        var unsubscribe = Interlocked.Exchange(ref _unsubscribe, null);
        unsubscribe?.Invoke();
    }
}
