namespace Bit.Bheadless;

/// <summary>
/// Render-fragment context exposed by <see cref="BitBhPopoverButton"/> and
/// <see cref="BitBhPopoverPanel"/> for state-driven rendering.
/// </summary>
public sealed record BitBhPopoverRenderContext
{
    /// <summary>Whether the popover panel is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the popover. Useful from inside the panel (e.g. a "Close" link).</summary>
    public required Action Close { get; init; }
}
