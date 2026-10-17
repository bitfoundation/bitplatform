
namespace Bit.Bink;

/// <summary>How a stroke's path should be painted.</summary>
public enum BinkStrokeGeometryMode : byte
{
    /// <summary>The path is a closed outline polygon to be filled (variable width strokes).</summary>
    Fill = 0,
    /// <summary>The path is a centerline to be stroked with a constant width.</summary>
    Centerline = 1,
}

/// <summary>
/// The renderable vector geometry of a stroke, produced by a <see cref="BinkBrush"/> from stroke points. Immutable and
/// cached by strokes, so it is only computed once per stroke version regardless of how many times it is drawn.
/// </summary>
public sealed class BinkStrokeGeometry
{
    /// <summary>Geometry of an empty stroke.</summary>
    public static readonly BinkStrokeGeometry Empty = new(BinkPathGeometry.Empty, BinkStrokeGeometryMode.Fill, 0, BinkLineCap.Round, BinkLineJoin.Round, BinkRect.Empty);

    /// <summary>Creates a stroke geometry.</summary>
    public BinkStrokeGeometry(BinkPathGeometry path, BinkStrokeGeometryMode mode, float lineWidth, BinkLineCap cap, BinkLineJoin join, BinkRect bounds)
    {
        Path = path;
        Mode = mode;
        LineWidth = lineWidth;
        Cap = cap;
        Join = join;
        Bounds = bounds;
    }

    /// <summary>The vector path.</summary>
    public BinkPathGeometry Path { get; }

    /// <summary>Whether to fill or stroke the path.</summary>
    public BinkStrokeGeometryMode Mode { get; }

    /// <summary>Line width for <see cref="BinkStrokeGeometryMode.Centerline"/> geometry.</summary>
    public float LineWidth { get; }

    /// <summary>Line cap for centerline geometry.</summary>
    public BinkLineCap Cap { get; }

    /// <summary>Line join for centerline geometry.</summary>
    public BinkLineJoin Join { get; }

    /// <summary>Bounds of the painted area including line width.</summary>
    public BinkRect Bounds { get; }

    /// <summary>True when there is nothing to draw.</summary>
    public bool IsEmpty => Path.IsEmpty;

    /// <summary>Draws the geometry with the given color, opacity and blend mode.</summary>
    public void Draw(IBinkRenderContext context, BinkColor color, float opacity, BinkBlendMode blend)
    {
        if (IsEmpty) return;
        if (Mode == BinkStrokeGeometryMode.Fill)
        {
            context.FillPath(Path, new BinkFillPaint(color, opacity, blend, BinkFillRule.NonZero));
        }
        else
        {
            context.StrokePath(Path, new BinkStrokePaint(color, LineWidth, opacity, blend, Cap, Join));
        }
    }
}
