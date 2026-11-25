namespace Bit.BlazorUI;

/// <summary>Extra information available to sources.</summary>
public sealed record BitCommandPaletteSourceContext(string? ModePrefix, string? ParentCommandId, IServiceProvider Services);
