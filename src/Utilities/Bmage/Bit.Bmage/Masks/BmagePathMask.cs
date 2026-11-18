using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Any vector outline, such as text from <see cref="IBmageTextRasterizer.GetTextPath"/> for a knockout, in pixels of the image.
/// </summary>
/// <param name="Path">The outline. Always in pixels: a path carries no units of its own.</param>
public sealed record BmagePathMask(BmageVectorPath Path) : BmageMask
{
    public BmageVectorPath Path { get; init; } = Path ?? throw new ArgumentNullException(nameof(Path));

    /// <summary>How overlapping contours are filled.</summary>
    public BmageFillRule Rule { get; init; }

    public override string Kind => "Path";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(BmageMaskBuffer target, BmageMaskRenderContext context)
        => BmageMaskRasterizer.Fill(target, context.Window, Units == BmageMaskUnits.Relative ? Path.Transform(context.ToPixels) : Path, Rule);

    public override RectangleF? GetCoverageBounds(Size size)
        => Path.IsEmpty ? RectangleF.Empty : BmageMaskRasterizer.Transform(Path.Bounds, BmageMaskRasterizer.ToPixels(Units, size));

    protected override BmageMask ScaleGeometry(double scale) => this with { Path = Path.Transform(Matrix3x2.CreateScale((float)scale)) };

    protected override BmageMask TransformGeometry(Matrix3x2 matrix) => this with { Path = Path.Transform(matrix) };
}
