namespace Bit.Butil.Demo.Server.Dtos;

/// <summary>One result of a search across everything this MCP server knows about Bit.Butil.</summary>
public record ButilSearchHitDto
{
    /// <summary>What was found: "Guide section", "Docs page", "API service", "API method", "Source file", ...</summary>
    public required string Kind { get; init; }

    public required string Title { get; init; }

    /// <summary>Where the hit sits: the owning section, type or group.</summary>
    public string? Context { get; init; }

    /// <summary>The tool call that returns the full text of this hit - call it verbatim.</summary>
    public required string Tool { get; init; }

    /// <summary>The matching text, with a little of what surrounds it.</summary>
    public required string Snippet { get; init; }
}
