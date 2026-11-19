namespace Bit.Balette;

/// <summary>Extra information available to sources.</summary>
public sealed record BaletteSourceContext(string? ModePrefix, string? ParentCommandId, IServiceProvider Services);
