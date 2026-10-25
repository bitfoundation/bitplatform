namespace Bit.BlazorUI;

/// <summary>
/// Supplies information about a CSS animation event - animationstart, animationend, animationiteration or
/// animationcancel - handled with the directives <see cref="Events.EventHandlers"/> registers.
/// </summary>
/// <remarks>
/// The animation events bubble, so a handler on an element also hears the animations of everything inside it;
/// <see cref="AnimationName"/> and <see cref="TargetId"/> are what tell its own animation from theirs. The fields
/// are filled in by the library's script (bit.blazorui.js): an event that reaches the handler before the script has
/// registered them, or in an app that does not load it, carries them empty.
/// </remarks>
public class BitAnimationEventArgs : EventArgs
{
    /// <summary>
    /// The name of the @keyframes rule the event belongs to (the animation-name of the animation).
    /// </summary>
    public string AnimationName { get; set; } = string.Empty;

    /// <summary>
    /// The time in seconds the animation had been running for when the event fired, not counting the time it was paused
    /// for. For an animationstart it is the time skipped by a negative animation-delay, which is normally zero.
    /// </summary>
    public double ElapsedTime { get; set; }

    /// <summary>
    /// The pseudo-element the animation runs on, starting with '::' (::before, ::after), or empty when it runs on the
    /// element itself.
    /// </summary>
    public string PseudoElement { get; set; } = string.Empty;

    /// <summary>
    /// The id of the element the animation runs on, or empty when it has none. The event bubbles, so it is the
    /// element that tells an element's own animation from one of its content: a handler on an element with an id hears
    /// its own animation when this is that id.
    /// </summary>
    public string TargetId { get; set; } = string.Empty;
}
