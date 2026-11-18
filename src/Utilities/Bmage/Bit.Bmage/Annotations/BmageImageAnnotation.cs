using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>An image stamp (watermark, logo, sticker) composited at a position.</summary>
public sealed record BmageImageAnnotation : BmageAnnotation
{
    /// <summary>The image to draw. Not owned by the annotation; the caller keeps it alive.</summary>
    public required BmageImageBuffer Image { get; init; }
    public required RectangleF Rectangle { get; init; }

    public override string Kind => "Image";
    public override RectangleF GetBounds() => Rectangle;

    public override void Render(BmageImageCanvas canvas, BmageAnnotationRenderContext context)
    {
        if (Image.IsDisposed) return;
        canvas.DrawImage(Image, Rectangle);
    }

    public override BmageAnnotation Translate(float dx, float dy) => this with { Rectangle = BmageRectangleAnnotation.Offset(Rectangle, dx, dy) };
    public override BmageAnnotation ForScale(double scale) => scale == 1 ? this : this with { Rectangle = BmageRectangleAnnotation.Scale(Rectangle, scale) };

    /// <summary>
    /// A move or a scale maps the stamp's rectangle exactly; a turn or a mirror turns the stamp with the image without
    /// mirroring its pixels, so a logo stays readable.
    /// </summary>
    public override BmageAnnotation Transform(Matrix3x2 matrix)
        => Rotation == 0 && BmageAffineParts.IsScaleAndTranslation(matrix) ? this with { Rectangle = MapUpright(Rectangle, matrix) } : base.Transform(matrix);
}
