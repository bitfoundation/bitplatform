namespace Bit.Bmage.Codecs.Avif;

// The inverse transforms (spec 7.13): the 2D process around the generated 1D DCT and ADST butterflies of
// Av1InverseTransforms, with the 4-point ADST, identity and Walsh-Hadamard transforms written out here.
internal sealed partial class Av1TileDecoder
{
    private readonly int[] _dequant = new int[32 * 32];
    private readonly int[] _residual = new int[64 * 64];
    private readonly int[] _t = new int[64];

    private void InverseDct(int n, int r)
    {
        var lo = -(1 << (r - 1));
        var hi = (1 << (r - 1)) - 1;
        var t = _t.AsSpan();
        switch (n)
        {
            case 2: Av1InverseTransforms.Dct4(t, lo, hi); break;
            case 3: Av1InverseTransforms.Dct8(t, lo, hi); break;
            case 4: Av1InverseTransforms.Dct16(t, lo, hi); break;
            case 5: Av1InverseTransforms.Dct32(t, lo, hi); break;
            default: Av1InverseTransforms.Dct64(t, lo, hi); break;
        }
    }

    private void InverseAdst(int n, int r)
    {
        var lo = -(1 << (r - 1));
        var hi = (1 << (r - 1)) - 1;
        if (n == 2) InverseAdst4();
        else if (n == 3) Av1InverseTransforms.Adst8(_t, lo, hi);
        else Av1InverseTransforms.Adst16(_t, lo, hi);
    }

    private void InverseAdst4()
    {
        const long sinpi19 = 1321, sinpi29 = 2482, sinpi39 = 3344, sinpi49 = 3803;
        var t = _t;
        long s0 = sinpi19 * t[0];
        long s1 = sinpi29 * t[0];
        long s2 = sinpi39 * t[1];
        long s3 = sinpi49 * t[2];
        long s4 = sinpi19 * t[2];
        long s5 = sinpi29 * t[3];
        long s6 = sinpi49 * t[3];
        long a7 = t[0] - t[2];
        var b7 = a7 + t[3];
        s0 += s3;
        s1 -= s4;
        s3 = s2;
        s2 = sinpi39 * b7;
        s0 += s5;
        s1 -= s6;
        var x0 = s0 + s3;
        var x1 = s1 + s3;
        var x2 = s2;
        var x3 = s0 + s1 - s3;
        t[0] = (int)((x0 + 2048) >> 12);
        t[1] = (int)((x1 + 2048) >> 12);
        t[2] = (int)((x2 + 2048) >> 12);
        t[3] = (int)((x3 + 2048) >> 12);
    }

    private void InverseIdentity(int n)
    {
        var t = _t;
        var n0 = 1 << n;
        switch (n)
        {
            case 2:
                for (var i = 0; i < n0; i++) t[i] = (int)(((long)t[i] * 5793 + 2048) >> 12);
                break;
            case 3:
                for (var i = 0; i < n0; i++) t[i] *= 2;
                break;
            case 4:
                for (var i = 0; i < n0; i++) t[i] = (int)(((long)t[i] * 11586 + 2048) >> 12);
                break;
            default:
                for (var i = 0; i < n0; i++) t[i] *= 4;
                break;
        }
    }

    private void InverseWht(int shift)
    {
        var t = _t;
        var a = t[0] >> shift;
        var c = t[1] >> shift;
        var d = t[2] >> shift;
        var b = t[3] >> shift;
        a += c;
        d -= b;
        var e = (a - d) >> 1;
        b = e - b;
        c = e - c;
        a -= b;
        d += c;
        t[0] = a;
        t[1] = b;
        t[2] = c;
        t[3] = d;
    }

    private static bool RowIsDct(int txType) => txType is 0 or 1 or 4 or 11;

    private static bool RowIsAdst(int txType) => txType is 2 or 3 or 5 or 6 or 7 or 8 or 13 or 15;

    private static bool ColIsDct(int txType) => txType is 0 or 2 or 5 or 10;

    private static bool ColIsAdst(int txType) => txType is 1 or 3 or 4 or 6 or 7 or 8 or 12 or 14;

    /// <summary>
    /// The 2D inverse transform of the dequantized coefficients (the first <paramref name="rows"/> by
    /// <paramref name="cols"/> of a <paramref name="tw"/>-wide block, the rest being zero) into <see cref="_residual"/>.
    /// </summary>
    private void InverseTransform2D(int txSz, int txType, int rows, int cols, int tw)
    {
        var log2W = Av1Tables.TxWidthLog2[txSz];
        var log2H = Av1Tables.TxHeightLog2[txSz];
        var w = 1 << log2W;
        var h = 1 << log2H;
        var rowShift = _lossless ? 0 : Av1Tables.TransformRowShift[txSz];
        var colShift = _lossless ? 0 : 4;
        var rowClampRange = _f.BitDepth + 8;
        var colClampRange = Math.Max(_f.BitDepth + 6, 16);
        var colLo = -(1 << (colClampRange - 1));
        var colHi = (1 << (colClampRange - 1)) - 1;
        var rect = Math.Abs(log2W - log2H) == 1;
        var t = _t;
        var residual = _residual;
        var dequant = _dequant;

        // A DCT of a vector that is zero but for its first entry is that entry times cos(pi/4) in every position (each
        // butterfly passes it on unchanged), which covers the very common blocks with one coefficient per row or column.
        var rowDcOnly = !_lossless && cols == 1 && RowIsDct(txType);
        for (var i = 0; i < h; i++)
        {
            var dst = i * w;
            if (i >= rows)
            {
                Array.Clear(residual, dst, w);
                continue;
            }
            if (rowDcOnly)
            {
                var dc = dequant[i * tw];
                if (rect) dc = (int)(((long)dc * 2896 + 2048) >> 12);
                var v = Round2((int)(((long)dc * 2896 + 2048) >> 12), rowShift);
                residual.AsSpan(dst, w).Fill(v < colLo ? colLo : v > colHi ? colHi : v);
                continue;
            }
            Array.Clear(t, 0, w);
            for (var j = 0; j < cols; j++) t[j] = dequant[i * tw + j];
            if (rect)
                for (var j = 0; j < cols; j++) t[j] = (int)(((long)t[j] * 2896 + 2048) >> 12);
            if (_lossless) InverseWht(2);
            else if (RowIsDct(txType)) InverseDct(log2W, rowClampRange);
            else if (RowIsAdst(txType)) InverseAdst(log2W, rowClampRange);
            else InverseIdentity(log2W);
            for (var j = 0; j < w; j++)
            {
                var v = Round2(t[j], rowShift);
                residual[dst + j] = v < colLo ? colLo : v > colHi ? colHi : v;
            }
        }

        if (!_lossless && rows == 1 && ColIsDct(txType))
        {
            for (var j = 0; j < w; j++)
            {
                var v = Round2((int)(((long)residual[j] * 2896 + 2048) >> 12), colShift);
                for (var i = 0; i < h; i++) residual[i * w + j] = v;
            }
            return;
        }
        for (var j = 0; j < w; j++)
        {
            for (var i = 0; i < h; i++) t[i] = residual[i * w + j];
            if (_lossless) InverseWht(0);
            else if (ColIsDct(txType)) InverseDct(log2H, colClampRange);
            else if (ColIsAdst(txType)) InverseAdst(log2H, colClampRange);
            else InverseIdentity(log2H);
            for (var i = 0; i < h; i++) residual[i * w + j] = Round2(t[i], colShift);
        }
    }
}
