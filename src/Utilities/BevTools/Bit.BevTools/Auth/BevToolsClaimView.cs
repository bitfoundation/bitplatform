namespace Bit.BevTools;

/// <summary>One claim of the signed-in user, its type shortened to the name it is known by ("role", not the schema URI).</summary>
public sealed record BevToolsClaimView(string Type, string Value, string? Issuer);
