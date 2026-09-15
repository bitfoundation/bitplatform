namespace Bit.Butil;

/// <summary>
/// One animation currently affecting an element, as reported by
/// <see cref="ElementReferenceAnimationExtensions.GetAnimations"/>.
/// </summary>
/// <remarks>
/// Unlike <see cref="AnimationHandle"/>, which only knows about animations Butil started, this
/// includes CSS animations and transitions the page never scripted - which is the reason to ask.
/// </remarks>
public class AnimationInfo
{
    /// <summary>
    /// The animation's own id. For a CSS animation this is the keyframes name; for a scripted one it
    /// is whatever was assigned, usually empty.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// <c>"idle"</c>, <c>"running"</c>, <c>"paused"</c> or <c>"finished"</c>.
    /// </summary>
    public string PlayState { get; set; } = string.Empty;

    /// <summary>Playback rate: 1 is normal speed, negative runs backwards.</summary>
    public double PlaybackRate { get; set; }

    /// <summary>
    /// How far in it is, in the unit named by <see cref="CurrentTimeUnit"/> - or null when the
    /// animation has no current time, as an idle one has not.
    /// </summary>
    /// <remarks>
    /// A scroll-driven animation measures progress in percent of its range rather than in
    /// milliseconds, so the number is meaningless without its unit: 0 with a <c>"percent"</c> unit is
    /// the start of the range, not the first millisecond.
    /// </remarks>
    public double? CurrentTime { get; set; }

    /// <summary>
    /// The unit <see cref="CurrentTime"/> is expressed in: <c>"ms"</c> for a time-driven animation,
    /// <c>"percent"</c> for a scroll-driven one, and empty when there is no current time.
    /// </summary>
    public string CurrentTimeUnit { get; set; } = string.Empty;

    /// <summary>
    /// When it was scheduled to start, on the timeline's own clock and in the unit named by
    /// <see cref="StartTimeUnit"/> - or null when it has not been scheduled.
    /// </summary>
    public double? StartTime { get; set; }

    /// <summary>The unit <see cref="StartTime"/> is expressed in. See <see cref="CurrentTimeUnit"/>.</summary>
    public string StartTimeUnit { get; set; } = string.Empty;

    /// <summary>True while a play or pause is waiting on the next frame to take effect.</summary>
    public bool Pending { get; set; }

    /// <summary>
    /// <c>"active"</c>, <c>"removed"</c> or <c>"persisted"</c> - whether a finished filling animation
    /// has been superseded and discarded. See <see cref="AnimationHandle.Persist"/>.
    /// </summary>
    public string ReplaceState { get; set; } = string.Empty;

    /// <summary>
    /// What kind it is: <c>"CSSAnimation"</c>, <c>"CSSTransition"</c> or <c>"Animation"</c> for a
    /// scripted one.
    /// </summary>
    public string Kind { get; set; } = string.Empty;
}
