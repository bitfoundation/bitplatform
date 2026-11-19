namespace Bit.Bink;

/// <summary>Extension methods to obtain views.</summary>
public static class BinkStrokePointsViewExtensions
{
    /// <summary>Creates a view over immutable points.</summary>
    public static BinkStrokePointsView AsView(this BinkStrokePoints points) => new(points.X, points.Y, points.Pressure, points.Time, points.TiltX, points.TiltY, points.Twist, points.Bounds);

    /// <summary>Creates a view over a live buffer.</summary>
    public static BinkStrokePointsView AsView(this BinkStrokePointBuffer buffer) => new(buffer.X, buffer.Y, buffer.Pressure, buffer.Time, buffer.TiltX, buffer.TiltY, buffer.Twist, buffer.Bounds);
}
