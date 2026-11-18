using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>A rectangle or ellipse with a leader line pointing at something, plus a text label.</summary>
public sealed record BmageCalloutAnnotation : BmageAnnotation
{
    public required RectangleF Bubble { get; init; }
    /// <summary>Where the leader line points, in image coordinates.</summary>
    public required Vector2 Target { get; init; }
    public string Text { get; init; } = string.Empty;
    public BmageTextStyle TextStyle { get; init; } = new() { Alignment = BmageTextAlignment.Center, Baseline = BmageTextBaseline.Middle };
    public BmageShapeStyle Style { get; init; } = BmageShapeStyle.Default with { FillColor = BmageRgba32.White };
    public float CornerRadius { get; init; } = 6f;

    public override string Kind => "Callout";

    public override RectangleF GetBounds()
    {
        var r = Bubble;
        return RectangleF.FromLTRB(MathF.Min(r.Left, Target.X), MathF.Min(r.Top, Target.Y), MathF.Max(r.Right, Target.X), MathF.Max(r.Bottom, Target.Y));
    }

    public override void Render(BmageImageCanvas canvas, BmageAnnotationRenderContext context)
    {
        // Leader: a triangle from the two bubble edge points nearest the target.
        var center = new Vector2(Bubble.X + Bubble.Width / 2, Bubble.Y + Bubble.Height / 2);
        var direction = Target - center;
        if (direction.LengthSquared() > 1e-3f)
        {
            direction = Vector2.Normalize(direction);
            var perpendicular = new Vector2(-direction.Y, direction.X) * MathF.Min(Bubble.Width, Bubble.Height) * 0.18f;
            var basePoint = EdgePoint(Bubble, center, direction);
            var tail = new BmagePathBuilder()
                .MoveTo(basePoint + perpendicular)
                .LineTo(Target)
                .LineTo(basePoint - perpendicular)
                .Close()
                .Build();
            if (Style.FillColor is { } tailFill) canvas.FillPath(tail, tailFill);
            if (Style.StrokeColor is { } tailStroke) canvas.StrokePath(tail, tailStroke, Style.Stroke);
        }
        var bubble = new BmagePathBuilder().AddRoundedRectangle(Bubble, CornerRadius).Build();
        if (Style.FillColor is { } fill) canvas.FillPath(bubble, fill);
        if (Style.StrokeColor is { } stroke) canvas.StrokePath(bubble, stroke, Style.Stroke);
        if (!string.IsNullOrEmpty(Text) && context.TextRasterizer is { } rasterizer)
            canvas.DrawText(Text, center, TextStyle with { MaxWidth = TextStyle.MaxWidth ?? Bubble.Width - 12 }, rasterizer);
    }

    private static Vector2 EdgePoint(RectangleF rect, Vector2 center, Vector2 direction)
    {
        // Intersect the ray from the centre with the rectangle border.
        var tx = direction.X == 0 ? float.MaxValue : (direction.X > 0 ? rect.Right - center.X : rect.Left - center.X) / direction.X;
        var ty = direction.Y == 0 ? float.MaxValue : (direction.Y > 0 ? rect.Bottom - center.Y : rect.Top - center.Y) / direction.Y;
        return center + direction * MathF.Min(tx, ty);
    }

    public override BmageAnnotation Translate(float dx, float dy) => this with
    {
        Bubble = BmageRectangleAnnotation.Offset(Bubble, dx, dy),
        Target = Target + new Vector2(dx, dy),
    };

    public override BmageAnnotation ForScale(double scale) => scale == 1 ? this : this with
    {
        Bubble = BmageRectangleAnnotation.Scale(Bubble, scale),
        Target = Target * (float)scale,
        TextStyle = TextStyle.ForScale(scale),
        Style = Style.ForScale(scale),
        CornerRadius = (float)(CornerRadius * scale),
    };

    /// <summary>The leader keeps pointing at its target; the bubble's centre follows the image and the bubble stays upright, so its text stays readable.</summary>
    public override BmageAnnotation Transform(Matrix3x2 matrix)
    {
        var m = Through(matrix);
        var s = ScaleOf(matrix);
        var centre = Vector2.Transform(new Vector2(Bubble.X + Bubble.Width / 2, Bubble.Y + Bubble.Height / 2), m);
        var size = new SizeF(Bubble.Width * s, Bubble.Height * s);
        return this with
        {
            Target = Vector2.Transform(Target, m),
            Bubble = new RectangleF(centre.X - size.Width / 2, centre.Y - size.Height / 2, size.Width, size.Height),
            TextStyle = TextStyle.ForScale(s),
            Style = Style.ForScale(s),
            CornerRadius = CornerRadius * s,
            Rotation = 0,
        };
    }
}
