namespace Bit.Butil;

/// <summary>
/// One <see href="https://developer.mozilla.org/en-US/docs/Web/API/DeviceMotionEvent">DeviceMotionEvent</see>
/// reading: how the device is accelerating and rotating.
/// </summary>
/// <remarks>
/// The three vectors are flattened into scalar members so the whole reading serializes without
/// nested objects. Missing values arrive as 0, as they do on hardware that reports only some of
/// them - a device with no gyroscope leaves the rotation members at 0.
/// </remarks>
public class DeviceMotionReading
{
    /// <summary>Acceleration along X, excluding gravity, in m/s².</summary>
    public double AccelerationX { get; set; }

    /// <summary>Acceleration along Y, excluding gravity, in m/s².</summary>
    public double AccelerationY { get; set; }

    /// <summary>Acceleration along Z, excluding gravity, in m/s².</summary>
    public double AccelerationZ { get; set; }

    /// <summary>Acceleration along X including gravity, in m/s². The only one some devices report.</summary>
    public double AccelerationIncludingGravityX { get; set; }

    /// <summary>Acceleration along Y including gravity, in m/s².</summary>
    public double AccelerationIncludingGravityY { get; set; }

    /// <summary>Acceleration along Z including gravity, in m/s². Reads about 9.8 on a device lying flat.</summary>
    public double AccelerationIncludingGravityZ { get; set; }

    /// <summary>Rotation rate about the Z axis, in degrees per second.</summary>
    public double RotationAlpha { get; set; }

    /// <summary>Rotation rate about the X axis, in degrees per second.</summary>
    public double RotationBeta { get; set; }

    /// <summary>Rotation rate about the Y axis, in degrees per second.</summary>
    public double RotationGamma { get; set; }

    /// <summary>
    /// How often the hardware refreshes these values, in milliseconds - the sensor's own rate, not
    /// the rate at which readings reach your handler.
    /// </summary>
    public double Interval { get; set; }
}
