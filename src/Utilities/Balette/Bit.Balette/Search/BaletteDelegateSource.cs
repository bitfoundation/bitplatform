namespace Bit.Balette;

/// <summary>Convenience <see cref="IBaletteSource"/> built from a delegate.</summary>
public sealed class BaletteDelegateSource : IBaletteSource
{
    private readonly Func<string, BaletteSourceContext, CancellationToken, Task<IReadOnlyList<BaletteCommand>>> _search;
    private readonly Func<string, string?, BaletteSourceContext, CancellationToken, Task<BaletteSourcePage>>? _searchPage;

    /// <summary>Creates a source from a delegate that takes the query.</summary>
    public BaletteDelegateSource(string name, Func<string, CancellationToken, Task<IReadOnlyList<BaletteCommand>>> search, int minQueryLength = 1, int maxResults = 20, bool applyMatcher = false)
        : this(name, (q, _, ct) => search(q, ct), minQueryLength, maxResults, applyMatcher)
    {
    }

    /// <summary>Creates a source from a delegate that takes the query and the source context.</summary>
    public BaletteDelegateSource(string name, Func<string, BaletteSourceContext, CancellationToken, Task<IReadOnlyList<BaletteCommand>>> search, int minQueryLength = 1, int maxResults = 20, bool applyMatcher = false)
    {
        Name = name;
        _search = search;
        MinQueryLength = minQueryLength;
        MaxResults = maxResults;
        ApplyMatcher = applyMatcher;
    }

    /// <summary>
    /// Creates a source that answers in pages: the delegate takes the query and the cursor of the page to load (null for
    /// the first) and returns a <see cref="BaletteSourcePage"/>, whose <see cref="BaletteSourcePage.NextCursor"/> - when
    /// there is one - the palette offers to load with a "Show more…" row.
    /// </summary>
    public BaletteDelegateSource(string name, Func<string, string?, BaletteSourceContext, CancellationToken, Task<BaletteSourcePage>> searchPage, int minQueryLength = 1, int maxResults = 20, bool applyMatcher = false)
        : this(name, async (q, ctx, ct) => (await searchPage(q, null, ctx, ct).ConfigureAwait(false)).Commands, minQueryLength, maxResults, applyMatcher)
    {
        _searchPage = searchPage;
    }

    /// <summary>The group heading of the source's results.</summary>
    public string Name { get; }

    /// <summary>The minimum query length before the source is asked.</summary>
    public int MinQueryLength { get; }

    /// <summary>The most results taken from the source.</summary>
    public int MaxResults { get; }

    /// <summary>Whether results are re-ranked with the registry.</summary>
    public bool ApplyMatcher { get; }

    /// <summary>Runs the delegate.</summary>
    public Task<IReadOnlyList<BaletteCommand>> SearchAsync(string query, BaletteSourceContext context, CancellationToken cancellationToken)
        => _search(query, context, cancellationToken);

    /// <summary>Runs the paging delegate, or - for a source made from one that returns a list - answers a single page.</summary>
    public async Task<BaletteSourcePage> SearchPageAsync(string query, string? cursor, BaletteSourceContext context, CancellationToken cancellationToken)
    {
        if (_searchPage is not null)
        {
            return await _searchPage(query, cursor, context, cancellationToken).ConfigureAwait(false);
        }

        return cursor is null ? new BaletteSourcePage(await _search(query, context, cancellationToken).ConfigureAwait(false)) : new BaletteSourcePage([]);
    }
}
