namespace Bit.Butil;

/// <summary>
/// One reading from a Generic Sensor. The eight sensors report different quantities, so this
/// carries all of them and leaves the ones a given sensor does not produce as null - which is also
/// what a reading looks like before the first sample has landed.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Sensor/reading_event">Sensor reading event</see>
/// </summary>
public class SensorReading
{
    /// <summary>Which sensor produced the reading, as the kebab-case name the browser uses.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// When the sample was taken, on the same clock as <c>performance.now()</c>, in milliseconds.
    /// </summary>
    public double Timestamp { get; set; }

    /// <summary>The x axis, for the vector sensors (accelerometer, gyroscope, magnetometer, gravity, linear acceleration).</summary>
    public double? X { get; set; }

    /// <summary>The y axis, for the vector sensors.</summary>
    public double? Y { get; set; }

    /// <summary>The z axis, for the vector sensors.</summary>
    public double? Z { get; set; }

    /// <summary>
    /// The orientation as a unit quaternion <c>[x, y, z, w]</c>, for the two orientation sensors.
    /// </summary>
    public double[]? Quaternion { get; set; }

    /// <summary>The light level in lux, for the ambient light sensor.</summary>
    public double? Illuminance { get; set; }
}
