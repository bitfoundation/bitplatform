namespace Bit.Butil.Demo.Server.Dtos;

/// <summary>A member (property, method, field or enum value) of a public Bit.Butil type.</summary>
public record ButilApiMemberDto
{
    public required string Name { get; init; }

    /// <summary>Property, Method, Field, Event or EnumValue.</summary>
    public required string Kind { get; init; }

    /// <summary>The C# type of the member, or the return type for a method.</summary>
    public string? Type { get; init; }

    /// <summary>The method's parameter list, e.g. "(string key, string value)".</summary>
    public string? Signature { get; init; }

    /// <summary>The constant value of a const field, or the value a property has on a fresh instance.</summary>
    public string? Default { get; init; }

    public string? Summary { get; init; }

    /// <summary>The XML remarks, when the member has any - they carry the caveats.</summary>
    public string? Remarks { get; init; }
}
