using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>A numbered step marker, for tutorials and bug reports.</summary>
public sealed record BmageStepAnnotation : BmageAnnotation
{
    public required Vector2 Center { get; init; }
    public required int Number { get; init; }
    public float Radius { get; init; } = 18f;
    public BmageRgba32 BackgroundColor { get; init; } = new(230, 40, 40);
    public BmageRgba32 ForegroundColor { get; init; } = BmageRgba32.White;
    public BmageTextStyle TextStyle { get; init; } = new() { Alignment = BmageTextAlignment.Center, Baseline = BmageTextBaseline.Middle, Bold = true };

    public override string Kind => "Step";
    public override RectangleF GetBounds() => new(Center.X - Radius, Center.Y - Radius, Radius * 2, Radius * 2);

    public override void Render(BmageImageCanvas canvas, BmageAnnotationRenderContext context)
    {
        canvas.FillCircle(Center, Radius, BackgroundColor);
        if (context.TextRasterizer is { } rasterizer)
        {
            var style = TextStyle with { FontSize = Radius * 1.15f, FillColor = ForegroundColor };
            canvas.DrawText(Number.ToString(System.Globalization.CultureInfo.InvariantCulture), Center, style, rasterizer);
        }
    }

    public override BmageAnnotation Translate(float dx, float dy) => this with { Center = Center + new Vector2(dx, dy) };

    public override BmageAnnotation ForScale(double scale) => scale == 1 ? this : this with
    {
        Center = Center * (float)scale,
        Radius = (float)(Radius * scale),
        TextStyle = TextStyle.ForScale(scale),
    };

    /// <summary>The marker follows the image and keeps its number upright.</summary>
    public override BmageAnnotation Transform(Matrix3x2 matrix)
    {
        var s = ScaleOf(matrix);
        return this with { Center = Vector2.Transform(Center, Through(matrix)), Radius = Radius * s, TextStyle = TextStyle.ForScale(s), Rotation = 0 };
    }

    public override bool HitTest(Vector2 point, float tolerance = 4f) => Vector2.Distance(point, Center) <= Radius + tolerance;
}
