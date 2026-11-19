using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>A polygon or polyline through arbitrary points.</summary>
public sealed record BmagePolygonAnnotation : BmageAnnotation
{
    private readonly Vector2[] _points = [];

    public required IReadOnlyList<Vector2> Points
    {
        get => _points;
        init => _points = value?.ToArray() ?? [];
    }

    public BmageShapeStyle Style { get; init; } = BmageShapeStyle.Default;
    public bool Closed { get; init; } = true;

    public override string Kind => "Polygon";

    public override RectangleF GetBounds()
    {
        if (_points.Length == 0) return RectangleF.Empty;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in _points)
        {
            minX = MathF.Min(minX, p.X); minY = MathF.Min(minY, p.Y);
            maxX = MathF.Max(maxX, p.X); maxY = MathF.Max(maxY, p.Y);
        }
        return RectangleF.FromLTRB(minX, minY, maxX, maxY);
    }

    public override void Render(BmageImageCanvas canvas, BmageAnnotationRenderContext context)
    {
        if (_points.Length < 2) return;
        var path = new BmagePathBuilder().AddPolygon(_points, Closed).Build();
        if (Closed && Style.FillColor is { } fill) canvas.FillPath(path, fill);
        if (Style.StrokeColor is { } stroke) canvas.StrokePath(path, stroke, Style.Stroke);
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
}
