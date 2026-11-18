using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>A translucent highlight rectangle, like a marker pen.</summary>
public sealed record BmageHighlightAnnotation : BmageAnnotation
{
    public required RectangleF Rectangle { get; init; }
    public BmageRgba32 Color { get; init; } = new(255, 235, 59, 110);

    public override string Kind => "Highlight";
    public override RectangleF GetBounds() => Rectangle;

    public override void Render(BmageImageCanvas canvas, BmageAnnotationRenderContext context)
        => canvas.FillRectangle(Rectangle, Color);

    public override BmageAnnotation Translate(float dx, float dy) => this with { Rectangle = BmageRectangleAnnotation.Offset(Rectangle, dx, dy) };
    public override BmageAnnotation ForScale(double scale) => scale == 1 ? this : this with { Rectangle = BmageRectangleAnnotation.Scale(Rectangle, scale) };

    public override BmageAnnotation Transform(Matrix3x2 matrix)
        => MapsUpright(matrix) ? this with { Rectangle = MapUpright(Rectangle, matrix) } : base.Transform(matrix);
}
