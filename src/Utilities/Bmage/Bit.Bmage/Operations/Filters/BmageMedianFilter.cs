using System.Buffers;
using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>Median filter for noise reduction. Radius 1 = 3×3 window, 2 = 5×5. Cost grows with the square of the radius.</summary>
public sealed class BmageMedianFilter : BmageRegionOperation
{
    public BmageMedianFilter(int radius = 1, Rectangle? region = null) : base(region)
    {
        if (radius is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be between 1 and 5.");
        Radius = radius;
    }

    public int Radius { get; }
    public override string Name => "Noise reduction";

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
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
    private static void Median3x3(BmageImageBuffer input, BmageImageBuffer output, Rectangle region, BmageOperationContext context)
        => BmageParallelRows.For(context, 0, input.Height, input.Width * 9L, (start, end, band) => Median3x3Rows(input, output, region, start, end, band));

    private static void Median3x3Rows(BmageImageBuffer input, BmageImageBuffer output, Rectangle region, int start, int end, BmageOperationContext context)
    {
        int w = input.Width, h = input.Height;
        ReadOnlySpan<BmageRgba32> src = input.Pixels;
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
                BmageRgba32 a0 = above[l], a1 = above[x], a2 = above[r], b0 = row[l], b1 = row[x], b2 = row[r], c0 = below[l], c1 = below[x], c2 = below[r];
                dst[x] = new BmageRgba32(
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
    private static void MedianSliding(BmageImageBuffer input, BmageImageBuffer output, Rectangle region, int radius, BmageOperationContext context)
        => BmageParallelRows.For(context, 0, input.Height, input.Width * (2L * radius + 1) * 6, (start, end, band) => MedianSlidingRows(input, output, region, radius, start, end, band));

    /// <summary>Every row builds its histograms afresh, so bands of rows are independent.</summary>
    private static void MedianSlidingRows(BmageImageBuffer input, BmageImageBuffer output, Rectangle region, int radius, int start, int end, BmageOperationContext context)
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
            ReadOnlySpan<BmageRgba32> src = input.Pixels;
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
                    dst[x] = new BmageRgba32((byte)medR, (byte)medG, (byte)medB, src[y * w + x].A);
                }
            }
        }
        finally
        {
            pool.Return(histograms);
        }

        static void AddPixel(Span<int> hist, BmageRgba32 p, int delta)
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

    public override IBmageImageOperation ForScale(double scale) => scale == 1 ? this : new BmageMedianFilter(Math.Clamp((int)Math.Round(Radius * scale), 1, 5), ScaleRegion(Region, scale));
}
