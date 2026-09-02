namespace Bit.Butil.Demo.Server.Dtos;

/// <summary>The full reference of one public Bit.Butil type.</summary>
public record ButilApiTypeDetailsDto
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string Kind { get; init; }

    /// <summary>How to obtain one, e.g. "@inject Bit.Butil.Clipboard clipboard" - null when it is not a service.</summary>
    public string? Inject { get; init; }

    public string[]? Implements { get; init; }

    public string? Summary { get; init; }

    public string? Remarks { get; init; }

    /// <summary>The documentation page covering this type, when the site has one.</summary>
    public string? DocsUrl { get; init; }

    public required ButilApiMemberDto[] Members { get; init; }
}
