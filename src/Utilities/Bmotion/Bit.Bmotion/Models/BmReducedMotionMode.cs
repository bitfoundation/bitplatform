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
