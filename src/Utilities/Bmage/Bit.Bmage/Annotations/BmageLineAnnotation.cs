using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>A straight line between two points.</summary>
public sealed record BmageLineAnnotation : BmageAnnotation
{
    public required Vector2 Start { get; init; }
    public required Vector2 End { get; init; }
    public BmageShapeStyle Style { get; init; } = BmageShapeStyle.Default;

    public override string Kind => "Line";

    public override RectangleF GetBounds() => RectangleF.FromLTRB(
        MathF.Min(Start.X, End.X), MathF.Min(Start.Y, End.Y), MathF.Max(Start.X, End.X), MathF.Max(Start.Y, End.Y));

    public override void Render(BmageImageCanvas canvas, BmageAnnotationRenderContext context)
    {
        if (Style.StrokeColor is { } stroke) canvas.DrawLine(Start, End, stroke, Style.Stroke);
    }

    public override BmageAnnotation Translate(float dx, float dy) => this with { Start = Start + new Vector2(dx, dy), End = End + new Vector2(dx, dy) };
    public override BmageAnnotation ForScale(double scale) => scale == 1 ? this : this with { Start = Start * (float)scale, End = End * (float)scale, Style = Style.ForScale(scale) };

    public override BmageAnnotation Transform(Matrix3x2 matrix)
    {
        var m = Through(matrix);
        return this with { Start = Vector2.Transform(Start, m), End = Vector2.Transform(End, m), Style = Style.ForScale(ScaleOf(matrix)), Rotation = 0 };
    }

    public override bool HitTest(Vector2 point, float tolerance = 4f)
        => DistanceToSegment(ToLocal(point), Start, End) <= tolerance + Style.Stroke.Width / 2;

    internal static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared();
        if (lengthSquared < 1e-6f) return Vector2.Distance(p, a);
        var t = Math.Clamp(Vector2.Dot(p - a, ab) / lengthSquared, 0f, 1f);
        return Vector2.Distance(p, a + ab * t);
    }
}
