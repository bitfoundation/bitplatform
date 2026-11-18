namespace Bit.Bmotion;

/// <summary>
/// How the engine plays (part of) an animation - reported by <see cref="BmotionAnimationEngine.AnimationEvent"/> and
/// the <c>Bit.Bmotion</c> meter, so it is never a guess why an animation ran where it did.
/// </summary>
public enum BmotionPlaybackStrategy
{
    /// <summary>
    /// The values land at once: a zero-length transition (<c>Bm.Tween(0)</c>, a reduced-motion snap, an instant
    /// set). Nothing is animated, by design.
    /// </summary>
    Instant,

    /// <summary>
    /// The browser plays it as a two-keyframe Web Animation with the curve as its easing (<c>linear()</c> samples or
    /// a keyword) - the cheapest shape, on every runtime.
    /// </summary>
    BrowserTimeline,

    /// <summary>
    /// The browser plays frames recorded by running the engine's own drivers ahead of time (per-property
    /// transitions, keyframes with per-segment easing, springs carrying velocity, inertia, arcs, ...) - exact, and on
    /// every runtime, at the cost of a larger payload.
    /// </summary>
    BrowserRecorded,

    /// <summary>
    /// The C# frame loop drives it, one synchronous call per frame (Blazor WebAssembly only): per-frame callbacks,
    /// a playback rate other than 1, SVG <c>d</c>/<c>points</c> morphs, transforms a drag owns, or what the browser
    /// couldn't reproduce exactly.
    /// </summary>
    FrameLoop,

    /// <summary>
    /// It needed the frame loop, and this runtime has none (Blazor Server): it landed on its target at once. The
    /// event's <see cref="BmotionAnimationEvent.Reason"/> says what needed the loop.
    /// </summary>
    InstantWithoutFrameLoop,
}
