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
    /// <param name="t">Milliseconds since the drawing began. See <see cref="T"/>.</param>
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
    /// Milliseconds since the <em>drawing</em> began, if you have it. Optional in the spec, but it
    /// materially improves the result - writing speed and pauses are part of how the recognizer
    /// separates characters - so pass the pointer event's timestamp when you can.
    /// </summary>
    /// <remarks>
    /// One clock for the whole drawing, not one per stroke: restarting it at every stroke would say
    /// that each stroke followed the last instantly, and the pause between two strokes is exactly the
    /// signal that tells one character from two. Take the time of the first point of the first stroke
    /// as zero and measure every later point against it.
    /// </remarks>
    public double? T { get; set; }
}
