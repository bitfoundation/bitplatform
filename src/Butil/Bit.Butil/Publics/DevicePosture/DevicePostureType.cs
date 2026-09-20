namespace Bit.Butil;

/// <summary>
/// How the device is folded right now.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/DevicePosture/type">DevicePosture.type</see>
/// </summary>
public enum DevicePostureType
{
    /// <summary>
    /// A flat screen: an ordinary device, or a foldable currently opened out. Everything that is
    /// not a foldable reports this, so it is the value a layout should treat as normal.
    /// </summary>
    Continuous,

    /// <summary>
    /// The screen is folded across a hinge - a laptop-like posture on a book-fold device, or a
    /// flip phone half-closed. The fold's position comes from CSS
    /// (<c>env(viewport-segment-*)</c>), not from this API.
    /// </summary>
    Folded
}
