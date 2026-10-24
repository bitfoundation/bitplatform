using System.Drawing;
using System.Numerics;
using Bit.Bmage.Annotations;
using Bit.Bmage.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;

namespace Bit.Bmage.Masks;

/// <summary>A rectangle, optionally with rounded corners.</summary>
/// <param name="Rectangle">The covered area, in the mask's <see cref="Mask.Units"/>.</param>
public sealed record RectangleMask(RectangleF Rectangle) : Mask
{
    /// <summary>
    /// Corner radius in the mask's units. With <see cref="MaskUnits.Relative"/> it is a fraction of the image's shorter
    /// side, so the corners stay round on an image of any shape.
    /// </summary>
    public float CornerRadius { get; init; }

    public override string Kind => "Rectangle";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
    {
        // Mapped to pixels before the corners are flattened, so their tolerance is a fraction of a pixel.
        var rect = MaskRasterizer.Transform(Rectangle, context.ToPixels);
        var radius = Units == MaskUnits.Relative ? CornerRadius * Math.Min(context.Size.Width, context.Size.Height) : CornerRadius;
        var builder = new PathBuilder();
        MaskRasterizer.Fill(target, context.Window, (radius > 0 ? builder.AddRoundedRectangle(rect, radius) : builder.AddRectangle(rect)).Build(), FillRule.NonZero);
    }

    public override RectangleF? GetCoverageBounds(Size size) => MaskRasterizer.Transform(Rectangle, MaskRasterizer.ToPixels(Units, size));

    protected override Mask ScaleGeometry(double scale) => this with
    {
        Rectangle = RectangleAnnotation.Scale(Rectangle, scale),
        CornerRadius = (float)(CornerRadius * scale),
    };

    /// <summary>An upright transform maps the rectangle exactly; any other turns it into the transformed outline.</summary>
    protected override Mask TransformGeometry(Matrix3x2 matrix)
    {
        if (AffineParts.PreservesAxes(matrix))
            return this with { Rectangle = AffineParts.MapRectangle(Rectangle, matrix), CornerRadius = CornerRadius * AffineParts.Of(matrix).Scale };
        var outline = CornerRadius > 0 ? new PathBuilder().AddRoundedRectangle(Rectangle, CornerRadius) : new PathBuilder().AddRectangle(Rectangle);
        return Inherit(new PathMask(outline.Build().Transform(matrix)));
    }
}

/// <summary>The ellipse inscribed in a rectangle. <see cref="Mask.Ellipse"/> gives the one inscribed in the whole image.</summary>
/// <param name="Rectangle">The rectangle the ellipse fills, in the mask's <see cref="Mask.Units"/>.</param>
public sealed record EllipseMask(RectangleF Rectangle) : Mask
{
    public override string Kind => "Ellipse";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
        => MaskRasterizer.Fill(target, context.Window, new PathBuilder().AddEllipse(MaskRasterizer.Transform(Rectangle, context.ToPixels)).Build(), FillRule.NonZero);

    public override RectangleF? GetCoverageBounds(Size size) => MaskRasterizer.Transform(Rectangle, MaskRasterizer.ToPixels(Units, size));

    protected override Mask ScaleGeometry(double scale) => this with { Rectangle = RectangleAnnotation.Scale(Rectangle, scale) };

    /// <summary>An upright transform maps the ellipse's rectangle exactly; any other turns it into the transformed outline.</summary>
    protected override Mask TransformGeometry(Matrix3x2 matrix) => AffineParts.PreservesAxes(matrix)
        ? this with { Rectangle = AffineParts.MapRectangle(Rectangle, matrix) }
        : Inherit(new PathMask(new PathBuilder().AddEllipse(Rectangle).Build().Transform(matrix)));
}

/// <summary>A polygon through arbitrary points: a lasso selection.</summary>
public sealed record PolygonMask : Mask
{
    private readonly Vector2[] _points = [];

    /// <param name="points">The corners in order, in the mask's <see cref="Mask.Units"/>. The polygon is closed automatically.</param>
    public PolygonMask(IReadOnlyList<Vector2> points) => Points = points;

    /// <summary>The corners in order, in the mask's units.</summary>
    public IReadOnlyList<Vector2> Points
    {
        get => _points;
        init => _points = value?.ToArray() ?? throw new ArgumentNullException(nameof(Points));
    }

    /// <summary>How self-intersecting outlines are filled.</summary>
    public FillRule Rule { get; init; }

    public override string Kind => "Polygon";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
    {
        if (_points.Length < 3) return;
        var m = context.ToPixels;
        MaskRasterizer.Fill(target, context.Window, new PathBuilder().AddPolygon([.. _points.Select(p => Vector2.Transform(p, m))]).Build(), Rule);
    }

    public override RectangleF? GetCoverageBounds(Size size)
    {
        if (_points.Length < 3) return RectangleF.Empty;
        return MaskRasterizer.Transform(MaskRasterizer.BoundsOf(_points), MaskRasterizer.ToPixels(Units, size));
    }

    protected override Mask ScaleGeometry(double scale) => this with { Points = [.. _points.Select(p => p * (float)scale)] };

    protected override Mask TransformGeometry(Matrix3x2 matrix) => this with { Points = [.. _points.Select(p => Vector2.Transform(p, matrix))] };
}

/// <summary>
/// Any vector outline, such as text from <see cref="ITextRasterizer.GetTextPath"/> for a knockout, in pixels of the image.
/// </summary>
/// <param name="Path">The outline. Always in pixels: a path carries no units of its own.</param>
public sealed record PathMask(VectorPath Path) : Mask
{
    public VectorPath Path { get; init; } = Path ?? throw new ArgumentNullException(nameof(Path));

    /// <summary>How overlapping contours are filled.</summary>
    public FillRule Rule { get; init; }

    public override string Kind => "Path";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
        => MaskRasterizer.Fill(target, context.Window, Units == MaskUnits.Relative ? Path.Transform(context.ToPixels) : Path, Rule);

    public override RectangleF? GetCoverageBounds(Size size)
        => Path.IsEmpty ? RectangleF.Empty : MaskRasterizer.Transform(Path.Bounds, MaskRasterizer.ToPixels(Units, size));

    protected override Mask ScaleGeometry(double scale) => this with { Path = Path.Transform(Matrix3x2.CreateScale((float)scale)) };

    protected override Mask TransformGeometry(Matrix3x2 matrix) => this with { Path = Path.Transform(matrix) };
}

/// <summary>One stroke of a mask brush.</summary>
public sealed record BrushStroke
{
    private readonly Vector2[] _points = [];

    /// <param name="points">The path the brush followed, in the mask's units.</param>
    /// <param name="width">The brush diameter, in the mask's units.</param>
    public BrushStroke(IReadOnlyList<Vector2> points, float width)
    {
        Points = points;
        Width = width;
    }

    /// <summary>The path the brush followed. Drawn smoothed, as freehand annotations are.</summary>
    public IReadOnlyList<Vector2> Points
    {
        get => _points;
        init => _points = value?.ToArray() ?? throw new ArgumentNullException(nameof(Points));
    }

    /// <summary>The brush diameter.</summary>
    public float Width
    {
        get;
        init => field = value > 0 && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Width), "A brush needs a positive width.");
    }

    /// <summary>How much coverage the stroke adds (or removes, when erasing), in [0,1].</summary>
    public float Opacity
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 1f;

    /// <summary>Removes coverage instead of adding it, like painting a mask with black.</summary>
    public bool Erase { get; init; }

    internal BrushStroke Scale(double scale) => this with { Points = [.. _points.Select(p => p * (float)scale)], Width = (float)(Width * scale) };
}

/// <summary>A mask painted with a brush, stroke by stroke, the way a selective-adjustment brush works in a photo editor.</summary>
public sealed record BrushMask : Mask
{
    private readonly BrushStroke[] _strokes = [];

    /// <param name="strokes">The strokes in the order they were painted.</param>
    public BrushMask(IReadOnlyList<BrushStroke> strokes) => Strokes = strokes;

    /// <summary>The strokes in the order they were painted; an erasing stroke removes what earlier strokes painted.</summary>
    public IReadOnlyList<BrushStroke> Strokes
    {
        get => _strokes;
        init => _strokes = value?.ToArray() ?? throw new ArgumentNullException(nameof(Strokes));
    }

    /// <summary>Returns the mask with one more stroke painted on top.</summary>
    public BrushMask Add(BrushStroke stroke)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        return this with { Strokes = [.. _strokes, stroke] };
    }

    public override string Kind => "Brush";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
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
            VectorPath outline;
            if (points.Length == 1)
            {
                var p = points[0];
                var r = width / 2;
                outline = new PathBuilder().AddEllipse(new RectangleF(p.X - r, p.Y - r, r * 2, r * 2)).Build();
            }
            else
            {
                var builder = new PathBuilder();
                FreehandAnnotation.AppendSmoothed(builder, points, closed: false);
                outline = Stroker.CreateStrokeOutline(builder.Build(), new StrokeStyle { Width = width, Cap = LineCap.Round, Join = LineJoin.Round });
            }
            MaskRasterizer.Fill(target, context.Window, outline, FillRule.NonZero, stroke.Opacity, stroke.Erase);
        }
    }

    public override RectangleF? GetCoverageBounds(Size size)
    {
        var painted = _strokes.Where(s => !s.Erase && s.Points.Count > 0).ToArray();
        if (painted.Length == 0) return RectangleF.Empty;
        // The smoothing curves bulge past the points at sharp turns, but a cubic never leaves the hull of its control
        // points, so those are included too.
        var bounds = MaskRasterizer.BoundsOf(painted.SelectMany(s => FreehandAnnotation.SmoothingHull(s.Points)));
        var reach = painted.Max(s => s.Width) / 2 + 2;
        bounds.Inflate(reach, reach);
        return MaskRasterizer.Transform(bounds, MaskRasterizer.ToPixels(Units, size));
    }

    protected override Mask ScaleGeometry(double scale) => this with { Strokes = [.. _strokes.Select(s => s.Scale(scale))] };

    protected override Mask TransformGeometry(Matrix3x2 matrix)
    {
        var scale = AffineParts.Of(matrix).Scale;
        return this with { Strokes = [.. _strokes.Select(s => s with { Points = [.. s.Points.Select(p => Vector2.Transform(p, matrix))], Width = Math.Max(0.01f, s.Width * scale) })] };
    }
}

/// <summary>
/// A graduated mask: fully covered up to <see cref="Start"/>, fading linearly to uncovered at <see cref="End"/> and
/// beyond, across the whole width of the image. The graduated filter of photo editors, for skies and horizons.
/// </summary>
/// <param name="Start">Where the coverage starts to fade, in the mask's units.</param>
/// <param name="End">Where the coverage reaches nothing, in the mask's units.</param>
public sealed record LinearGradientMask(Vector2 Start, Vector2 End) : Mask
{
    public override string Kind => "Linear gradient";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
    {
        var m = context.ToPixels;
        var start = Vector2.Transform(Start, m);
        var axis = Vector2.Transform(End, m) - start;
        var lengthSquared = axis.LengthSquared();
        if (!(lengthSquared > 1e-6f))
        {
            // No direction to fade along: everything is on the covered side.
            target.Fill(255);
            return;
        }
        var step = axis / lengthSquared;
        var window = context.Window;
        ParallelRows.For(context.Operation, window.Top, window.Bottom, window.Width, (first, last, band) =>
        {
            for (var y = first; y < last; y++)
            {
                if (((y - first) & 63) == 0) band.ThrowIfCancellationRequested();
                var row = target.GetRow(y - window.Top);
                var t0 = Vector2.Dot(new Vector2(0.5f, y + 0.5f) - start, step);
                for (var x = window.Left; x < window.Right; x++)
                {
                    var t = t0 + x * step.X;
                    row[x - window.Left] = t <= 0 ? (byte)255 : t >= 1 ? (byte)0 : Rgba32.ClampToByte((1 - t) * 255f);
                }
            }
        });
    }

    protected override Mask ScaleGeometry(double scale) => this with { Start = Start * (float)scale, End = End * (float)scale };

    protected override Mask TransformGeometry(Matrix3x2 matrix) => this with { Start = Vector2.Transform(Start, matrix), End = Vector2.Transform(End, matrix) };
}

/// <summary>
/// A radial mask: fully covered within <see cref="Hardness"/> of its radius, fading smoothly to uncovered at the
/// ellipse's edge. The radial filter of photo editors, for spotlighting a subject or a custom vignette when inverted.
/// </summary>
/// <param name="Center">The centre, in the mask's units.</param>
/// <param name="RadiusX">The horizontal radius, in the mask's units.</param>
/// <param name="RadiusY">The vertical radius, in the mask's units.</param>
public sealed record RadialGradientMask(Vector2 Center, float RadiusX, float RadiusY) : Mask
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

    protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
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
        ParallelRows.For(context.Operation, area.Top, area.Bottom, area.Width, (first, last, band) =>
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
                    row[x - window.Left] = Rgba32.ClampToByte(t * t * (3 - 2 * t) * 255f);
                }
            }
        });
    }

    public override RectangleF? GetCoverageBounds(Size size)
    {
        var (center, rx, ry) = InPixels(MaskRasterizer.ToPixels(Units, size));
        var angle = Rotation * MathF.PI / 180f;
        var cos = MathF.Abs(MathF.Cos(angle));
        var sin = MathF.Abs(MathF.Sin(angle));
        var hx = rx * cos + ry * sin;
        var hy = rx * sin + ry * cos;
        return RectangleF.FromLTRB(center.X - hx - 1, center.Y - hy - 1, center.X + hx + 1, center.Y + hy + 1);
    }

    protected override Mask ScaleGeometry(double scale) => this with
    {
        Center = Center * (float)scale,
        RadiusX = (float)(RadiusX * scale),
        RadiusY = (float)(RadiusY * scale),
    };

    /// <summary>The centre follows the image, the radii scale with it and the ellipse turns with it; it is symmetric, so a mirror only reverses its turn.</summary>
    protected override Mask TransformGeometry(Matrix3x2 matrix)
    {
        var parts = AffineParts.Of(matrix);
        return this with
        {
            Center = Vector2.Transform(Center, matrix),
            RadiusX = RadiusX * parts.Scale,
            RadiusY = RadiusY * parts.Scale,
            Rotation = AffineParts.Normalize(parts.Mirrored ? parts.Angle - Rotation : Rotation + parts.Angle),
        };
    }
}

/// <summary>
/// Selects pixels by brightness: fully covered where the luminance (Rec. 709, in [0,1]) lies between
/// <see cref="Low"/> and <see cref="High"/>, fading over <see cref="Softness"/> beyond them. Recover highlights
/// with <c>new LuminanceRangeMask(0.75f, 1f)</c>, lift shadows with <c>new LuminanceRangeMask(0f, 0.25f)</c>.
/// </summary>
/// <param name="Low">The darkest fully covered luminance.</param>
/// <param name="High">The brightest fully covered luminance.</param>
public sealed record LuminanceRangeMask(float Low, float High) : Mask
{
    public float Low { get; init; } = Math.Clamp(Low, 0f, 1f);
    public float High { get; init; } = High >= Low ? Math.Clamp(High, 0f, 1f) : throw new ArgumentOutOfRangeException(nameof(High), "The range's top must not be below its bottom.");

    /// <summary>How far beyond the range, in luminance, the coverage takes to fade to nothing. Default 0.1.</summary>
    public float Softness
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 0.1f;

    public override string Kind => "Luminance range";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
    {
        var source = context.RequireSource("luminance range");
        // Luminance is a byte, so coverage is a 256 entry table.
        var table = new byte[256];
        for (var v = 0; v < 256; v++)
        {
            var l = v / 255f;
            var distance = l < Low ? Low - l : l > High ? l - High : 0f;
            table[v] = distance <= 0 ? (byte)255 : Softness <= 0 ? (byte)0 : Rgba32.ClampToByte(Math.Max(0, 1 - distance / Softness) * 255f);
        }
        var window = context.Window;
        ParallelRows.For(context.Operation, window.Top, window.Bottom, window.Width / 2, (first, last, band) =>
        {
            for (var y = first; y < last; y++)
            {
                if (((y - first) & 63) == 0) band.ThrowIfCancellationRequested();
                var pixels = source.GetRow(y).Slice(window.Left, window.Width);
                var row = target.GetRow(y - window.Top);
                for (var x = 0; x < row.Length; x++) row[x] = table[MaskBuffer.Luma(pixels[x])];
            }
        });
    }
}

/// <summary>
/// Selects pixels close in colour to <see cref="Color"/>: fully covered within <see cref="Tolerance"/>, fading over
/// <see cref="Softness"/>. Distances use the "redmean" weighting, which tracks perceived difference far better than
/// plain RGB distance at almost no cost; 1 is the distance between black and white.
/// </summary>
/// <param name="Color">The colour to select.</param>
public sealed record ColorRangeMask(Rgba32 Color) : Mask
{
    /// <summary>The largest distance that is still fully covered. Default 0.15.</summary>
    public float Tolerance
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 0.15f;

    /// <summary>How much further the coverage takes to fade to nothing. Default 0.1.</summary>
    public float Softness
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 0.1f;

    public override string Kind => "Colour range";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
    {
        var source = context.RequireSource("colour range");
        var color = Color;
        var tolerance = Tolerance;
        var softness = Softness;
        var window = context.Window;
        ParallelRows.For(context.Operation, window.Top, window.Bottom, window.Width * 2L, (first, last, band) =>
        {
            for (var y = first; y < last; y++)
            {
                if (((y - first) & 63) == 0) band.ThrowIfCancellationRequested();
                var pixels = source.GetRow(y).Slice(window.Left, window.Width);
                var row = target.GetRow(y - window.Top);
                for (var x = 0; x < row.Length; x++)
                {
                    var d = Distance(pixels[x], color);
                    row[x] = d <= tolerance ? (byte)255 : softness <= 0 ? (byte)0 : Rgba32.ClampToByte(Math.Max(0, 1 - (d - tolerance) / softness) * 255f);
                }
            }
        });
    }

    /// <summary>The redmean distance between two colours, scaled so black to white is 1.</summary>
    internal static float Distance(Rgba32 a, Rgba32 b)
    {
        const float inv = 1f / 255f;
        var mean = (a.R + b.R) * 0.5f * inv;
        var dr = (a.R - b.R) * inv;
        var dg = (a.G - b.G) * inv;
        var db = (a.B - b.B) * inv;
        return MathF.Sqrt((2 + mean) * dr * dr + 4 * dg * dg + (3 - mean) * db * db) / 3f;
    }
}

/// <summary>Which channel of an image a <see cref="BitmapMask"/> reads.</summary>
public enum MaskChannel
{
    /// <summary>The alpha channel: opaque is covered. A cut-out from a background removal model.</summary>
    Alpha = 0,
    /// <summary>The luminance, weighted by alpha: white is covered. A greyscale matte or a painted mask.</summary>
    Luminance,
}

/// <summary>
/// A mask taken from an image, stretched over the whole image it is applied to: the output of a segmentation or
/// background removal model, a matte painted elsewhere, or a mask the editor captured. Its resolution need not match.
/// </summary>
/// <param name="Image">The mask's pixels. Not owned by the mask: the caller keeps it alive for as long as the mask is used.</param>
public sealed record BitmapMask(ImageBuffer Image) : Mask
{
    public ImageBuffer Image { get; init; } = Image ?? throw new ArgumentNullException(nameof(Image));

    /// <summary>Which channel holds the coverage.</summary>
    public MaskChannel Channel { get; init; }

    public override string Kind => "Bitmap";

    private protected override bool DrawsWindows => true;

    protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
    {
        if (Image.IsDisposed) throw new ObjectDisposedException(nameof(Image), "The image this mask reads has been disposed.");
        using var values = Channel == MaskChannel.Luminance
            ? MaskBuffer.FromLuminance(Image, context.Operation.Allocator)
            : MaskBuffer.FromAlpha(Image, context.Operation.Allocator);
        if (values.Size == context.Size)
        {
            CopyWindow(values, target, context.Window);
            return;
        }
        using var resized = values.Resize(context.Size.Width, context.Size.Height, context.Operation.Allocator);
        CopyWindow(resized, target, context.Window);
    }

    private static void CopyWindow(MaskBuffer whole, MaskBuffer target, Rectangle window)
    {
        for (var y = 0; y < window.Height; y++) whole.GetRow(window.Top + y).Slice(window.Left, window.Width).CopyTo(target.GetRow(y));
    }
}

/// <summary>Rasterises outlines into masks.</summary>
internal static class MaskRasterizer
{
    /// <summary>
    /// Adds a filled outline's coverage, scaled by <paramref name="opacity"/>, to the mask holding
    /// <paramref name="window"/> of the image: each value becomes the larger of itself and the coverage, or with
    /// <paramref name="erase"/> is reduced by it. The outline is in pixels of the image, and a pixel's coverage does not
    /// depend on the window it is drawn in.
    /// </summary>
    public static void Fill(MaskBuffer target, Rectangle window, VectorPath path, FillRule rule, float opacity = 1f, bool erase = false)
    {
        var scale = Math.Clamp(opacity, 0f, 1f) * 255f;
        if (scale <= 0) return;
        Rasterizer.Rasterize(path, window, rule, (y, x0, coverage) =>
        {
            var row = target.GetRow(y - window.Top).Slice(x0 - window.Left, coverage.Length);
            for (var i = 0; i < coverage.Length; i++)
            {
                var c = coverage[i];
                if (c <= 0.0005f) continue;
                var v = Rgba32.ClampToByte(MathF.Min(c, 1f) * scale);
                row[i] = erase ? (byte)((row[i] * (255 - v) + 127) / 255) : Math.Max(row[i], v);
            }
        });
    }

    public static Matrix3x2 ToPixels(MaskUnits units, Size size)
        => units == MaskUnits.Relative ? Matrix3x2.CreateScale(size.Width, size.Height) : Matrix3x2.Identity;

    public static RectangleF Transform(RectangleF rect, Matrix3x2 m) => AffineParts.MapRectangle(rect, m);

    public static RectangleF BoundsOf(IEnumerable<Vector2> points)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in points)
        {
            minX = MathF.Min(minX, p.X); minY = MathF.Min(minY, p.Y);
            maxX = MathF.Max(maxX, p.X); maxY = MathF.Max(maxY, p.Y);
        }
        return minX > maxX ? RectangleF.Empty : RectangleF.FromLTRB(minX, minY, maxX, maxY);
    }
}
