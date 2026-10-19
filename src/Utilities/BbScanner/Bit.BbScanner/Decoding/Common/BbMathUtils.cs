using System.Runtime.CompilerServices;

namespace Bit.BbScanner;

/// <summary>Small numeric helpers shared by the detectors.</summary>
public static class BbMathUtils
{
    /// <summary>Rounds to the nearest integer, halves away from zero.</summary>
    /// <param name="value">The value to round.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Round(float value) => (int)(value + (value < 0.0f ? -0.5f : 0.5f));

    /// <summary>Squared Euclidean distance, which avoids a square root when only ordering matters.</summary>
    /// <param name="aX">First point, x.</param>
    /// <param name="aY">First point, y.</param>
    /// <param name="bX">Second point, x.</param>
    /// <param name="bY">Second point, y.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceSquared(float aX, float aY, float bX, float bY)
    {
        var dx = aX - bX;
        var dy = aY - bY;
        return (dx * dx) + (dy * dy);
    }

    /// <summary>Euclidean distance between two points.</summary>
    /// <param name="aX">First point, x.</param>
    /// <param name="aY">First point, y.</param>
    /// <param name="bX">Second point, x.</param>
    /// <param name="bY">Second point, y.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(float aX, float aY, float bX, float bY) =>
        MathF.Sqrt(DistanceSquared(aX, aY, bX, bY));

    /// <summary>Sums a span of counters.</summary>
    /// <param name="values">The values to sum.</param>
    public static int Sum(ReadOnlySpan<int> values)
    {
        var total = 0;
        foreach (var value in values)
        {
            total += value;
        }

        return total;
    }
}
