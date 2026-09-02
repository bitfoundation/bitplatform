namespace Bit.Butil;

/// <summary>
/// Which set of axes an orientation sensor reports against. Only the orientation sensors take one.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/OrientationSensor">OrientationSensor</see>
/// </summary>
public enum SensorReferenceFrame
{
    /// <summary>
    /// The device's own axes, ignoring how the screen is rotated. Right for anything reasoning
    /// about the hardware itself.
    /// </summary>
    Device,

    /// <summary>
    /// Axes that follow the current screen orientation, so "up" stays up after the user rotates
    /// the phone. Right for anything drawn on screen.
    /// </summary>
    Screen
}
