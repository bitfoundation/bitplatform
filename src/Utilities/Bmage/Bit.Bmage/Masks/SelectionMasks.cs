using System.Buffers;
using System.Drawing;
using System.Numerics;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;

namespace Bit.Bmage.Masks;

/// <summary>
/// The region of similar colour around one or more points: the magic wand. From each seed it spreads to neighbouring
/// pixels whose colour is within <see cref="Tolerance"/> of the seed's, so it selects a sky, a wall or a plain
/// background up to where the colour changes.
/// </summary>
/// <remarks>
/// <para>
/// Colours are compared premultiplied by alpha, so every fully transparent pixel matches every other whatever colour it
/// stores, and seeding on transparency selects the transparent area. The distance is the redmean distance
/// <see cref="ColorRangeMask"/> uses, with black to white being 1, combined with the difference in alpha.
/// </para>
/// <para>
/// Pixels just beyond the tolerance, within <see cref="Softness"/> more, that touch the region are partly covered, so its
/// edge is anti-aliased the way the image's own edges are. They are not spread through. With <see cref="Contiguous"/>
/// off, every pixel within the tolerance of a seed's colour is selected wherever it lies: select by colour.
/// </para>
/// <para>
/// The seeds are positions, in <see cref="Mask.Units"/>; the colours are read from the image the mask is applied to, so
/// on a proxy preview they come from the proxy. A seed outside the image is moved to its nearest pixel.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Make a product photo's white background transparent, flooding in from the four corners.
/// pipeline.ApplyMask(new FloodMask([new(0, 0), new(1, 0), new(0, 1), new(1, 1)])
/// {
///     Units = MaskUnits.Relative,
///     Tolerance = 0.08f,
///     Inverted = true,
/// });
/// </code>
/// </example>
public sealed record FloodMask : Mask
{
    private readonly Vector2[] _seeds = [];

    /// <param name="seeds">Where to start, at least one point.</param>
    public FloodMask(IReadOnlyList<Vector2> seeds) => Seeds = seeds;

    /// <summary>The points the region spreads from, in <see cref="Mask.Units"/>.</summary>
    public IReadOnlyList<Vector2> Seeds
    {
        get => _seeds;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Count == 0) throw new ArgumentException("A flood needs at least one seed.", nameof(value));
            if (value.Any(p => !float.IsFinite(p.X) || !float.IsFinite(p.Y))) throw new ArgumentException("Seeds must be finite points.", nameof(value));
            _seeds = [.. value];
        }
    }

    /// <summary>The largest colour distance from a seed's colour that is still selected, in [0,1]. Default 0.12.</summary>
    public float Tolerance
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 0.12f;

    /// <summary>How much further the edge fades out, in [0,1] of colour distance. Default 0.04; 0 gives a hard edge.</summary>
    public float Softness
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 0.04f;

    /// <summary>True to select only pixels connected to a seed; false to select matching colours anywhere.</summary>
    public bool Contiguous { get; init; } = true;

    public override string Kind => "Flood";

    protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
    {
        var source = context.RequireSource("flood");
        int width = source.Width, height = source.Height;
        var seeds = new (int X, int Y, Vector4 Color)[_seeds.Length];
        for (var i = 0; i < seeds.Length; i++)
        {
            var p = Vector2.Transform(_seeds[i], context.ToPixels);
            var x = Math.Clamp((int)MathF.Floor(p.X), 0, width - 1);
            var y = Math.Clamp((int)MathF.Floor(p.Y), 0, height - 1);
            seeds[i] = (x, y, Premultiplied(source[x, y]));
        }
        var tolerance = Tolerance;
        var softness = Softness;
        if (!Contiguous)
        {
            ParallelRows.For(context.Operation, 0, height, width * 2L * seeds.Length, (first, last, band) =>
            {
                for (var y = first; y < last; y++)
                {
                    if (((y - first) & 31) == 0) band.ThrowIfCancellationRequested();
                    var pixels = source.GetRow(y);
                    var row = target.GetRow(y);
                    for (var x = 0; x < width; x++)
                    {
                        var p = Premultiplied(pixels[x]);
                        var best = 0;
                        foreach (var seed in seeds) best = Math.Max(best, Coverage(Distance(p, seed.Color), tolerance, softness));
                        row[x] = (byte)best;
                    }
                }
            });
            return;
        }

        foreach (var seed in seeds) Fill(source, target, seed.X, seed.Y, seed.Color, tolerance, context.Operation);
        if (softness > 0) SoftenEdge(source, target, seeds, tolerance, softness, context.Operation);
    }

    /// <summary>A scanline flood from one seed, marking every connected pixel within the tolerance with 255.</summary>
    private static void Fill(ImageBuffer source, MaskBuffer target, int x, int y, Vector4 color, float tolerance, OperationContext context)
    {
        if (target.GetRow(y)[x] == 255) return;
        int width = source.Width, height = source.Height;
        bool Matches(int px, int py) => Distance(Premultiplied(source[px, py]), color) <= tolerance;
        if (!Matches(x, y)) return;
        var spans = new Stack<(int X, int Y)>();
        spans.Push((x, y));
        var visited = 0;
        while (spans.TryPop(out var start))
        {
            if ((++visited & 1023) == 0) context.ThrowIfCancellationRequested();
            var row = target.GetRow(start.Y);
            if (row[start.X] == 255) continue;
            var left = start.X;
            while (left > 0 && row[left - 1] != 255 && Matches(left - 1, start.Y)) left--;
            var right = start.X;
            while (right < width - 1 && row[right + 1] != 255 && Matches(right + 1, start.Y)) right++;
            row.Slice(left, right - left + 1).Fill(255);
            // Each run of matching, unfilled pixels touching the span above and below starts a span of its own.
            foreach (var ny in (ReadOnlySpan<int>)[start.Y - 1, start.Y + 1])
            {
                if ((uint)ny >= (uint)height) continue;
                var next = target.GetRow(ny);
                var inRun = false;
                for (var nx = left; nx <= right; nx++)
                {
                    var open = next[nx] != 255 && Matches(nx, ny);
                    if (open && !inRun) spans.Push((nx, ny));
                    inRun = open;
                }
            }
        }
    }

    /// <summary>Partly covers the pixels beside the region that are just beyond the tolerance, for an anti-aliased edge.</summary>
    private static void SoftenEdge(ImageBuffer source, MaskBuffer target, (int X, int Y, Vector4 Color)[] seeds, float tolerance, float softness, OperationContext context)
    {
        int width = source.Width, height = source.Height;
        for (var y = 0; y < height; y++)
        {
            if ((y & 31) == 0) context.ThrowIfCancellationRequested();
            var row = target.GetRow(y);
            var above = y > 0 ? target.GetRow(y - 1) : default;
            var below = y < height - 1 ? target.GetRow(y + 1) : default;
            var pixels = source.GetRow(y);
            for (var x = 0; x < width; x++)
            {
                if (row[x] != 0) continue;
                var touches = (x > 0 && row[x - 1] == 255) || (x < width - 1 && row[x + 1] == 255)
                    || (!above.IsEmpty && above[x] == 255) || (!below.IsEmpty && below[x] == 255);
                if (!touches) continue;
                var p = Premultiplied(pixels[x]);
                var best = 0;
                foreach (var seed in seeds) best = Math.Max(best, Coverage(Distance(p, seed.Color), tolerance, softness));
                // Only the fade beyond the tolerance: a pixel within it that the flood did not reach belongs to another region.
                row[x] = (byte)Math.Min(best, 254);
            }
        }
    }

    private static int Coverage(float distance, float tolerance, float softness)
    {
        if (distance <= tolerance) return 255;
        if (softness <= 0) return 0;
        return (int)MathF.Round(Math.Max(0, 1 - (distance - tolerance) / softness) * 255);
    }

    private static Vector4 Premultiplied(Rgba32 p)
    {
        var a = p.A / 255f;
        return new Vector4(p.R / 255f * a, p.G / 255f * a, p.B / 255f * a, a);
    }

    /// <summary>The redmean distance between premultiplied colours, with black to white being 1, or the alpha difference if larger.</summary>
    private static float Distance(Vector4 a, Vector4 b)
    {
        var mean = (a.X + b.X) * 0.5f;
        var d = a - b;
        var color = MathF.Sqrt((2 + mean) * d.X * d.X + 4 * d.Y * d.Y + (3 - mean) * d.Z * d.Z) / 3f;
        return MathF.Max(color, MathF.Abs(d.W));
    }

    protected override Mask ScaleGeometry(double scale) => this with { Seeds = [.. _seeds.Select(p => p * (float)scale)] };

    protected override Mask TransformGeometry(Matrix3x2 matrix) => this with { Seeds = [.. _seeds.Select(p => Vector2.Transform(p, matrix))] };
}

/// <summary>How a <see cref="MorphedMask"/> changes the mask it wraps.</summary>
public enum MaskMorphology
{
    /// <summary>Grows the covered area by the distance in every direction.</summary>
    Expand = 0,
    /// <summary>Shrinks the covered area by the distance from every edge.</summary>
    Contract,
    /// <summary>A band the distance wide, centred on the mask's edge: an outline of the selection.</summary>
    Border,
}

/// <summary>
/// Another mask grown, shrunk or turned into a band along its edge: the expand, contract and border commands of a
/// selection. A selection that misses a subject's outermost pixels, a cut-out with a halo to trim, an outline to stroke.
/// </summary>
/// <remarks>
/// <para>
/// Distances are exact Euclidean distances from the source mask's edge, where its coverage crosses half, so corners
/// round off evenly. The new edge is anti-aliased, and pixels the source already covered partly keep at least that
/// coverage when expanding, and at most it when contracting. Beyond the image nothing is covered and nothing is
/// uncovered: a selection along the image's edge does not shrink away from it.
/// </para>
/// <para>
/// The source is drawn for the whole image, its own feather included; the morphed mask's <see cref="Mask.Feather"/>
/// softens the result afterwards. Distances are in pixels of the image the mask applies to, and rescale for a proxy.
/// </para>
/// </remarks>
public sealed record MorphedMask : Mask
{
    /// <summary>The largest distance, in pixels.</summary>
    public const float MaxDistance = 250;

    /// <param name="source">The mask to change.</param>
    /// <param name="morphology">Expand, contract or border.</param>
    /// <param name="distance">How far, in pixels: the growth, the shrinkage, or the width of the border.</param>
    public MorphedMask(Mask source, MaskMorphology morphology, float distance)
    {
        Source = source;
        Morphology = morphology;
        Distance = distance;
    }

    /// <summary>The mask changed.</summary>
    public Mask Source
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(Source));
    }

    public MaskMorphology Morphology { get; init; }

    /// <summary>The distance in pixels, up to 250.</summary>
    public float Distance
    {
        get;
        init => field = float.IsFinite(value) && value >= 0 && value <= MaxDistance
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Distance), value, $"The distance must be between 0 and {MaxDistance} pixels.");
    }

    public override string Kind => "Morphed";

    protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
    {
        var operation = context.Operation;
        using var source = Source.RenderWindow(context.Size, new Rectangle(Point.Empty, context.Size), context.Source, operation);
        var distance = Distance;
        if (distance < 0.01f)
        {
            if (Morphology != MaskMorphology.Border) source.Values.CopyTo(target.Values);
            return;
        }
        switch (Morphology)
        {
            case MaskMorphology.Expand:
                Grow(source, target, distance, operation);
                break;
            case MaskMorphology.Contract:
                Shrink(source, target, distance, operation);
                break;
            default:
            {
                // Grown by half the width and shrunk by half of it, the band between the two.
                Grow(source, target, distance / 2, operation);
                using var inner = MaskBuffer.Create(source.Width, source.Height, 0, operation.Allocator, operation.Limits);
                Shrink(source, inner, distance / 2, operation);
                for (var y = 0; y < target.Height; y++)
                {
                    var row = target.GetRow(y);
                    var cut = inner.GetRow(y);
                    for (var x = 0; x < row.Length; x++) row[x] = (byte)Math.Min(row[x], 255 - cut[x]);
                }
                break;
            }
        }
    }

    /// <summary>The source grown by <paramref name="distance"/>, keeping whatever it covered already.</summary>
    private static void Grow(MaskBuffer source, MaskBuffer target, float distance, OperationContext context)
        => Morph(source, target, distance, inside: v => v >= 128, (original, reached) => (byte)Math.Max(original, reached), context);

    /// <summary>The source shrunk by <paramref name="distance"/>: its uncovered part grown, keeping at most what it covered.</summary>
    private static void Shrink(MaskBuffer source, MaskBuffer target, float distance, OperationContext context)
        => Morph(source, target, distance, inside: v => v < 128, (original, reached) => (byte)Math.Min(original, 255 - reached), context);

    /// <summary>
    /// For every pixel, how far the distance reaches it from the set <paramref name="inside"/> picks, anti-aliased to a
    /// byte, combined with the source by <paramref name="combine"/>. An exact Euclidean distance transform: each column's
    /// vertical distances first, kept in a byte a pixel, then each row's lower envelope of parabolas.
    /// </summary>
    private static void Morph(MaskBuffer source, MaskBuffer target, float distance, Func<byte, bool> inside, Func<byte, byte, byte> combine, OperationContext context)
    {
        int width = source.Width, height = source.Height;
        // The farthest a pixel can be and still be reached, with the pixel of anti-aliasing beyond it; no further is needed.
        var cap = Math.Min(254, (int)MathF.Ceiling(distance) + 2);
        using var vertical = MaskBuffer.Create(width, height, 0, context.Allocator, context.Limits);
        // Downwards then upwards: each pixel's distance to the nearest inside pixel of its column, capped.
        for (var y = 0; y < height; y++)
        {
            if ((y & 63) == 0) context.ThrowIfCancellationRequested();
            var row = source.GetRow(y);
            var g = vertical.GetRow(y);
            var above = y > 0 ? vertical.GetRow(y - 1) : default;
            for (var x = 0; x < width; x++)
                g[x] = inside(row[x]) ? (byte)0 : above.IsEmpty ? (byte)255 : (byte)Math.Min(255, above[x] + 1);
        }
        for (var y = height - 2; y >= 0; y--)
        {
            if ((y & 63) == 0) context.ThrowIfCancellationRequested();
            var g = vertical.GetRow(y);
            var below = vertical.GetRow(y + 1);
            for (var x = 0; x < width; x++) g[x] = (byte)Math.Min(g[x], below[x] + 1);
        }

        ParallelRows.For(context, 0, height, width * 4L, (first, last, band) =>
        {
            var pool = ArrayPool<float>.Shared;
            var f = pool.Rent(width);
            var d = pool.Rent(width);
            var z = pool.Rent(width + 1);
            var v = ArrayPool<int>.Shared.Rent(width);
            try
            {
                for (var y = first; y < last; y++)
                {
                    if (((y - first) & 31) == 0) band.ThrowIfCancellationRequested();
                    var g = vertical.GetRow(y);
                    for (var x = 0; x < width; x++) f[x] = g[x] > cap ? float.PositiveInfinity : g[x] * (float)g[x];
                    LowerEnvelope(f, d, v, z, width);
                    var original = source.GetRow(y);
                    var output = target.GetRow(y);
                    for (var x = 0; x < width; x++)
                    {
                        // A pixel whose centre is d from the nearest inside centre is d - 0.5 from the edge between them;
                        // moving the edge out by the distance covers it by how far past it the edge now reaches.
                        var reached = float.IsPositiveInfinity(d[x]) ? 0f : Math.Clamp(distance - MathF.Sqrt(d[x]) + 1f, 0f, 1f);
                        output[x] = combine(original[x], (byte)MathF.Round(reached * 255));
                    }
                }
            }
            finally
            {
                pool.Return(f);
                pool.Return(d);
                pool.Return(z);
                ArrayPool<int>.Shared.Return(v);
            }
        });
    }

    /// <summary>
    /// The squared distance transform of one row (Felzenszwalb and Huttenlocher): <c>d[q] = min over p of (q - p)² + f[p]</c>,
    /// with infinite values of <paramref name="f"/> standing for "not inside".
    /// </summary>
    private static void LowerEnvelope(float[] f, float[] d, int[] v, float[] z, int n)
    {
        var k = -1;
        for (var q = 0; q < n; q++)
        {
            if (float.IsPositiveInfinity(f[q])) continue;
            var s = float.NegativeInfinity;
            while (k >= 0)
            {
                var p = v[k];
                s = (f[q] + q * (float)q - (f[p] + p * (float)p)) / (2f * (q - p));
                if (s > z[k]) break;
                k--;
            }
            k++;
            v[k] = q;
            z[k] = k == 0 ? float.NegativeInfinity : s;
        }
        if (k < 0)
        {
            d.AsSpan(0, n).Fill(float.PositiveInfinity);
            return;
        }
        var j = 0;
        for (var q = 0; q < n; q++)
        {
            while (j < k && z[j + 1] < q) j++;
            var p = v[j];
            d[q] = (q - p) * (float)(q - p) + f[p];
        }
    }

    protected override Mask ScaleGeometry(double scale) => this;

    /// <summary>Scales the source by its own units, the distance, and the morphed mask's own feather.</summary>
    public override Mask ForScale(double scale) => scale == 1 ? this : this with
    {
        Source = Source.ForScale(scale),
        Distance = (float)Math.Min(MaxDistance, Distance * scale),
        Feather = (float)(Feather * scale),
    };

    /// <summary>Transforms the source by its own units, and scales the distance and the morphed mask's own feather.</summary>
    public override Mask Transform(Matrix3x2 matrix)
    {
        if (matrix.IsIdentity) return this;
        var scale = AffineParts.Of(matrix).Scale;
        return this with { Source = Source.Transform(matrix), Distance = Math.Min(MaxDistance, Distance * scale), Feather = Feather * scale };
    }
}
