using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>A rectangle, optionally with rounded corners.</summary>
public sealed record BmageRectangleAnnotation : BmageAnnotation
{
    public required RectangleF Rectangle { get; init; }
    public BmageShapeStyle Style { get; init; } = BmageShapeStyle.Default;
    /// <summary>Corner radius in pixels.</summary>
    public float CornerRadius { get; init; }

    public override string Kind => "Rectangle";
    public override RectangleF GetBounds() => Rectangle;

    public override void Render(BmageImageCanvas canvas, BmageAnnotationRenderContext context)
    {
        var path = CornerRadius > 0
            ? new BmagePathBuilder().AddRoundedRectangle(Rectangle, CornerRadius).Build()
            : new BmagePathBuilder().AddRectangle(Rectangle).Build();
        if (Style.FillColor is { } fill) canvas.FillPath(path, fill);
        if (Style.StrokeColor is { } stroke) canvas.StrokePath(path, stroke, Style.Stroke);
    }

    public override BmageAnnotation Translate(float dx, float dy) => this with { Rectangle = Offset(Rectangle, dx, dy) };

    public override BmageAnnotation ForScale(double scale) => scale == 1 ? this : this with
    {
        Rectangle = Scale(Rectangle, scale),
        CornerRadius = (float)(CornerRadius * scale),
        Style = Style.ForScale(scale),
    };

    public override BmageAnnotation Transform(Matrix3x2 matrix)
    {
        if (!MapsUpright(matrix)) return base.Transform(matrix);
        var s = ScaleOf(matrix);
        return this with { Rectangle = MapUpright(Rectangle, matrix), CornerRadius = CornerRadius * s, Style = Style.ForScale(s) };
    }

    internal static RectangleF Offset(RectangleF r, float dx, float dy) => new(r.X + dx, r.Y + dy, r.Width, r.Height);
    internal static RectangleF Scale(RectangleF r, double s) => new((float)(r.X * s), (float)(r.Y * s), (float)(r.Width * s), (float)(r.Height * s));
}
