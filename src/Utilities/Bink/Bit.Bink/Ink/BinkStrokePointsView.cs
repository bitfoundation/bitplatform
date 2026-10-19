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
    {
        X = x;
        Y = y;
        Pressure = pressure;
        Time = time;
        Bounds = bounds;
    }

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
    public static BinkStrokePointsView AsView(this BinkStrokePoints points) => new(points.X, points.Y, points.Pressure, points.Time, points.Bounds);

    /// <summary>Creates a view over a live buffer.</summary>
    public static BinkStrokePointsView AsView(this BinkStrokePointBuffer buffer) => new(buffer.X, buffer.Y, buffer.Pressure, buffer.Time, buffer.Bounds);
}
