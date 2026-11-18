using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>The ellipse inscribed in a rectangle. <see cref="BmageMask.Ellipse"/> gives the one inscribed in the whole image.</summary>
/// <param name="Rectangle">The rectangle the ellipse fills, in the mask's <see cref="BmageMask.Units"/>.</param>
public sealed record BmageEllipseMask(RectangleF Rectangle) : BmageMask
{
    public override string Kind => "Ellipse";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(BmageMaskBuffer target, BmageMaskRenderContext context)
        => BmageMaskRasterizer.Fill(target, context.Window, new BmagePathBuilder().AddEllipse(BmageMaskRasterizer.Transform(Rectangle, context.ToPixels)).Build(), BmageFillRule.NonZero);

    public override RectangleF? GetCoverageBounds(Size size) => BmageMaskRasterizer.Transform(Rectangle, BmageMaskRasterizer.ToPixels(Units, size));

    protected override BmageMask ScaleGeometry(double scale) => this with { Rectangle = BmageRectangleAnnotation.Scale(Rectangle, scale) };

    /// <summary>An upright transform maps the ellipse's rectangle exactly; any other turns it into the transformed outline.</summary>
    protected override BmageMask TransformGeometry(Matrix3x2 matrix) => BmageAffineParts.PreservesAxes(matrix)
        ? this with { Rectangle = BmageAffineParts.MapRectangle(Rectangle, matrix) }
        : Inherit(new BmagePathMask(new BmagePathBuilder().AddEllipse(Rectangle).Build().Transform(matrix)));
}
