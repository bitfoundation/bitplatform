using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Hides part of the image. Unlike other annotations this one reads the pixels underneath, so it is applied by the
/// renderer before the overlay pass.
/// </summary>
public sealed record BmageRedactionAnnotation : BmageAnnotation
{
    private const float DefaultStrength = 12f;

    /// <summary>A block or radius this large already reaches past any image, so a larger one hides no differently.</summary>
    private const float MaxStrength = 1e8f;

    public required RectangleF Rectangle { get; init; }
    public BmageRedactionMode Mode { get; init; } = BmageRedactionMode.Solid;
    public BmageRgba32 Color { get; init; } = BmageRgba32.Black;
    /// <summary>Block size for <see cref="BmageRedactionMode.Pixelate"/> or radius for <see cref="BmageRedactionMode.Blur"/>.</summary>
    public float Strength { get; init; } = DefaultStrength;

    public override string Kind => "Redaction";
    public override RectangleF GetBounds() => Rectangle;

    public override void Render(BmageImageCanvas canvas, BmageAnnotationRenderContext context)
    {
        var area = Rectangle;
        if (Mode is not (BmageRedactionMode.Pixelate or BmageRedactionMode.Blur))
        {
            canvas.FillRectangle(area, Color);
            return;
        }

        // The filters work on device pixels, so the region the user sees (after the canvas transform, which carries
        // this annotation's rotation) is mapped there first. Rounding outwards means a partly covered pixel is
        // redacted rather than left readable.
        var outline = new BmagePathBuilder().AddRectangle(area).Build().Transform(canvas.Transform);
        var bounds = outline.Bounds;
        if (!float.IsFinite(bounds.Left) || !float.IsFinite(bounds.Top) || !float.IsFinite(bounds.Right) || !float.IsFinite(bounds.Bottom)) return;
        var device = System.Drawing.Rectangle.FromLTRB(
            (int)MathF.Floor(bounds.Left), (int)MathF.Floor(bounds.Top), (int)MathF.Ceiling(bounds.Right), (int)MathF.Ceiling(bounds.Bottom));
        device.Intersect(canvas.Clip);
        device.Intersect(canvas.Buffer.Bounds);
        if (device.IsEmptyArea()) return;

        var transform = canvas.Transform;
        var axisAligned = transform.M12 == 0 && transform.M21 == 0;
        if (axisAligned)
        {
            Redact(canvas.Buffer, device, context.Operation);
            return;
        }

        // Rotated: filter the bounding box on a copy, then blend it back only inside the rotated outline.
        using var filtered = canvas.Buffer.CopyRegion(device);
        Redact(filtered, new System.Drawing.Rectangle(System.Drawing.Point.Empty, filtered.Size), context.Operation);
        var target = canvas.Buffer;
        BmageRasterizer.Rasterize(outline, device, BmageFillRule.NonZero, (y, x0, coverage) =>
        {
            var row = target.GetRow(y);
            var source = filtered.GetRow(y - device.Y);
            for (var i = 0; i < coverage.Length; i++)
            {
                var a = Math.Min(1f, coverage[i]);
                if (a <= 0) continue;
                var x = x0 + i;
                row[x] = a >= 0.999f ? source[x - device.X] : Lerp(row[x], source[x - device.X], a);
            }
        });
    }

    private static BmageRgba32 Lerp(BmageRgba32 from, BmageRgba32 to, float t) => new(
        (byte)MathF.Round(from.R + (to.R - from.R) * t),
        (byte)MathF.Round(from.G + (to.G - from.G) * t),
        (byte)MathF.Round(from.B + (to.B - from.B) * t),
        (byte)MathF.Round(from.A + (to.A - from.A) * t));

    /// <summary>Pixelates or blurs a region of a buffer in place.</summary>
    private void Redact(BmageImageBuffer buffer, System.Drawing.Rectangle region, BmageOperationContext context)
    {
        // The strength comes from a document, or from scaling one, so it can be infinite or not a number at all. It is
        // clamped rather than left to fail the render, and one that is not a number hides as much as the default does.
        var strength = float.IsNaN(Strength) ? DefaultStrength : Math.Clamp(Strength, 1f, MaxStrength);
        IBmageImageOperation filter = Mode == BmageRedactionMode.Pixelate
            ? new BmagePixelateFilter(Math.Max(2, (int)strength), region)
            : new BmageBlurFilter(strength, region);
        filter.Apply(buffer, context.WithProgress(null).WithMutation(true));
    }

    public override BmageAnnotation Translate(float dx, float dy) => this with { Rectangle = BmageRectangleAnnotation.Offset(Rectangle, dx, dy) };

    public override BmageAnnotation ForScale(double scale) => scale == 1 ? this : this with
    {
        Rectangle = BmageRectangleAnnotation.Scale(Rectangle, scale),
        Strength = (float)Math.Max(1, Strength * scale),
    };

    /// <remarks>
    /// Upright, the rectangle maps exactly. Turned by an angle that is not a quarter turn, the redaction keeps its own
    /// rotation and is drawn over every device pixel its turned outline touches, so nothing it covered comes back into view.
    /// </remarks>
    public override BmageAnnotation Transform(Matrix3x2 matrix)
        => MapsUpright(matrix) ? this with { Rectangle = MapUpright(Rectangle, matrix), Strength = Math.Max(1, Strength * ScaleOf(matrix)) } : base.Transform(matrix);
}
