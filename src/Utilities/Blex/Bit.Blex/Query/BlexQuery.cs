using System.ComponentModel;

namespace Bit.Blex;

/// <summary>
/// An observer of one cached query: what a component reads -- the status, the error, whether a fetch is
/// running -- whatever the data's type. <see cref="BlexQuery{T}"/> adds the data.
/// </summary>
/// <remarks>
/// <see cref="BlexQueryStatus"/> says whether there is data; <see cref="BlexFetchStatus"/> says whether a
/// request is running. They are independent: a query refetching in the background is
/// <c>Success</c> and <c>Fetching</c> at once, which is why <see cref="IsLoading"/> (no data yet) and
/// <see cref="IsRefetching"/> (data shown, newer on its way) are different things.
/// </remarks>
public abstract class BlexQuery : INotifyPropertyChanged, IDisposable
{
    private protected BlexQuery(BlexQueryClient client, BlexQueryOptions options)
    {
        Client = client;
        Options = options;
    }

    private protected BlexQueryClient Client { get; }

    /// <summary>The key of the observed query.</summary>
    public abstract BlexQueryKey Key { get; }

    /// <summary>How this observer wants the query fetched.</summary>
    public BlexQueryOptions Options { get; private protected set; }

    /// <summary>Whether there is data, an error, or neither yet. Placeholder data counts as <see cref="BlexQueryStatus.Success"/>.</summary>
    public abstract BlexQueryStatus Status { get; }

    /// <summary>Whether a fetch is running.</summary>
    public abstract BlexFetchStatus FetchStatus { get; }

    /// <summary>The error of the last fetch, or <c>null</c>. Data from an earlier success is kept alongside it.</summary>
    public abstract Exception? Error { get; }

    /// <summary>When the cached data was fetched (or set), or <c>null</c> when there is none.</summary>
    public abstract DateTimeOffset? DataUpdatedAt { get; }

    /// <summary>How many times the current (or last) fetch failed, retries included.</summary>
    public abstract int FailureCount { get; }

    /// <summary>The most recent failure of the current (or last) fetch -- set while it is still being retried.</summary>
    public abstract Exception? FailureReason { get; }

    /// <summary>Whether there is data to show -- cached, or placeholder.</summary>
    public abstract bool HasData { get; }

    /// <summary>Whether the data shown is the previous key's, kept while this key loads (<see cref="BlexQueryOptions.KeepPreviousData"/>).</summary>
    public abstract bool IsPlaceholderData { get; }

    /// <summary>Whether the cached data is older than this observer's stale time, invalidated, or missing.</summary>
    public abstract bool IsStale { get; }

    /// <summary>No data yet: the first fetch has not finished.</summary>
    public bool IsPending => Status == BlexQueryStatus.Pending;

    /// <summary>The last fetch failed.</summary>
    public bool IsError => Status == BlexQueryStatus.Error;

    /// <summary>There is data.</summary>
    public bool IsSuccess => Status == BlexQueryStatus.Success;

    /// <summary>A fetch is running.</summary>
    public bool IsFetching => FetchStatus == BlexFetchStatus.Fetching;

    /// <summary>The first fetch is running: nothing to show but a loading state.</summary>
    public bool IsLoading => IsFetching && IsPending;

    /// <summary>Data -- or the error of the last fetch -- is shown and a newer answer is being fetched.</summary>
    public bool IsRefetching => IsFetching && !IsPending;

    /// <summary>A fetch is waiting for the network to come back (<see cref="BlexFetchStatus.Paused"/>).</summary>
    public bool IsPaused => FetchStatus == BlexFetchStatus.Paused;

    /// <summary>Raised after the observed query changed. Handler exceptions are isolated and reported.</summary>
    public event Action? Changed;

    /// <summary>
    /// Raised with the other change notification, always with an empty property name ("all properties
    /// changed"), so XAML bindings can bind straight to an observer.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Fetches again now, cancelling a fetch already running. Completes once it has settled; never throws.</summary>
    public abstract Task RefetchAsync();

    /// <summary>Completes once no fetch is running -- at once when none is. Never throws.</summary>
    /// <example><code>
    /// // Prerendering: wait, so the HTML carries the data rather than a spinner.
    /// protected override async Task OnParametersSetAsync()
    ///     => await UseQuery(["todo", Id], ct => Api.GetTodoAsync(Id, ct)).WhenSettledAsync();
    /// </code></example>
    public abstract Task WhenSettledAsync();

    /// <summary>Stops observing. The query stays cached for its GC time, so another observer can pick it up.</summary>
    public abstract void Dispose();

    /// <summary>Moves to a fresh entry for the same key after the cached one was removed.</summary>
    internal abstract void Rebind();

    /// <summary>Stops polling for good: the client was disposed, and an observer nobody disposed must not keep it alive.</summary>
    internal abstract void StopPolling();

    private protected void RaiseChanged() => Client.RaiseIsolated(Changed, PropertyChanged, this, Key);
}
