namespace Bit.BlazorUI;

/// <summary>
/// Provides commands asynchronously for a query (e.g. search an API for documents, users, issues).
/// Sources are queried with debouncing and cancellation; results are shown under <see cref="Name"/>.
/// </summary>
public interface IBitCommandPaletteSource
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
    Task<IReadOnlyList<BitCommandPaletteCommand>> SearchAsync(string query, BitCommandPaletteSourceContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Searches one page of results: the first when <paramref name="cursor"/> is null, the one after an earlier page when
    /// it is that page's <see cref="BitCommandPaletteSourcePage.NextCursor"/>. A page with a cursor of its own gets a "Show more…" row
    /// that asks for the next one, so a server never has to send everything it found. By default a single page: what
    /// <see cref="SearchAsync"/> returns, and no more.
    /// </summary>
    async Task<BitCommandPaletteSourcePage> SearchPageAsync(string query, string? cursor, BitCommandPaletteSourceContext context, CancellationToken cancellationToken)
        => cursor is null ? new BitCommandPaletteSourcePage(await SearchAsync(query, context, cancellationToken).ConfigureAwait(false)) : new BitCommandPaletteSourcePage([]);
}
