namespace Bit.BlazorUI;

/// <summary>
/// Supplies information about a CSS transition event - transitionrun, transitionstart, transitionend or
/// transitioncancel - handled with the directives <see cref="Events.EventHandlers"/> registers.
/// </summary>
/// <remarks>
/// The transition events bubble, and a change that transitions several properties fires one event per property, so
/// a handler on an element also hears every transition of everything inside it; <see cref="PropertyName"/> and
/// <see cref="TargetId"/> are what tell the one it waits for from the rest. The fields are filled in by the library's
/// script (bit.blazorui.js): an event that reaches the handler before the script has registered them, or in an app
/// that does not load it, carries them empty.
/// </remarks>
public class BitTransitionEventArgs : EventArgs
{
    /// <summary>
    /// The name of the CSS property the transition is for (opacity, transform), in its longhand form: a transition of
    /// the padding shorthand fires one event for each of padding-top, padding-right, padding-bottom and padding-left.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// The time in seconds the transition had been running for when the event fired, not counting its
    /// transition-delay.
    /// </summary>
    public double ElapsedTime { get; set; }

    /// <summary>
    /// The pseudo-element the transition runs on, starting with '::' (::before, ::after), or empty when it runs on the
    /// element itself.
    /// </summary>
    public string PseudoElement { get; set; } = string.Empty;

    /// <summary>
    /// The id of the element the transition runs on, or empty when it has none. The event bubbles, so it is the
    /// element that tells an element's own transition from one of its content: a handler on an element with an id
    /// hears its own transition when this is that id.
    /// </summary>
    public string TargetId { get; set; } = string.Empty;
}
