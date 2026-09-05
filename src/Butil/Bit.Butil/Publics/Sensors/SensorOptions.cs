namespace Bit.Butil;

/// <summary>
/// How a sensor should sample.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Sensor">Sensor</see>
/// </summary>
public class SensorOptions
{
    /// <summary>
    /// Readings per second. A hint, not a contract: the platform caps it, and a high rate is
    /// exactly what browsers throttle to keep a page from fingerprinting the hardware. Null lets
    /// the platform choose.
    /// </summary>
    public double? Frequency { get; set; }

    /// <summary>
    /// The axes a spatial sensor reports against - every type except
    /// <see cref="SensorType.AmbientLight"/>, which has no axes and ignores it.
    /// </summary>
    public SensorReferenceFrame? ReferenceFrame { get; set; }

    /// <summary>
    /// The shortest gap, in milliseconds, between two readings reaching .NET. Readings arriving
    /// sooner are dropped in JS, before the interop round-trip is paid for. 0 forwards every one.
    /// </summary>
    /// <remarks>
    /// <see cref="Frequency"/> is only a hint to the platform, and the motion sensors default to
    /// 60 Hz - which on Blazor Server is 60 messages a second per subscription, each one a render.
    /// This is the cap that actually holds, and it defaults to the same 100 ms
    /// <see cref="DeviceOrientation"/> uses for the legacy event streams. Lower it for a reading a
    /// UI genuinely animates from; set it to 0 only when every sample matters.
    /// </remarks>
    public int MinIntervalMs { get; set; } = 100;
}
