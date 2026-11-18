using System.Buffers;
using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

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
/// The source is drawn for the whole image, its own feather included; the morphed mask's <see cref="BmageMask.Feather"/>
/// softens the result afterwards. Distances are in pixels of the image the mask applies to, and rescale for a proxy.
/// </para>
/// </remarks>
public sealed record BmageMorphedMask : BmageMask
{
    /// <summary>The largest distance, in pixels.</summary>
    public const float MaxDistance = 250;

    /// <param name="source">The mask to change.</param>
    /// <param name="morphology">Expand, contract or border.</param>
    /// <param name="distance">How far, in pixels: the growth, the shrinkage, or the width of the border.</param>
    public BmageMorphedMask(BmageMask source, BmageMaskMorphology morphology, float distance)
    {
        Source = source;
        Morphology = morphology;
        Distance = distance;
    }

    /// <summary>The mask changed.</summary>
    public BmageMask Source
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(Source));
    }

    public BmageMaskMorphology Morphology { get; init; }

    /// <summary>The distance in pixels, up to 250.</summary>
    public float Distance
    {
        get;
        init => field = float.IsFinite(value) && value >= 0 && value <= MaxDistance
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Distance), value, $"The distance must be between 0 and {MaxDistance} pixels.");
    }

    public override string Kind => "Morphed";

    protected internal override void RenderCoverage(BmageMaskBuffer target, BmageMaskRenderContext context)
    {
        var operation = context.Operation;
        using var source = Source.RenderWindow(context.Size, new Rectangle(Point.Empty, context.Size), context.Source, operation);
        var distance = Distance;
        if (distance < 0.01f)
        {
            if (Morphology != BmageMaskMorphology.Border) source.Values.CopyTo(target.Values);
            return;
        }
        switch (Morphology)
        {
            case BmageMaskMorphology.Expand:
                Grow(source, target, distance, operation);
                break;
            case BmageMaskMorphology.Contract:
                Shrink(source, target, distance, operation);
                break;
            default:
            {
                // Grown by half the width and shrunk by half of it, the band between the two.
                Grow(source, target, distance / 2, operation);
                using var inner = BmageMaskBuffer.Create(source.Width, source.Height, 0, operation.Allocator, operation.Limits);
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
    private static void Grow(BmageMaskBuffer source, BmageMaskBuffer target, float distance, BmageOperationContext context)
        => Morph(source, target, distance, inside: v => v >= 128, (original, reached) => (byte)Math.Max(original, reached), context);

    /// <summary>The source shrunk by <paramref name="distance"/>: its uncovered part grown, keeping at most what it covered.</summary>
    private static void Shrink(BmageMaskBuffer source, BmageMaskBuffer target, float distance, BmageOperationContext context)
        => Morph(source, target, distance, inside: v => v < 128, (original, reached) => (byte)Math.Min(original, 255 - reached), context);

    /// <summary>
    /// For every pixel, how far the distance reaches it from the set <paramref name="inside"/> picks, anti-aliased to a
    /// byte, combined with the source by <paramref name="combine"/>. An exact Euclidean distance transform: each column's
    /// vertical distances first, kept in a byte a pixel, then each row's lower envelope of parabolas.
    /// </summary>
    private static void Morph(BmageMaskBuffer source, BmageMaskBuffer target, float distance, Func<byte, bool> inside, Func<byte, byte, byte> combine, BmageOperationContext context)
    {
        int width = source.Width, height = source.Height;
        // The farthest a pixel can be and still be reached, with the pixel of anti-aliasing beyond it; no further is needed.
        var cap = Math.Min(254, (int)MathF.Ceiling(distance) + 2);
        using var vertical = BmageMaskBuffer.Create(width, height, 0, context.Allocator, context.Limits);
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

        BmageParallelRows.For(context, 0, height, width * 4L, (first, last, band) =>
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

    protected override BmageMask ScaleGeometry(double scale) => this;

    /// <summary>Scales the source by its own units, the distance, and the morphed mask's own feather.</summary>
    public override BmageMask ForScale(double scale) => scale == 1 ? this : this with
    {
        Source = Source.ForScale(scale),
        Distance = (float)Math.Min(MaxDistance, Distance * scale),
        Feather = (float)(Feather * scale),
    };

    /// <summary>Transforms the source by its own units, and scales the distance and the morphed mask's own feather.</summary>
    public override BmageMask Transform(Matrix3x2 matrix)
    {
        if (matrix.IsIdentity) return this;
        var scale = BmageAffineParts.Of(matrix).Scale;
        return this with { Source = Source.Transform(matrix), Distance = Math.Min(MaxDistance, Distance * scale), Feather = Feather * scale };
    }
}
