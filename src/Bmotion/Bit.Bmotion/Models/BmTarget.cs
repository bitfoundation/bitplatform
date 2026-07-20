namespace Bit.Bmotion;
/// <summary>
/// Union type for animation target parameters (Initial, Animate, Exit, WhileHover, …).
/// Can be implicitly constructed from <see cref="BmProps"/>, a variant name string,
/// or <c>false</c> to disable the target entirely.
/// </summary>
public sealed class BmTarget
{
    /// <summary>Direct set of animation properties.</summary>
    public BmProps? Props { get; private init; }

    /// <summary>Name of a variant defined in the nearest Bmotion ancestor's Variants dictionary.</summary>
    public string? Variant { get; private init; }

    /// <summary>When true this target is explicitly disabled (e.g. <c>Initial="false"</c>).</summary>
    public bool IsDisabled { get; private init; }

    public bool HasProps => Props != null;
    public bool IsVariant => Variant != null;

    /// <summary>
    /// Value-based equivalence between two targets, used for change detection.
    /// Two prop targets are equivalent when their <see cref="BmProps"/> values match;
    /// two variant targets when they name the same variant.
    /// </summary>
    internal static bool AreEquivalent(BmTarget? a, BmTarget? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.IsDisabled || b.IsDisabled) return a.IsDisabled == b.IsDisabled;
        if (a.IsVariant || b.IsVariant)
            return string.Equals(a.Variant, b.Variant, StringComparison.OrdinalIgnoreCase);
        if (a.Props is null || b.Props is null) return a.Props is null && b.Props is null;
        return a.Props.ValueEquals(b.Props);
    }

    // ── Implicit conversions ──────────────────────────────────────────────────
    // Null inputs convert to a null target (not a target wrapping null) so downstream code can
    // distinguish "no target set" from "target set to empty props" - e.g. the variant-fallback
    // check in Bmotion only fires when Animate is genuinely null.
    public static implicit operator BmTarget?(BmProps? props)
        => props is null ? null : new() { Props = props };

    public static implicit operator BmTarget?(string? variant)
        => variant is null ? null : new() { Variant = variant };

    public static implicit operator BmTarget(bool value)
        => value ? new() { Props = new BmProps() } : new() { IsDisabled = true };
}
