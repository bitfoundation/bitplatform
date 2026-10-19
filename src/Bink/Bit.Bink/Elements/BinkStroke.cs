using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// A freehand ink stroke: processed points plus the brush that renders them. The vector geometry is derived
/// lazily and cached per (points, brush) pair, so it is built once and reused across redraws and exports.
/// </summary>
public sealed record BinkStroke : BinkElement
{
    private BinkStrokeGeometry? _geometry;
    private BinkStrokePoints? _geometryPoints;
    private BinkBrush? _geometryBrush;

    /// <summary>Creates a stroke.</summary>
    /// <param name="points">Processed points in local coordinates.</param>
    /// <param name="brush">The brush.</param>
    public BinkStroke(BinkStrokePoints points, BinkBrush brush)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(brush);
        Points = points;
        Brush = brush;
    }

    /// <summary>The points.</summary>
    public BinkStrokePoints Points { get; init; }

    /// <summary>The brush.</summary>
    public BinkBrush Brush { get; init; }

    /// <summary>Which device produced the stroke.</summary>
    public BinkStrokeInputInfo Input { get; init; } = BinkStrokeInputInfo.Unknown;

    /// <inheritdoc />
    public override string TypeName => Brush.BlendMode == BinkBlendMode.DestinationOut ? "Eraser stroke" : "Stroke";

    /// <inheritdoc />
    public override bool IsSelectable => Brush.BlendMode != BinkBlendMode.DestinationOut;

    /// <summary>True when this stroke erases content beneath it instead of drawing.</summary>
    public bool IsEraser => Brush.BlendMode == BinkBlendMode.DestinationOut;

    /// <summary>
    /// Bounds in local coordinates. Exact once the outline has been built for rendering, and a conservative
    /// estimate from the brush before then. Asking for bounds must never force geometry generation: loading a
    /// document reads the bounds of every element, and building every outline up front would cost far more time
    /// and memory than drawing the visible ones.
    /// </summary>
    public override BinkRect LocalBounds
        => HasGeometry ? _geometry!.Bounds : Brush.GetBounds(Points.Bounds);

    /// <summary>True when the vector geometry is already built and cached for the current points and brush.</summary>
    public bool HasGeometry => _geometry is { IsEmpty: false } && ReferenceEquals(_geometryPoints, Points) && ReferenceEquals(_geometryBrush, Brush);

    /// <summary>The cached vector geometry.</summary>
    public BinkStrokeGeometry Geometry
    {
        get
        {
            BinkStrokeGeometry? g = _geometry;
            if (g is not null && ReferenceEquals(_geometryPoints, Points) && ReferenceEquals(_geometryBrush, Brush)) return g;
            g = Points.Count == 0 ? BinkStrokeGeometry.Empty : Brush.BuildGeometry(Points.AsView());
            _geometry = g;
            _geometryPoints = Points;
            _geometryBrush = Brush;
            return g;
        }
    }

    /// <summary>Attaches pre-built geometry (used when a live stroke is committed so nothing is recomputed).</summary>
    internal BinkStroke WithGeometry(BinkStrokeGeometry geometry)
    {
        var copy = this with { };
        copy._geometry = geometry;
        copy._geometryPoints = Points;
        copy._geometryBrush = Brush;
        return copy;
    }

    /// <inheritdoc />
    protected override void DrawCore(IBinkRenderContext context)
        => Geometry.Draw(context, Brush.Color, Brush.Opacity, Brush.BlendMode);

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance)
    {
        float radius = Brush.HitTestWidth / 2f + tolerance;
        float r2 = radius * radius;
        int n = Points.Count;
        if (n == 0) return false;
        ReadOnlySpan<float> xs = Points.X;
        ReadOnlySpan<float> ys = Points.Y;
        if (n == 1) return (localPoint - new Vector2(xs[0], ys[0])).LengthSquared() <= r2;
        for (int i = 1; i < n; i++)
        {
            if (BinkGeometryMath.DistanceToSegmentSquared(localPoint, new Vector2(xs[i - 1], ys[i - 1]), new Vector2(xs[i], ys[i])) <= r2) return true;
        }
        return false;
    }

    /// <inheritdoc />
    public override bool IntersectsRect(BinkRect rect)
    {
        if (!Bounds.Intersects(rect)) return false;
        if (rect.Contains(Bounds)) return true;

        // Test in document space. Mapping the rectangle into local space instead and taking its bounding box
        // would, under rotation, cover far more than the rectangle and select strokes it does not touch.
        Matrix3x2 transform = Transform;
        bool identity = transform.IsIdentity;
        float scale = identity ? 1f : MathF.Sqrt(MathF.Abs(transform.GetDeterminant()));
        if (!float.IsFinite(scale) || scale < BinkGeometryMath.Epsilon) scale = 1f;
        BinkRect padded = rect.Inflate(Brush.HitTestWidth / 2f * scale);
        int n = Points.Count;
        ReadOnlySpan<float> xs = Points.X;
        ReadOnlySpan<float> ys = Points.Y;
        if (n == 0) return false;
        Vector2 previous = new(xs[0], ys[0]);
        if (!identity) previous = Vector2.Transform(previous, transform);
        if (n == 1) return padded.Contains(previous);
        for (int i = 1; i < n; i++)
        {
            Vector2 next = new(xs[i], ys[i]);
            if (!identity) next = Vector2.Transform(next, transform);
            if (BinkGeometryMath.SegmentIntersectsRect(previous, next, padded)) return true;
            previous = next;
        }
        return false;
    }

    /// <summary>Returns a stroke whose points have the transform baked in (transform becomes identity).</summary>
    public BinkStroke Flatten()
    {
        if (Transform.IsIdentity) return this;
        float scale = MathF.Sqrt(MathF.Abs(Transform.GetDeterminant()));
        BinkBrush brush = Math.Abs(scale - 1f) > 1e-4f ? Brush with { Width = Brush.Width * scale, TaperStart = Brush.TaperStart * scale, TaperEnd = Brush.TaperEnd * scale } : Brush;
        return this with { Points = Points.Transform(Transform), Brush = brush, Transform = Matrix3x2.Identity };
    }

    /// <inheritdoc />
    public override string GetAccessibleDescription()
    {
        if (Description is not null) return Description;
        return FormattableString.Invariant($"{TypeName}, {Points.Count} points, {Brush.Kind} brush, color {Brush.Color.ToCss()}");
    }
}
