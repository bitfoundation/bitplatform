namespace Bit.Bmage;

/// <summary>Integer inverse DCT (Loeffler-Ligtenberg-Moschytz, 13-bit fixed point) matching the accuracy of the reference "islow" method.</summary>
internal static class BmageIdct
{
    private const int ConstBits = 13;
    private const int Pass1Bits = 2;
    private const int F0298 = 2446, F0390 = 3196, F0541 = 4433, F0765 = 6270, F0899 = 7373, F1175 = 9633,
        F1501 = 12299, F1847 = 15137, F1961 = 16069, F2053 = 16819, F2562 = 20995, F3072 = 25172;

    [ThreadStatic] private static int[]? t_workspace;

    /// <summary>Transforms 64 dequantised coefficients (natural order) and writes 8×8 samples into <paramref name="output"/>.</summary>
    public static void Transform(ReadOnlySpan<int> input, byte[] output, int offset, int stride)
    {
        var ws = t_workspace ??= new int[64];
        // Pass 1: columns.
        for (var col = 0; col < 8; col++)
        {
            if (input[8 + col] == 0 && input[16 + col] == 0 && input[24 + col] == 0 && input[32 + col] == 0 && input[40 + col] == 0 && input[48 + col] == 0 && input[56 + col] == 0)
            {
                var dc = input[col] << Pass1Bits;
                for (var r = 0; r < 8; r++) ws[r * 8 + col] = dc;
                continue;
            }
            var z2 = input[16 + col]; var z3 = input[48 + col];
            var z1 = (z2 + z3) * F0541;
            var tmp2 = z1 + z3 * -F1847;
            var tmp3 = z1 + z2 * F0765;
            z2 = input[col]; z3 = input[32 + col];
            var tmp0 = (z2 + z3) << ConstBits;
            var tmp1 = (z2 - z3) << ConstBits;
            var tmp10 = tmp0 + tmp3; var tmp13 = tmp0 - tmp3; var tmp11 = tmp1 + tmp2; var tmp12 = tmp1 - tmp2;

            tmp0 = input[56 + col]; tmp1 = input[40 + col]; tmp2 = input[24 + col]; tmp3 = input[8 + col];
            z1 = tmp0 + tmp3; z2 = tmp1 + tmp2; z3 = tmp0 + tmp2; var z4 = tmp1 + tmp3;
            var z5 = (z3 + z4) * F1175;
            tmp0 *= F0298; tmp1 *= F2053; tmp2 *= F3072; tmp3 *= F1501;
            z1 *= -F0899; z2 *= -F2562; z3 *= -F1961; z4 *= -F0390;
            z3 += z5; z4 += z5;
            tmp0 += z1 + z3; tmp1 += z2 + z4; tmp2 += z2 + z3; tmp3 += z1 + z4;

            const int shift = ConstBits - Pass1Bits;
            const int round = 1 << (shift - 1);
            ws[col] = (tmp10 + tmp3 + round) >> shift;
            ws[56 + col] = (tmp10 - tmp3 + round) >> shift;
            ws[8 + col] = (tmp11 + tmp2 + round) >> shift;
            ws[48 + col] = (tmp11 - tmp2 + round) >> shift;
            ws[16 + col] = (tmp12 + tmp1 + round) >> shift;
            ws[40 + col] = (tmp12 - tmp1 + round) >> shift;
            ws[24 + col] = (tmp13 + tmp0 + round) >> shift;
            ws[32 + col] = (tmp13 - tmp0 + round) >> shift;
        }
        // Pass 2: rows.
        const int shift2 = ConstBits + Pass1Bits + 3;
        const int round2 = 1 << (shift2 - 1);
        for (var row = 0; row < 8; row++)
        {
            var w = row * 8;
            var o = offset + row * stride;
            if (ws[w + 1] == 0 && ws[w + 2] == 0 && ws[w + 3] == 0 && ws[w + 4] == 0 && ws[w + 5] == 0 && ws[w + 6] == 0 && ws[w + 7] == 0)
            {
                var dc = Clamp(((ws[w] + (1 << (Pass1Bits + 2))) >> (Pass1Bits + 3)) + 128);
                for (var i = 0; i < 8; i++) output[o + i] = dc;
                continue;
            }
            var z2 = ws[w + 2]; var z3 = ws[w + 6];
            var z1 = (z2 + z3) * F0541;
            var tmp2 = z1 + z3 * -F1847;
            var tmp3 = z1 + z2 * F0765;
            var tmp0 = (ws[w] + ws[w + 4]) << ConstBits;
            var tmp1 = (ws[w] - ws[w + 4]) << ConstBits;
            var tmp10 = tmp0 + tmp3; var tmp13 = tmp0 - tmp3; var tmp11 = tmp1 + tmp2; var tmp12 = tmp1 - tmp2;

            tmp0 = ws[w + 7]; tmp1 = ws[w + 5]; tmp2 = ws[w + 3]; tmp3 = ws[w + 1];
            z1 = tmp0 + tmp3; z2 = tmp1 + tmp2; z3 = tmp0 + tmp2; var z4 = tmp1 + tmp3;
            var z5 = (z3 + z4) * F1175;
            tmp0 *= F0298; tmp1 *= F2053; tmp2 *= F3072; tmp3 *= F1501;
            z1 *= -F0899; z2 *= -F2562; z3 *= -F1961; z4 *= -F0390;
            z3 += z5; z4 += z5;
            tmp0 += z1 + z3; tmp1 += z2 + z4; tmp2 += z2 + z3; tmp3 += z1 + z4;

            output[o] = Clamp(((tmp10 + tmp3 + round2) >> shift2) + 128);
            output[o + 7] = Clamp(((tmp10 - tmp3 + round2) >> shift2) + 128);
            output[o + 1] = Clamp(((tmp11 + tmp2 + round2) >> shift2) + 128);
            output[o + 6] = Clamp(((tmp11 - tmp2 + round2) >> shift2) + 128);
            output[o + 2] = Clamp(((tmp12 + tmp1 + round2) >> shift2) + 128);
            output[o + 5] = Clamp(((tmp12 - tmp1 + round2) >> shift2) + 128);
            output[o + 3] = Clamp(((tmp13 + tmp0 + round2) >> shift2) + 128);
            output[o + 4] = Clamp(((tmp13 - tmp0 + round2) >> shift2) + 128);
        }
    }

    private static byte Clamp(int v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
}
