namespace Bit.Butil;

/// <summary>
/// Which set of axes a spatial sensor reports against - the accelerometer, gyroscope, magnetometer,
/// gravity and linear-acceleration sensors as well as both orientation sensors. Only
/// <see cref="SensorType.AmbientLight"/> has no axes to frame and ignores it.
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
