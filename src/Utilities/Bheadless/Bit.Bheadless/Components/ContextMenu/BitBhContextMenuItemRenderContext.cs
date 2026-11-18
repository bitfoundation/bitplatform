namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by <see cref="BitBhContextMenuItem"/>.</summary>
public sealed record BitBhContextMenuItemRenderContext
{
    /// <summary>Whether this item is highlighted.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Whether this item is disabled.</summary>
    public required bool Disabled { get; init; }
}
