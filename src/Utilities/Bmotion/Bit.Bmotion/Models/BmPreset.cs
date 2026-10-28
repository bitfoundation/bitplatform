namespace Bit.Bmotion;

/// <summary>
/// A reusable animation: what an element starts from, animates to, leaves with and does while hovered, pressed,
/// focused or in view, and how it moves. Hand one to <see cref="Bmotion.Preset"/> - any target or transition set on
/// the component itself wins over the preset's - or build your own design system's set:
/// <code>
/// &lt;Bmotion Preset="BmPresets.Slide(BmEdge.Bottom)"&gt;...&lt;/Bmotion&gt;
///
/// // compose: a fade that also scales, on a spring
/// var dialog = (BmPresets.Fade() + BmPresets.Scale(0.95)) with { Transition = Bm.Spring(bounce: 0.2, duration: 0.4) };
/// </code>
/// See <see cref="BmPresets"/> for the built-in set.
/// </summary>
public sealed record BmPreset
{
    /// <summary>Where the element starts (<see cref="Bmotion.Initial"/>).</summary>
    public BmProps? Initial { get; init; }

    /// <summary>What it animates to on mount and rests at (<see cref="Bmotion.Animate"/>).</summary>
    public BmProps? Animate { get; init; }

    /// <summary>What it animates to before it leaves (<see cref="Bmotion.Exit"/>; needs a presence component).</summary>
    public BmProps? Exit { get; init; }

    /// <summary>While the pointer hovers it (<see cref="Bmotion.WhileHover"/>).</summary>
    public BmProps? WhileHover { get; init; }

    /// <summary>While it is pressed (<see cref="Bmotion.WhileTap"/>).</summary>
    public BmProps? WhileTap { get; init; }

    /// <summary>While it has focus (<see cref="Bmotion.WhileFocus"/>).</summary>
    public BmProps? WhileFocus { get; init; }

    /// <summary>While it is in the viewport (<see cref="Bmotion.WhileInView"/>).</summary>
    public BmProps? WhileInView { get; init; }

    /// <summary>When it counts as in view (<see cref="Bmotion.Viewport"/>).</summary>
    public BmViewport? Viewport { get; init; }

    /// <summary>How it moves (<see cref="Bmotion.Transition"/>).</summary>
    public BmTransition? Transition { get; init; }

    /// <summary>
    /// Layers <paramref name="over"/> onto <paramref name="under"/>: each target merges property by property (the
    /// right-hand side's value wins where both set one - so a fade plus a slide fades <em>and</em> slides), and the
    /// right-hand side's transition and viewport win when it has them.
    /// </summary>
    public static BmPreset operator +(BmPreset under, BmPreset over)
    {
        ArgumentNullException.ThrowIfNull(under);
        ArgumentNullException.ThrowIfNull(over);
        return new BmPreset
        {
            Initial = BmProps.Merge(under.Initial, over.Initial),
            Animate = BmProps.Merge(under.Animate, over.Animate),
            Exit = BmProps.Merge(under.Exit, over.Exit),
            WhileHover = BmProps.Merge(under.WhileHover, over.WhileHover),
            WhileTap = BmProps.Merge(under.WhileTap, over.WhileTap),
            WhileFocus = BmProps.Merge(under.WhileFocus, over.WhileFocus),
            WhileInView = BmProps.Merge(under.WhileInView, over.WhileInView),
            Viewport = over.Viewport ?? under.Viewport,
            Transition = over.Transition ?? under.Transition,
        };
    }
}
