namespace Bit.Butil;

/// <summary>
/// What kind of session to ask for, the mode of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/XRSystem/requestSession">XRSystem.requestSession()</see>.
/// </summary>
public enum XrSessionMode
{
    /// <summary>
    /// Rendered into a canvas in the page, using the device's sensors for orientation. No headset and
    /// no user gesture needed - the "magic window" mode, and the only one most phones offer.
    /// </summary>
    Inline,

    /// <summary>A headset takes over the display entirely. Needs a user gesture and a connected device.</summary>
    ImmersiveVr,

    /// <summary>Rendered over the real world - a passthrough headset, or a phone's camera. Needs a user gesture.</summary>
    ImmersiveAr
}
