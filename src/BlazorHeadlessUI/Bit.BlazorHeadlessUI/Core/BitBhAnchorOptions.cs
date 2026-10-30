namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Configuration for anchoring a floating panel (dropdown, popover, tooltip …)
/// relative to a reference element (button, trigger) or a point.
///
/// <para>
/// Mirrors the Headless UI <c>anchor</c> prop and the Floating UI middleware set:
/// placement (<see cref="To"/>), gap, offset, viewport padding, flip, shift,
/// size (exposed as CSS variables), arrow positioning and hide-when-detached.
/// </para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BitBhMenuItems Anchor="@(new BitBhAnchorOptions { To = "bottom start", Gap = 4 })"&gt;
/// </code>
/// Or with the implicit string conversion:
/// <code>
/// &lt;BitBhMenuItems Anchor="@("bottom start")"&gt;
/// </code>
///
/// <para><b>CSS variables set on the floating element:</b></para>
/// <list type="bullet">
///   <item><c>--bit-bh-anchor-width</c> / <c>--bit-bh-anchor-height</c> — size of the reference (<c>--button-width</c> is an alias).</item>
///   <item><c>--bit-bh-available-width</c> / <c>--bit-bh-available-height</c> — space left on the chosen side (use for <c>max-height</c>).</item>
///   <item><c>--bit-bh-transform-origin</c> — origin pointing at the reference, for scale animations.</item>
///   <item><c>--anchor-gap</c>, <c>--anchor-offset</c>, <c>--anchor-padding</c> — the configured values.</item>
/// </list>
/// <para>Data attributes: <c>data-side</c>, <c>data-align</c>, <c>data-anchor</c>, <c>data-reference-hidden</c>.</para>
/// </summary>
public sealed class BitBhAnchorOptions
{
    /// <summary>
    /// Where to position the floating panel relative to the reference element.
    /// Use <c>"top"</c>, <c>"right"</c>, <c>"bottom"</c>, or <c>"left"</c> to center
    /// along the appropriate edge. Combine with <c>"start"</c> or <c>"end"</c> for
    /// corner alignment, e.g. <c>"bottom start"</c>, <c>"top end"</c>. <c>start</c> /
    /// <c>end</c> follow the writing direction (they flip in RTL).
    /// Default: <c>"bottom"</c>.
    /// </summary>
    public string To { get; set; } = "bottom";

    /// <summary>The space (px) between the reference element and the floating panel. Default: 0.</summary>
    public int Gap { get; set; }

    /// <summary>The distance (px) the floating panel is nudged along the alignment axis. Default: 0.</summary>
    public int Offset { get; set; }

    /// <summary>The minimum space (px) between the floating panel and the viewport edges. Default: 8.</summary>
    public int Padding { get; set; } = 8;

    /// <summary>Flip to the opposite side when there isn't enough room. Default: true.</summary>
    public bool Flip { get; set; } = true;

    /// <summary>Shift along the alignment axis to stay inside the viewport. Default: true.</summary>
    public bool Shift { get; set; } = true;

    /// <summary>Make the panel at least as wide as the reference element. Default: false.</summary>
    public bool MatchReferenceWidth { get; set; }

    /// <summary>Hide the panel (<c>visibility:hidden</c>) when the reference scrolls out of view. Default: false.</summary>
    public bool HideWhenDetached { get; set; }

    /// <summary>Minimum distance (px) between an arrow and the panel's corners. Default: 0.</summary>
    public int ArrowPadding { get; set; }

    /// <summary>Creates an <see cref="BitBhAnchorOptions"/> from a placement string.</summary>
    /// <param name="placement">A placement string like <c>"bottom"</c>, <c>"bottom start"</c>, <c>"top end"</c>.</param>
    public static BitBhAnchorOptions Parse(string placement) => new() { To = placement };

    /// <summary>Implicit conversion from a placement string.</summary>
    public static implicit operator BitBhAnchorOptions(string placement) => Parse(placement);

    /// <summary>Returns a copy with <see cref="To"/> replaced.</summary>
    public BitBhAnchorOptions WithPlacement(string placement)
    {
        var copy = (BitBhAnchorOptions)MemberwiseClone();
        copy.To = placement;
        return copy;
    }

    internal Dictionary<string, object?> ToJs(string? arrowId = null) => new()
    {
        ["to"] = To,
        ["gap"] = Gap,
        ["offset"] = Offset,
        ["padding"] = Padding,
        ["flip"] = Flip,
        ["shift"] = Shift,
        ["matchWidth"] = MatchReferenceWidth,
        ["hideWhenDetached"] = HideWhenDetached,
        ["arrowPadding"] = ArrowPadding,
        ["arrowId"] = arrowId,
    };
}
