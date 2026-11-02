namespace Bit.Bmage.Codecs.Avif;

// Prediction (spec 7.11): intra prediction with its edge filters, chroma from luma, palettes and intra block copy.
internal sealed partial class Av1TileDecoder
{
    private const int EdgeOffset = 16;
    private readonly int[] _aboveRow = new int[EdgeOffset + 3 * 128 + 16];
    private readonly int[] _leftCol = new int[EdgeOffset + 3 * 128 + 16];
    private readonly int[] _edge = new int[3 * 128 + 16];
    private readonly int[] _interIntermediate = new int[(128 + 8) * 128];

    private void PredictIntra(int plane, int x, int y, bool haveLeft, bool haveAbove, bool haveAboveRight, bool haveBelowLeft, int mode, int log2W, int log2H)
    {
        var p = _f.Planes[plane];
        var data = p.Data;
        var stride = p.Stride;
        var w = 1 << log2W;
        var h = 1 << log2H;
        var subX = plane > 0 ? _f.SubX : 0;
        var subY = plane > 0 ? _f.SubY : 0;
        var maxX = ((_f.MiCols * 4) >> subX) - 1;
        var maxY = ((_f.MiRows * 4) >> subY) - 1;
        var bitDepth = _f.BitDepth;
        var above = _aboveRow;
        var left = _leftCol;
        const int o = EdgeOffset;
        var n = w + h;

        if (!haveAbove && haveLeft)
        {
            int v = data[y * stride + x - 1];
            above.AsSpan(o, n).Fill(v);
        }
        else if (!haveAbove)
        {
            above.AsSpan(o, n).Fill((1 << (bitDepth - 1)) - 1);
        }
        else
        {
            var aboveLimit = Math.Min(maxX, x + (haveAboveRight ? 2 * w : w) - 1);
            var row = (y - 1) * stride;
            for (var i = 0; i < n; i++) above[o + i] = data[row + Math.Min(aboveLimit, x + i)];
        }
        if (!haveLeft && haveAbove)
        {
            int v = data[(y - 1) * stride + x];
            left.AsSpan(o, n).Fill(v);
        }
        else if (!haveLeft)
        {
            left.AsSpan(o, n).Fill((1 << (bitDepth - 1)) + 1);
        }
        else
        {
            var leftLimit = Math.Min(maxY, y + (haveBelowLeft ? 2 * h : h) - 1);
            for (var i = 0; i < n; i++) left[o + i] = data[Math.Min(leftLimit, y + i) * stride + x - 1];
        }
        int corner;
        if (haveAbove && haveLeft) corner = data[(y - 1) * stride + x - 1];
        else if (haveAbove) corner = data[(y - 1) * stride + x];
        else if (haveLeft) corner = data[y * stride + x - 1];
        else corner = 1 << (bitDepth - 1);
        above[o - 1] = corner;
        left[o - 1] = corner;

        var dst = y * stride + x;
        if (plane == 0 && _useFilterIntra) RecursiveIntraPrediction(data, dst, stride, w, h);
        else if (IsDirectionalMode(mode)) DirectionalIntraPrediction(plane, x, y, haveLeft, haveAbove, mode, w, h, maxX, maxY, data, dst, stride);
        else if (mode is SmoothPred or SmoothVPred or SmoothHPred) SmoothIntraPrediction(mode, log2W, log2H, w, h, data, dst, stride);
        else if (mode == DcPred) DcIntraPrediction(haveLeft, haveAbove, log2W, log2H, w, h, data, dst, stride);
        else PaethIntraPrediction(w, h, data, dst, stride);
    }

    private void PaethIntraPrediction(int w, int h, ushort[] data, int dst, int stride)
    {
        var above = _aboveRow;
        var left = _leftCol;
        const int o = EdgeOffset;
        var topLeft = above[o - 1];
        for (var i = 0; i < h; i++)
        {
            var row = dst + i * stride;
            var l = left[o + i];
            for (var j = 0; j < w; j++)
            {
                var a = above[o + j];
                var b = a + l - topLeft;
                var pLeft = Math.Abs(b - l);
                var pTop = Math.Abs(b - a);
                var pTopLeft = Math.Abs(b - topLeft);
                int v;
                if (pLeft <= pTop && pLeft <= pTopLeft) v = l;
                else if (pTop <= pTopLeft) v = a;
                else v = topLeft;
                data[row + j] = (ushort)v;
            }
        }
    }

    private void RecursiveIntraPrediction(ushort[] data, int dst, int stride, int w, int h)
    {
        var above = _aboveRow;
        var left = _leftCol;
        const int o = EdgeOffset;
        var w4 = w >> 2;
        var h2 = h >> 1;
        Span<int> p = stackalloc int[7];
        var taps = Av1Tables.IntraFilterTaps;
        var mode = _filterIntraMode;
        for (var i2 = 0; i2 < h2; i2++)
        {
            for (var j4 = 0; j4 < w4; j4++)
            {
                for (var i = 0; i < 7; i++)
                {
                    if (i < 5)
                    {
                        if (i2 == 0) p[i] = above[o + (j4 << 2) + i - 1];
                        else if (j4 == 0 && i == 0) p[i] = left[o + (i2 << 1) - 1];
                        else p[i] = data[dst + ((i2 << 1) - 1) * stride + (j4 << 2) + i - 1];
                    }
                    else
                    {
                        if (j4 == 0) p[i] = left[o + (i2 << 1) + i - 5];
                        else p[i] = data[dst + ((i2 << 1) + i - 5) * stride + (j4 << 2) - 1];
                    }
                }
                for (var i1 = 0; i1 < 2; i1++)
                {
                    for (var j1 = 0; j1 < 4; j1++)
                    {
                        var pr = 0;
                        var t = (mode * 8 + (i1 << 2) + j1) * 7;
                        for (var i = 0; i < 7; i++) pr += taps[t + i] * p[i];
                        data[dst + ((i2 << 1) + i1) * stride + (j4 << 2) + j1] = (ushort)Clip1(Round2Signed(pr, 4));
                    }
                }
            }
        }
    }

    private void DcIntraPrediction(bool haveLeft, bool haveAbove, int log2W, int log2H, int w, int h, ushort[] data, int dst, int stride)
    {
        var above = _aboveRow;
        var left = _leftCol;
        const int o = EdgeOffset;
        int avg;
        if (haveLeft && haveAbove)
        {
            var sum = 0;
            for (var k = 0; k < h; k++) sum += left[o + k];
            for (var k = 0; k < w; k++) sum += above[o + k];
            sum += (w + h) >> 1;
            avg = sum / (w + h);
        }
        else if (haveLeft)
        {
            var sum = 0;
            for (var k = 0; k < h; k++) sum += left[o + k];
            avg = Clip1((sum + (h >> 1)) >> log2H);
        }
        else if (haveAbove)
        {
            var sum = 0;
            for (var k = 0; k < w; k++) sum += above[o + k];
            avg = Clip1((sum + (w >> 1)) >> log2W);
        }
        else
        {
            avg = 1 << (_f.BitDepth - 1);
        }
        for (var i = 0; i < h; i++) data.AsSpan(dst + i * stride, w).Fill((ushort)avg);
    }

    private static ReadOnlySpan<byte> SmoothWeights(int log2) => log2 switch
    {
        2 => Av1Tables.SmWeightsTx4x4,
        3 => Av1Tables.SmWeightsTx8x8,
        4 => Av1Tables.SmWeightsTx16x16,
        5 => Av1Tables.SmWeightsTx32x32,
        _ => Av1Tables.SmWeightsTx64x64,
    };

    private void SmoothIntraPrediction(int mode, int log2W, int log2H, int w, int h, ushort[] data, int dst, int stride)
    {
        var above = _aboveRow;
        var left = _leftCol;
        const int o = EdgeOffset;
        var wx = SmoothWeights(log2W);
        var wy = SmoothWeights(log2H);
        var bottomLeft = left[o + h - 1];
        var topRight = above[o + w - 1];
        for (var i = 0; i < h; i++)
        {
            var row = dst + i * stride;
            for (var j = 0; j < w; j++)
            {
                int v;
                if (mode == SmoothPred)
                    v = Round2(wy[i] * above[o + j] + (256 - wy[i]) * bottomLeft + wx[j] * left[o + i] + (256 - wx[j]) * topRight, 9);
                else if (mode == SmoothVPred)
                    v = Round2(wy[i] * above[o + j] + (256 - wy[i]) * bottomLeft, 8);
                else
                    v = Round2(wx[j] * left[o + i] + (256 - wx[j]) * topRight, 8);
                data[row + j] = (ushort)v;
            }
        }
    }

    private void DirectionalIntraPrediction(int plane, int x, int y, bool haveLeft, bool haveAbove, int mode, int w, int h, int maxX, int maxY, ushort[] data, int dst, int stride)
    {
        var above = _aboveRow;
        var left = _leftCol;
        const int o = EdgeOffset;
        var angleDelta = plane == 0 ? _angleDeltaY : _angleDeltaUV;
        var pAngle = Av1Tables.ModeToAngle[mode] + angleDelta * 3;
        var upsampleAbove = 0;
        var upsampleLeft = 0;
        if (_seq.EnableIntraEdgeFilter)
        {
            var filterType = 0;
            if (pAngle != 90 && pAngle != 180)
            {
                if (pAngle > 90 && pAngle < 180 && w + h >= 24)
                {
                    var s = left[o] * 5 + above[o - 1] * 6 + above[o] * 5;
                    var c = Round2(s, 4);
                    above[o - 1] = c;
                    left[o - 1] = c;
                }
                filterType = GetFilterType(plane) ? 1 : 0;
                if (haveAbove)
                {
                    var strength = IntraEdgeFilterStrength(w, h, filterType, pAngle - 90);
                    var numPx = Math.Min(w, maxX - x + 1) + (pAngle < 90 ? h : 0) + 1;
                    IntraEdgeFilter(above, numPx, strength);
                }
                if (haveLeft)
                {
                    var strength = IntraEdgeFilterStrength(w, h, filterType, pAngle - 180);
                    var numPx = Math.Min(h, maxY - y + 1) + (pAngle > 180 ? w : 0) + 1;
                    IntraEdgeFilter(left, numPx, strength);
                }
            }
            else
            {
                filterType = GetFilterType(plane) ? 1 : 0;
            }
            upsampleAbove = UseUpsample(w, h, filterType, pAngle - 90) ? 1 : 0;
            if (upsampleAbove == 1) IntraEdgeUpsample(above, w + (pAngle < 90 ? h : 0));
            upsampleLeft = UseUpsample(w, h, filterType, pAngle - 180) ? 1 : 0;
            if (upsampleLeft == 1) IntraEdgeUpsample(left, h + (pAngle > 180 ? w : 0));
        }
        int dx = 0, dy = 0;
        if (pAngle < 90) dx = Av1Tables.DrIntraDerivative[pAngle];
        else if (pAngle > 90 && pAngle < 180) dx = Av1Tables.DrIntraDerivative[180 - pAngle];
        if (pAngle > 90 && pAngle < 180) dy = Av1Tables.DrIntraDerivative[pAngle - 90];
        else if (pAngle > 180) dy = Av1Tables.DrIntraDerivative[270 - pAngle];

        if (pAngle < 90)
        {
            var maxBaseX = (w + h - 1) << upsampleAbove;
            for (var i = 0; i < h; i++)
            {
                var row = dst + i * stride;
                var idx = (i + 1) * dx;
                var shift = ((idx << upsampleAbove) >> 1) & 0x1F;
                for (var j = 0; j < w; j++)
                {
                    var b = (idx >> (6 - upsampleAbove)) + (j << upsampleAbove);
                    data[row + j] = (ushort)(b < maxBaseX
                        ? Round2(above[o + b] * (32 - shift) + above[o + b + 1] * shift, 5)
                        : above[o + maxBaseX]);
                }
            }
        }
        else if (pAngle > 90 && pAngle < 180)
        {
            for (var i = 0; i < h; i++)
            {
                var row = dst + i * stride;
                for (var j = 0; j < w; j++)
                {
                    var idx = (j << 6) - (i + 1) * dx;
                    var b = idx >> (6 - upsampleAbove);
                    if (b >= -(1 << upsampleAbove))
                    {
                        var shift = ((idx << upsampleAbove) >> 1) & 0x1F;
                        data[row + j] = (ushort)Round2(above[o + b] * (32 - shift) + above[o + b + 1] * shift, 5);
                    }
                    else
                    {
                        idx = (i << 6) - (j + 1) * dy;
                        b = idx >> (6 - upsampleLeft);
                        var shift = ((idx << upsampleLeft) >> 1) & 0x1F;
                        data[row + j] = (ushort)Round2(left[o + b] * (32 - shift) + left[o + b + 1] * shift, 5);
                    }
                }
            }
        }
        else if (pAngle > 180)
        {
            for (var j = 0; j < w; j++)
            {
                var idx = (j + 1) * dy;
                var shift = ((idx << upsampleLeft) >> 1) & 0x1F;
                var b0 = idx >> (6 - upsampleLeft);
                for (var i = 0; i < h; i++)
                {
                    var b = b0 + (i << upsampleLeft);
                    data[dst + i * stride + j] = (ushort)Round2(left[o + b] * (32 - shift) + left[o + b + 1] * shift, 5);
                }
            }
        }
        else if (pAngle == 90)
        {
            for (var i = 0; i < h; i++)
                for (var j = 0; j < w; j++)
                    data[dst + i * stride + j] = (ushort)above[o + j];
        }
        else
        {
            for (var i = 0; i < h; i++)
                data.AsSpan(dst + i * stride, w).Fill((ushort)left[o + i]);
        }
    }

    private bool GetFilterType(int plane)
    {
        var aboveSmooth = false;
        var leftSmooth = false;
        if (plane == 0 ? _availU : _availUChroma)
        {
            var r = _miRow - 1;
            var c = _miCol;
            if (plane > 0)
            {
                if (_f.SubX == 1 && (_miCol & 1) == 0) c++;
                if (_f.SubY == 1 && (_miRow & 1) == 1) r--;
            }
            aboveSmooth = IsSmooth(r, c, plane);
        }
        if (plane == 0 ? _availL : _availLChroma)
        {
            var r = _miRow;
            var c = _miCol - 1;
            if (plane > 0)
            {
                if (_f.SubX == 1 && (_miCol & 1) == 1) c--;
                if (_f.SubY == 1 && (_miRow & 1) == 0) r++;
            }
            leftSmooth = IsSmooth(r, c, plane);
        }
        return aboveSmooth || leftSmooth;
    }

    private bool IsSmooth(int row, int col, int plane)
    {
        var mi = Mi(row, col);
        int mode;
        if (plane == 0)
        {
            mode = _f.YModes[mi];
        }
        else
        {
            // RefFrames[ 0 ] > INTRA_FRAME never holds in an intra frame.
            mode = _f.UVModes[mi];
        }
        return mode is SmoothPred or SmoothVPred or SmoothHPred;
    }

    private static int IntraEdgeFilterStrength(int w, int h, int filterType, int delta)
    {
        var d = Math.Abs(delta);
        var blkWh = w + h;
        var strength = 0;
        if (filterType == 0)
        {
            if (blkWh <= 8)
            {
                if (d >= 56) strength = 1;
            }
            else if (blkWh <= 12)
            {
                if (d >= 40) strength = 1;
            }
            else if (blkWh <= 16)
            {
                if (d >= 40) strength = 1;
            }
            else if (blkWh <= 24)
            {
                if (d >= 8) strength = 1;
                if (d >= 16) strength = 2;
                if (d >= 32) strength = 3;
            }
            else if (blkWh <= 32)
            {
                strength = 1;
                if (d >= 4) strength = 2;
                if (d >= 32) strength = 3;
            }
            else
            {
                strength = 3;
            }
        }
        else
        {
            if (blkWh <= 8)
            {
                if (d >= 40) strength = 1;
                if (d >= 64) strength = 2;
            }
            else if (blkWh <= 16)
            {
                if (d >= 20) strength = 1;
                if (d >= 48) strength = 2;
            }
            else if (blkWh <= 24)
            {
                if (d >= 4) strength = 3;
            }
            else
            {
                strength = 3;
            }
        }
        return strength;
    }

    private static bool UseUpsample(int w, int h, int filterType, int delta)
    {
        var d = Math.Abs(delta);
        var blkWh = w + h;
        if (d <= 0 || d >= 40) return false;
        return filterType == 0 ? blkWh <= 16 : blkWh <= 8;
    }

    /// <summary>The intra edge filter: buf is AboveRow or LeftCol, with element -1 at <see cref="EdgeOffset"/> - 1.</summary>
    private void IntraEdgeFilter(int[] buf, int sz, int strength)
    {
        if (strength == 0) return;
        const int o = EdgeOffset;
        var edge = _edge;
        for (var i = 0; i < sz; i++) edge[i] = buf[o + i - 1];
        var k = Av1Tables.IntraEdgeKernel;
        var kb = (strength - 1) * 5;
        for (var i = 1; i < sz; i++)
        {
            var s = 0;
            for (var j = 0; j < 5; j++)
            {
                var kk = Clip3(0, sz - 1, i - 2 + j);
                s += k[kb + j] * edge[kk];
            }
            buf[o + i - 1] = (s + 8) >> 4;
        }
    }

    private void IntraEdgeUpsample(int[] buf, int numPx)
    {
        const int o = EdgeOffset;
        var dup = _edge;
        dup[0] = buf[o - 1];
        for (var i = -1; i < numPx; i++) dup[i + 2] = buf[o + i];
        dup[numPx + 2] = buf[o + numPx - 1];
        buf[o - 2] = dup[0];
        for (var i = 0; i < numPx; i++)
        {
            var s = -dup[i] + 9 * dup[i + 1] + 9 * dup[i + 2] - dup[i + 3];
            s = Clip1(Round2(s, 4));
            buf[o + 2 * i - 1] = s;
            buf[o + 2 * i] = dup[i + 2];
        }
    }

    private void PredictPalette(int plane, int startX, int startY, int x, int y, int txSz)
    {
        var w = Av1Tables.TxWidth[txSz];
        var h = Av1Tables.TxHeight[txSz];
        var palette = plane == 0 ? _paletteY : plane == 1 ? _paletteU : _paletteV;
        var map = plane == 0 ? _colorMapY : _colorMapUV;
        var p = _f.Planes[plane];
        for (var i = 0; i < h; i++)
        {
            var row = (startY + i) * p.Stride + startX;
            var mapRow = (y * 4 + i) * 64 + x * 4;
            for (var j = 0; j < w; j++) p.Data[row + j] = (ushort)palette[map[mapRow + j]];
        }
    }

    private void PredictChromaFromLuma(int plane, int startX, int startY, int txSz)
    {
        var w = Av1Tables.TxWidth[txSz];
        var h = Av1Tables.TxHeight[txSz];
        var subX = _f.SubX;
        var subY = _f.SubY;
        var alpha = plane == 1 ? _cflAlphaU : _cflAlphaV;
        var luma = _f.Planes[0];
        var chroma = _f.Planes[plane];
        Span<int> l = stackalloc int[32 * 32];
        var lumaAvg = 0;
        for (var i = 0; i < h; i++)
        {
            var lumaY = Math.Min((startY + i) << subY, _maxLumaH - (1 << subY));
            for (var j = 0; j < w; j++)
            {
                var lumaX = Math.Min((startX + j) << subX, _maxLumaW - (1 << subX));
                var t = 0;
                for (var dy = 0; dy <= subY; dy++)
                    for (var dx = 0; dx <= subX; dx++)
                        t += luma.Data[(lumaY + dy) * luma.Stride + lumaX + dx];
                var v = t << (3 - subX - subY);
                l[i * w + j] = v;
                lumaAvg += v;
            }
        }
        lumaAvg = Round2(lumaAvg, Av1Tables.TxWidthLog2[txSz] + Av1Tables.TxHeightLog2[txSz]);
        for (var i = 0; i < h; i++)
        {
            var row = (startY + i) * chroma.Stride + startX;
            for (var j = 0; j < w; j++)
            {
                var dc = chroma.Data[row + j];
                var scaledLuma = Round2Signed(alpha * (l[i * w + j] - lumaAvg), 6);
                chroma.Data[row + j] = (ushort)Clip1(dc + scaledLuma);
            }
        }
    }

    // ------------------------------------------------------------------ intra block copy

    private void ComputePrediction()
    {
        if (!_isInter) return;
        for (var plane = 0; plane < 1 + (_hasChroma ? 2 : 0); plane++)
        {
            var planeSz = GetPlaneResidualSize(_miSize, plane);
            var subX = plane > 0 ? _f.SubX : 0;
            var subY = plane > 0 ? _f.SubY : 0;
            var baseX = (_miCol >> subX) * 4;
            var baseY = (_miRow >> subY) * 4;
            // Every block of an intra frame is INTRA_FRAME, so the whole plane block is predicted from this block's
            // vector (the someUseIntra case).
            PredictIntrabc(plane, baseX, baseY, Av1Tables.Num4x4BlocksWide[planeSz] * 4, Av1Tables.Num4x4BlocksHigh[planeSz] * 4);
        }
    }

    private void PredictIntrabc(int plane, int x, int y, int w, int h)
    {
        var subX = plane > 0 ? _f.SubX : 0;
        var subY = plane > 0 ? _f.SubY : 0;
        // Motion vector scaling with the current frame as the reference: the scale is exactly 1.
        var startX = (((long)x << 4) + ((2 * _mvCol) >> subX)) * 64 + 32;
        var startY = (((long)y << 4) + ((2 * _mvRow) >> subY)) * 64 + 32;
        var lastX = ((_f.MiCols * 4 + subX) >> subX) - 1;
        var lastY = ((_f.MiRows * 4 + subY) >> subY) - 1;
        var interRound0 = _f.BitDepth == 12 ? 5 : 3;
        var interRound1 = _f.BitDepth == 12 ? 9 : 11;
        var p = _f.Planes[plane];
        var data = p.Data;
        var stride = p.Stride;
        var filters = Av1Tables.SubpelFilters;
        const int bilinear = 3;
        var intermediateHeight = h + 7;
        var intermediate = _interIntermediate;
        var y0 = (int)(startY >> 10);
        for (var r = 0; r < intermediateHeight; r++)
        {
            var srcRow = Clip3(0, lastY, y0 + r - 3) * stride;
            for (var c = 0; c < w; c++)
            {
                var pos = startX + 1024L * c;
                var f = (bilinear * 16 + (int)((pos >> 6) & 15)) * 8;
                var px = (int)(pos >> 10);
                var s = 0;
                for (var t = 0; t < 8; t++) s += filters[f + t] * data[srcRow + Clip3(0, lastX, px + t - 3)];
                intermediate[r * w + c] = Round2(s, interRound0);
            }
        }
        for (var r = 0; r < h; r++)
        {
            var pos = (startY & 1023) + 1024L * r;
            var f = (bilinear * 16 + (int)((pos >> 6) & 15)) * 8;
            var baseRow = (int)(pos >> 10);
            var dst = (y + r) * stride + x;
            for (var c = 0; c < w; c++)
            {
                var s = 0;
                for (var t = 0; t < 8; t++) s += filters[f + t] * intermediate[(baseRow + t) * w + c];
                data[dst + c] = (ushort)Clip1(Round2(s, interRound1));
            }
        }
    }
}
