namespace Bit.Bmotion;

/// <summary>
/// How a prerendered CSS animation honours <c>prefers-reduced-motion</c>. The component resolves it
/// from the same inputs as its runtime reduced-motion check, except that the OS preference itself
/// is left to a media query - nothing can ask the browser while the page is being prerendered.
/// </summary>
internal enum BmotionPrerenderMotionPolicy
{
    /// <summary>Always the full animation.</summary>
    Full,

    /// <summary>Always the reduced animation (transforms snap, opacity and colour still animate).</summary>
    Reduced,

    /// <summary>The full animation, or the reduced one when the OS asks for reduced motion.</summary>
    FollowOs,
}
