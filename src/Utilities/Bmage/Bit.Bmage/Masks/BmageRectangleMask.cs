using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>A rectangle, optionally with rounded corners.</summary>
/// <param name="Rectangle">The covered area, in the mask's <see cref="BmageMask.Units"/>.</param>
public sealed record BmageRectangleMask(RectangleF Rectangle) : BmageMask
{
    /// <summary>
    /// Corner radius in the mask's units. With <see cref="BmageMaskUnits.Relative"/> it is a fraction of the image's shorter
    /// side, so the corners stay round on an image of any shape.
    /// </summary>
    public float CornerRadius { get; init; }

    public override string Kind => "Rectangle";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(BmageMaskBuffer target, BmageMaskRenderContext context)
    {
        // Mapped to pixels before the corners are flattened, so their tolerance is a fraction of a pixel.
        var rect = BmageMaskRasterizer.Transform(Rectangle, context.ToPixels);
        var radius = Units == BmageMaskUnits.Relative ? CornerRadius * Math.Min(context.Size.Width, context.Size.Height) : CornerRadius;
        var builder = new BmagePathBuilder();
        BmageMaskRasterizer.Fill(target, context.Window, (radius > 0 ? builder.AddRoundedRectangle(rect, radius) : builder.AddRectangle(rect)).Build(), BmageFillRule.NonZero);
    }

    public override RectangleF? GetCoverageBounds(Size size) => BmageMaskRasterizer.Transform(Rectangle, BmageMaskRasterizer.ToPixels(Units, size));

    protected override BmageMask ScaleGeometry(double scale) => this with
    {
        Rectangle = BmageRectangleAnnotation.Scale(Rectangle, scale),
        CornerRadius = (float)(CornerRadius * scale),
    };

    /// <summary>An upright transform maps the rectangle exactly; any other turns it into the transformed outline.</summary>
    protected override BmageMask TransformGeometry(Matrix3x2 matrix)
    {
        if (BmageAffineParts.PreservesAxes(matrix))
            return this with { Rectangle = BmageAffineParts.MapRectangle(Rectangle, matrix), CornerRadius = CornerRadius * BmageAffineParts.Of(matrix).Scale };
        var outline = CornerRadius > 0 ? new BmagePathBuilder().AddRoundedRectangle(Rectangle, CornerRadius) : new BmagePathBuilder().AddRectangle(Rectangle);
        return Inherit(new BmagePathMask(outline.Build().Transform(matrix)));
    }
}
