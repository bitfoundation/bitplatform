namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by drawer parts for state-driven rendering.</summary>
public sealed record BitBhDrawerRenderContext
{
    /// <summary>Whether the drawer is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the drawer.</summary>
    public required Action Close { get; init; }
}
