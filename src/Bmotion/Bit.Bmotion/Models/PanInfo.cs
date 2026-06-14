namespace Bit.Bmotion.Models;

/// <summary>
/// Information about a pan gesture provided to <c>OnPan</c> callbacks.
/// Matches the Framer Motion pan event info shape.
/// </summary>
public class PanInfo
{
    /// <summary>Current pointer position relative to the document.</summary>
    public required PointInfo Point { get; init; }

    /// <summary>Distance moved since the last event.</summary>
    public required PointInfo Delta { get; init; }

    /// <summary>Total distance moved since the pan gesture started.</summary>
    public required PointInfo Offset { get; init; }

    /// <summary>Current velocity of the pointer (pixels per second).</summary>
    public required PointInfo Velocity { get; init; }
}

/// <summary>A 2-D point with <see cref="X"/> and <see cref="Y"/> components.</summary>
public class PointInfo
{
    public double X { get; set; }
    public double Y { get; set; }
}
