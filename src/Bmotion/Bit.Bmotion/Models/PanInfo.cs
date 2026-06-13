namespace Bit.Bmotion.Models;

/// <summary>
/// Information about a pan gesture provided to <c>OnPan</c> callbacks.
/// Matches the Framer Motion pan event info shape.
/// </summary>
public class PanInfo
{
    /// <summary>Current pointer position relative to the document.</summary>
    public PointInfo Point { get; set; } = new();

    /// <summary>Distance moved since the last event.</summary>
    public PointInfo Delta { get; set; } = new();

    /// <summary>Total distance moved since the pan gesture started.</summary>
    public PointInfo Offset { get; set; } = new();

    /// <summary>Current velocity of the pointer (pixels per second).</summary>
    public PointInfo Velocity { get; set; } = new();
}

/// <summary>A 2-D point with <see cref="X"/> and <see cref="Y"/> components.</summary>
public class PointInfo
{
    public double X { get; set; }
    public double Y { get; set; }
}
