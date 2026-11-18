using System.Buffers;
using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// One stroke of a <see cref="BmagePaintLayer"/>'s brush: the path the pointer took, the brush it was painted with, and
/// the pen pressure along it when there was any.
/// </summary>
/// <remarks>
/// The brush is round. <see cref="Hardness"/> shapes its edge: 1 is crisp, anti-aliased like a pen; lower values fade
/// from that fraction of the radius out to the rim, like an airbrush. Within one stroke the paint does not build up where
/// the brush passes twice, so a stroke has one even opacity, <see cref="Opacity"/>; strokes laid over one another do
/// build up, as in a painting app.
/// </remarks>
public sealed record BmagePaintStroke
{
    private readonly Vector2[] _points = [];
    private readonly float[]? _pressures;

    /// <param name="points">The path, in pixels of the image the layer is composited onto. At least one point.</param>
    /// <param name="size">The brush diameter in pixels.</param>
    /// <param name="color">The paint colour; its alpha multiplies <see cref="Opacity"/>.</param>
    public BmagePaintStroke(IReadOnlyList<Vector2> points, float size, BmageRgba32 color)
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
    public BmageRgba32 Color { get; init; }

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
        var bounds = BmageMaskRasterizer.BoundsOf(BmageFreehandAnnotation.SmoothingHull(_points));
        var reach = Size / 2 + 2;
        bounds.Inflate(reach, reach);
        return bounds;
    }

    internal BmagePaintStroke Scale(double scale) => this with { Points = [.. _points.Select(p => p * (float)scale)], Size = (float)Math.Max(0.01, Size * scale) };

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
            var builder = new BmagePathBuilder();
            BmageFreehandAnnotation.AppendSmoothed(builder, _points, closed: false);
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
