namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/UserActivation">UserActivation</see> -
/// the browser's record of whether the user has interacted with the page.
/// </summary>
public class UserActivationState
{
    /// <summary>
    /// Sticky activation: the user has interacted at least once since the page loaded. Gates things
    /// like autoplay with sound, and never becomes false again.
    /// </summary>
    public bool HasBeenActive { get; set; }

    /// <summary>
    /// Transient activation: an interaction happened recently enough to still be spendable on a
    /// gesture-gated API (opening a window, reading the clipboard, requesting storage access).
    /// Expires after a few seconds, and some APIs consume it.
    /// </summary>
    public bool IsActive { get; set; }
}
