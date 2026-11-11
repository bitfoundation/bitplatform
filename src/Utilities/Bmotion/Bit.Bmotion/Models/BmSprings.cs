namespace Bit.Bmotion;

/// <summary>
/// Named springs for the motion interfaces need, so a component can say <em>how</em> it should feel instead of
/// tuning physics. Each call returns a new <see cref="BmSpring"/>, free to adjust (<c>var s = BmSprings.Bouncy(); s.Delay = 0.1;</c>):
/// <code>
/// &lt;Bmotion Animate="Bm.To(scale: 1)" Transition="BmSprings.Snappy()"&gt;...&lt;/Bmotion&gt;
/// </code>
/// The durations below are when the motion visibly arrives; a spring then takes a little longer to come to rest
/// (its completion). The duration-based ones (all but <see cref="Heavy"/>) use the <c>bounce</c>/<c>duration</c>
/// model, so they take the same time over any distance; <see cref="BmSpring.Analyze"/> gives the exact settling time,
/// overshoot and trace of any of them. Like every spring they keep a thrown or interrupted element's velocity.
/// </summary>
public static class BmSprings
{
    /// <summary>
    /// Critically damped, arriving in about 0.5 s without overshoot - the safe default for interface chrome (menus,
    /// panels, tabs).
    /// </summary>
    public static BmSpring Smooth() => Bm.Spring(bounce: 0, duration: 0.5);

    /// <summary>
    /// Quick, arriving in about 0.3 s and settling crisply, with at most a hair of overshoot - buttons, toggles,
    /// anything that answers a click.
    /// </summary>
    public static BmSpring Snappy() => Bm.Spring(bounce: 0.25, duration: 0.3);

    /// <summary>
    /// A visible, playful bounce (about 11 % past the target), arriving in about 0.5 s - entrances that should feel
    /// lively (badges, toasts, likes).
    /// </summary>
    public static BmSpring Bouncy() => Bm.Spring(bounce: 0.6, duration: 0.5);

    /// <summary>
    /// Slow and soft, arriving in about 0.8 s without a visible bounce - large surfaces, page-level and ambient motion.
    /// </summary>
    public static BmSpring Gentle() => Bm.Spring(bounce: 0.1, duration: 0.8);

    /// <summary>Fast and firm, arriving in about 0.2 s without overshoot - small state changes that must not draw attention.</summary>
    public static BmSpring Stiff() => Bm.Spring(bounce: 0, duration: 0.2);

    /// <summary>
    /// A heavy mass that takes a moment to get going and settles without wobble in about a second - drawers, sheets,
    /// things with weight. A physical spring (stiffness 120, damping 38, mass 3: critically damped) rather than a
    /// duration-based one.
    /// </summary>
    public static BmSpring Heavy() => Bm.Spring(stiffness: 120, damping: 38, mass: 3);

    /// <summary>
    /// Near-instant and critically damped, arriving in about 0.15 s - for following input (an element trailing the
    /// pointer, a value tracking a slider), where any lag or overshoot feels like latency.
    /// </summary>
    public static BmSpring Responsive() => Bm.Spring(bounce: 0, duration: 0.15);
}
