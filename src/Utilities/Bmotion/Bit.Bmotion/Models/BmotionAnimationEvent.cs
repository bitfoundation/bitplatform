namespace Bit.Bmotion;

/// <summary>
/// One step in the life of an animation, raised by <see cref="BmotionAnimationEngine.AnimationEvent"/>. An animation
/// whose properties play in different ways (some in the browser, some on the frame loop) reports one
/// <see cref="BmotionAnimationEventKind.Started"/> and one end per part. It carries property names, never the animated
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
    BmotionAnimationEventKind Kind,
    string ElementId,
    IReadOnlyList<string> Properties,
    BmotionPlaybackStrategy Strategy,
    string? Reason)
{
    /// <summary>For an end, how long the part ran, in milliseconds since its start; 0 for a start.</summary>
    public double ElapsedMs { get; init; }
}
