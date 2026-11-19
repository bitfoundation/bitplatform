using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>An ellipse inscribed in a rectangle.</summary>
public sealed record BmageEllipseAnnotation : BmageAnnotation
{
    public required RectangleF Rectangle { get; init; }
    public BmageShapeStyle Style { get; init; } = BmageShapeStyle.Default;

    public override string Kind => "Ellipse";
    public override RectangleF GetBounds() => Rectangle;

    public override void Render(BmageImageCanvas canvas, BmageAnnotationRenderContext context)
    {
        var path = new BmagePathBuilder().AddEllipse(Rectangle).Build();
        if (Style.FillColor is { } fill) canvas.FillPath(path, fill);
        if (Style.StrokeColor is { } stroke) canvas.StrokePath(path, stroke, Style.Stroke);
    }

    public override BmageAnnotation Translate(float dx, float dy) => this with { Rectangle = BmageRectangleAnnotation.Offset(Rectangle, dx, dy) };
    public override BmageAnnotation ForScale(double scale) => scale == 1 ? this : this with { Rectangle = BmageRectangleAnnotation.Scale(Rectangle, scale), Style = Style.ForScale(scale) };

    public override BmageAnnotation Transform(Matrix3x2 matrix)
        => MapsUpright(matrix) ? this with { Rectangle = MapUpright(Rectangle, matrix), Style = Style.ForScale(ScaleOf(matrix)) } : base.Transform(matrix);

    public override bool HitTest(Vector2 point, float tolerance = 4f)
    {
        var local = point;
        if (Rotation != 0 && Matrix3x2.Invert(GetTransform(), out var inverse)) local = Vector2.Transform(point, inverse);
        var rx = Rectangle.Width / 2 + tolerance;
        var ry = Rectangle.Height / 2 + tolerance;
        if (rx <= 0 || ry <= 0) return false;
        var dx = (local.X - (Rectangle.X + Rectangle.Width / 2)) / rx;
        var dy = (local.Y - (Rectangle.Y + Rectangle.Height / 2)) / ry;
        return dx * dx + dy * dy <= 1f;
    }
}
