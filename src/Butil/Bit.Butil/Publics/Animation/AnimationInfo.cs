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
    /// How far in it is, in milliseconds - or as a percentage of the range for a scroll-driven one.
    /// </summary>
    public double CurrentTime { get; set; }

    /// <summary>When it was scheduled to start, on the timeline's own clock.</summary>
    public double StartTime { get; set; }

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
