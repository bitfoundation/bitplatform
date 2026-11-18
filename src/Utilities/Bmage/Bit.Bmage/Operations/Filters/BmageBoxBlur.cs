using System.Buffers;
using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

internal static class BmageBoxBlur
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
    public static void GaussianInPlace(BmageImageBuffer buffer, Rectangle region, float sigma, BmageOperationContext context)
        => Gaussian(buffer, region, sigma, context, consume: null);

    /// <summary>
    /// Blurs a region, handing each finished row to <paramref name="consume"/> (with its index in the region) instead of
    /// storing it, or storing it in place when that is null. A consumer may overwrite the region's row it is given:
    /// the blur has read every row it needs from there by then.
    /// </summary>
    public static void Gaussian(BmageImageBuffer buffer, Rectangle region, float sigma, BmageOperationContext context, BlurredRow? consume)
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
    private static void Run(BmageImageBuffer buffer, Rectangle region, List<IRowStage> stages, BmageOperationContext context, BlurredRow? consume)
    {
        var w = region.Width;
        var h = region.Height;
        var pool = ArrayPool<Vector4>.Shared;
        var input = pool.Rent(w);
        var finished = consume is null ? null : ArrayPool<BmageRgba32>.Shared.Rent(w);
        var stored = 0;
        try
        {
            // sinks[i] feeds stage i; the last one stores finished rows.
            var sinks = new RowSink[stages.Count + 1];
            sinks[stages.Count] = (y, row) =>
            {
                if (consume is null)
                {
                    BmagePixelRows.StoreUnpremultiplied(row, buffer.GetRow(region.Y + y).Slice(region.X, w));
                }
                else
                {
                    var bytes = finished.AsSpan(0, w);
                    BmagePixelRows.StoreUnpremultiplied(row, bytes);
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
                BmagePixelRows.LoadPremultiplied(buffer.GetRow(region.Y + y).Slice(region.X, w), row);
                sinks[0](y, row);
            }
            if (stored != h) throw new InvalidOperationException($"The blur stored {stored} of {h} rows.");
            context.ReportProgress(1);
        }
        finally
        {
            pool.Return(input);
            if (finished is not null) ArrayPool<BmageRgba32>.Shared.Return(finished);
            foreach (var stage in stages) stage.Dispose();
        }
    }

    private delegate void RowSink(int y, ReadOnlySpan<Vector4> row);

    /// <summary>Receives a finished row of a blur, by its index in the region.</summary>
    public delegate void BlurredRow(int y, ReadOnlySpan<BmageRgba32> row);

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
