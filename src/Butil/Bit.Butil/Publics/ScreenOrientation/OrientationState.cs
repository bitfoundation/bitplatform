namespace Bit.Butil;

/// <summary>
/// The screen orientation as one value, so a reader and a change handler both get the angle and the
/// type together rather than in two round trips.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation">ScreenOrientation</see>
/// </summary>
public class OrientationState
{
    /// <summary>
    /// Degrees clockwise from the device's natural orientation - 0, 90, 180 or 270.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/angle">ScreenOrientation.angle</see>
    /// </summary>
    public ushort Angle { get; set; }

    /// <summary>
    /// The orientation as a named axis and direction.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/type">ScreenOrientation.type</see>
    /// </summary>
    public ScreenOrientationType Type { get; set; }
}
