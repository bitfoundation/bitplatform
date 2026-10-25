using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// An arbitrary vector path with a shape style: icons, stickers, logos, curves drawn by an application, and what SVG
/// paths become. The geometry is any mix of lines, quadratic and cubic Bézier curves in one or more sub-paths.
/// </summary>
/// <example>
/// <code>
/// var heart = BinkPathElement.FromSvgPathData("M 50 30 C 50 0 0 0 0 30 C 0 60 50 80 50 100 C 50 80 100 60 100 30 C 100 0 50 0 50 30 Z")
/// {
///     Style = new BinkShapeStyle { FillColor = BinkColor.Parse("#e11d48"), StrokeWidth = 0f },
/// };
/// </code>
/// </example>
public sealed record BinkPathElement : BinkShapeElement
{
    /// <summary>The most segments a path element may have.</summary>
    public const int MaxSegments = 100_000;

    private readonly BinkPathGeometry _path = BinkPathGeometry.Empty;

    /// <summary>Creates a path element.</summary>
    /// <param name="path">The geometry, in local coordinates.</param>
    public BinkPathElement(BinkPathGeometry path) { Path = path; }

    /// <summary>The geometry, in local coordinates.</summary>
    /// <exception cref="ArgumentException">The path has more than <see cref="MaxSegments"/> segments or a coordinate that is not finite.</exception>
    public BinkPathGeometry Path
    {
        get => _path;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.VerbCount > MaxSegments) throw new ArgumentException($"A path can have at most {MaxSegments} segments.", nameof(value));
            foreach (float c in value.Coordinates)
            {
                if (!float.IsFinite(c)) throw new ArgumentException("Path coordinates must be finite.", nameof(value));
            }
            _path = value;
        }
    }

    /// <summary>How overlapping and nested sub-paths fill. <see cref="BinkFillRule.EvenOdd"/> cuts holes, as a letter O has.</summary>
    public BinkFillRule FillRule { get; init; } = BinkFillRule.NonZero;

    /// <summary>Creates a path element from SVG path data (the <c>d</c> attribute); see <see cref="BinkSvgPathData"/>.</summary>
    /// <exception cref="FormatException">The data is malformed.</exception>
    public static BinkPathElement FromSvgPathData(string data) => new(BinkSvgPathData.Parse(data, MaxSegments));

    /// <inheritdoc />
    public override string TypeName => "Path";

    /// <inheritdoc />
    /// <remarks>The bounds of the curves' control points, which always contain the curves themselves.</remarks>
    protected override BinkRect GeometryBounds => _path.Bounds;

    /// <inheritdoc />
    public override BinkPathGeometry GetPath() => _path;

    /// <inheritdoc />
    protected override void DrawCore(IBinkRenderContext context)
    {
        if (_path.IsEmpty) return;
        if (Style.FillColor is { } fill && !fill.IsTransparent) context.FillPath(_path, Style.ToFillPaint() with { Rule = FillRule });
        if (Style.StrokeWidth > 0) context.StrokePath(_path, Style.ToStrokePaint());
    }

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => HitTestPath(localPoint, tolerance);

    /// <inheritdoc />
    private protected override BinkFillRule? CombinedFillRule => FillRule;
}
