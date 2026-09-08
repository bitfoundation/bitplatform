namespace Bit.Butil;

/// <summary>
/// One feature located within a detected face - an eye, the nose, the mouth.
/// </summary>
/// <remarks>
/// The underlying API gives each landmark a list of points outlining it. Only the first is carried
/// across, because that is the one a caller can actually draw without knowing the platform's outline
/// conventions; <see cref="PointCount"/> says how many there were.
/// </remarks>
public class FaceLandmark
{
    /// <summary>The feature, e.g. <c>"eye"</c>, <c>"nose"</c>, <c>"mouth"</c>. Empty when the platform doesn't say.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>The first point's x, in the source element's pixel coordinates.</summary>
    public double X { get; set; }

    /// <summary>The first point's y, in the source element's pixel coordinates.</summary>
    public double Y { get; set; }

    /// <summary>How many points the platform reported for this landmark.</summary>
    public int PointCount { get; set; }
}
