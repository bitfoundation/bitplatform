namespace Bit.Bmotion;

/// <summary>
/// A component's part in a presence's exits (see <see cref="BmotionPresenceContext.Register()"/>). Dispose it when the
/// component is disposed: a registration that is gone holds nothing up.
/// </summary>
public sealed class BmotionPresenceRegistration : IDisposable
{
    private BmotionPresenceContext? _presence;

    internal BmotionPresenceRegistration(BmotionPresenceContext presence) => _presence = presence;

    /// <summary>Whether the content is present - <c>false</c> once it has been removed and is exiting.</summary>
    public bool IsPresent => _presence?.IsPresent ?? true;

    /// <summary>Whether this registration has reported the current exit over (<see cref="SafeToRemove"/>).</summary>
    public bool IsExitComplete => _presence?.HasCompleted(this) == true;

    /// <summary>
    /// Reports this component's exit over: once everything else in the presence has finished too, the content is
    /// removed. Ignored while the content is present - an exit cancelled by a re-entry completes nothing - so it is
    /// safe to call from an exit that was overtaken.
    /// </summary>
    public void SafeToRemove() => _presence?.Complete(this);

    /// <summary>Stops taking part: a presence waiting on this registration no longer does.</summary>
    public void Dispose()
    {
        var presence = _presence;
        _presence = null;
        presence?.Remove(this);
    }
}
