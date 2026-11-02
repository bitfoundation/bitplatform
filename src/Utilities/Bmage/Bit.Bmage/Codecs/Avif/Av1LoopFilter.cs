namespace Bit.Bmage.Codecs.Avif;

/// <summary>The deblocking loop filter (spec 7.14), applied in place to the frame's planes.</summary>
internal static class Av1LoopFilter
{
    public static void Apply(Av1Frame f, CancellationToken cancellationToken)
    {
        var hdr = f.Hdr;
        for (var plane = 0; plane < f.NumPlanes; plane++)
        {
            if (plane != 0 && hdr.LoopFilterLevel[1 + plane] == 0) continue;
            var rowStep = plane == 0 ? 1 : 1 << f.SubY;
            var colStep = plane == 0 ? 1 : 1 << f.SubX;
            for (var pass = 0; pass < 2; pass++)
            {
                for (var row = 0; row < f.MiRows; row += rowStep)
                {
                    if ((row & 15) == 0) cancellationToken.ThrowIfCancellationRequested();
                    for (var col = 0; col < f.MiCols; col += colStep) Edge(f, plane, pass, row, col);
                }
            }
        }
    }

    private static void Edge(Av1Frame f, int plane, int pass, int row, int col)
    {
        var hdr = f.Hdr;
        var subX = plane == 0 ? 0 : f.SubX;
        var subY = plane == 0 ? 0 : f.SubY;
        var dx = pass == 0 ? 1 : 0;
        var dy = pass == 0 ? 0 : 1;
        var x = col * 4;
        var y = row * 4;
        row |= subY;
        col |= subX;
        if (x >= hdr.FrameWidth || y >= hdr.FrameHeight) return;
        if (pass == 0 && x == 0) return;
        if (pass == 1 && y == 0) return;
        var xP = x >> subX;
        var yP = y >> subY;
        var prevRow = row - (dy << subY);
        var prevCol = col - (dx << subX);
        var mi = row * f.MiCols + col;
        var miSize = f.MiSizes[mi];
        var lf = f.LoopfilterTxSizes[plane];
        var lfStride = f.LfStride[plane];
        int txSz = lf[(row >> subY) * lfStride + (col >> subX)];
        var planeSize = Av1Tables.SubsampledSize[(miSize * 2 + subX) * 2 + subY];
        var skip = f.Skips[mi] != 0;
        var isIntra = f.RefFrames0[mi] <= 0;
        int prevTxSz = lf[(prevRow >> subY) * lfStride + (prevCol >> subX)];
        bool isBlockEdge, isTxEdge;
        if (pass == 0)
        {
            isBlockEdge = xP % (Av1Tables.Num4x4BlocksWide[planeSize] * 4) == 0;
            isTxEdge = xP % Av1Tables.TxWidth[txSz] == 0;
        }
        else
        {
            isBlockEdge = yP % (Av1Tables.Num4x4BlocksHigh[planeSize] * 4) == 0;
            isTxEdge = yP % Av1Tables.TxHeight[txSz] == 0;
        }
        if (!isTxEdge || !(isBlockEdge || !skip || isIntra)) return;
        int baseSize = pass == 0
            ? Math.Min(Av1Tables.TxWidth[prevTxSz], Av1Tables.TxWidth[txSz])
            : Math.Min(Av1Tables.TxHeight[prevTxSz], Av1Tables.TxHeight[txSz]);
        var filterSize = plane == 0 ? Math.Min(16, baseSize) : Math.Min(8, baseSize);
        var lvl = Level(f, row, col, plane, pass);
        if (lvl == 0) lvl = Level(f, prevRow, prevCol, plane, pass);
        if (lvl == 0) return;
        var sharpness = hdr.LoopFilterSharpness;
        var shift = sharpness > 4 ? 2 : sharpness > 0 ? 1 : 0;
        var limit = sharpness > 0 ? Math.Clamp(lvl >> shift, 1, 9 - sharpness) : Math.Max(1, lvl >> shift);
        var blimit = 2 * (lvl + 2) + limit;
        var thresh = lvl >> 4;
        var p = f.Planes[plane];
        var step = dx != 0 ? 1 : p.Stride;
        for (var i = 0; i < 4; i++)
        {
            var sx = xP + dy * i;
            var sy = yP + dx * i;
            SampleFilter(p.Data, sy * p.Stride + sx, step, plane, limit, blimit, thresh, filterSize, f.BitDepth);
        }
    }

    private static int Level(Av1Frame f, int row, int col, int plane, int pass)
    {
        var hdr = f.Hdr;
        var mi = row * f.MiCols + col;
        var segment = f.SegmentIds[mi];
        var i = plane == 0 ? pass : plane + 1;
        var deltaLF = 0;
        if (f.DeltaLFs is { } deltas) deltaLF = deltas[mi * 4 + (hdr.DeltaLfMulti ? i : 0)];
        var lvlSeg = Math.Clamp(deltaLF + hdr.LoopFilterLevel[i], 0, 63);
        var feature = 1 + i;
        if (hdr.SegmentationEnabled && hdr.FeatureEnabled[segment, feature])
            lvlSeg = Math.Clamp(hdr.FeatureData[segment, feature] + lvlSeg, 0, 63);
        if (hdr.LoopFilterDeltaEnabled)
        {
            var nShift = lvlSeg >> 5;
            // Every block of an intra frame uses INTRA_FRAME.
            lvlSeg += hdr.LoopFilterRefDeltas[0] << nShift;
            lvlSeg = Math.Clamp(lvlSeg, 0, 63);
        }
        return lvlSeg;
    }

    private static void SampleFilter(ushort[] d, int pos, int step, int plane, int limit, int blimit, int thresh, int filterSize, int bitDepth)
    {
        int q0 = d[pos], q1 = d[pos + step], q2 = d[pos + step * 2], q3 = d[pos + step * 3];
        int p0 = d[pos - step], p1 = d[pos - step * 2], p2 = d[pos - step * 3], p3 = d[pos - step * 4];
        var bdShift = bitDepth - 8;
        var hevMask = false;
        var threshBd = thresh << bdShift;
        hevMask |= Math.Abs(p1 - p0) > threshBd;
        hevMask |= Math.Abs(q1 - q0) > threshBd;
        int filterLen;
        if (filterSize == 4) filterLen = 4;
        else if (plane != 0) filterLen = 6;
        else if (filterSize == 8) filterLen = 8;
        else filterLen = 16;
        var limitBd = limit << bdShift;
        var blimitBd = blimit << bdShift;
        var mask = Math.Abs(p1 - p0) > limitBd || Math.Abs(q1 - q0) > limitBd || Math.Abs(p0 - q0) * 2 + Math.Abs(p1 - q1) / 2 > blimitBd;
        if (filterLen >= 6) mask |= Math.Abs(p2 - p1) > limitBd || Math.Abs(q2 - q1) > limitBd;
        if (filterLen >= 8) mask |= Math.Abs(p3 - p2) > limitBd || Math.Abs(q3 - q2) > limitBd;
        if (mask) return;
        var thresholdBd = 1 << bdShift;
        var flatMask = false;
        if (filterSize >= 8)
        {
            var m = Math.Abs(p1 - p0) > thresholdBd || Math.Abs(q1 - q0) > thresholdBd || Math.Abs(p2 - p0) > thresholdBd || Math.Abs(q2 - q0) > thresholdBd;
            if (filterLen >= 8) m |= Math.Abs(p3 - p0) > thresholdBd || Math.Abs(q3 - q0) > thresholdBd;
            flatMask = !m;
        }
        var flatMask2 = false;
        if (filterSize >= 16)
        {
            int q4 = d[pos + step * 4], q5 = d[pos + step * 5], q6 = d[pos + step * 6];
            int p4 = d[pos - step * 5], p5 = d[pos - step * 6], p6 = d[pos - step * 7];
            flatMask2 = !(Math.Abs(p6 - p0) > thresholdBd || Math.Abs(q6 - q0) > thresholdBd || Math.Abs(p5 - p0) > thresholdBd
                || Math.Abs(q5 - q0) > thresholdBd || Math.Abs(p4 - p0) > thresholdBd || Math.Abs(q4 - q0) > thresholdBd);
        }
        if (filterSize == 4 || !flatMask) NarrowFilter(d, pos, step, hevMask, bitDepth);
        else if (filterSize == 8 || !flatMask2) WideFilter(d, pos, step, plane, 3);
        else WideFilter(d, pos, step, plane, 4);
    }

    private static void NarrowFilter(ushort[] d, int pos, int step, bool hevMask, int bitDepth)
    {
        var lo = -(1 << (bitDepth - 1));
        var hi = (1 << (bitDepth - 1)) - 1;
        var offset = 0x80 << (bitDepth - 8);
        int q0 = d[pos], q1 = d[pos + step], p0 = d[pos - step], p1 = d[pos - step * 2];
        var ps1 = p1 - offset;
        var ps0 = p0 - offset;
        var qs0 = q0 - offset;
        var qs1 = q1 - offset;
        var filter = hevMask ? Math.Clamp(ps1 - qs1, lo, hi) : 0;
        filter = Math.Clamp(filter + 3 * (qs0 - ps0), lo, hi);
        var filter1 = Math.Clamp(filter + 4, lo, hi) >> 3;
        var filter2 = Math.Clamp(filter + 3, lo, hi) >> 3;
        d[pos] = (ushort)(Math.Clamp(qs0 - filter1, lo, hi) + offset);
        d[pos - step] = (ushort)(Math.Clamp(ps0 + filter2, lo, hi) + offset);
        if (!hevMask)
        {
            filter = (filter1 + 1) >> 1;
            d[pos + step] = (ushort)(Math.Clamp(qs1 - filter, lo, hi) + offset);
            d[pos - step * 2] = (ushort)(Math.Clamp(ps1 + filter, lo, hi) + offset);
        }
    }

    private static void WideFilter(ushort[] d, int pos, int step, int plane, int log2Size)
    {
        int n;
        if (log2Size == 4) n = 6;
        else if (plane == 0) n = 3;
        else n = 2;
        var n2 = log2Size == 3 && plane == 0 ? 0 : 1;
        Span<int> s = stackalloc int[14];
        for (var k = -(n + 1); k <= n; k++) s[k + 7] = d[pos + k * step];
        Span<int> filtered = stackalloc int[12];
        for (var i = -n; i < n; i++)
        {
            var t = 0;
            for (var j = -n; j <= n; j++)
            {
                var p = Math.Clamp(i + j, -(n + 1), n);
                var tap = Math.Abs(j) <= n2 ? 2 : 1;
                t += s[p + 7] * tap;
            }
            filtered[i + 6] = (t + (1 << (log2Size - 1))) >> log2Size;
        }
        for (var i = -n; i < n; i++) d[pos + i * step] = (ushort)filtered[i + 6];
    }
}
