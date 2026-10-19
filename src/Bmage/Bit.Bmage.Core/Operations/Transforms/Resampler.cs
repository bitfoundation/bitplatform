using System.Buffers;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using Bit.Bmage.Geometry;

namespace Bit.Bmage.Operations.Transforms;

/// <summary>
/// High quality separable resampling with premultiplied-alpha filtering. Works on rectangular regions so a single routine
/// implements every <see cref="ResizeMode"/>. The vertical pass runs first and produces one floating point row at a time,
/// so no full-size intermediate buffer is needed.
/// </summary>
public static class Resampler
{
    /// <summary>Precomputed contribution window for one output index.</summary>
    private readonly struct Window
    {
        public readonly int Start;   // first source index
        public readonly int Count;   // number of taps
        public readonly int Offset;  // offset into the shared weight array
        public Window(int start, int count, int offset) { Start = start; Count = count; Offset = offset; }
    }

    private sealed class Kernel
    {
        public Window[] Windows = [];
        public float[] Weights = [];
        public int MaxTaps;
    }

    /// <summary>Radius (support) of the filter in source pixels at scale 1.</summary>
    public static float GetRadius(ResamplingFilter filter) => filter switch
    {
        ResamplingFilter.Box => 0.5f,
        ResamplingFilter.Bilinear => 1f,
        ResamplingFilter.Bicubic => 2f,
        ResamplingFilter.Lanczos3 => 3f,
        _ => 1f,
    };

    /// <summary>Evaluates the filter function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Evaluate(ResamplingFilter filter, float x)
    {
        x = MathF.Abs(x);
        switch (filter)
        {
            case ResamplingFilter.Box:
                return x <= 0.5f ? 1f : 0f;
            case ResamplingFilter.Bilinear:
                return x < 1f ? 1f - x : 0f;
            case ResamplingFilter.Bicubic:
            {
                const float a = -0.5f; // Catmull-Rom
                if (x < 1f) return ((a + 2f) * x - (a + 3f)) * x * x + 1f;
                if (x < 2f) return ((a * x - 5f * a) * x + 8f * a) * x - 4f * a;
                return 0f;
            }
            case ResamplingFilter.Lanczos3:
            {
                if (x < 1e-6f) return 1f;
                if (x >= 3f) return 0f;
                var px = MathF.PI * x;
                return 3f * MathF.Sin(px) * MathF.Sin(px / 3f) / (px * px);
            }
            default:
                return x < 1f ? 1f - x : 0f;
        }
    }

    /// <summary>Resolves <see cref="ResamplingFilter.Auto"/> based on the scale factors.</summary>
    public static ResamplingFilter Resolve(ResamplingFilter filter, double scaleX, double scaleY)
    {
        if (filter != ResamplingFilter.Auto) return filter;
        return scaleX < 1 || scaleY < 1 ? ResamplingFilter.Lanczos3 : ResamplingFilter.Bicubic;
    }

    /// <remarks>
    /// <paramref name="srcCount"/> is how many source pixels may be sampled; <paramref name="srcExtent"/> is the
    /// continuous width the region represents. They are normally equal, but after a box reduction the reduced image
    /// holds ceil(n/f) pixels while representing only n/f pixels worth of content, because the last block is partial.
    /// Mapping against the pixel count instead of the true extent stretches the image, so the two are tracked apart.
    /// </remarks>
    private static Kernel BuildKernel(ResamplingFilter filter, int srcStart, int srcCount, double srcExtent, int dstLength)
    {
        var scale = dstLength / srcExtent;                   // destination pixels per source pixel
        var filterScale = scale < 1 ? 1 / scale : 1;         // widen when downscaling
        var support = GetRadius(filter) * filterScale;
        var maxTaps = (int)Math.Ceiling(support * 2) + 2;
        var windows = new Window[dstLength];
        var weights = new float[dstLength * maxTaps];
        var offset = 0;
        var srcEnd = srcStart + srcCount;
        var inv = (float)(1 / filterScale);

        for (var i = 0; i < dstLength; i++)
        {
            var center = srcStart + (i + 0.5) / scale;
            var start = (int)Math.Floor(center - support);
            var end = (int)Math.Ceiling(center + support);
            if (start < srcStart) start = srcStart;
            if (end > srcEnd) end = srcEnd;
            if (end <= start) { start = Math.Clamp((int)center, srcStart, srcEnd - 1); end = start + 1; }
            var count = Math.Min(end - start, maxTaps);
            float sum = 0;
            for (var j = 0; j < count; j++)
            {
                var w = Evaluate(filter, (float)((start + j + 0.5 - center) * inv));
                weights[offset + j] = w;
                sum += w;
            }
            if (sum <= 1e-8f)
            {
                // Degenerate (e.g. box filter falling between samples): fall back to nearest.
                Array.Clear(weights, offset, count);
                var nearest = Math.Clamp((int)center - start, 0, count - 1);
                weights[offset + nearest] = 1f;
            }
            else
            {
                var norm = 1f / sum;
                for (var j = 0; j < count; j++) weights[offset + j] *= norm;
            }
            windows[i] = new Window(start, count, offset);
            offset += count;
        }
        return new Kernel { Windows = windows, Weights = weights, MaxTaps = maxTaps };
    }

    /// <summary>
    /// Resamples <paramref name="sourceRect"/> of <paramref name="source"/> into <paramref name="destinationRect"/> of
    /// <paramref name="destination"/>. Pixels of the destination outside the rectangle are left untouched.
    /// </summary>
    /// <remarks>
    /// <paramref name="destinationRect"/> may extend beyond the destination buffer, including a negative origin. The
    /// filter kernels are always computed for the full rectangle and only the visible part is written, so rendering a
    /// sub-region produces bit-identical pixels to rendering the whole image and cropping it. This is what makes fusing
    /// a resize with a following crop exact.
    /// </remarks>
    public static void Resample(ImageBuffer source, Rectangle sourceRect, ImageBuffer destination, Rectangle destinationRect, ResamplingFilter filter, OperationContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        sourceRect.Intersect(source.Bounds);
        if (sourceRect.IsEmptyArea()) throw new ArgumentException("Source rectangle is empty.", nameof(sourceRect));
        ResampleCore(source, sourceRect, sourceRect.Width, sourceRect.Height, destination, destinationRect, filter, context ?? OperationContext.Default);
    }

    /// <remarks>
    /// <c>extentX</c> and <c>extentY</c> are the continuous size of the source region, which differs from the
    /// rectangle size only after a box reduction has left a partial final block.
    /// </remarks>
    private static void ResampleCore(ImageBuffer source, Rectangle sourceRect, double extentX, double extentY, ImageBuffer destination, Rectangle destinationRect, ResamplingFilter filter, OperationContext context)
    {
        if (destinationRect.Width <= 0 || destinationRect.Height <= 0) throw new ArgumentException("Destination rectangle is empty.", nameof(destinationRect));

        var clip = destinationRect;
        clip.Intersect(destination.Bounds);
        if (clip.IsEmptyArea()) return;

        filter = Resolve(filter, destinationRect.Width / extentX, destinationRect.Height / extentY);
        if (filter == ResamplingFilter.NearestNeighbor)
        {
            ResampleNearest(source, sourceRect, destination, destinationRect, clip, context);
            return;
        }

        // Large reductions make the filter support enormous: shrinking 30x with Lanczos needs 180 taps per axis per
        // output pixel. An exact integer box average costs one pass and removes exactly the frequencies the wide kernel
        // was there to remove, so doing that first and finishing with the requested filter is both far faster and
        // visually equivalent. The reduced image carries a fractional extent when the block size does not divide the
        // region evenly, so the second pass still maps the original content across the whole destination.
        var (factorX, factorY) = GetPrescaleFactors(extentX, extentY, destinationRect.Size);
        if (factorX > 1 || factorY > 1)
        {
            using var reduced = BoxReduce(source, sourceRect, factorX, factorY, context);
            ResampleCore(reduced, reduced.Bounds, extentX / factorX, extentY / factorY, destination, destinationRect, filter, context);
            return;
        }

        var kx = BuildKernel(filter, sourceRect.X, sourceRect.Width, extentX, destinationRect.Width);
        var ky = BuildKernel(filter, sourceRect.Y, sourceRect.Height, extentY, destinationRect.Height);

        var srcW = sourceRect.Width;
        var pool = ArrayPool<Vector4>.Shared;
        var accum = pool.Rent(srcW);
        var cacheSize = ky.MaxTaps + 2;
        var cacheRows = new Vector4[cacheSize][];
        var cacheIndex = new int[cacheSize];
        for (var i = 0; i < cacheSize; i++) { cacheRows[i] = pool.Rent(srcW); cacheIndex[i] = -1; }
        var outRow = pool.Rent(destinationRect.Width);
        try
        {
            var accumSpan = accum.AsSpan(0, srcW);
            var outSpan = outRow.AsSpan(0, destinationRect.Width);
            var scale = new Vector4(255f);
            var half = new Vector4(0.5f);

            for (var dy = clip.Top; dy < clip.Bottom; dy++)
            {
                if ((dy & 15) == 0)
                {
                    context.ThrowIfCancellationRequested();
                    context.ReportProgress((dy - clip.Top) / (double)clip.Height);
                }
                var wy = ky.Windows[dy - destinationRect.Y];

                // Vertical pass: weighted sum of source rows into accum (premultiplied).
                accumSpan.Clear();
                for (var t = 0; t < wy.Count; t++)
                {
                    var sy = wy.Start + t;
                    var w = ky.Weights[wy.Offset + t];
                    if (w == 0f) continue;
                    var row = GetRow(source, sy, sourceRect.X, srcW, cacheRows, cacheIndex);
                    var wv = new Vector4(w);
                    for (var x = 0; x < srcW; x++) accumSpan[x] += row[x] * wv;
                }

                // Horizontal pass, computed only for the columns that are actually written.
                for (var dx = clip.Left - destinationRect.X; dx < clip.Right - destinationRect.X; dx++)
                {
                    var wx = kx.Windows[dx];
                    var acc = Vector4.Zero;
                    var s = wx.Start - sourceRect.X;
                    for (var t = 0; t < wx.Count; t++)
                        acc += accumSpan[s + t] * kx.Weights[wx.Offset + t];
                    outSpan[dx] = acc;
                }

                // Un-premultiply and store.
                var dst = destination.GetRow(dy);
                for (var dx = clip.Left; dx < clip.Right; dx++)
                {
                    var p = outSpan[dx - destinationRect.X];
                    var a = p.W;
                    if (a > 1e-5f)
                    {
                        var invA = 1f / a;
                        p = new Vector4(p.X * invA, p.Y * invA, p.Z * invA, a);
                    }
                    else p = Vector4.Zero;
                    var v = Vector4.Clamp(p, Vector4.Zero, Vector4.One) * scale + half;
                    dst[dx] = new Rgba32((byte)v.X, (byte)v.Y, (byte)v.Z, (byte)v.W);
                }
            }
            context.ReportProgress(1);
        }
        finally
        {
            pool.Return(accum);
            pool.Return(outRow);
            foreach (var r in cacheRows) pool.Return(r);
        }
    }

    /// <summary>
    /// Chooses integer decimation factors that leave the final resample working at no more than a 2x reduction, which
    /// keeps the filter support small. Returns (1,1) when the reduction is already modest.
    /// </summary>
    private static (int X, int Y) GetPrescaleFactors(double extentX, double extentY, Size destination)
    {
        static int Factor(double from, int to)
        {
            if (to <= 0) return 1;
            var ratio = from / to;
            // Only worth it past a 4x reduction; below that the wide kernel is cheap enough.
            if (ratio < 4) return 1;
            var factor = (int)(ratio / 2);
            return Math.Max(1, factor);
        }
        return (Factor(extentX, destination.Width), Factor(extentY, destination.Height));
    }

    /// <summary>
    /// Averages blocks of <paramref name="factorX"/> x <paramref name="factorY"/> source pixels into one output pixel,
    /// working in premultiplied alpha so transparent regions do not darken their neighbours.
    /// </summary>
    private static ImageBuffer BoxReduce(ImageBuffer source, Rectangle sourceRect, int factorX, int factorY, OperationContext context)
    {
        var width = Math.Max(1, (sourceRect.Width + factorX - 1) / factorX);
        var height = Math.Max(1, (sourceRect.Height + factorY - 1) / factorY);
        var output = context.Allocate(width, height, clear: false);
        try
        {
            for (var y = 0; y < height; y++)
            {
                if ((y & 31) == 0) context.ThrowIfCancellationRequested();
                var y0 = sourceRect.Y + y * factorY;
                var y1 = Math.Min(y0 + factorY, sourceRect.Bottom);
                var dst = output.GetRow(y);
                for (var x = 0; x < width; x++)
                {
                    var x0 = sourceRect.X + x * factorX;
                    var x1 = Math.Min(x0 + factorX, sourceRect.Right);
                    long r = 0, g = 0, b = 0, a = 0;
                    var count = 0;
                    for (var sy = y0; sy < y1; sy++)
                    {
                        var row = source.GetRow(sy);
                        for (var sx = x0; sx < x1; sx++)
                        {
                            var p = row[sx];
                            // Premultiply so fully transparent pixels contribute no colour.
                            r += p.R * p.A;
                            g += p.G * p.A;
                            b += p.B * p.A;
                            a += p.A;
                            count++;
                        }
                    }
                    if (count == 0 || a == 0)
                    {
                        dst[x] = Rgba32.Transparent;
                        continue;
                    }
                    var half = a / 2;
                    dst[x] = new Rgba32((byte)((r + half) / a), (byte)((g + half) / a), (byte)((b + half) / a), (byte)((a + count / 2) / count));
                }
            }
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    private static Span<Vector4> GetRow(ImageBuffer source, int y, int x0, int width, Vector4[][] cacheRows, int[] cacheIndex)
    {
        var slot = y % cacheRows.Length;
        var row = cacheRows[slot];
        if (cacheIndex[slot] != y)
        {
            var src = source.GetRow(y).Slice(x0, width);
            const float inv = 1f / 255f;
            for (var x = 0; x < width; x++)
            {
                var p = src[x];
                var a = p.A * inv;
                row[x] = new Vector4(p.R * inv * a, p.G * inv * a, p.B * inv * a, a);
            }
            cacheIndex[slot] = y;
        }
        return row.AsSpan(0, width);
    }

    private static void ResampleNearest(ImageBuffer source, Rectangle sourceRect, ImageBuffer destination, Rectangle destinationRect, Rectangle clip, OperationContext context)
    {
        var sx = sourceRect.Width / (double)destinationRect.Width;
        var sy = sourceRect.Height / (double)destinationRect.Height;
        var xmap = new int[clip.Width];
        for (var dx = 0; dx < clip.Width; dx++)
            xmap[dx] = Math.Clamp(sourceRect.X + (int)((dx + clip.Left - destinationRect.X + 0.5) * sx), sourceRect.Left, sourceRect.Right - 1);
        for (var dy = clip.Top; dy < clip.Bottom; dy++)
        {
            if ((dy & 63) == 0) context.ThrowIfCancellationRequested();
            var srcY = Math.Clamp(sourceRect.Y + (int)((dy - destinationRect.Y + 0.5) * sy), sourceRect.Top, sourceRect.Bottom - 1);
            var srcRow = source.GetRow(srcY);
            var dst = destination.GetRow(dy);
            for (var dx = 0; dx < clip.Width; dx++) dst[clip.Left + dx] = srcRow[xmap[dx]];
        }
        context.ReportProgress(1);
    }
}
