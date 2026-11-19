using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>An arrow with a filled head at the end point. The most used annotation for screenshots and bug reports.</summary>
public sealed record BmageArrowAnnotation : BmageAnnotation
{
    public required Vector2 Start { get; init; }
    public required Vector2 End { get; init; }
    public BmageShapeStyle Style { get; init; } = BmageShapeStyle.Default;
    /// <summary>Head length as a multiple of the stroke width.</summary>
    public float HeadLengthFactor { get; init; } = 4f;
    /// <summary>Head half-width as a multiple of the stroke width.</summary>
    public float HeadWidthFactor { get; init; } = 3f;

    public override string Kind => "Arrow";

    public override RectangleF GetBounds()
    {
        var head = Style.Stroke.Width * MathF.Max(HeadLengthFactor, HeadWidthFactor);
        var r = RectangleF.FromLTRB(MathF.Min(Start.X, End.X), MathF.Min(Start.Y, End.Y), MathF.Max(Start.X, End.X), MathF.Max(Start.Y, End.Y));
        r.Inflate(head, head);
        return r;
    }

    public override void Render(BmageImageCanvas canvas, BmageAnnotationRenderContext context)
    {
        var color = Style.StrokeColor ?? Style.FillColor;
        if (color is { } c) canvas.DrawArrow(Start, End, c, Style.Stroke, HeadLengthFactor, HeadWidthFactor);
    }

    public override BmageAnnotation Translate(float dx, float dy) => this with { Start = Start + new Vector2(dx, dy), End = End + new Vector2(dx, dy) };
    public override BmageAnnotation ForScale(double scale) => scale == 1 ? this : this with { Start = Start * (float)scale, End = End * (float)scale, Style = Style.ForScale(scale) };

    public override BmageAnnotation Transform(Matrix3x2 matrix)
    {
        var m = Through(matrix);
        return this with { Start = Vector2.Transform(Start, m), End = Vector2.Transform(End, m), Style = Style.ForScale(ScaleOf(matrix)), Rotation = 0 };
    }

    public override bool HitTest(Vector2 point, float tolerance = 4f)
        => BmageLineAnnotation.DistanceToSegment(ToLocal(point), Start, End) <= tolerance + Style.Stroke.Width;
}
