using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// A radial mask: fully covered within <see cref="Hardness"/> of its radius, fading smoothly to uncovered at the
/// ellipse's edge. The radial filter of photo editors, for spotlighting a subject or a custom vignette when inverted.
/// </summary>
/// <param name="Center">The centre, in the mask's units.</param>
/// <param name="RadiusX">The horizontal radius, in the mask's units.</param>
/// <param name="RadiusY">The vertical radius, in the mask's units.</param>
public sealed record BmageRadialGradientMask(Vector2 Center, float RadiusX, float RadiusY) : BmageMask
{
    /// <summary>The fraction of the radius that is fully covered before the fade starts, in [0,1]. Default 0.5.</summary>
    public float Hardness
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 0.5f;

    /// <summary>Clockwise rotation of the ellipse in degrees, applied in pixels around its centre.</summary>
    public float Rotation { get; init; }

    public override string Kind => "Radial gradient";

    private protected override bool DrawsWindows => true;

    private (Vector2 Center, float Rx, float Ry) InPixels(Matrix3x2 m)
        => (Vector2.Transform(Center, m), MathF.Abs(RadiusX * m.M11), MathF.Abs(RadiusY * m.M22));

    protected internal override void RenderCoverage(BmageMaskBuffer target, BmageMaskRenderContext context)
    {
        var (center, rx, ry) = InPixels(context.ToPixels);
        if (!(rx > 1e-3f) || !(ry > 1e-3f)) return;
        var angle = -Rotation * MathF.PI / 180f;
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        var hardness = Hardness;
        var fade = 1 - hardness;
        var size = context.Size;
        var bounds = GetCoverageBounds(size)!.Value;
        var area = Rectangle.FromLTRB((int)MathF.Floor(Math.Max(bounds.Left, 0)), (int)MathF.Floor(Math.Max(bounds.Top, 0)),
            (int)MathF.Ceiling(Math.Min(bounds.Right, size.Width)), (int)MathF.Ceiling(Math.Min(bounds.Bottom, size.Height)));
        var window = context.Window;
        area.Intersect(window);
        if (area.Width <= 0 || area.Height <= 0) return;
        BmageParallelRows.For(context.Operation, area.Top, area.Bottom, area.Width, (first, last, band) =>
        {
            for (var y = first; y < last; y++)
            {
                if (((y - first) & 63) == 0) band.ThrowIfCancellationRequested();
                var row = target.GetRow(y - window.Top);
                var dy = y + 0.5f - center.Y;
                for (var x = area.Left; x < area.Right; x++)
                {
                    var dx = x + 0.5f - center.X;
                    // Into the ellipse's own axes, then normalised so its edge is at distance 1.
                    var u = (dx * cos - dy * sin) / rx;
                    var v = (dx * sin + dy * cos) / ry;
                    var d = MathF.Sqrt(u * u + v * v);
                    if (d >= 1f) continue;
                    if (d <= hardness || fade <= 0)
                    {
                        row[x - window.Left] = 255;
                        continue;
                    }
                    var t = (1 - d) / fade;
                    row[x - window.Left] = BmageRgba32.ClampToByte(t * t * (3 - 2 * t) * 255f);
                }
            }
        });
    }

    public override RectangleF? GetCoverageBounds(Size size)
    {
        var (center, rx, ry) = InPixels(BmageMaskRasterizer.ToPixels(Units, size));
        var angle = Rotation * MathF.PI / 180f;
        var cos = MathF.Abs(MathF.Cos(angle));
        var sin = MathF.Abs(MathF.Sin(angle));
        var hx = rx * cos + ry * sin;
        var hy = rx * sin + ry * cos;
        return RectangleF.FromLTRB(center.X - hx - 1, center.Y - hy - 1, center.X + hx + 1, center.Y + hy + 1);
    }

    protected override BmageMask ScaleGeometry(double scale) => this with
    {
        Center = Center * (float)scale,
        RadiusX = (float)(RadiusX * scale),
        RadiusY = (float)(RadiusY * scale),
    };

    /// <summary>The centre follows the image, the radii scale with it and the ellipse turns with it; it is symmetric, so a mirror only reverses its turn.</summary>
    protected override BmageMask TransformGeometry(Matrix3x2 matrix)
    {
        var parts = BmageAffineParts.Of(matrix);
        return this with
        {
            Center = Vector2.Transform(Center, matrix),
            RadiusX = RadiusX * parts.Scale,
            RadiusY = RadiusY * parts.Scale,
            Rotation = BmageAffineParts.Normalize(parts.Mirrored ? parts.Angle - Rotation : Rotation + parts.Angle),
        };
    }
}
