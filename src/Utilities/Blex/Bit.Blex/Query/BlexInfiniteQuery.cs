namespace Bit.Blex.Query;

/// <summary>
/// An observer of a paged query -- a feed, "load more", infinite scroll: the pages loaded so far, whether there is
/// another, and <see cref="FetchNextPageAsync"/> to load it (TanStack Query's <c>useInfiniteQuery</c>, forward only).
/// Created by <see cref="BlexQueryClient.InfiniteQuery{TPage, TParam}"/> or a component's <c>UseInfiniteQuery</c>;
/// dispose it when done.
/// </summary>
/// <remarks>
/// <para>
/// The pages are one cached query, so everything a query does applies to them as a whole: deduplication, stale and
/// GC times, retries, invalidation, refetching on focus, the prerender handoff. A refetch -- stale on mount, an
/// invalidation, <see cref="RefetchAsync"/> -- fetches every page loaded so far again, in order, each page's param
/// following from the page before it as the server returns it now; a page that no longer has a next one ends the
/// list there.
/// </para>
/// <para>
/// Loading the next page waits for a fetch already running (the first page, a refetch) and then appends to the pages
/// it brought. Asking again while the next page loads joins that request.
/// </para>
/// </remarks>
/// <typeparam name="TPage">One page, as the server returns it.</typeparam>
/// <typeparam name="TParam">
/// What a page is fetched by: a page number, a cursor. <c>getNextPageParam</c> returns <c>null</c> when there is no next
/// page, so use a nullable type for a value type (<c>int?</c>).
/// </typeparam>
public sealed class BlexInfiniteQuery<TPage, TParam> : BlexQuery
{
    private readonly TParam _initialPageParam;
    private Func<TParam, CancellationToken, Task<TPage>> _fetchPage;
    private Func<TPage, IReadOnlyList<TPage>, TParam?> _getNextPageParam;
    private BlexQuery<BlexInfiniteData<TPage, TParam>> _pages = default!;
    private Task? _nextPage;
    private int _fetchingNextPage;
    private bool _disposed;

    internal BlexInfiniteQuery(
        BlexQueryClient client,
        BlexQueryOptions options,
        Func<TParam, CancellationToken, Task<TPage>> fetchPage,
        TParam initialPageParam,
        Func<TPage, IReadOnlyList<TPage>, TParam?> getNextPageParam)
        : base(client, options)
    {
        _fetchPage = fetchPage;
        _initialPageParam = initialPageParam;
        _getNextPageParam = getNextPageParam;
    }

    /// <summary>Starts following the observer of the pages; before it mounts, so the first fetch can read them.</summary>
    internal void Observe(BlexQuery<BlexInfiniteData<TPage, TParam>> pages)
    {
        _pages = pages;
        pages.Changed += RaiseChanged;
    }

    /// <summary>The pages and their params; empty until the first page arrives.</summary>
    public BlexInfiniteData<TPage, TParam> Data => _pages.Data ?? BlexInfiniteData<TPage, TParam>.Empty;

    /// <summary>The pages loaded so far, first to last.</summary>
    public IReadOnlyList<TPage> Pages => Data.Pages;

    /// <summary>The param each page was fetched with.</summary>
    public IReadOnlyList<TParam> PageParams => Data.PageParams;

    /// <summary>Whether the last page loaded has a next one.</summary>
    public bool HasNextPage => TryGetNextPageParam(Data.Pages, out _);

    /// <summary>Whether this observer is loading the next page right now (waiting for a running fetch to settle first included).</summary>
    public bool IsFetchingNextPage => Volatile.Read(ref _fetchingNextPage) > 0;

    /// <inheritdoc />
    public override BlexQueryKey Key => _pages.Key;

    /// <inheritdoc />
    public override BlexQueryStatus Status => _pages.Status;

    /// <inheritdoc />
    public override BlexFetchStatus FetchStatus => _pages.FetchStatus;

    /// <inheritdoc />
    public override Exception? Error => _pages.Error;

    /// <inheritdoc />
    public override DateTimeOffset? DataUpdatedAt => _pages.DataUpdatedAt;

    /// <inheritdoc />
    public override int FailureCount => _pages.FailureCount;

    /// <inheritdoc />
    public override Exception? FailureReason => _pages.FailureReason;

    /// <inheritdoc />
    public override bool HasData => _pages.HasData;

    /// <inheritdoc />
    public override bool IsPlaceholderData => false;

    /// <inheritdoc />
    public override bool IsStale => _pages.IsStale;

    /// <summary>
    /// Loads the page after the last one and appends it -- once a fetch already running has settled, so it follows
    /// the pages that fetch brings. Does nothing when there is no next page. Completes once the page has landed (or
    /// failed: the error is in <see cref="BlexQuery.Error"/>, the pages loaded before stay); never throws.
    /// </summary>
    public Task FetchNextPageAsync()
    {
        if (_disposed)
            return Task.CompletedTask;

        // Asked again while it loads -- a double click, a scroll sentinel seen twice: the same page, not the one after.
        if (_nextPage is { IsCompleted: false } loading)
            return loading;

        return _nextPage = LoadNextPageAsync();
    }

    private async Task LoadNextPageAsync()
    {
        Interlocked.Increment(ref _fetchingNextPage);
        RaiseChanged();
        try
        {
            // A fetch that starts between the running one settling and this asking -- a focus refetch, an invalidation
            // posted to the same dispatcher -- is waited for in turn, rather than taken for the next page's fetch.
            while (true)
            {
                await _pages.WhenSettledAsync().ConfigureAwait(true);
                if (_disposed || !HasNextPage)
                    return;

                if (_pages.TryFetchOnceAsync(FetchNextPageDataAsync) is { } fetch)
                {
                    await fetch.ConfigureAwait(true);
                    return;
                }
            }
        }
        finally
        {
            Interlocked.Decrement(ref _fetchingNextPage);
            if (!_disposed)
                RaiseChanged();
        }
    }

    /// <summary>
    /// Adopts a newer page fetch, next-param rule and options for the same key -- the closures a component builds on
    /// every parameter change may capture newer values.
    /// </summary>
    public void Update(
        Func<TParam, CancellationToken, Task<TPage>> fetchPage,
        Func<TPage, IReadOnlyList<TPage>, TParam?> getNextPageParam,
        BlexQueryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(fetchPage);
        ArgumentNullException.ThrowIfNull(getNextPageParam);
        if (_disposed)
            return;

        _fetchPage = fetchPage;
        _getNextPageParam = getNextPageParam;
        Options = options ?? Options;
        _pages.Update(FetchPagesAsync, Options);
    }

    /// <summary>Fetches every page loaded so far again (cancelling a fetch already running). Never throws.</summary>
    public override Task RefetchAsync() => _disposed ? Task.CompletedTask : _pages.RefetchAsync();

    /// <inheritdoc />
    public override Task WhenSettledAsync() => _pages.WhenSettledAsync();

    /// <inheritdoc />
    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _pages.Changed -= RaiseChanged;
        _pages.Dispose();
    }

    internal override void Rebind() => _pages.Rebind();

    internal override void StopPolling() => _pages.StopPolling();

    /// <summary>The query's fetch: every page loaded so far, again and in order -- or the first, when there are none.</summary>
    internal async Task<BlexInfiniteData<TPage, TParam>> FetchPagesAsync(CancellationToken cancellationToken)
    {
        var count = Math.Max(1, Data.Pages.Count);
        var pages = new List<TPage>(count);
        var pageParams = new List<TParam>(count);
        var param = _initialPageParam;
        while (true)
        {
            var page = await _fetchPage(param, cancellationToken).ConfigureAwait(true);
            pages.Add(page);
            pageParams.Add(param);

            // The params after the first follow from the pages as the server returns them now: an item inserted
            // since moves every cursor after it.
            if (pages.Count >= count || !TryGetNextPageParam(pages, out param))
                return new BlexInfiniteData<TPage, TParam>(pages, pageParams);
        }
    }

    private async Task<BlexInfiniteData<TPage, TParam>> FetchNextPageDataAsync(CancellationToken cancellationToken)
    {
        var current = Data;
        if (!TryGetNextPageParam(current.Pages, out var param))
            return current;

        var page = await _fetchPage(param, cancellationToken).ConfigureAwait(true);
        return new BlexInfiniteData<TPage, TParam>([.. current.Pages, page], [.. current.PageParams, param]);
    }

    private bool TryGetNextPageParam(IReadOnlyList<TPage> pages, out TParam param)
    {
        if (pages.Count > 0 && _getNextPageParam(pages[^1], pages) is { } next)
        {
            param = next;
            return true;
        }

        param = default!;
        return false;
    }
}
