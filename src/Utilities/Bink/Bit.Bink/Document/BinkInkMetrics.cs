namespace Bit.Bink;

/// <summary>What <see cref="BinkDocumentQuery.MeasureInk"/> found.</summary>
/// <param name="StrokeCount">Strokes drawn, eraser strokes not counted.</param>
/// <param name="PointCount">Points across those strokes.</param>
/// <param name="Length">The total length of the ink in document units, as drawn (transforms applied).</param>
/// <param name="Duration">The time the pen was down, summed over strokes; zero for strokes without timing.</param>
/// <param name="Bounds">The area the ink covers.</param>
public readonly record struct BinkInkMetrics(int StrokeCount, long PointCount, float Length, TimeSpan Duration, BinkRect Bounds)
{
    /// <summary>True when there is no ink at all.</summary>
    public bool IsEmpty => StrokeCount == 0;
}
