
namespace Bit.Bink;

/// <summary>
/// A pressure-sensitive pen: variable width outline with round caps and a subtle taper at the end.
/// The default brush for handwriting and signatures.
/// </summary>
public sealed record BinkPenBrush : BinkBrush
{
    /// <summary>The serialized kind: <c>pen</c>.</summary>
    public const string KindName = "pen";

    /// <summary>Creates a pen with sensible defaults: black, 4 units wide, the S-curve pressure response.</summary>
    public BinkPenBrush()
    {
        Width = 4f;
        PressureCurve = BinkPressureCurve.Pen;
        TaperEnd = 6f;
    }

    /// <inheritdoc />
    public override string Kind => KindName;

    /// <inheritdoc />
    public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
        => BinkVariableWidthBrushes.Build(points, builder, this, roundCaps: true);
}

/// <summary>
/// A pencil: thin, slightly translucent, with strong pressure response. Renders as a variable-width outline.
/// </summary>
public sealed record BinkPencilBrush : BinkBrush
{
    /// <summary>The serialized kind: <c>pencil</c>.</summary>
    public const string KindName = "pencil";

    /// <summary>Creates a pencil with defaults (2 units wide, 85% opacity).</summary>
    public BinkPencilBrush()
    {
        Width = 2f;
        Opacity = 0.85f;
        PressureCurve = new BinkPressureCurve(BinkPressureCurveKind.EaseIn, 0.35f, 1f);
        Smoothing = 0.35f;
        Spacing = 0.15f;
    }

    /// <inheritdoc />
    public override string Kind => KindName;

    /// <inheritdoc />
    public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
        => BinkVariableWidthBrushes.Build(points, builder, this, roundCaps: true);
}

/// <summary>
/// A marker: constant width, round caps, opaque. Renders as a stroked smooth centerline, which keeps geometry small.
/// </summary>
public sealed record BinkMarkerBrush : BinkBrush
{
    /// <summary>The serialized kind: <c>marker</c>.</summary>
    public const string KindName = "marker";

    /// <summary>Creates a marker with defaults (8 units wide, no pressure).</summary>
    public BinkMarkerBrush()
    {
        Width = 8f;
        PressureCurve = BinkPressureCurve.Constant;
        SimulatePressure = false;
    }

    /// <inheritdoc />
    public override string Kind => KindName;

    /// <inheritdoc />
    public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
        => BinkCenterlineBrushes.Build(points, builder, Width, BinkLineCap.Round, BinkLineJoin.Round);

    /// <inheritdoc />
    public override BinkRect GetBounds(BinkRect pointBounds) => base.GetBounds(BinkCenterlineBrushes.WithSplineOvershoot(pointBounds));
}

/// <summary>
/// A highlighter: wide, translucent, flat caps, multiply blend so text underneath stays legible and
/// overlapping highlights do not turn opaque.
/// </summary>
public sealed record BinkHighlighterBrush : BinkBrush
{
    /// <summary>The serialized kind: <c>highlighter</c>.</summary>
    public const string KindName = "highlighter";

    /// <summary>Creates a yellow highlighter (20 units wide, 45% opacity, multiply).</summary>
    public BinkHighlighterBrush()
    {
        Color = new BinkColor(255, 235, 59);
        Width = 20f;
        Opacity = 0.45f;
        BlendMode = BinkBlendMode.Multiply;
        PressureCurve = BinkPressureCurve.Constant;
        SimulatePressure = false;
        Smoothing = 0.6f;
        Spacing = 0.2f;
    }

    /// <inheritdoc />
    public override string Kind => KindName;

    /// <inheritdoc />
    public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
        => BinkCenterlineBrushes.Build(points, builder, Width, BinkLineCap.Butt, BinkLineJoin.Round);

    /// <inheritdoc />
    public override BinkRect GetBounds(BinkRect pointBounds) => base.GetBounds(BinkCenterlineBrushes.WithSplineOvershoot(pointBounds));
}

/// <summary>
/// A pixel eraser. Strokes drawn with it are stored as vector elements that erase whatever is beneath them
/// within the same layer (destination-out compositing). Unlike the stroke and segment erasers this does not
/// modify other elements, so it can be undone and re-rendered exactly, and exports as an SVG mask.
/// </summary>
public sealed record BinkPixelEraserBrush : BinkBrush
{
    /// <summary>The serialized kind: <c>pixel-eraser</c>.</summary>
    public const string KindName = "pixel-eraser";

    /// <summary>Creates a 20-unit eraser.</summary>
    public BinkPixelEraserBrush()
    {
        Width = 20f;
        BlendMode = BinkBlendMode.DestinationOut;
        PressureCurve = BinkPressureCurve.Constant;
        SimulatePressure = false;
        Smoothing = 0.3f;
    }

    /// <inheritdoc />
    public override string Kind => KindName;

    /// <inheritdoc />
    public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
        => BinkVariableWidthBrushes.Build(points, builder, this, roundCaps: true);
}

/// <summary>Shared geometry helpers for brushes that render variable-width outlines.</summary>
public static class BinkVariableWidthBrushes
{
    /// <summary>Builds outline geometry for a brush.</summary>
    public static BinkStrokeGeometry Build(BinkStrokePointsView points, BinkPathBuilder builder, BinkBrush brush, bool roundCaps)
    {
        var options = new BinkOutlineOptions(brush.Width)
        {
            PressureCurve = brush.PressureCurve,
            TaperStart = brush.TaperStart,
            TaperEnd = brush.TaperEnd,
            RoundCaps = roundCaps,
            Smooth = true,
        };
        BinkRect bounds = BinkStrokeOutliner.Build(points, options, builder);
        if (builder.IsEmpty) return BinkStrokeGeometry.Empty;
        return new BinkStrokeGeometry(builder.ToGeometry(), BinkStrokeGeometryMode.Fill, 0f, BinkLineCap.Round, BinkLineJoin.Round, bounds.Inflate(1f));
    }
}

/// <summary>Shared geometry helpers for brushes that render a constant-width centerline.</summary>
public static class BinkCenterlineBrushes
{
    /// <summary>Builds a smooth centerline path (Catmull-Rom) to be stroked at <paramref name="width"/>.</summary>
    public static BinkStrokeGeometry Build(BinkStrokePointsView points, BinkPathBuilder builder, float width, BinkLineCap cap, BinkLineJoin join)
    {
        builder.Reset();
        int n = points.Count;
        if (n == 0) return BinkStrokeGeometry.Empty;
        if (n == 1)
        {
            // A single tap: draw a dot of the line width so it is visible.
            builder.AddCircle(points.PositionAt(0), width / 2f);
            return new BinkStrokeGeometry(builder.ToGeometry(), BinkStrokeGeometryMode.Fill, 0f, cap, join, points.Bounds.Inflate(width / 2f + 1f));
        }
        Span<System.Numerics.Vector2> pts = n <= 1024 ? stackalloc System.Numerics.Vector2[n] : new System.Numerics.Vector2[n];
        for (int i = 0; i < n; i++) pts[i] = points.PositionAt(i);
        builder.AddCatmullRom(pts);
        // Round caps and joins stay within half the width of the centerline; square caps reach the corner of a
        // half-width square and miter joins reach the miter limit (4, the SVG and raster default).
        float reach = join == BinkLineJoin.Miter ? 4f : cap == BinkLineCap.Square ? MathF.Sqrt(2f) : 1f;
        float pad = width / 2f * reach + 1f;
        // The spline overshoots its points where the stroke turns sharply, so the points' bounds are too small.
        // The curve stays inside the hull of its control points, which the builder's bounds cover.
        return new BinkStrokeGeometry(builder.ToGeometry(), BinkStrokeGeometryMode.Centerline, width, cap, join, builder.Bounds.Inflate(pad));
    }

    /// <summary>
    /// Grows point bounds to cover the spline <see cref="Build"/> draws through them. A Catmull-Rom control point
    /// sits at most a sixth of the distance between its neighbours away from its point, so the curve can leave the
    /// points' bounds by up to a sixth of their size on each axis.
    /// </summary>
    internal static BinkRect WithSplineOvershoot(BinkRect pointBounds)
        => pointBounds.IsUnset ? pointBounds : pointBounds.Inflate(pointBounds.Width / 6f, pointBounds.Height / 6f);
}
