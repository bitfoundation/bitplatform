
namespace Bit.Bink;

/// <summary>Configurable resource limits that protect against pathological documents. All limits are inclusive maxima.</summary>
public sealed record BinkDocumentLimits
{
    /// <summary>Generous defaults suitable for interactive editors.</summary>
    public static readonly BinkDocumentLimits Default = new();

    /// <summary>Maximum number of layers.</summary>
    public int MaxLayers { get; init; } = 256;

    /// <summary>Maximum number of elements across all layers.</summary>
    public int MaxElements { get; init; } = 200_000;

    /// <summary>
    /// Maximum number of points in a single stroke. It also bounds the segments of a path element, which can never
    /// have more than <see cref="BinkPathElement.MaxSegments"/>, nor fewer than 16.
    /// </summary>
    public int MaxPointsPerStroke { get; init; } = 100_000;

    /// <summary>The most segments a path element can have under these limits.</summary>
    internal int MaxPathSegments => Math.Min(BinkPathElement.MaxSegments, Math.Max(MaxPointsPerStroke, 16));

    /// <summary>Maximum total number of stroke points in the document.</summary>
    public long MaxTotalPoints { get; init; } = 20_000_000;

    /// <summary>Maximum absolute coordinate value accepted from untrusted input.</summary>
    public float MaxCoordinate { get; init; } = 1_000_000f;

    /// <summary>Maximum brush/stroke width accepted from untrusted input.</summary>
    public float MaxStrokeWidth { get; init; } = 2_000f;
}
