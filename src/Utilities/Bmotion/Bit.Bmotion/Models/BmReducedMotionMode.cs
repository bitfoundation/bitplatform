namespace Bit.Bmotion;

/// <summary>
/// Controls how Bit.Bmotion honors the user's <c>prefers-reduced-motion</c> accessibility setting,
/// set globally via <see cref="BitBmotionOptions.ReducedMotion"/>. A local
/// <c>&lt;BmotionConfig ReduceMotion="true|false"&gt;</c> always overrides this for its subtree.
/// </summary>
public enum BmReducedMotionMode
{
    /// <summary>
    /// <b>Back-compat default.</b> The OS <c>prefers-reduced-motion</c> preference is respected only
    /// for elements inside a <see cref="BmotionConfig"/>; elements without one always animate. This
    /// is the behavior of Bit.Bmotion before the option existed.
    /// </summary>
    IgnoreUnlessConfigured = 0,

    /// <summary>
    /// <b>Recommended.</b> Respect the OS <c>prefers-reduced-motion</c> preference everywhere, with
    /// no <see cref="BmotionConfig"/> required - matching the web-platform default and Motion's
    /// <c>"user"</c> semantics. When reduced, transforms/layout are suppressed (they snap to their
    /// target) while opacity and color still animate.
    /// </summary>
    User = 1,

    /// <summary>Always reduce motion, regardless of the OS preference.</summary>
    Always = 2,

    /// <summary>Never reduce motion, regardless of the OS preference (an explicit override off).</summary>
    Never = 3,
}

/// <summary>
/// What one element does when motion is reduced - set on <see cref="Bmotion.ReducedMotion"/> or a
/// <see cref="BmPreset.ReducedMotion"/>. Whether motion is reduced at all is the user's and the app's decision
/// (<see cref="BmReducedMotionMode"/>, <see cref="BmotionConfig"/>); this says how an element answers it, since a
/// decorative flourish and a progress bar shouldn't answer the same way. Reducing motion is about vestibular comfort:
/// the motion that triggers discomfort is movement, scaling and parallax, not changes of colour or opacity.
/// </summary>
public enum BmReducedMotionBehavior
{
    /// <summary>
    /// The default: movement, scaling, rotation and size changes land at once, while opacity, colours, filters and
    /// shadows keep their timing - an entrance becomes a fade, and a shake does nothing.
    /// </summary>
    Reduce = 0,

    /// <summary>
    /// Everything lands at once, fades included - for decorative motion that adds nothing once it is calm (an ambient
    /// loop, a hero crossfade), or content that must simply be there.
    /// </summary>
    Instant = 1,

    /// <summary>
    /// The animation plays in full - for motion that carries information and is calm already: a progress bar
    /// filling, a counter, an element settling where a drag left it. Use it sparingly, and never for motion that
    /// travels far, zooms or spins.
    /// </summary>
    Full = 2,
}
