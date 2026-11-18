using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// The region of similar colour around one or more points: the magic wand. From each seed it spreads to neighbouring
/// pixels whose colour is within <see cref="Tolerance"/> of the seed's, so it selects a sky, a wall or a plain
/// background up to where the colour changes.
/// </summary>
/// <remarks>
/// <para>
/// Colours are compared premultiplied by alpha, so every fully transparent pixel matches every other whatever colour it
/// stores, and seeding on transparency selects the transparent area. The distance is the redmean distance
/// <see cref="BmageColorRangeMask"/> uses, with black to white being 1, combined with the difference in alpha.
/// </para>
/// <para>
/// Pixels just beyond the tolerance, within <see cref="Softness"/> more, that touch the region are partly covered, so its
/// edge is anti-aliased the way the image's own edges are. They are not spread through. With <see cref="Contiguous"/>
/// off, every pixel within the tolerance of a seed's colour is selected wherever it lies: select by colour.
/// </para>
/// <para>
/// The seeds are positions, in <see cref="BmageMask.Units"/>; the colours are read from the image the mask is applied to, so
/// on a proxy preview they come from the proxy. A seed outside the image is moved to its nearest pixel.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Make a product photo's white background transparent, flooding in from the four corners.
/// pipeline.ApplyMask(new BmageFloodMask([new(0, 0), new(1, 0), new(0, 1), new(1, 1)])
/// {
///     Units = BmageMaskUnits.Relative,
///     Tolerance = 0.08f,
///     Inverted = true,
/// });
/// </code>
/// </example>
public sealed record BmageFloodMask : BmageMask
{
    private readonly Vector2[] _seeds = [];

    /// <param name="seeds">Where to start, at least one point.</param>
    public BmageFloodMask(IReadOnlyList<Vector2> seeds) => Seeds = seeds;

    /// <summary>The points the region spreads from, in <see cref="BmageMask.Units"/>.</summary>
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

    protected internal override void RenderCoverage(BmageMaskBuffer target, BmageMaskRenderContext context)
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
            BmageParallelRows.For(context.Operation, 0, height, width * 2L * seeds.Length, (first, last, band) =>
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
    private static void Fill(BmageImageBuffer source, BmageMaskBuffer target, int x, int y, Vector4 color, float tolerance, BmageOperationContext context)
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
    private static void SoftenEdge(BmageImageBuffer source, BmageMaskBuffer target, (int X, int Y, Vector4 Color)[] seeds, float tolerance, float softness, BmageOperationContext context)
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

    private static Vector4 Premultiplied(BmageRgba32 p)
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

    protected override BmageMask ScaleGeometry(double scale) => this with { Seeds = [.. _seeds.Select(p => p * (float)scale)] };

    protected override BmageMask TransformGeometry(Matrix3x2 matrix) => this with { Seeds = [.. _seeds.Select(p => Vector2.Transform(p, matrix))] };
}
