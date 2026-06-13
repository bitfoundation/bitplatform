namespace Bit.Bmotion.Models;

/// <summary>
/// Union type for animation target parameters (Initial, Animate, Exit, WhileHover, …).
/// Can be implicitly constructed from <see cref="AnimationProps"/>, a variant name string,
/// or <c>false</c> to disable the target entirely.
/// </summary>
public sealed class AnimationTarget
{
    /// <summary>Direct set of animation properties.</summary>
    public AnimationProps? Props { get; private init; }

    /// <summary>Name of a variant defined in the nearest Motion ancestor's Variants dictionary.</summary>
    public string? Variant { get; private init; }

    /// <summary>When true this target is explicitly disabled (e.g. <c>Initial="false"</c>).</summary>
    public bool IsDisabled { get; private init; }

    public bool HasProps => Props != null;
    public bool IsVariant => Variant != null;

    // ── Implicit conversions ──────────────────────────────────────────────────
    public static implicit operator AnimationTarget(AnimationProps props)
        => new() { Props = props };

    public static implicit operator AnimationTarget(string variant)
        => new() { Variant = variant };

    public static implicit operator AnimationTarget(bool value)
        => value ? new() { Props = new AnimationProps() } : new() { IsDisabled = true };
}
