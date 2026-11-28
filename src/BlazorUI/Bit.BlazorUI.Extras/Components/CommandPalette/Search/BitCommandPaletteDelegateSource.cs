namespace Bit.BlazorUI;

/// <summary>Convenience <see cref="IBitCommandPaletteSource"/> built from a delegate.</summary>
public sealed class BitCommandPaletteDelegateSource : IBitCommandPaletteSource
{
    private readonly Func<string, BitCommandPaletteSourceContext, CancellationToken, Task<IReadOnlyList<BitCommandPaletteCommand>>> _search;
    private readonly Func<string, string?, BitCommandPaletteSourceContext, CancellationToken, Task<BitCommandPaletteSourcePage>>? _searchPage;

    /// <summary>Creates a source from a delegate that takes the query.</summary>
    public BitCommandPaletteDelegateSource(string name, Func<string, CancellationToken, Task<IReadOnlyList<BitCommandPaletteCommand>>> search, int minQueryLength = 1, int maxResults = 20, bool applyMatcher = false)
        : this(name, (q, _, ct) => search(q, ct), minQueryLength, maxResults, applyMatcher)
    {
    }

    /// <summary>Creates a source from a delegate that takes the query and the source context.</summary>
    public BitCommandPaletteDelegateSource(string name, Func<string, BitCommandPaletteSourceContext, CancellationToken, Task<IReadOnlyList<BitCommandPaletteCommand>>> search, int minQueryLength = 1, int maxResults = 20, bool applyMatcher = false)
    {
        Name = name;
        _search = search;
        MinQueryLength = minQueryLength;
        MaxResults = maxResults;
        ApplyMatcher = applyMatcher;
    }

    /// <summary>
    /// Creates a source that answers in pages: the delegate takes the query and the cursor of the page to load (null for
    /// the first) and returns a <see cref="BitCommandPaletteSourcePage"/>, whose <see cref="BitCommandPaletteSourcePage.NextCursor"/> - when
    /// there is one - the palette offers to load with a "Show more…" row.
    /// </summary>
    public BitCommandPaletteDelegateSource(string name, Func<string, string?, BitCommandPaletteSourceContext, CancellationToken, Task<BitCommandPaletteSourcePage>> searchPage, int minQueryLength = 1, int maxResults = 20, bool applyMatcher = false)
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
    public Task<IReadOnlyList<BitCommandPaletteCommand>> SearchAsync(string query, BitCommandPaletteSourceContext context, CancellationToken cancellationToken)
        => _search(query, context, cancellationToken);

    /// <summary>Runs the paging delegate, or - for a source made from one that returns a list - answers a single page.</summary>
    public async Task<BitCommandPaletteSourcePage> SearchPageAsync(string query, string? cursor, BitCommandPaletteSourceContext context, CancellationToken cancellationToken)
    {
        if (_searchPage is not null)
        {
            return await _searchPage(query, cursor, context, cancellationToken).ConfigureAwait(false);
        }

        return cursor is null ? new BitCommandPaletteSourcePage(await _search(query, context, cancellationToken).ConfigureAwait(false)) : new BitCommandPaletteSourcePage([]);
    }
}
