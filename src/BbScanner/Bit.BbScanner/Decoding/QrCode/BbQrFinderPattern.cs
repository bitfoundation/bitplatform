namespace Bit.BbScanner;

/// <summary>
/// A candidate QR finder pattern: the centre of one of the three large concentric squares.
/// </summary>
/// <remarks>
/// A pattern accumulates evidence rather than being accepted or rejected on first sight. Each
/// time a scan line crosses the same square, the centre estimate is averaged in and
/// <see cref="Count"/> grows, so a real finder pattern that is crossed many times outranks a
/// coincidental one that is crossed once.
/// </remarks>
public sealed class BbQrFinderPattern
{
    /// <summary>Creates a candidate from a single observation.</summary>
    /// <param name="x">Centre x in image pixels.</param>
    /// <param name="y">Centre y in image pixels.</param>
    /// <param name="estimatedModuleSize">Estimated module width in pixels.</param>
    public BbQrFinderPattern(float x, float y, float estimatedModuleSize)
        : this(x, y, estimatedModuleSize, 1)
    {
    }

    private BbQrFinderPattern(float x, float y, float estimatedModuleSize, int count)
    {
        X = x;
        Y = y;
        EstimatedModuleSize = estimatedModuleSize;
        Count = count;
    }

    /// <summary>Centre x in image pixels.</summary>
    public float X { get; }

    /// <summary>Centre y in image pixels.</summary>
    public float Y { get; }

    /// <summary>Estimated module width in pixels.</summary>
    public float EstimatedModuleSize { get; }

    /// <summary>How many independent observations support this candidate.</summary>
    public int Count { get; }

    /// <summary>The centre as a point.</summary>
    public BbScanPoint Point => new(X, Y);

    /// <summary>
    /// Returns whether an observation is close enough to be the same pattern, allowing a
    /// tolerance of one module in position and a proportional tolerance in module size.
    /// </summary>
    /// <param name="moduleSize">Observed module size.</param>
    /// <param name="i">Observed centre y.</param>
    /// <param name="j">Observed centre x.</param>
    public bool AboutEquals(float moduleSize, float i, float j)
    {
        if (Math.Abs(i - Y) > moduleSize || Math.Abs(j - X) > moduleSize)
        {
            return false;
        }

        var moduleSizeDiff = Math.Abs(moduleSize - EstimatedModuleSize);
        return moduleSizeDiff <= 1.0f || moduleSizeDiff <= EstimatedModuleSize;
    }

    /// <summary>Returns a candidate that averages this one with a new observation.</summary>
    /// <param name="i">Observed centre y.</param>
    /// <param name="j">Observed centre x.</param>
    /// <param name="newModuleSize">Observed module size.</param>
    public BbQrFinderPattern CombineEstimate(float i, float j, float newModuleSize)
    {
        var combinedCount = Count + 1;
        return new BbQrFinderPattern(
            ((Count * X) + j) / combinedCount,
            ((Count * Y) + i) / combinedCount,
            ((Count * EstimatedModuleSize) + newModuleSize) / combinedCount,
            combinedCount);
    }
}
