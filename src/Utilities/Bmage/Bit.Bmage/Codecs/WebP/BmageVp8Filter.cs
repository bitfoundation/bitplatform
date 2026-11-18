namespace Bit.Bmage;

/// <summary>The VP8 loop filters (RFC 6386 section 15), along one edge of <c>count</c> pixels.</summary>
/// <remarks>
/// <c>step</c> crosses the edge (1 for a vertical edge, the stride for a horizontal one); <c>advance</c> moves along it.
/// </remarks>
internal static class BmageVp8Filter
{
    private static int SignedClamp(int v) => v < -128 ? -128 : v > 127 ? 127 : v;

    public static void SimpleEdge(byte[] px, int pos, int step, int advance, int limit)
    {
        for (var i = 0; i < 16; i++, pos += advance)
        {
            int p1 = px[pos - 2 * step], p0 = px[pos - step], q0 = px[pos], q1 = px[pos + step];
            if (Math.Abs(p0 - q0) * 2 + (Math.Abs(p1 - q1) >> 1) <= limit) Common(px, pos, step, outerTaps: true);
        }
    }

    public static void MacroblockEdge(byte[] px, int pos, int step, int advance, int count, int edgeLimit, int interior, int hevThreshold)
    {
        for (var i = 0; i < count; i++, pos += advance)
        {
            if (!NormalThreshold(px, pos, step, edgeLimit, interior)) continue;
            if (HighEdgeVariance(px, pos, step, hevThreshold)) Common(px, pos, step, outerTaps: true);
            else
            {
                int p2 = px[pos - 3 * step], p1 = px[pos - 2 * step], p0 = px[pos - step];
                int q0 = px[pos], q1 = px[pos + step], q2 = px[pos + 2 * step];
                var w = SignedClamp(SignedClamp(p1 - q1) + 3 * (q0 - p0));
                var a = (27 * w + 63) >> 7;
                px[pos - step] = BmageVp8Decoder.Clip8(p0 + a);
                px[pos] = BmageVp8Decoder.Clip8(q0 - a);
                a = (18 * w + 63) >> 7;
                px[pos - 2 * step] = BmageVp8Decoder.Clip8(p1 + a);
                px[pos + step] = BmageVp8Decoder.Clip8(q1 - a);
                a = (9 * w + 63) >> 7;
                px[pos - 3 * step] = BmageVp8Decoder.Clip8(p2 + a);
                px[pos + 2 * step] = BmageVp8Decoder.Clip8(q2 - a);
            }
        }
    }

    public static void SubblockEdge(byte[] px, int pos, int step, int advance, int count, int edgeLimit, int interior, int hevThreshold)
    {
        for (var i = 0; i < count; i++, pos += advance)
            if (NormalThreshold(px, pos, step, edgeLimit, interior))
                Common(px, pos, step, HighEdgeVariance(px, pos, step, hevThreshold));
    }

    private static bool NormalThreshold(byte[] px, int pos, int step, int edgeLimit, int interior)
    {
        int p3 = px[pos - 4 * step], p2 = px[pos - 3 * step], p1 = px[pos - 2 * step], p0 = px[pos - step];
        int q0 = px[pos], q1 = px[pos + step], q2 = px[pos + 2 * step], q3 = px[pos + 3 * step];
        return Math.Abs(p0 - q0) * 2 + (Math.Abs(p1 - q1) >> 1) <= 2 * edgeLimit + interior
            && Math.Abs(p3 - p2) <= interior && Math.Abs(p2 - p1) <= interior && Math.Abs(p1 - p0) <= interior
            && Math.Abs(q3 - q2) <= interior && Math.Abs(q2 - q1) <= interior && Math.Abs(q1 - q0) <= interior;
    }

    private static bool HighEdgeVariance(byte[] px, int pos, int step, int threshold)
        => Math.Abs(px[pos - 2 * step] - px[pos - step]) > threshold || Math.Abs(px[pos + step] - px[pos]) > threshold;

    private static void Common(byte[] px, int pos, int step, bool outerTaps)
    {
        int p1 = px[pos - 2 * step], p0 = px[pos - step], q0 = px[pos], q1 = px[pos + step];
        var a = 3 * (q0 - p0);
        if (outerTaps) a += SignedClamp(p1 - q1);
        a = SignedClamp(a);
        var f1 = (a + 4 > 127 ? 127 : a + 4) >> 3;
        var f2 = (a + 3 > 127 ? 127 : a + 3) >> 3;
        px[pos - step] = BmageVp8Decoder.Clip8(p0 + f2);
        px[pos] = BmageVp8Decoder.Clip8(q0 - f1);
        if (!outerTaps)
        {
            a = (f1 + 1) >> 1;
            px[pos - 2 * step] = BmageVp8Decoder.Clip8(p1 + a);
            px[pos + step] = BmageVp8Decoder.Clip8(q1 - a);
        }
    }
}
