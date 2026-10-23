using System.Buffers;
using System.Drawing;
using System.Numerics;
using Bit.Bmage.Annotations;
using Bit.Bmage.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Masks;
using Bit.Bmage.Operations;

namespace Bit.Bmage.Layers;

/// <summary>
/// One stroke of a <see cref="PaintLayer"/>'s brush: the path the pointer took, the brush it was painted with, and
/// the pen pressure along it when there was any.
/// </summary>
/// <remarks>
/// The brush is round. <see cref="Hardness"/> shapes its edge: 1 is crisp, anti-aliased like a pen; lower values fade
/// from that fraction of the radius out to the rim, like an airbrush. Within one stroke the paint does not build up where
/// the brush passes twice, so a stroke has one even opacity, <see cref="Opacity"/>; strokes laid over one another do
/// build up, as in a painting app.
/// </remarks>
public sealed record PaintStroke
{
    private readonly Vector2[] _points = [];
    private readonly float[]? _pressures;

    /// <param name="points">The path, in pixels of the image the layer is composited onto. At least one point.</param>
    /// <param name="size">The brush diameter in pixels.</param>
    /// <param name="color">The paint colour; its alpha multiplies <see cref="Opacity"/>.</param>
    public PaintStroke(IReadOnlyList<Vector2> points, float size, Rgba32 color)
    {
        Points = points;
        Size = size;
        Color = color;
    }

    /// <summary>The path the brush followed. Painted smoothed, as freehand annotations are drawn.</summary>
    public IReadOnlyList<Vector2> Points
    {
        get => _points;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Count == 0) throw new ArgumentException("A stroke needs at least one point.", nameof(value));
            if (value.Any(p => !float.IsFinite(p.X) || !float.IsFinite(p.Y))) throw new ArgumentException("Stroke points must be finite.", nameof(value));
            _points = [.. value];
        }
    }

    /// <summary>
    /// Pen pressure in [0,1] at each point, or null for a mouse or a finger, which paint at full pressure. When given, it
    /// must have one value per point.
    /// </summary>
    public IReadOnlyList<float>? Pressures
    {
        get => _pressures;
        init
        {
            if (value is not null && value.Count != _points.Length)
                throw new ArgumentException($"A stroke of {_points.Length} points needs as many pressures, not {value.Count}.", nameof(value));
            _pressures = value is null ? null : [.. value.Select(p => Math.Clamp(float.IsNaN(p) ? 1 : p, 0f, 1f))];
        }
    }

    /// <summary>The brush diameter in pixels, at full pressure.</summary>
    public float Size
    {
        get;
        init => field = value > 0 && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Size), "A brush needs a positive size.");
    }

    /// <summary>The paint colour. Ignored by an erasing stroke.</summary>
    public Rgba32 Color { get; init; }

    /// <summary>The stroke's opacity in [0,1].</summary>
    public float Opacity
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 1f;

    /// <summary>How crisp the brush's edge is, in [0,1]. 1 is a hard, anti-aliased edge; 0 fades from the centre out.</summary>
    public float Hardness
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 0.8f;

    /// <summary>Removes the layer's paint where it passes instead of adding colour: the eraser.</summary>
    public bool Erase { get; init; }

    /// <summary>Whether pen pressure narrows the brush. On by default.</summary>
    public bool PressureAffectsSize { get; init; } = true;

    /// <summary>Whether pen pressure lightens the paint. Off by default.</summary>
    public bool PressureAffectsOpacity { get; init; }

    /// <summary>The area the stroke can paint, in pixels.</summary>
    public RectangleF GetBounds()
    {
        // The smoothing curves bulge past the points at sharp turns, but a cubic never leaves the hull of its control
        // points, so those are included too.
        var bounds = MaskRasterizer.BoundsOf(SmoothingHull());
        var reach = Size / 2 + 2;
        bounds.Inflate(reach, reach);
        return bounds;
    }

    /// <summary>The points and the control points <see cref="FreehandAnnotation.AppendSmoothed"/> draws its curves through.</summary>
    private IEnumerable<Vector2> SmoothingHull()
    {
        foreach (var p in _points) yield return p;
        if (_points.Length < 3) yield break;
        for (var i = 0; i < _points.Length - 1; i++)
        {
            var p0 = _points[Math.Max(i - 1, 0)];
            var p1 = _points[i];
            var p2 = _points[i + 1];
            var p3 = _points[Math.Min(i + 2, _points.Length - 1)];
            yield return p1 + (p2 - p0) / 6f;
            yield return p2 - (p3 - p1) / 6f;
        }
    }

    internal PaintStroke Scale(double scale) => this with { Points = [.. _points.Select(p => p * (float)scale)], Size = (float)Math.Max(0.01, Size * scale) };

    /// <summary>
    /// Paints the stroke's coverage, in [0,1], into <paramref name="coverage"/>, which holds <paramref name="area"/> of
    /// the image row by row, keeping the larger of what is there and what the brush lays down. Nothing is written outside
    /// <paramref name="clip"/>, a part of the area.
    /// </summary>
    internal void RenderCoverage(Span<float> coverage, Rectangle area, Rectangle clip, CancellationToken cancellationToken)
    {
        var points = SmoothedPath(out var pressures);
        var radius = Size / 2;
        // Dabs close enough together that the stroke's edge shows no scallops, even for a soft brush.
        var spacing = Math.Max(0.5f, Size * (Hardness >= 0.999f ? 0.1f : 0.15f));
        var previous = points[0];
        StampDab(coverage, area, clip, previous, Radius(pressures[0]), Strength(pressures[0]));
        var carried = 0f;
        for (var i = 1; i < points.Length; i++)
        {
            if ((i & 255) == 0) cancellationToken.ThrowIfCancellationRequested();
            var next = points[i];
            var length = Vector2.Distance(previous, next);
            var travelled = spacing - carried;
            while (travelled <= length)
            {
                var t = length > 0 ? travelled / length : 1;
                var pressure = pressures[i - 1] + (pressures[i] - pressures[i - 1]) * t;
                StampDab(coverage, area, clip, Vector2.Lerp(previous, next, t), Radius(pressure), Strength(pressure));
                travelled += spacing;
            }
            carried = length - (travelled - spacing);
            previous = next;
        }
        // The stroke always ends where the pointer did.
        StampDab(coverage, area, clip, points[^1], Radius(pressures[^1]), Strength(pressures[^1]));

        float Radius(float pressure) => PressureAffectsSize ? Math.Max(0.5f, radius * pressure) : radius;
        float Strength(float pressure) => PressureAffectsOpacity ? pressure : 1f;
    }

    /// <summary>
    /// The path drawn through the points, flattened, with the pressure at each of its points interpolated by distance
    /// along the original points.
    /// </summary>
    private Vector2[] SmoothedPath(out float[] pressures)
    {
        Vector2[] path;
        if (_points.Length < 3)
        {
            path = _points;
        }
        else
        {
            var builder = new PathBuilder();
            FreehandAnnotation.AppendSmoothed(builder, _points, closed: false);
            path = [.. builder.Build().Contours.SelectMany(c => c)];
            if (path.Length == 0) path = _points;
        }
        pressures = new float[path.Length];
        if (_pressures is not { } given || given.Length != _points.Length)
        {
            pressures.AsSpan().Fill(1f);
            return path;
        }
        // Distance along the original points, and along the smoothed path, each as a fraction of its whole.
        var along = Cumulative(_points);
        var smoothed = Cumulative(path);
        var j = 0;
        for (var i = 0; i < path.Length; i++)
        {
            var s = smoothed[i];
            while (j < along.Length - 2 && along[j + 1] < s) j++;
            var span = along[j + 1] - along[j];
            var t = span > 0 ? Math.Clamp((s - along[j]) / span, 0, 1) : 0;
            pressures[i] = given[j] + (given[Math.Min(j + 1, given.Length - 1)] - given[j]) * t;
        }
        return path;

        static float[] Cumulative(Vector2[] points)
        {
            var lengths = new float[points.Length];
            for (var i = 1; i < points.Length; i++) lengths[i] = lengths[i - 1] + Vector2.Distance(points[i - 1], points[i]);
            var total = lengths[^1];
            if (total > 0) for (var i = 0; i < lengths.Length; i++) lengths[i] /= total;
            return lengths;
        }
    }

    /// <summary>One round dab of the brush, kept by its larger value where it overlaps paint already laid in this stroke.</summary>
    private void StampDab(Span<float> coverage, Rectangle area, Rectangle clip, Vector2 centre, float radius, float strength)
    {
        var x0 = Math.Max(clip.Left, (int)MathF.Floor(centre.X - radius - 1));
        var y0 = Math.Max(clip.Top, (int)MathF.Floor(centre.Y - radius - 1));
        var x1 = Math.Min(clip.Right, (int)MathF.Ceiling(centre.X + radius + 1));
        var y1 = Math.Min(clip.Bottom, (int)MathF.Ceiling(centre.Y + radius + 1));
        if (x0 >= x1 || y0 >= y1 || strength <= 0) return;
        var hard = Hardness >= 0.999f;
        var inner = radius * Hardness;
        // Beyond this distance the dab lays down nothing; within the core it lays down its whole strength, since the rim's
        // anti-aliasing and a soft brush's fade both begin further out. Only the ring between needs a square root.
        var reach = radius + 0.5f;
        var reach2 = reach * reach;
        var core = hard ? radius - 0.5f : Math.Min(radius - 0.5f, inner);
        var core2 = core > 0 ? core * core : -1f;
        for (var y = y0; y < y1; y++)
        {
            var dy = y + 0.5f - centre.Y;
            var dy2 = dy * dy;
            if (dy2 >= reach2) continue;
            // The row's part of the dab, from the circle's equation, with half a pixel to spare on each side.
            var half = MathF.Sqrt(reach2 - dy2);
            var xs = Math.Max(x0, (int)MathF.Floor(centre.X - half - 0.5f));
            var xe = Math.Min(x1, (int)MathF.Ceiling(centre.X + half + 0.5f));
            var row = coverage.Slice((y - area.Top) * area.Width, area.Width);
            for (var x = xs; x < xe; x++)
            {
                var dx = x + 0.5f - centre.X;
                var d2 = dx * dx + dy2;
                float value;
                if (d2 <= core2)
                {
                    value = strength;
                }
                else
                {
                    var d = MathF.Sqrt(d2);
                    // The rim is anti-aliased over a pixel either way; a soft brush also fades from its inner radius out.
                    value = Math.Clamp(radius + 0.5f - d, 0f, 1f);
                    if (!hard && d > inner)
                    {
                        var t = Math.Clamp((d - inner) / (radius - inner), 0f, 1f);
                        value *= 1 - t * t * (3 - 2 * t);
                    }
                    value *= strength;
                }
                ref var c = ref row[x - area.Left];
                if (value > c) c = value;
            }
        }
    }
}

/// <summary>
/// A layer painted with a brush, stroke by stroke: freehand colour, retouching by hand, or an eraser taking paint away.
/// The strokes are kept, not their pixels, so painting costs kilobytes of history, previews on a proxy at proxy
/// resolution, and replays at full resolution on export.
/// </summary>
/// <remarks>
/// Strokes are painted in order onto a transparent layer: a painting stroke lays its colour over what is there, an
/// erasing one removes the layer's own paint, never the image below. The layer is then composited with its blend mode,
/// opacity and mask like any other. Coordinates are those of the image the layer is composited onto.
/// </remarks>
public sealed record PaintLayer : Layer
{
    private readonly PaintStroke[] _strokes = [];

    /// <summary>A layer with no paint yet.</summary>
    public PaintLayer()
    {
    }

    /// <param name="strokes">The strokes in the order they were painted.</param>
    public PaintLayer(IReadOnlyList<PaintStroke> strokes) => Strokes = strokes;

    /// <summary>The strokes in the order they were painted.</summary>
    public IReadOnlyList<PaintStroke> Strokes
    {
        get => _strokes;
        init => _strokes = value?.ToArray() ?? throw new ArgumentNullException(nameof(Strokes));
    }

    /// <summary>The layer with one more stroke painted on top.</summary>
    public PaintLayer Add(PaintStroke stroke)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        return this with { Strokes = [.. _strokes, stroke] };
    }

    public override string Kind => "Paint";

    public override bool IsNoOp => base.IsNoOp || !_strokes.Any(s => !s.Erase);

    protected internal override void CompositeCore(ImageBuffer target, OperationContext context)
    {
        var area = PaintedArea(target.Size);
        if (area.IsEmptyArea()) return;
        using var raster = Rasterize(area, context);
        BlendRows(target, area, context, y => raster.GetRow(y - area.Y));
    }

    /// <summary>The pixels of the image the strokes can reach.</summary>
    private Rectangle PaintedArea(Size size)
    {
        var bounds = RectangleF.Empty;
        foreach (var stroke in _strokes)
        {
            if (stroke.Erase) continue;
            bounds = bounds.IsEmpty ? stroke.GetBounds() : RectangleF.Union(bounds, stroke.GetBounds());
        }
        if (bounds.IsEmpty) return Rectangle.Empty;
        var area = Rectangle.FromLTRB((int)MathF.Floor(Math.Max(bounds.Left, -1)), (int)MathF.Floor(Math.Max(bounds.Top, -1)),
            (int)MathF.Ceiling(Math.Min(bounds.Right, size.Width + 1)), (int)MathF.Ceiling(Math.Min(bounds.Bottom, size.Height + 1)));
        area.Intersect(new Rectangle(Point.Empty, size));
        return area;
    }

    /// <summary>The strokes painted in order onto transparency, over <paramref name="area"/>. The caller owns the result.</summary>
    internal ImageBuffer Rasterize(Rectangle area, OperationContext context)
    {
        var raster = ImageBuffer.Create(area.Width, area.Height, context.Allocator, clear: true, ImageLimits.Unrestricted);
        var length = area.Width * area.Height;
        var coverage = ArrayPool<float>.Shared.Rent(length);
        try
        {
            foreach (var stroke in _strokes)
            {
                context.ThrowIfCancellationRequested();
                var reach = Rectangle.Round(stroke.GetBounds());
                reach.Inflate(1, 1);
                reach.Intersect(area);
                if (reach.IsEmptyArea()) continue;
                var cells = coverage.AsSpan(0, length);
                ClearRows(cells, area, reach);
                stroke.RenderCoverage(cells, area, reach, context.CancellationToken);
                Lay(raster, cells, area, reach, stroke);
            }
            return raster;
        }
        catch
        {
            raster.Dispose();
            throw;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(coverage);
        }
    }

    private static void ClearRows(Span<float> coverage, Rectangle area, Rectangle reach)
    {
        for (var y = reach.Top; y < reach.Bottom; y++)
            coverage.Slice((y - area.Top) * area.Width + (reach.Left - area.Left), reach.Width).Clear();
    }

    /// <summary>Lays one stroke's paint onto the layer, or takes the layer's paint away for an eraser.</summary>
    private static void Lay(ImageBuffer raster, ReadOnlySpan<float> coverage, Rectangle area, Rectangle reach, PaintStroke stroke)
    {
        var opacity = stroke.Opacity * (stroke.Erase ? 1f : stroke.Color.A / 255f);
        var color = new Vector3(stroke.Color.R, stroke.Color.G, stroke.Color.B);
        for (var y = reach.Top; y < reach.Bottom; y++)
        {
            var cells = coverage.Slice((y - area.Top) * area.Width, area.Width);
            var row = raster.GetRow(y - area.Top);
            for (var x = reach.Left; x < reach.Right; x++)
            {
                var a = cells[x - area.Left] * opacity;
                if (a <= 0) continue;
                ref var p = ref row[x - area.Left];
                var below = p.A / 255f;
                if (stroke.Erase)
                {
                    p = new Rgba32(p.R, p.G, p.B, Rgba32.ClampToByte(below * (1 - a) * 255f));
                    continue;
                }
                // Source over, in straight alpha.
                var outAlpha = a + below * (1 - a);
                var mixed = (color * a + new Vector3(p.R, p.G, p.B) * below * (1 - a)) / outAlpha;
                p = new Rgba32(Rgba32.ClampToByte(mixed.X), Rgba32.ClampToByte(mixed.Y), Rgba32.ClampToByte(mixed.Z), Rgba32.ClampToByte(outAlpha * 255f));
            }
        }
    }

    protected override Layer ScaleCore(double scale) => this with { Strokes = [.. _strokes.Select(s => s.Scale(scale))] };

    /// <summary>Every point of every stroke is mapped exactly, and the brush sizes scale with the image.</summary>
    protected override Layer TransformCore(Matrix3x2 matrix)
    {
        var scale = AffineParts.Of(matrix).Scale;
        return this with { Strokes = [.. _strokes.Select(s => s with { Points = [.. s.Points.Select(p => Vector2.Transform(p, matrix))], Size = Math.Max(0.01f, s.Size * scale) })] };
    }
}
