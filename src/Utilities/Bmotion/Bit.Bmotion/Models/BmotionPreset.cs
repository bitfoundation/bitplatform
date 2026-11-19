namespace Bit.Bmotion;

/// <summary>
/// A reusable animation: what an element starts from, animates to, leaves with and does while hovered, pressed,
/// focused or in view, and how it moves. Hand one to <see cref="Bmotion.Preset"/> - any target or transition set on
/// the component itself wins over the preset's - or build your own design system's set:
/// <code>
/// &lt;Bmotion Preset="BmotionPresets.Slide(BmotionEdge.Bottom)"&gt;...&lt;/Bmotion&gt;
///
/// // compose: a fade that also scales, on a spring
/// var dialog = (BmotionPresets.Fade() + BmotionPresets.Scale(0.95)) with { Transition = Bm.Spring(bounce: 0.2, duration: 0.4) };
/// </code>
/// See <see cref="BmotionPresets"/> for the built-in set.
/// </summary>
public sealed record BmotionPreset
{
    /// <summary>Where the element starts (<see cref="Bmotion.Initial"/>).</summary>
    public BmotionProps? Initial { get; init; }

    /// <summary>What it animates to on mount and rests at (<see cref="Bmotion.Animate"/>).</summary>
    public BmotionProps? Animate { get; init; }

    /// <summary>What it animates to before it leaves (<see cref="Bmotion.Exit"/>; needs a presence component).</summary>
    public BmotionProps? Exit { get; init; }

    /// <summary>While the pointer hovers it (<see cref="Bmotion.WhileHover"/>).</summary>
    public BmotionProps? WhileHover { get; init; }

    /// <summary>While it is pressed (<see cref="Bmotion.WhileTap"/>).</summary>
    public BmotionProps? WhileTap { get; init; }

    /// <summary>While it has focus (<see cref="Bmotion.WhileFocus"/>).</summary>
    public BmotionProps? WhileFocus { get; init; }

    /// <summary>While it is in the viewport (<see cref="Bmotion.WhileInView"/>).</summary>
    public BmotionProps? WhileInView { get; init; }

    /// <summary>When it counts as in view (<see cref="Bmotion.Viewport"/>).</summary>
    public BmotionViewport? Viewport { get; init; }

    /// <summary>How it moves (<see cref="Bmotion.Transition"/>).</summary>
    public BmotionTransition? Transition { get; init; }

    /// <summary>
    /// What the element does when motion is reduced (<see cref="Bmotion.ReducedMotion"/>, which wins when set);
    /// <c>null</c> is <see cref="BmotionReducedMotionBehavior.Reduce"/>.
    /// </summary>
    public BmotionReducedMotionBehavior? ReducedMotion { get; init; }

    /// <summary>
    /// Layers <paramref name="over"/> onto <paramref name="under"/>: each target merges property by property (the
    /// right-hand side's value wins where both set one - so a fade plus a slide fades <em>and</em> slides), and the
    /// right-hand side's transition, viewport and reduced-motion behaviour win when it has them.
    /// </summary>
    public static BmotionPreset operator +(BmotionPreset under, BmotionPreset over)
    {
        ArgumentNullException.ThrowIfNull(under);
        ArgumentNullException.ThrowIfNull(over);
        return new BmotionPreset
        {
            Initial = BmotionProps.Merge(under.Initial, over.Initial),
            Animate = BmotionProps.Merge(under.Animate, over.Animate),
            Exit = BmotionProps.Merge(under.Exit, over.Exit),
            WhileHover = BmotionProps.Merge(under.WhileHover, over.WhileHover),
            WhileTap = BmotionProps.Merge(under.WhileTap, over.WhileTap),
            WhileFocus = BmotionProps.Merge(under.WhileFocus, over.WhileFocus),
            WhileInView = BmotionProps.Merge(under.WhileInView, over.WhileInView),
            Viewport = over.Viewport ?? under.Viewport,
            Transition = over.Transition ?? under.Transition,
            ReducedMotion = over.ReducedMotion ?? under.ReducedMotion,
        };
    }
}
