namespace Bit.Butil.Demo.Server.Dtos;

/// <summary>What a search answered with: its hits, or the reason there are none.</summary>
public record ButilSearchResultDto
{
    /// <summary>The best matches, most relevant first.</summary>
    public required ButilSearchHitDto[] Hits { get; init; }

    /// <summary>
    /// Set only when Hits is empty. A query can come back empty two different ways - nothing
    /// matched, or the query was phrased entirely in words too common to search on - and an agent
    /// cannot tell them apart from an empty list.
    /// </summary>
    public string? Message { get; init; }
}
