namespace Bit.Butil;

/// <summary>
/// The sensors of the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Sensor_APIs">Generic Sensor API</see>.
/// Unlike the legacy <see cref="DeviceOrientation"/> events these have an explicit sample rate, a
/// per-sensor permission and their own error channel.
/// </summary>
public enum SensorType
{
    /// <summary>Acceleration on all three axes, gravity included, in m/s².</summary>
    Accelerometer,

    /// <summary>Angular velocity on all three axes, in rad/s.</summary>
    Gyroscope,

    /// <summary>The ambient magnetic field on all three axes, in µT.</summary>
    Magnetometer,

    /// <summary>Orientation as a quaternion, relative to the Earth - so it knows where north is.</summary>
    AbsoluteOrientation,

    /// <summary>Orientation as a quaternion, relative to wherever the device started - no compass needed.</summary>
    RelativeOrientation,

    /// <summary>The gravity component of the acceleration, in m/s² - which way is down.</summary>
    Gravity,

    /// <summary>Acceleration with gravity removed, in m/s² - how the device is actually being moved.</summary>
    LinearAcceleration,

    /// <summary>Ambient light level, in lux.</summary>
    AmbientLight
}
