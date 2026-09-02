namespace Bit.Butil;

/// <summary>
/// One sampled point of a handwriting stroke.
/// </summary>
public class HandwritingPoint
{
    /// <summary>Creates an empty point. Required for deserialization.</summary>
    public HandwritingPoint() { }

    /// <summary>Creates a point.</summary>
    /// <param name="x">X, in CSS pixels relative to the drawing surface.</param>
    /// <param name="y">Y, in CSS pixels relative to the drawing surface.</param>
    /// <param name="t">Milliseconds since the stroke began. See <see cref="T"/>.</param>
    public HandwritingPoint(double x, double y, double? t = null)
    {
        X = x;
        Y = y;
        T = t;
    }

    /// <summary>X, in CSS pixels relative to the drawing surface.</summary>
    public double X { get; set; }

    /// <summary>Y, in CSS pixels relative to the drawing surface.</summary>
    public double Y { get; set; }

    /// <summary>
    /// Milliseconds since the stroke began, if you have it. Optional in the spec, but it materially
    /// improves the result - writing speed and pauses are part of how the recognizer separates
    /// characters - so pass the pointer event's timestamp when you can.
    /// </summary>
    public double? T { get; set; }
}
