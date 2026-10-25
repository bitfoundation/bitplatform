namespace Bit.Balette;

/// <summary>
/// Provides commands asynchronously for a query (e.g. search an API for documents, users, issues).
/// Sources are queried with debouncing and cancellation; results are shown under <see cref="Name"/>.
/// </summary>
public interface IBaletteSource
{
    /// <summary>Group heading for results of this source.</summary>
    string Name { get; }

    /// <summary>Minimum query length before the source is asked. 0 queries on an empty box too.</summary>
    int MinQueryLength => 1;

    /// <summary>Upper bound of results taken from this source.</summary>
    int MaxResults => 20;

    /// <summary>
    /// When true, returned commands are re-scored and interleaved with registry results by the matcher.
    /// When false (default), the source's own order is kept and shown as a block.
    /// </summary>
    bool ApplyMatcher => false;

    /// <summary>Search for commands matching <paramref name="query"/>.</summary>
    Task<IReadOnlyList<BaletteCommand>> SearchAsync(string query, BaletteSourceContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Searches one page of results: the first when <paramref name="cursor"/> is null, the one after an earlier page when
    /// it is that page's <see cref="BaletteSourcePage.NextCursor"/>. A page with a cursor of its own gets a "Show more…" row
    /// that asks for the next one, so a server never has to send everything it found. By default a single page: what
    /// <see cref="SearchAsync"/> returns, and no more.
    /// </summary>
    async Task<BaletteSourcePage> SearchPageAsync(string query, string? cursor, BaletteSourceContext context, CancellationToken cancellationToken)
        => cursor is null ? new BaletteSourcePage(await SearchAsync(query, context, cancellationToken).ConfigureAwait(false)) : new BaletteSourcePage([]);
}

/// <summary>
/// One page of an <see cref="IBaletteSource"/>'s results (see <see cref="IBaletteSource.SearchPageAsync"/>): the commands,
/// the cursor of the page after it - null when there is none - and, when the source knows it, how many results there
/// are in all, which the "Show 37 more…" row counts down from.
/// </summary>
public sealed record BaletteSourcePage(IReadOnlyList<BaletteCommand> Commands, string? NextCursor = null, int? TotalCount = null)
{
    /// <summary>Whether there is a page after this one.</summary>
    public bool HasMore => NextCursor is not null;
}

/// <summary>Extra information available to sources.</summary>
public sealed record BaletteSourceContext(string? ModePrefix, string? ParentCommandId, IServiceProvider Services);

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

/// <summary>A source backed by an in-memory list, filtered with the palette's matcher (useful for large static datasets).</summary>
public sealed class BaletteStaticSource : IBaletteSource
{
    private readonly Func<IEnumerable<BaletteCommand>> _items;

    /// <summary>Creates a source over a fixed list.</summary>
    public BaletteStaticSource(string name, IEnumerable<BaletteCommand> items, int maxResults = 50)
        : this(name, () => items, maxResults)
    {
    }

    /// <summary>Creates a source over a list read again on every search.</summary>
    public BaletteStaticSource(string name, Func<IEnumerable<BaletteCommand>> items, int maxResults = 50)
    {
        Name = name;
        _items = items;
        MaxResults = maxResults;
    }

    /// <summary>The group heading of the source's results.</summary>
    public string Name { get; }

    /// <summary>Always 1.</summary>
    public int MinQueryLength => 1;

    /// <summary>The most results returned.</summary>
    public int MaxResults { get; }

    /// <summary>Always true: the list is matched and ranked by the palette's matcher.</summary>
    public bool ApplyMatcher => true;

    /// <summary>
    /// Matches the list with the palette's matcher and returns the best <see cref="MaxResults"/> of it, best first.
    /// The palette keeps only <see cref="MaxResults"/> of what a source returns, so handing it the unfiltered list
    /// would let only its first items ever be found.
    /// </summary>
    public Task<IReadOnlyList<BaletteCommand>> SearchAsync(string query, BaletteSourceContext context, CancellationToken cancellationToken)
    {
        var options = context.Services.GetService(typeof(BaletteOptions)) as BaletteOptions;
        var matcher = options?.Matcher ?? BaletteFuzzyMatcher.Default;
        var weights = options?.Weights ?? new BaletteMatchWeights();
        var parsed = BaletteQuery.Parse(query);

        var matches = new List<BaletteMatch>();
        foreach (var command in _items())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (matcher.Match(command, parsed, weights) is { } match)
            {
                matches.Add(match);
            }
        }

        IReadOnlyList<BaletteCommand> best = [.. matches.OrderByDescending(m => m.Score).Take(MaxResults).Select(m => m.Command)];
        return Task.FromResult(best);
    }
}
