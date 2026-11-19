using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>A freehand stroke captured from pointer input.</summary>
public sealed record BmageFreehandAnnotation : BmageAnnotation
{
    private readonly Vector2[] _points = [];

    /// <summary>The stroke path in image coordinates.</summary>
    public required IReadOnlyList<Vector2> Points
    {
        get => _points;
        init => _points = value?.ToArray() ?? [];
    }

    public BmageShapeStyle Style { get; init; } = BmageShapeStyle.Default with { Stroke = new BmageStrokeStyle { Width = 4f, Cap = BmageLineCap.Round, Join = BmageLineJoin.Round } };

    /// <summary>Fill and close the stroke instead of drawing an open line (a lasso highlight).</summary>
    public bool Closed { get; init; }

    public override string Kind => "Freehand";

    public override RectangleF GetBounds()
    {
        if (_points.Length == 0) return RectangleF.Empty;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in _points)
        {
            minX = MathF.Min(minX, p.X); minY = MathF.Min(minY, p.Y);
            maxX = MathF.Max(maxX, p.X); maxY = MathF.Max(maxY, p.Y);
        }
        var r = RectangleF.FromLTRB(minX, minY, maxX, maxY);
        var w = Style.Stroke.Width;
        r.Inflate(w, w);
        return r;
    }

    public override void Render(BmageImageCanvas canvas, BmageAnnotationRenderContext context)
    {
        if (_points.Length == 0) return;
        if (_points.Length == 1)
        {
            if (Style.StrokeColor is { } dot) canvas.FillCircle(_points[0], Style.Stroke.Width / 2, dot);
            return;
        }
        var builder = new BmagePathBuilder();
        AppendSmoothed(builder, _points, Closed);
        var path = builder.Build();
        if (Closed && Style.FillColor is { } fill) canvas.FillPath(path, fill);
        if (Style.StrokeColor is { } stroke) canvas.StrokePath(path, stroke, Style.Stroke);
    }

    /// <summary>Draws through the points with Catmull-Rom style smoothing so fast strokes do not look polygonal.</summary>
    internal static void AppendSmoothed(BmagePathBuilder builder, IReadOnlyList<Vector2> points, bool closed)
    {
        if (points.Count < 3)
        {
            builder.AddPolyline(points);
            if (closed) builder.Close();
            return;
        }
        builder.MoveTo(points[0]);
        for (var i = 0; i < points.Count - 1; i++)
        {
            var (c1, c2) = ControlPoints(points, i);
            builder.CubicTo(c1, c2, points[i + 1]);
        }
        if (closed) builder.Close();
    }

    /// <summary>
    /// The points and the control points <see cref="AppendSmoothed"/> draws its curves through. A cubic never leaves the
    /// hull of its control points, so the bounds of these hold the whole curve, bulges at sharp turns included.
    /// </summary>
    internal static IEnumerable<Vector2> SmoothingHull(IReadOnlyList<Vector2> points)
    {
        foreach (var p in points) yield return p;
        if (points.Count < 3) yield break;
        for (var i = 0; i < points.Count - 1; i++)
        {
            var (c1, c2) = ControlPoints(points, i);
            yield return c1;
            yield return c2;
        }
    }

    /// <summary>The control points of the curve from point <paramref name="i"/> to the next.</summary>
    private static (Vector2, Vector2) ControlPoints(IReadOnlyList<Vector2> points, int i)
    {
        var p0 = points[Math.Max(i - 1, 0)];
        var p1 = points[i];
        var p2 = points[i + 1];
        var p3 = points[Math.Min(i + 2, points.Count - 1)];
        return (p1 + (p2 - p0) / 6f, p2 - (p3 - p1) / 6f);
    }

    public override BmageAnnotation Translate(float dx, float dy)
    {
        var offset = new Vector2(dx, dy);
        return this with { Points = _points.Select(p => p + offset).ToArray() };
    }

    public override BmageAnnotation ForScale(double scale) => scale == 1 ? this : this with
    {
        Points = _points.Select(p => p * (float)scale).ToArray(),
        Style = Style.ForScale(scale),
    };

    public override BmageAnnotation Transform(Matrix3x2 matrix)
    {
        var m = Through(matrix);
        return this with { Points = [.. _points.Select(p => Vector2.Transform(p, m))], Style = Style.ForScale(ScaleOf(matrix)), Rotation = 0 };
    }

    public override bool HitTest(Vector2 point, float tolerance = 4f)
    {
        var limit = tolerance + Style.Stroke.Width / 2;
        point = ToLocal(point);
        for (var i = 0; i + 1 < _points.Length; i++)
            if (BmageLineAnnotation.DistanceToSegment(point, _points[i], _points[i + 1]) <= limit) return true;
        return _points.Length == 1 && Vector2.Distance(point, _points[0]) <= limit;
    }
}
