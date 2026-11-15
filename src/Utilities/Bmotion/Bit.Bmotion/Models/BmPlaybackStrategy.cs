namespace Bit.Bmotion;

/// <summary>
/// How the engine plays (part of) an animation - reported by <see cref="BmotionAnimationEngine.AnimationEvent"/> and
/// the <c>Bit.Bmotion</c> meter, so it is never a guess why an animation ran where it did.
/// </summary>
public enum BmPlaybackStrategy
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

/// <summary>What happened to (part of) an animation.</summary>
public enum BmAnimationEventKind
{
    /// <summary>It started playing with the event's <see cref="BmotionAnimationEvent.Strategy"/>.</summary>
    Started,

    /// <summary>It reached its end.</summary>
    Finished,

    /// <summary>
    /// It ended before its end: stopped, replaced by a newer animation of the same properties, its element removed,
    /// or the start overtaken before it played.
    /// </summary>
    Interrupted,
}

/// <summary>
/// One step in the life of an animation, raised by <see cref="BmotionAnimationEngine.AnimationEvent"/>. An animation
/// whose properties play in different ways (some in the browser, some on the frame loop) reports one
/// <see cref="BmAnimationEventKind.Started"/> and one end per part. It carries property names, never the animated
/// values or anything of the page's content, so it is safe to forward to telemetry.
/// </summary>
/// <param name="Kind">Started, finished or interrupted.</param>
/// <param name="ElementId">The engine id of the element (the <c>id</c> attribute <c>&lt;Bmotion&gt;</c> renders).</param>
/// <param name="Properties">The animated properties of this part, by engine key (<c>x</c>, <c>opacity</c>, <c>backgroundColor</c>, ...).</param>
/// <param name="Strategy">How this part plays.</param>
/// <param name="Reason">
/// Why the part plays on the frame loop, or lands at once without one - <c>null</c> when the browser plays it or it
/// is instant by design.
/// </param>
public sealed record BmotionAnimationEvent(
    BmAnimationEventKind Kind,
    string ElementId,
    IReadOnlyList<string> Properties,
    BmPlaybackStrategy Strategy,
    string? Reason)
{
    /// <summary>For an end, how long the part ran, in milliseconds since its start; 0 for a start.</summary>
    public double ElapsedMs { get; init; }
}
