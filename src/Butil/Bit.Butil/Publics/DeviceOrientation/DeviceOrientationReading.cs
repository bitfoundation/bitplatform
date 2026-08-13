namespace Bit.Butil;

/// <summary>
/// One <see href="https://developer.mozilla.org/en-US/docs/Web/API/DeviceOrientationEvent">DeviceOrientationEvent</see>
/// reading: the device's tilt, in degrees.
/// </summary>
/// <remarks>
/// A browser that fires the event without sensor data reports nulls for the angles; those arrive
/// here as 0 rather than as nullable doubles, so a reading is always safe to use in arithmetic.
/// </remarks>
public class DeviceOrientationReading
{
    /// <summary>
    /// Rotation about the Z axis, 0 to 360. With <see cref="Absolute"/> readings this is a compass
    /// heading; otherwise it is relative to wherever the device was when the listener attached.
    /// </summary>
    public double Alpha { get; set; }

    /// <summary>Front-to-back tilt about the X axis, -180 to 180. 0 is flat, 90 is upright.</summary>
    public double Beta { get; set; }

    /// <summary>Left-to-right tilt about the Y axis, -90 to 90.</summary>
    public double Gamma { get; set; }

    /// <summary>
    /// True when <see cref="Alpha"/> is measured against the earth's coordinate frame. False means
    /// the runtime only had the relative event, so alpha is not a heading.
    /// </summary>
    public bool Absolute { get; set; }
}
