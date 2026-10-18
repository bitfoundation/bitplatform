namespace Bit.Butil.Demo.Server.Dtos;

/// <summary>A public type of the Bit.Butil assembly.</summary>
public record ButilApiTypeDto
{
    public required string Name { get; init; }

    /// <summary>Service, Static class, Interface, Enum, Attribute, Delegate, Class, Struct or Record.</summary>
    public required string Kind { get; init; }

    /// <summary>True for a class marked [ButilService] - the ones you inject by their own name.</summary>
    public required bool IsInjectable { get; init; }

    public string? Summary { get; init; }
}
