namespace Bit.Bmage.Codecs.Avif;

/// <summary>
/// Superres upscaling (spec 7.16) and loop restoration (7.17). Restoration runs on regions that lie within one
/// restoration unit and one 64-row stripe, which the spec notes gives the same output as its 4x4 blocks.
/// </summary>
internal static class Av1Restoration
{
    public static Av1Plane[] Upscale(Av1Frame f, Av1Plane[] input)
    {
        var hdr = f.Hdr;
        var output = new Av1Plane[f.NumPlanes];
        try
        {
            for (var plane = 0; plane < f.NumPlanes; plane++)
            {
                var subX = plane > 0 ? f.SubX : 0;
                var subY = plane > 0 ? f.SubY : 0;
                var downscaledPlaneW = Av1Frame.Round2(hdr.FrameWidth, subX);
                var upscaledPlaneW = Av1Frame.Round2(hdr.UpscaledWidth, subX);
                var planeH = Av1Frame.Round2(hdr.FrameHeight, subY);
                var stepX = ((downscaledPlaneW << 14) + upscaledPlaneW / 2) / upscaledPlaneW;
                var err = upscaledPlaneW * stepX - (downscaledPlaneW << 14);
                var initialSubpelX = (-((upscaledPlaneW - downscaledPlaneW) << 13) + upscaledPlaneW / 2) / upscaledPlaneW + (1 << 7) - err / 2;
                initialSubpelX &= (1 << 14) - 1;
                var miW = f.MiCols >> subX;
                var maxX = miW * 4 - 1;
                var src = input[plane];
                var dst = output[plane] = new Av1Plane(upscaledPlaneW + 8, src.Height);
                var filter = Av1Tables.UpscaleFilter;
                var maxValue = (1 << f.BitDepth) - 1;
                for (var y = 0; y < planeH; y++)
                {
                    var srcRow = y * src.Stride;
                    var dstRow = y * dst.Stride;
                    for (var x = 0; x < upscaledPlaneW; x++)
                    {
                        var srcX = -(1 << 14) + initialSubpelX + x * stepX;
                        var srcXPx = srcX >> 14;
                        var srcXSubpel = (srcX & ((1 << 14) - 1)) >> 8;
                        var sum = 0;
                        for (var k = 0; k < 8; k++)
                        {
                            var sampleX = Math.Clamp(srcXPx + k - 3, 0, maxX);
                            sum += src.Data[srcRow + sampleX] * filter[srcXSubpel * 8 + k];
                        }
                        var v = (sum + 64) >> 7;
                        dst.Data[dstRow + x] = (ushort)(v < 0 ? 0 : v > maxValue ? maxValue : v);
                    }
                }
            }
            return output;
        }
        catch
        {
            foreach (var plane in output) plane?.Dispose();
            throw;
        }
    }

    /// <summary>Returns LrFrame: <paramref name="cdef"/> itself when no restoration applies, otherwise new planes.</summary>
    public static Av1Plane[] Apply(Av1Frame f, Av1Plane[] curr, Av1Plane[] cdef, CancellationToken cancellationToken)
    {
        var hdr = f.Hdr;
        if (!hdr.UsesLr) return cdef;
        var lr = new Av1Plane[f.NumPlanes];
        try
        {
            for (var p = 0; p < f.NumPlanes; p++) lr[p] = cdef[p].Clone();
            for (var plane = 0; plane < f.NumPlanes; plane++)
            {
                if (hdr.FrameRestorationType[plane] == Av1FrameHeader.RestoreNone) continue;
                var subX = plane > 0 ? f.SubX : 0;
                var subY = plane > 0 ? f.SubY : 0;
                var unitSize = hdr.LoopRestorationSize[plane];
                var unitRows = f.LrUnitRows[plane];
                var unitCols = f.LrUnitCols[plane];
                var ctx = new Context
                {
                    Frame = f,
                    Plane = plane,
                    Curr = curr[plane],
                    Cdef = cdef[plane],
                    Lr = lr[plane],
                    PlaneEndX = Av1Frame.Round2(hdr.UpscaledWidth, subX) - 1,
                    PlaneEndY = Av1Frame.Round2(hdr.FrameHeight, subY) - 1,
                };
                for (var stripeNum = 0; ; stripeNum++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ctx.StripeStartY = (-8 + stripeNum * 64) >> subY;
                    ctx.StripeEndY = ctx.StripeStartY + (64 >> subY) - 1;
                    var y0 = Math.Max(0, ctx.StripeStartY);
                    if (y0 > ctx.PlaneEndY) break;
                    var y1 = Math.Min(ctx.StripeEndY, ctx.PlaneEndY);
                    var lumaY = Math.Max(0, stripeNum * 64 - 8);
                    var unitRow = Math.Min(unitRows - 1, ((lumaY + 8) >> subY) / unitSize);
                    for (var unitCol = 0; unitCol < unitCols; unitCol++)
                    {
                        var x0 = unitCol * unitSize;
                        if (x0 > ctx.PlaneEndX) break;
                        var x1 = unitCol == unitCols - 1 ? ctx.PlaneEndX : Math.Min(ctx.PlaneEndX, (unitCol + 1) * unitSize - 1);
                        var unit = unitRow * unitCols + unitCol;
                        var rType = f.LrType[plane][unit];
                        if (rType == Av1FrameHeader.RestoreWiener) Wiener(ctx, unit, x0, y0, x1 - x0 + 1, y1 - y0 + 1);
                        else if (rType == Av1FrameHeader.RestoreSgrproj) SelfGuided(ctx, unit, x0, y0, x1 - x0 + 1, y1 - y0 + 1);
                    }
                }
            }
            return lr;
        }
        catch
        {
            foreach (var plane in lr) plane?.Dispose();
            throw;
        }
    }

    private sealed class Context
    {
        public required Av1Frame Frame;
        public required int Plane;
        public required Av1Plane Curr;
        public required Av1Plane Cdef;
        public required Av1Plane Lr;
        public required int PlaneEndX;
        public required int PlaneEndY;
        public int StripeStartY;
        public int StripeEndY;

        public int Source(int x, int y)
        {
            x = Math.Max(0, Math.Min(PlaneEndX, x));
            y = Math.Max(0, Math.Min(PlaneEndY, y));
            if (y < StripeStartY)
            {
                y = Math.Max(StripeStartY - 2, y);
                return Curr.Data[y * Curr.Stride + x];
            }
            if (y > StripeEndY)
            {
                y = Math.Min(StripeEndY + 2, y);
                return Curr.Data[y * Curr.Stride + x];
            }
            return Cdef.Data[y * Cdef.Stride + x];
        }
    }

    private static void Wiener(Context ctx, int unit, int x, int y, int w, int h)
    {
        var f = ctx.Frame;
        var bitDepth = f.BitDepth;
        var interRound0 = bitDepth == 12 ? 5 : 3;
        var interRound1 = bitDepth == 12 ? 9 : 11;
        var coeffs = f.LrWiener[ctx.Plane];
        Span<int> vfilter = stackalloc int[7];
        Span<int> hfilter = stackalloc int[7];
        WienerCoefficients(coeffs.AsSpan(unit * 6, 3), vfilter);
        WienerCoefficients(coeffs.AsSpan(unit * 6 + 3, 3), hfilter);
        var offset = 1 << (bitDepth + 7 - interRound0 - 1);
        var limit = (1 << (bitDepth + 1 + 7 - interRound0)) - 1;
        var intermediate = new int[(h + 6) * w];
        var row = new int[w + 6];
        for (var r = 0; r < h + 6; r++)
        {
            for (var c = 0; c < w + 6; c++) row[c] = ctx.Source(x + c - 3, y + r - 3);
            for (var c = 0; c < w; c++)
            {
                var s = 0;
                for (var t = 0; t < 7; t++) s += hfilter[t] * row[c + t];
                var v = (s + (1 << (interRound0 - 1))) >> interRound0;
                intermediate[r * w + c] = Math.Clamp(v, -offset, limit - offset);
            }
        }
        var maxValue = (1 << bitDepth) - 1;
        var lr = ctx.Lr;
        for (var r = 0; r < h; r++)
        {
            var dst = (y + r) * lr.Stride + x;
            for (var c = 0; c < w; c++)
            {
                var s = 0;
                for (var t = 0; t < 7; t++) s += vfilter[t] * intermediate[(r + t) * w + c];
                var v = (s + (1 << (interRound1 - 1))) >> interRound1;
                lr.Data[dst + c] = (ushort)(v < 0 ? 0 : v > maxValue ? maxValue : v);
            }
        }
    }

    private static void WienerCoefficients(ReadOnlySpan<sbyte> coeff, Span<int> filter)
    {
        filter[3] = 128;
        for (var i = 0; i < 3; i++)
        {
            int c = coeff[i];
            filter[i] = c;
            filter[6 - i] = c;
            filter[3] -= 2 * c;
        }
    }

    private static void SelfGuided(Context ctx, int unit, int x, int y, int w, int h)
    {
        var f = ctx.Frame;
        var set = f.LrSgrSet[ctx.Plane][unit];
        var flt0 = BoxFilter(ctx, x, y, w, h, set, 0);
        var flt1 = BoxFilter(ctx, x, y, w, h, set, 1);
        int w0 = f.LrSgrXqd[ctx.Plane][unit * 2];
        int w1 = f.LrSgrXqd[ctx.Plane][unit * 2 + 1];
        var w2 = (1 << 7) - w0 - w1;
        var maxValue = (1 << f.BitDepth) - 1;
        var cdef = ctx.Cdef;
        var lr = ctx.Lr;
        for (var i = 0; i < h; i++)
        {
            for (var j = 0; j < w; j++)
            {
                long u = cdef.Data[(y + i) * cdef.Stride + x + j] << 4;
                var v = w1 * u;
                v += flt0 is not null ? w0 * (long)flt0[i * w + j] : w0 * u;
                v += flt1 is not null ? w2 * (long)flt1[i * w + j] : w2 * u;
                var s = (int)((v + (1 << 10)) >> 11);
                lr.Data[(y + i) * lr.Stride + x + j] = (ushort)(s < 0 ? 0 : s > maxValue ? maxValue : s);
            }
        }
    }

    private static int[]? BoxFilter(Context ctx, int x, int y, int w, int h, int set, int pass)
    {
        var r = Av1Tables.SgrParams[set * 4 + pass * 2];
        if (r == 0) return null;
        var eps = Av1Tables.SgrParams[set * 4 + pass * 2 + 1];
        var bitDepth = ctx.Frame.BitDepth;
        var n = (2 * r + 1) * (2 * r + 1);
        var n2e = n * n * eps;
        var s = ((1 << 20) + n2e / 2) / n2e;
        var oneOverN = ((1 << 12) + n / 2) / n;

        // Source samples with a margin of r + 1 on every side, then the box sums of each window.
        var m = r + 1;
        var sw = w + 2 * m;
        var sh = h + 2 * m;
        var src = new int[sw * sh];
        for (var i = 0; i < sh; i++)
            for (var j = 0; j < sw; j++)
                src[i * sw + j] = ctx.Source(x + j - m, y + i - m);

        var aw = w + 2;
        var ah = h + 2;
        var a = new int[aw * ah];
        var b = new int[aw * ah];
        for (var i = -1; i < h + 1; i++)
        {
            for (var j = -1; j < w + 1; j++)
            {
                long sumSq = 0;
                long sum = 0;
                for (var dy = -r; dy <= r; dy++)
                {
                    var row = (i + dy + m) * sw + j + m;
                    for (var dx = -r; dx <= r; dx++)
                    {
                        long c = src[row + dx];
                        sumSq += c * c;
                        sum += c;
                    }
                }
                var aa = Round2(sumSq, 2 * (bitDepth - 8));
                var d = Round2(sum, bitDepth - 8);
                var p = Math.Max(0, aa * n - d * d);
                var z = Round2(p * s, 20);
                long a2;
                if (z >= 255) a2 = 256;
                else if (z == 0) a2 = 1;
                else a2 = ((z << 8) + z / 2) / (z + 1);
                var b2 = (256 - a2) * sum * oneOverN;
                a[(i + 1) * aw + j + 1] = (int)a2;
                b[(i + 1) * aw + j + 1] = (int)Round2(b2, 12);
            }
        }

        var output = new int[w * h];
        var cdef = ctx.Cdef;
        for (var i = 0; i < h; i++)
        {
            var shift = 5;
            if (pass == 0 && (i & 1) != 0) shift = 4;
            for (var j = 0; j < w; j++)
            {
                long av = 0;
                long bv = 0;
                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        int weight;
                        if (pass == 0) weight = ((i + dy) & 1) != 0 ? (dx == 0 ? 6 : 5) : 0;
                        else weight = dx == 0 || dy == 0 ? 4 : 3;
                        if (weight == 0) continue;
                        var idx = (i + dy + 1) * aw + j + dx + 1;
                        av += weight * a[idx];
                        bv += weight * b[idx];
                    }
                }
                var v = av * cdef.Data[(y + i) * cdef.Stride + x + j] + bv;
                output[i * w + j] = (int)Round2(v, 8 + shift - 4);
            }
        }
        return output;
    }

    private static long Round2(long x, int n) => n == 0 ? x : (x + (1L << (n - 1))) >> n;
}
