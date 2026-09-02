namespace Bit.Butil.Demo.Server.Dtos;

/// <summary>What GetButilApiDetails answers: the type's reference, the list to pick one from, or why there is none.</summary>
public record ButilApiDetailsResultDto
{
    /// <summary>The full reference of the type, when a public type goes by the requested name.</summary>
    public ButilApiTypeDetailsDto? Details { get; init; }

    /// <summary>
    /// Every public type, set instead of Details when the call named none. A listing does not earn
    /// a tool of its own - it is what the retrieval tool answers when asked for nothing in
    /// particular, which is the one moment a caller wants it.
    /// </summary>
    public ButilApiTypeDto[]? Types { get; init; }

    /// <summary>Set instead of Details when nothing matched - it names the closest candidates.</summary>
    public string? Message { get; init; }
}
