using System.Buffers;
using System.Drawing;
using System.Numerics;
using Bit.Bmage.Geometry;

namespace Bit.Bmage.Operations.Filters;

/// <summary>Base class for operations that optionally act on a sub-region of the image (e.g. redacting part of a screenshot).</summary>
public abstract class RegionOperation : ImageOperation
{
    protected RegionOperation(Rectangle? region) => Region = region;

    /// <summary>The region affected, in input coordinates, or null for the whole image.</summary>
    public Rectangle? Region { get; }

    /// <summary>Resolves the effective region for an input size.</summary>
    protected Rectangle GetRegion(Size input)
    {
        var full = new Rectangle(Point.Empty, input);
        if (Region is not { } r) return full;
        r.Intersect(full);
        return r;
    }

    /// <summary>Scales the region for a proxy image.</summary>
    protected static Rectangle? ScaleRegion(Rectangle? region, double scale)
    {
        if (region is not { } r || scale == 1) return region;
        var x = (int)Math.Round(r.X * scale);
        var y = (int)Math.Round(r.Y * scale);
        return new Rectangle(x, y, Math.Max(1, (int)Math.Round(r.Right * scale) - x), Math.Max(1, (int)Math.Round(r.Bottom * scale) - y));
    }
}

/// <summary>
/// Gaussian blur approximated with three successive box blurs (O(n) per pixel, independent of radius), operating on
/// premultiplied alpha so transparent pixels do not darken edges.
/// </summary>
public sealed class BlurFilter : RegionOperation
{
    public BlurFilter(float radius, Rectangle? region = null) : base(region)
    {
        if (radius < 0 || !float.IsFinite(radius)) throw new ArgumentOutOfRangeException(nameof(radius));
        Radius = radius;
    }

    /// <summary>Blur radius (standard deviation) in pixels.</summary>
    public float Radius { get; }
    public override string Name => "Blur";
    public override bool IsIdentity => Radius < 0.05f;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var target = GetMutableCopy(source, context);
        var region = GetRegion(source.Size);
        if (region.IsEmptyArea() || IsIdentity) return target;
        BoxBlur.GaussianInPlace(target, region, Radius, context);
        return target;
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new BlurFilter((float)(Radius * scale), ScaleRegion(Region, scale));
}

internal static class BoxBlur
{
    /// <summary>
    /// Three box passes approximate a gaussian well once their integer radii are large enough; below a standard
    /// deviation of about 2 they can be far off, down to doing nothing at all below about 0.55, so a small blur, or the
    /// blur inside a sharpen previewed on a proxy, did nothing. There a sampled gaussian of at most 13 taps is used
    /// whenever the boxes would miss the requested deviation by more than 10%, which also keeps a slider dragged across
    /// these radii from jumping in strength.
    /// </summary>
    private const float SampledGaussianLimit = 2f;

    /// <summary>
    /// Boxes this wide already reach far past any image, so a larger deviation blurs no differently; past about ten
    /// times this their integer radii overflowed, and the blur did nothing at all.
    /// </summary>
    private const float MaxSigma = 1e8f;

    /// <summary>The standard deviation three box passes of these radii actually blur by.</summary>
    private static double BoxSigma(int[] radii)
    {
        var variance = 0.0;
        foreach (var r in radii) variance += ((2.0 * r + 1) * (2.0 * r + 1) - 1) / 12;
        return Math.Sqrt(variance);
    }

    /// <summary>Box radii for three passes approximating a gaussian with the given sigma (Ivan Kutskir's method).</summary>
    public static int[] BoxesForGauss(float sigma, int n)
    {
        var wIdeal = Math.Sqrt(12 * sigma * sigma / n + 1);
        var wl = (int)Math.Floor(wIdeal);
        if (wl % 2 == 0) wl--;
        var wu = wl + 2;
        var mIdeal = (12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4.0 * wl - 4);
        var m = (int)Math.Round(mIdeal);
        var sizes = new int[n];
        for (var i = 0; i < n; i++) sizes[i] = ((i < m ? wl : wu) - 1) / 2;
        return sizes;
    }

    /// <summary>
    /// Blurs a region in place, in premultiplied alpha, streaming it row by row: every pass that needs neighbouring rows
    /// keeps only those rows, so the memory is a few dozen rows rather than two full size float copies of the region.
    /// </summary>
    public static void GaussianInPlace(ImageBuffer buffer, Rectangle region, float sigma, OperationContext context)
        => Gaussian(buffer, region, sigma, context, consume: null);

    /// <summary>
    /// Blurs a region, handing each finished row to <paramref name="consume"/> (with its index in the region) instead of
    /// storing it, or storing it in place when that is null. A consumer may overwrite the region's row it is given:
    /// the blur has read every row it needs from there by then.
    /// </summary>
    public static void Gaussian(ImageBuffer buffer, Rectangle region, float sigma, OperationContext context, BlurredRow? consume)
    {
        if (region.Width <= 0 || region.Height <= 0) return;
        sigma = Math.Min(sigma, MaxSigma);
        var stages = new List<IRowStage>();
        var boxes = BoxesForGauss(sigma, 3);
        if (sigma < SampledGaussianLimit && Math.Abs(BoxSigma(boxes) - sigma) > 0.1 * sigma)
        {
            stages.Add(new GaussianPass(SampledKernel(sigma), region.Width, region.Height));
        }
        else
        {
            foreach (var r in boxes)
            {
                if (r <= 0) continue;
                stages.Add(new BoxPass(r, region.Width, region.Height));
            }
        }
        Run(buffer, region, stages, context, consume);
    }

    /// <summary>
    /// Pushes the region's rows through the stages and stores each finished row back. Every stage emits its rows in
    /// order and only after reading every input row it needs, so a row is never overwritten while a stage may still
    /// read it.
    /// </summary>
    private static void Run(ImageBuffer buffer, Rectangle region, List<IRowStage> stages, OperationContext context, BlurredRow? consume)
    {
        var w = region.Width;
        var h = region.Height;
        var pool = ArrayPool<Vector4>.Shared;
        var input = pool.Rent(w);
        var finished = consume is null ? null : ArrayPool<Rgba32>.Shared.Rent(w);
        var stored = 0;
        try
        {
            // sinks[i] feeds stage i; the last one stores finished rows.
            var sinks = new RowSink[stages.Count + 1];
            sinks[stages.Count] = (y, row) =>
            {
                if (consume is null)
                {
                    PixelRows.StoreUnpremultiplied(row, buffer.GetRow(region.Y + y).Slice(region.X, w));
                }
                else
                {
                    var bytes = finished.AsSpan(0, w);
                    PixelRows.StoreUnpremultiplied(row, bytes);
                    consume(y, bytes);
                }
                stored++;
            };
            for (var i = stages.Count - 1; i >= 0; i--)
            {
                var stage = stages[i];
                var next = sinks[i + 1];
                sinks[i] = (y, row) => stage.Push(y, row, next);
            }

            for (var y = 0; y < h; y++)
            {
                if ((y & 31) == 0)
                {
                    context.ThrowIfCancellationRequested();
                    context.ReportProgress(y / (double)h);
                }
                var row = input.AsSpan(0, w);
                PixelRows.LoadPremultiplied(buffer.GetRow(region.Y + y).Slice(region.X, w), row);
                sinks[0](y, row);
            }
            if (stored != h) throw new InvalidOperationException($"The blur stored {stored} of {h} rows.");
            context.ReportProgress(1);
        }
        finally
        {
            pool.Return(input);
            if (finished is not null) ArrayPool<Rgba32>.Shared.Return(finished);
            foreach (var stage in stages) stage.Dispose();
        }
    }

    private delegate void RowSink(int y, ReadOnlySpan<Vector4> row);

    /// <summary>Receives a finished row of a blur, by its index in the region.</summary>
    public delegate void BlurredRow(int y, ReadOnlySpan<Rgba32> row);

    private interface IRowStage : IDisposable
    {
        /// <summary>Takes input row <paramref name="y"/> (rows arrive in order) and emits every output row it completes.</summary>
        void Push(int y, ReadOnlySpan<Vector4> row, RowSink emit);
    }

    /// <summary>
    /// One pass of the three-pass box blur: horizontally across each row as it arrives, then vertically as a sliding
    /// window over the rows. Output row y is the running sum after adding row y + r and removing row y - r - 1, both
    /// clamped to the region, exactly as when each direction walked the whole region at once.
    /// </summary>
    private sealed class BoxPass : IRowStage
    {
        private const int MaxEdgeRepeats = 1024;

        private readonly int _r, _w, _h;
        private readonly float _iarr;
        private readonly Vector4[][] _ring;
        private readonly Vector4[] _sum, _out;
        private int _received, _emitted;
        private bool _started;

        public BoxPass(int r, int w, int h)
        {
            _r = r; _w = w; _h = h;
            _iarr = 1f / (r + r + 1);
            var pool = ArrayPool<Vector4>.Shared;
            // Rows y - r - 1 to y + r are live at once.
            _ring = new Vector4[Math.Min(2 * r + 2, h)][];
            for (var i = 0; i < _ring.Length; i++) _ring[i] = pool.Rent(w);
            _sum = pool.Rent(w);
            _out = pool.Rent(w);
        }

        private Span<Vector4> Row(int y) => _ring[y % _ring.Length].AsSpan(0, _w);

        public void Push(int y, ReadOnlySpan<Vector4> src, RowSink emit)
        {
            // The horizontal pass writes straight into the window's row.
            var dst = Row(y);
            int r = _r, w = _w;
            // Multiplying by a vector rather than a float: identical products, several times faster on the interpreter.
            var iarr = new Vector4(_iarr);
            var fv = src[0];
            var lv = src[w - 1];
            var val = (r + 1) * fv;
            var inside = Math.Min(r, w);
            for (var j = 0; j < inside; j++) val += src[j];
            if (r - inside > MaxEdgeRepeats) val += (r - inside) * lv;
            else for (var j = inside; j < r; j++) val += lv;
            for (var x = 0; x < w; x++)
            {
                var add = x + r < w ? src[x + r] : lv;
                var sub = x - r - 1 >= 0 ? src[x - r - 1] : fv;
                val += add - sub;
                dst[x] = val * iarr;
            }
            _received = y + 1;
            Drain(emit);
        }

        private void Drain(RowSink emit)
        {
            var r = _r;
            if (!_started)
            {
                // The sum starts as (r + 1) copies of the first row plus rows 0 to r - 1, clamped to the last row.
                if (_received < Math.Min(r, _h) || _received == 0) return;
                var first = Row(0);
                var inside = Math.Min(r, _h);
                var sum = _sum.AsSpan(0, _w);
                for (var x = 0; x < _w; x++)
                {
                    var v = (r + 1) * first[x];
                    for (var j = 0; j < inside; j++) v += Row(j)[x];
                    if (r - inside > MaxEdgeRepeats) v += (r - inside) * Row(_h - 1)[x];
                    else for (var j = inside; j < r; j++) v += Row(_h - 1)[x];
                    sum[x] = v;
                }
                _started = true;
            }
            while (_emitted < _h && Math.Min(_emitted + r, _h - 1) < _received)
            {
                var y = _emitted;
                var add = Row(Math.Min(y + r, _h - 1));
                var sub = Row(Math.Max(y - r - 1, 0));
                var sum = _sum.AsSpan(0, _w);
                var dst = _out.AsSpan(0, _w);
                var iarr = new Vector4(_iarr);
                for (var x = 0; x < dst.Length; x++)
                {
                    var v = sum[x] + add[x] - sub[x];
                    sum[x] = v;
                    dst[x] = v * iarr;
                }
                _emitted++;
                emit(y, dst);
            }
        }

        public void Dispose()
        {
            var pool = ArrayPool<Vector4>.Shared;
            foreach (var row in _ring) pool.Return(row);
            pool.Return(_sum);
            pool.Return(_out);
        }
    }

    /// <summary>Normalised weights of a gaussian sampled at whole pixel offsets, from -radius to radius.</summary>
    private static float[] SampledKernel(float sigma)
    {
        var radius = Math.Max(1, (int)Math.Ceiling(3 * sigma));
        var kernel = new float[2 * radius + 1];
        var sum = 0.0;
        for (var i = -radius; i <= radius; i++)
        {
            var weight = Math.Exp(-(i * i) / (2.0 * sigma * sigma));
            kernel[i + radius] = (float)weight;
            sum += weight;
        }
        for (var i = 0; i < kernel.Length; i++) kernel[i] = (float)(kernel[i] / sum);
        return kernel;
    }

    /// <summary>
    /// A sampled gaussian across each row as it arrives, repeating the edge pixels beyond its ends, then down the rows
    /// over a window of the last 2 * radius + 1 of them.
    /// </summary>
    private sealed class GaussianPass : IRowStage
    {
        private readonly float[] _kernel;
        private readonly Vector4[] _lanes;
        private readonly int _w, _h, _radius;
        private readonly Vector4[][] _ring;
        private readonly Vector4[] _out;
        private int _received, _emitted;

        public GaussianPass(float[] kernel, int w, int h)
        {
            _kernel = kernel; _w = w; _h = h;
            _lanes = Array.ConvertAll(kernel, k => new Vector4(k));
            _radius = kernel.Length / 2;
            var pool = ArrayPool<Vector4>.Shared;
            _ring = new Vector4[Math.Min(kernel.Length, h)][];
            for (var i = 0; i < _ring.Length; i++) _ring[i] = pool.Rent(w);
            _out = pool.Rent(w);
        }

        private Span<Vector4> Row(int y) => _ring[y % _ring.Length].AsSpan(0, _w);

        public void Push(int y, ReadOnlySpan<Vector4> src, RowSink emit)
        {
            var kernel = _lanes;
            int w = _w, radius = _radius;
            var row = Row(y);
            for (var x = 0; x < w; x++)
            {
                var acc = Vector4.Zero;
                if (x >= radius && x + radius < w)
                {
                    var window = src.Slice(x - radius, kernel.Length);
                    for (var k = 0; k < kernel.Length; k++) acc += window[k] * kernel[k];
                }
                else
                {
                    for (var k = 0; k < kernel.Length; k++) acc += src[Math.Clamp(x + k - radius, 0, w - 1)] * kernel[k];
                }
                row[x] = acc;
            }
            _received = y + 1;
            while (_emitted < _h && Math.Min(_emitted + radius, _h - 1) < _received)
            {
                var dst = _out.AsSpan(0, w);
                dst.Clear();
                for (var k = 0; k < kernel.Length; k++)
                {
                    var source = Row(Math.Clamp(_emitted + k - radius, 0, _h - 1));
                    var weight = kernel[k];
                    for (var x = 0; x < dst.Length; x++) dst[x] += source[x] * weight;
                }
                emit(_emitted++, dst);
            }
        }

        public void Dispose()
        {
            var pool = ArrayPool<Vector4>.Shared;
            foreach (var row in _ring) pool.Return(row);
            pool.Return(_out);
        }
    }
}

/// <summary>Unsharp-mask sharpening: adds the difference between the image and a blurred copy.</summary>
public sealed class SharpenFilter : RegionOperation
{
    public SharpenFilter(float amount = 0.5f, float radius = 1f, float threshold = 0f, Rectangle? region = null) : base(region)
    {
        if (amount < 0 || !float.IsFinite(amount)) throw new ArgumentOutOfRangeException(nameof(amount));
        if (radius <= 0 || !float.IsFinite(radius)) throw new ArgumentOutOfRangeException(nameof(radius));
        Amount = amount; Radius = radius; Threshold = Math.Clamp(threshold, 0f, 1f);
    }

    /// <summary>Strength of the effect. 0 = none, 1 = strong.</summary>
    public float Amount { get; }
    /// <summary>Radius of the blur used to find edges.</summary>
    public float Radius { get; }
    /// <summary>Minimum difference (0..1) before sharpening is applied; suppresses noise amplification.</summary>
    public float Threshold { get; }
    public override string Name => "Sharpen";
    public override bool IsIdentity => Amount == 0;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var region = GetRegion(source.Size);
        var target = GetMutableCopy(source, context);
        if (region.IsEmptyArea() || IsIdentity) return target;
        var threshold = (int)(Threshold * 255);
        var amount = Amount;
        // Each channel's result depends only on its value and its blurred value, so above a few thousand pixels it is
        // cheaper to work out all 65536 pairs once than to repeat the arithmetic per pixel.
        var table = (long)region.Width * region.Height > 1 << 17 ? SharpenTable(amount, threshold) : null;
        try
        {
            // The blur streams the region's rows and hands over each blurred row once it no longer needs the original
            // above it, so the original row is still there to sharpen and no blurred copy of the region is kept.
            BoxBlur.Gaussian(target, region, Radius, context, (y, blurred) =>
            {
                var dst = target.GetRow(region.Y + y).Slice(region.X, region.Width);
                if (table is not null)
                {
                    for (var x = 0; x < dst.Length; x++)
                    {
                        var p = dst[x];
                        var b = blurred[x];
                        dst[x] = new Rgba32(table[p.R << 8 | b.R], table[p.G << 8 | b.G], table[p.B << 8 | b.B], p.A);
                    }
                    return;
                }
                for (var x = 0; x < dst.Length; x++)
                {
                    var p = dst[x];
                    var b = blurred[x];
                    dst[x] = new Rgba32(Sharpen(p.R, b.R, amount, threshold), Sharpen(p.G, b.G, amount, threshold), Sharpen(p.B, b.B, amount, threshold), p.A);
                }
            });
        }
        finally
        {
            if (table is not null) ArrayPool<byte>.Shared.Return(table);
        }
        return target;
    }

    /// <summary>
    /// <see cref="Sharpen"/> for every pair of a value and its blurred value, indexed value * 256 + blurred, in an array
    /// rented from the pool.
    /// </summary>
    private static byte[] SharpenTable(float amount, int threshold)
    {
        var table = ArrayPool<byte>.Shared.Rent(256 * 256);
        for (var v = 0; v < 256; v++)
            for (var b = 0; b < 256; b++)
                table[v << 8 | b] = Sharpen((byte)v, (byte)b, amount, threshold);
        return table;
    }

    private static byte Sharpen(byte v, byte blurred, float amount, int threshold)
    {
        var diff = v - blurred;
        if (Math.Abs(diff) < threshold) return v;
        return Rgba32.ClampToByte(v + diff * amount);
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new SharpenFilter(Amount, (float)Math.Max(0.5, Radius * scale), Threshold, ScaleRegion(Region, scale));
}

/// <summary>Replaces blocks of pixels with their average colour (mosaic). Commonly used for redaction.</summary>
public sealed class PixelateFilter : RegionOperation
{
    public PixelateFilter(int blockSize, Rectangle? region = null) : base(region)
    {
        if (blockSize < 1) throw new ArgumentOutOfRangeException(nameof(blockSize));
        BlockSize = blockSize;
    }

    public int BlockSize { get; }
    public override string Name => "Pixelate";
    public override bool IsIdentity => BlockSize == 1;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var target = GetMutableCopy(source, context);
        var region = GetRegion(source.Size);
        if (region.IsEmptyArea() || IsIdentity) return target;
        var bs = BlockSize;
        for (var by = region.Top; by < region.Bottom; by += bs)
        {
            context.ThrowIfCancellationRequested();
            var bh = Math.Min(bs, region.Bottom - by);
            for (var bx = region.Left; bx < region.Right; bx += bs)
            {
                var bw = Math.Min(bs, region.Right - bx);
                long r = 0, g = 0, b = 0, a = 0;
                for (var y = 0; y < bh; y++)
                {
                    var row = target.GetRow(by + y).Slice(bx, bw);
                    foreach (var p in row) { r += p.R * p.A; g += p.G * p.A; b += p.B * p.A; a += p.A; }
                }
                var n = bw * bh;
                Rgba32 avg = a == 0 ? Rgba32.Transparent : new Rgba32((byte)(r / a), (byte)(g / a), (byte)(b / a), (byte)(a / n));
                for (var y = 0; y < bh; y++) target.GetRow(by + y).Slice(bx, bw).Fill(avg);
            }
        }
        return target;
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new PixelateFilter(Math.Max(1, (int)Math.Round(BlockSize * scale)), ScaleRegion(Region, scale));
}

/// <summary>Adds pseudo-random noise. Deterministic for a given seed.</summary>
public sealed class NoiseFilter : RegionOperation
{
    public NoiseFilter(float amount, bool monochrome = false, int seed = 0, Rectangle? region = null) : base(region)
    {
        Amount = Math.Clamp(amount, 0f, 1f); Monochrome = monochrome; Seed = seed;
    }

    /// <summary>Noise strength 0..1.</summary>
    public float Amount { get; }
    public bool Monochrome { get; }
    public int Seed { get; }
    public override string Name => "Noise";
    public override bool IsIdentity => Amount == 0;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var target = GetMutableCopy(source, context);
        var region = GetRegion(source.Size);
        if (region.IsEmptyArea() || IsIdentity) return target;
        var range = (int)(Amount * 255);
        var state = Seed == 0 ? 0x9E3779B9u : (uint)Seed;
        for (var y = region.Top; y < region.Bottom; y++)
        {
            if ((y & 31) == 0) context.ThrowIfCancellationRequested();
            var row = target.GetRow(y).Slice(region.Left, region.Width);
            for (var x = 0; x < row.Length; x++)
            {
                var p = row[x];
                int nr, ng, nb;
                if (Monochrome) { nr = ng = nb = Next(ref state, range); }
                else { nr = Next(ref state, range); ng = Next(ref state, range); nb = Next(ref state, range); }
                row[x] = new Rgba32(Rgba32.ClampToByte(p.R + nr), Rgba32.ClampToByte(p.G + ng), Rgba32.ClampToByte(p.B + nb), p.A);
            }
        }
        return target;
    }

    private static int Next(ref uint state, int range)
    {
        // xorshift32
        state ^= state << 13; state ^= state >> 17; state ^= state << 5;
        if (range == 0) return 0;
        return (int)(state % (uint)(range * 2 + 1)) - range;
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new NoiseFilter(Amount, Monochrome, Seed, ScaleRegion(Region, scale));
}

/// <summary>Median filter for noise reduction. Radius 1 = 3×3 window, 2 = 5×5. Cost grows with the square of the radius.</summary>
public sealed class MedianFilter : RegionOperation
{
    public MedianFilter(int radius = 1, Rectangle? region = null) : base(region)
    {
        if (radius is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be between 1 and 5.");
        Radius = radius;
    }

    public int Radius { get; }
    public override string Name => "Noise reduction";

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var region = GetRegion(source.Size);
        var output = context.CanMutateSource ? source : source.Clone(context.Allocator);
        if (region.IsEmptyArea()) return output;
        using var input = source.CopyRegion(region, context.Allocator);
        if (Radius == 1) Median3x3(input, output, region, context);
        else MedianSliding(input, output, region, Radius, context);
        context.ReportProgress(1);
        return output;
    }

    /// <summary>
    /// The 3×3 median through a 19 comparison selection network per channel, instead of sorting nine values three times
    /// per pixel. Edges repeat the border pixels, exactly as the general path does.
    /// </summary>
    private static void Median3x3(ImageBuffer input, ImageBuffer output, Rectangle region, OperationContext context)
        => ParallelRows.For(context, 0, input.Height, input.Width * 9L, (start, end, band) => Median3x3Rows(input, output, region, start, end, band));

    private static void Median3x3Rows(ImageBuffer input, ImageBuffer output, Rectangle region, int start, int end, OperationContext context)
    {
        int w = input.Width, h = input.Height;
        ReadOnlySpan<Rgba32> src = input.Pixels;
        for (var y = start; y < end; y++)
        {
            if (((y - start) & 15) == 0) { context.ThrowIfCancellationRequested(); context.ReportProgress(y / (double)h); }
            var above = src.Slice(Math.Max(y - 1, 0) * w, w);
            var row = src.Slice(y * w, w);
            var below = src.Slice(Math.Min(y + 1, h - 1) * w, w);
            var dst = output.GetRow(region.Y + y).Slice(region.X, w);
            for (var x = 0; x < w; x++)
            {
                var l = x > 0 ? x - 1 : 0;
                var r = x + 1 < w ? x + 1 : w - 1;
                Rgba32 a0 = above[l], a1 = above[x], a2 = above[r], b0 = row[l], b1 = row[x], b2 = row[r], c0 = below[l], c1 = below[x], c2 = below[r];
                dst[x] = new Rgba32(
                    Median9(a0.R, a1.R, a2.R, b0.R, b1.R, b2.R, c0.R, c1.R, c2.R),
                    Median9(a0.G, a1.G, a2.G, b0.G, b1.G, b2.G, c0.G, c1.G, c2.G),
                    Median9(a0.B, a1.B, a2.B, b0.B, b1.B, b2.B, c0.B, c1.B, c2.B),
                    b1.A);
            }
        }
    }

    /// <summary>The median of nine values (Paeth's network, as in Graphics Gems' opt_med9).</summary>
    private static byte Median9(int p0, int p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8)
    {
        Sort(ref p1, ref p2); Sort(ref p4, ref p5); Sort(ref p7, ref p8);
        Sort(ref p0, ref p1); Sort(ref p3, ref p4); Sort(ref p6, ref p7);
        Sort(ref p1, ref p2); Sort(ref p4, ref p5); Sort(ref p7, ref p8);
        Sort(ref p0, ref p3); Sort(ref p5, ref p8); Sort(ref p4, ref p7);
        Sort(ref p3, ref p6); Sort(ref p1, ref p4); Sort(ref p2, ref p5);
        Sort(ref p4, ref p7); Sort(ref p4, ref p2); Sort(ref p6, ref p4);
        Sort(ref p4, ref p2);
        return (byte)p4;

        static void Sort(ref int a, ref int b)
        {
            if (a > b) (a, b) = (b, a);
        }
    }

    /// <summary>
    /// Larger windows keep a 256 bin histogram per channel and slide it along each row (Huang's algorithm): moving one
    /// pixel right removes a column and adds one, and the median moves by the few bins those changes shift it. The
    /// window is the same edge-repeating multiset the direct method sorts, so the result is identical.
    /// </summary>
    private static void MedianSliding(ImageBuffer input, ImageBuffer output, Rectangle region, int radius, OperationContext context)
        => ParallelRows.For(context, 0, input.Height, input.Width * (2L * radius + 1) * 6, (start, end, band) => MedianSlidingRows(input, output, region, radius, start, end, band));

    /// <summary>Every row builds its histograms afresh, so bands of rows are independent.</summary>
    private static void MedianSlidingRows(ImageBuffer input, ImageBuffer output, Rectangle region, int radius, int start, int end, OperationContext context)
    {
        int w = input.Width, h = input.Height;
        var size = 2 * radius + 1;
        var mid = size * size / 2;
        var pool = ArrayPool<int>.Shared;
        var histograms = pool.Rent(3 * 256);
        var rows = new int[size];
        try
        {
            var hist = histograms.AsSpan(0, 3 * 256);
            ReadOnlySpan<Rgba32> src = input.Pixels;
            for (var y = start; y < end; y++)
            {
                if (((y - start) & 15) == 0) { context.ThrowIfCancellationRequested(); context.ReportProgress(y / (double)h); }
                for (var i = 0; i < size; i++) rows[i] = Math.Clamp(y + i - radius, 0, h - 1) * w;

                hist.Clear();
                for (var dx = -radius; dx <= radius; dx++)
                {
                    var sx = Math.Clamp(dx, 0, w - 1);
                    foreach (var offset in rows) AddPixel(hist, src[offset + sx], 1);
                }
                var (medR, belowR) = FindMedian(hist[..256], mid);
                var (medG, belowG) = FindMedian(hist.Slice(256, 256), mid);
                var (medB, belowB) = FindMedian(hist.Slice(512, 256), mid);

                var dst = output.GetRow(region.Y + y).Slice(region.X, w);
                for (var x = 0; x < w; x++)
                {
                    if (x > 0)
                    {
                        var leaving = Math.Clamp(x - radius - 1, 0, w - 1);
                        var entering = Math.Clamp(x + radius, 0, w - 1);
                        if (leaving != entering)
                        {
                            foreach (var offset in rows)
                            {
                                var o = src[offset + leaving];
                                var n = src[offset + entering];
                                hist[o.R]--; if (o.R < medR) belowR--;
                                hist[256 + o.G]--; if (o.G < medG) belowG--;
                                hist[512 + o.B]--; if (o.B < medB) belowB--;
                                hist[n.R]++; if (n.R < medR) belowR++;
                                hist[256 + n.G]++; if (n.G < medG) belowG++;
                                hist[512 + n.B]++; if (n.B < medB) belowB++;
                            }
                            Settle(hist[..256], mid, ref medR, ref belowR);
                            Settle(hist.Slice(256, 256), mid, ref medG, ref belowG);
                            Settle(hist.Slice(512, 256), mid, ref medB, ref belowB);
                        }
                    }
                    dst[x] = new Rgba32((byte)medR, (byte)medG, (byte)medB, src[y * w + x].A);
                }
            }
        }
        finally
        {
            pool.Return(histograms);
        }

        static void AddPixel(Span<int> hist, Rgba32 p, int delta)
        {
            hist[p.R] += delta;
            hist[256 + p.G] += delta;
            hist[512 + p.B] += delta;
        }

        static (int Median, int Below) FindMedian(ReadOnlySpan<int> hist, int mid)
        {
            var below = 0;
            for (var v = 0; v < 256; v++)
            {
                if (below + hist[v] > mid) return (v, below);
                below += hist[v];
            }
            return (255, below - hist[255]);
        }

        // Moves the median to the bin holding the middle element, given how many elements now lie below it.
        static void Settle(ReadOnlySpan<int> hist, int mid, ref int median, ref int below)
        {
            while (below > mid) { median--; below -= hist[median]; }
            while (below + hist[median] <= mid) { below += hist[median]; median++; }
        }
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new MedianFilter(Math.Clamp((int)Math.Round(Radius * scale), 1, 5), ScaleRegion(Region, scale));
}

/// <summary>Applies an arbitrary convolution kernel (e.g. edge detection, emboss).</summary>
public sealed class ConvolutionFilter : RegionOperation
{
    private readonly float[] _kernel;

    public ConvolutionFilter(float[] kernel, int kernelWidth, int kernelHeight, float divisor = 1f, float bias = 0f, Rectangle? region = null, string? name = null) : base(region)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        if (kernelWidth < 1 || kernelHeight < 1 || kernelWidth % 2 == 0 || kernelHeight % 2 == 0) throw new ArgumentException("Kernel dimensions must be odd and positive.");
        // In long: two large dimensions would overflow int and pass for a short kernel.
        if (kernel.Length != (long)kernelWidth * kernelHeight) throw new ArgumentException("Kernel length does not match dimensions.", nameof(kernel));
        if (divisor == 0) throw new ArgumentOutOfRangeException(nameof(divisor));
        _kernel = (float[])kernel.Clone();
        KernelWidth = kernelWidth; KernelHeight = kernelHeight; Divisor = divisor; Bias = bias;
        _name = name ?? "Convolution";
    }

    private readonly string _name;
    public IReadOnlyList<float> Kernel => _kernel;
    public int KernelWidth { get; }
    public int KernelHeight { get; }
    public float Divisor { get; }
    /// <summary>Constant added to each output channel, in [0,1] units.</summary>
    public float Bias { get; }
    public override string Name => _name;

    /// <summary>3×3 emboss kernel.</summary>
    public static ConvolutionFilter Emboss(Rectangle? region = null) => new([-2, -1, 0, -1, 1, 1, 0, 1, 2], 3, 3, 1f, 0f, region, "Emboss");

    /// <summary>3×3 Laplacian edge detection kernel.</summary>
    public static ConvolutionFilter EdgeDetect(Rectangle? region = null) => new([-1, -1, -1, -1, 8, -1, -1, -1, -1], 3, 3, 1f, 0f, region, "Edge detect");

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var region = GetRegion(source.Size);
        var output = context.CanMutateSource ? source : source.Clone(context.Allocator);
        if (region.IsEmptyArea()) return output;
        using var input = source.CopyRegion(region, context.Allocator);
        int w = input.Width, h = input.Height, kw = KernelWidth, kh = KernelHeight, rx = kw / 2, ry = kh / 2;
        var inv = 1f / Divisor;
        var bias = Bias * 255f;
        // Every output pixel reads the copy of the input, never the output, so bands of rows run side by side.
        ParallelRows.For(context, 0, h, (long)w * kw * kh, (start, end, band) =>
        {
            var src = input.Pixels;
            for (var y = start; y < end; y++)
            {
                if (((y - start) & 15) == 0) { band.ThrowIfCancellationRequested(); band.ReportProgress(y / (double)h); }
                var dst = output.GetRow(region.Y + y).Slice(region.X, w);
                for (var x = 0; x < w; x++)
                {
                    float r = 0, g = 0, b = 0;
                    for (var ky = 0; ky < kh; ky++)
                    {
                        var sy = Math.Clamp(y + ky - ry, 0, h - 1);
                        for (var kx = 0; kx < kw; kx++)
                        {
                            var sx = Math.Clamp(x + kx - rx, 0, w - 1);
                            var k = _kernel[ky * kw + kx];
                            var p = src[sy * w + sx];
                            r += p.R * k; g += p.G * k; b += p.B * k;
                        }
                    }
                    dst[x] = new Rgba32(Rgba32.ClampToByte(r * inv + bias), Rgba32.ClampToByte(g * inv + bias), Rgba32.ClampToByte(b * inv + bias), src[y * w + x].A);
                }
            }
        });
        context.ReportProgress(1);
        return output;
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new ConvolutionFilter(_kernel, KernelWidth, KernelHeight, Divisor, Bias, ScaleRegion(Region, scale), _name);
}

/// <summary>Darkens (or tints) the corners of the image with a smooth radial falloff.</summary>
public sealed class VignetteFilter : ImageOperation
{
    public VignetteFilter(float amount = 0.5f, float radius = 0.75f, float softness = 0.5f, Rgba32? color = null)
    {
        Amount = Math.Clamp(amount, 0f, 1f);
        Radius = Math.Clamp(radius, 0.05f, 2f);
        Softness = Math.Clamp(softness, 0.01f, 1f);
        Color = color ?? Rgba32.Black;
    }

    /// <summary>Strength 0..1.</summary>
    public float Amount { get; }
    /// <summary>Distance from centre (as a fraction of the half diagonal) where the effect reaches full strength.</summary>
    public float Radius { get; }
    /// <summary>Width of the transition band as a fraction of the radius.</summary>
    public float Softness { get; }
    public Rgba32 Color { get; }
    public override string Name => "Vignette";
    public override bool IsIdentity => Amount == 0;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var target = GetMutableCopy(source, context);
        if (IsIdentity) return target;
        int w = target.Width, h = target.Height;
        var cx = w / 2f; var cy = h / 2f;
        var halfDiag = MathF.Sqrt(cx * cx + cy * cy);
        var start = Radius * (1f - Softness) * halfDiag;
        var end = Radius * halfDiag;
        var invRange = 1f / MathF.Max(1e-3f, end - start);
        var cr = Color.R; var cg = Color.G; var cb = Color.B;
        var amount = Amount;
        // Each pixel depends on its own position and value alone.
        ParallelRows.For(context, 0, h, w, (first, last, band) =>
        {
            for (var y = first; y < last; y++)
            {
                if (((y - first) & 31) == 0) band.ThrowIfCancellationRequested();
                var row = target.GetRow(y);
                var dy = y + 0.5f - cy;
                for (var x = 0; x < w; x++)
                {
                    var dx = x + 0.5f - cx;
                    var d = MathF.Sqrt(dx * dx + dy * dy);
                    var t = Math.Clamp((d - start) * invRange, 0f, 1f);
                    t = t * t * (3f - 2f * t) * amount;
                    if (t <= 0f) continue;
                    var p = row[x];
                    row[x] = new Rgba32((byte)(p.R + (cr - p.R) * t), (byte)(p.G + (cg - p.G) * t), (byte)(p.B + (cb - p.B) * t), p.A);
                }
            }
        });
        return target;
    }
}
