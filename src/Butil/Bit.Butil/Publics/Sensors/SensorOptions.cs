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
    /// The axes an orientation sensor reports against. Ignored by every other sensor type.
    /// </summary>
    public SensorReferenceFrame? ReferenceFrame { get; set; }
}
