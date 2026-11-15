using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// A stack-only, allocation-free view over stroke points that works for both the immutable
/// <see cref="BinkStrokePoints"/> and the live <see cref="BinkStrokePointBuffer"/>. Geometry generators take this
/// so the same code renders committed and in-progress strokes.
/// </summary>
public readonly ref struct BinkStrokePointsView
{
    /// <summary>Creates a view over parallel spans.</summary>
    public BinkStrokePointsView(ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> pressure, ReadOnlySpan<float> time, BinkRect bounds)
        : this(x, y, pressure, time, default, default, default, bounds)
    {
    }

    /// <summary>Creates a view over parallel spans, with the pen's tilt and twist where the device reported them.</summary>
    /// <param name="x">X coordinates.</param>
    /// <param name="y">Y coordinates.</param>
    /// <param name="pressure">Pressure values.</param>
    /// <param name="time">Time values in milliseconds.</param>
    /// <param name="tiltX">Tilt along x in degrees, or empty when there is none.</param>
    /// <param name="tiltY">Tilt along y in degrees, or empty when there is none.</param>
    /// <param name="twist">Barrel rotation in degrees, or empty when there is none.</param>
    /// <param name="bounds">Bounds of positions.</param>
    public BinkStrokePointsView(ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> pressure, ReadOnlySpan<float> time, ReadOnlySpan<float> tiltX, ReadOnlySpan<float> tiltY, ReadOnlySpan<float> twist, BinkRect bounds)
    {
        X = x;
        Y = y;
        Pressure = pressure;
        Time = time;
        // The two tilts are kept together or not at all, so a reader of one can always read the other.
        bool tilted = tiltX.Length == x.Length && tiltY.Length == x.Length;
        TiltX = tilted ? tiltX : default;
        TiltY = tilted ? tiltY : default;
        Twist = twist.Length == x.Length ? twist : default;
        Bounds = bounds;
    }

    /// <summary>Tilt along x in degrees, -90 to 90, one per point; empty when the device reported none.</summary>
    public ReadOnlySpan<float> TiltX { get; }

    /// <summary>Tilt along y in degrees, -90 to 90, one per point; empty when the device reported none.</summary>
    public ReadOnlySpan<float> TiltY { get; }

    /// <summary>Barrel rotation in degrees, 0 to 359, one per point; empty when the device reported none.</summary>
    public ReadOnlySpan<float> Twist { get; }

    /// <summary>X coordinates.</summary>
    public ReadOnlySpan<float> X { get; }

    /// <summary>Y coordinates.</summary>
    public ReadOnlySpan<float> Y { get; }

    /// <summary>Pressure values.</summary>
    public ReadOnlySpan<float> Pressure { get; }

    /// <summary>Time values in milliseconds.</summary>
    public ReadOnlySpan<float> Time { get; }

    /// <summary>Bounds of positions.</summary>
    public BinkRect Bounds { get; }

    /// <summary>Number of points.</summary>
    public int Count => X.Length;

    /// <summary>Position of a point.</summary>
    public Vector2 PositionAt(int index) => new(X[index], Y[index]);
}

/// <summary>Extension methods to obtain views.</summary>
public static class BinkStrokePointsViewExtensions
{
    /// <summary>Creates a view over immutable points.</summary>
    public static BinkStrokePointsView AsView(this BinkStrokePoints points) => new(points.X, points.Y, points.Pressure, points.Time, points.TiltX, points.TiltY, points.Twist, points.Bounds);

    /// <summary>Creates a view over a live buffer.</summary>
    public static BinkStrokePointsView AsView(this BinkStrokePointBuffer buffer) => new(buffer.X, buffer.Y, buffer.Pressure, buffer.Time, buffer.TiltX, buffer.TiltY, buffer.Twist, buffer.Bounds);
}
