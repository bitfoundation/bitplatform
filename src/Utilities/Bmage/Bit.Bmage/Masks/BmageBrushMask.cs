using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>A mask painted with a brush, stroke by stroke, the way a selective-adjustment brush works in a photo editor.</summary>
public sealed record BmageBrushMask : BmageMask
{
    private readonly BmageBrushStroke[] _strokes = [];

    /// <param name="strokes">The strokes in the order they were painted.</param>
    public BmageBrushMask(IReadOnlyList<BmageBrushStroke> strokes) => Strokes = strokes;

    /// <summary>The strokes in the order they were painted; an erasing stroke removes what earlier strokes painted.</summary>
    public IReadOnlyList<BmageBrushStroke> Strokes
    {
        get => _strokes;
        init => _strokes = value?.ToArray() ?? throw new ArgumentNullException(nameof(Strokes));
    }

    /// <summary>Returns the mask with one more stroke painted on top.</summary>
    public BmageBrushMask Add(BmageBrushStroke stroke)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        return this with { Strokes = [.. _strokes, stroke] };
    }

    public override string Kind => "Brush";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(BmageMaskBuffer target, BmageMaskRenderContext context)
    {
        foreach (var stroke in _strokes)
        {
            context.Operation.ThrowIfCancellationRequested();
            if (stroke.Points.Count == 0) continue;
            // Mapped to pixels before the curves are flattened, so their tolerance is a fraction of a pixel. The width
            // scales with the units like everything else; non-uniform relative units average the two axes.
            var m = context.ToPixels;
            var width = stroke.Width * (MathF.Abs(m.M11) + MathF.Abs(m.M22)) / 2;
            var points = stroke.Points.Select(p => Vector2.Transform(p, m)).ToArray();
            BmageVectorPath outline;
            if (points.Length == 1)
            {
                var p = points[0];
                var r = width / 2;
                outline = new BmagePathBuilder().AddEllipse(new RectangleF(p.X - r, p.Y - r, r * 2, r * 2)).Build();
            }
            else
            {
                var builder = new BmagePathBuilder();
                BmageFreehandAnnotation.AppendSmoothed(builder, points, closed: false);
                outline = BmageStroker.CreateStrokeOutline(builder.Build(), new BmageStrokeStyle { Width = width, Cap = BmageLineCap.Round, Join = BmageLineJoin.Round });
            }
            BmageMaskRasterizer.Fill(target, context.Window, outline, BmageFillRule.NonZero, stroke.Opacity, stroke.Erase);
        }
    }

    public override RectangleF? GetCoverageBounds(Size size)
    {
        var painted = _strokes.Where(s => !s.Erase && s.Points.Count > 0).ToArray();
        if (painted.Length == 0) return RectangleF.Empty;
        // The smoothing curves bulge past the points at sharp turns, but a cubic never leaves the hull of its control
        // points, so those are included too.
        var bounds = BmageMaskRasterizer.BoundsOf(painted.SelectMany(s => BmageFreehandAnnotation.SmoothingHull(s.Points)));
        var reach = painted.Max(s => s.Width) / 2 + 2;
        bounds.Inflate(reach, reach);
        return BmageMaskRasterizer.Transform(bounds, BmageMaskRasterizer.ToPixels(Units, size));
    }

    protected override BmageMask ScaleGeometry(double scale) => this with { Strokes = [.. _strokes.Select(s => s.Scale(scale))] };

    protected override BmageMask TransformGeometry(Matrix3x2 matrix)
    {
        var scale = BmageAffineParts.Of(matrix).Scale;
        return this with { Strokes = [.. _strokes.Select(s => s with { Points = [.. s.Points.Select(p => Vector2.Transform(p, matrix))], Width = Math.Max(0.01f, s.Width * scale) })] };
    }
}
