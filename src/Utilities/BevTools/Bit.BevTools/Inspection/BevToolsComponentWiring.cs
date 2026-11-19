namespace Bit.BevTools;

/// <summary>What one component wired up in its latest render, and whether it could be read at all.</summary>
public sealed record BevToolsComponentWiring(IReadOnlyList<BevToolsEventBinding> Bindings, int TotalBindings, string? Unavailable)
{
    public static BevToolsComponentWiring NotReadable(string reason) => new([], 0, reason);
}
