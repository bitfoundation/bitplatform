using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>A polygon through arbitrary points: a lasso selection.</summary>
public sealed record BmagePolygonMask : BmageMask
{
    private readonly Vector2[] _points = [];

    /// <param name="points">The corners in order, in the mask's <see cref="BmageMask.Units"/>. The polygon is closed automatically.</param>
    public BmagePolygonMask(IReadOnlyList<Vector2> points) => Points = points;

    /// <summary>The corners in order, in the mask's units.</summary>
    public IReadOnlyList<Vector2> Points
    {
        get => _points;
        init => _points = value?.ToArray() ?? throw new ArgumentNullException(nameof(Points));
    }

    /// <summary>How self-intersecting outlines are filled.</summary>
    public BmageFillRule Rule { get; init; }

    public override string Kind => "Polygon";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(BmageMaskBuffer target, BmageMaskRenderContext context)
    {
        if (_points.Length < 3) return;
        var m = context.ToPixels;
        BmageMaskRasterizer.Fill(target, context.Window, new BmagePathBuilder().AddPolygon([.. _points.Select(p => Vector2.Transform(p, m))]).Build(), Rule);
    }

    public override RectangleF? GetCoverageBounds(Size size)
    {
        if (_points.Length < 3) return RectangleF.Empty;
        return BmageMaskRasterizer.Transform(BmageMaskRasterizer.BoundsOf(_points), BmageMaskRasterizer.ToPixels(Units, size));
    }

    protected override BmageMask ScaleGeometry(double scale) => this with { Points = [.. _points.Select(p => p * (float)scale)] };

    protected override BmageMask TransformGeometry(Matrix3x2 matrix) => this with { Points = [.. _points.Select(p => Vector2.Transform(p, matrix))] };
}
