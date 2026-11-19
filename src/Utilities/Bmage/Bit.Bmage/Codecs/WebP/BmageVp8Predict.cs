namespace Bit.Bmage;

/// <summary>Intra predictors of VP8, writing into a macroblock work area laid out <see cref="BmageVp8Decoder.Bps"/> bytes per row.</summary>
internal static class BmageVp8Predict
{
    private const int Bps = BmageVp8Decoder.Bps;

    public static void Luma16(int mode, Span<byte> b, int o, bool hasLeft, bool hasTop) => Block(mode, b, o, 16, hasLeft, hasTop);

    public static void Chroma8(int mode, Span<byte> b, int o, bool hasLeft, bool hasTop) => Block(mode, b, o, 8, hasLeft, hasTop);

    private static void Block(int mode, Span<byte> b, int o, int size, bool hasLeft, bool hasTop)
    {
        switch (mode)
        {
            case BmageVp8Decoder.BDc:
            {
                var shift = size == 16 ? 4 : 3;
                int dc;
                if (hasTop && hasLeft)
                {
                    var sum = 0;
                    for (var i = 0; i < size; i++) sum += b[o - Bps + i] + b[o + i * Bps - 1];
                    dc = (sum + size) >> (shift + 1);
                }
                else if (hasTop || hasLeft)
                {
                    var sum = 0;
                    for (var i = 0; i < size; i++) sum += hasTop ? b[o - Bps + i] : b[o + i * Bps - 1];
                    dc = (sum + (size >> 1)) >> shift;
                }
                else dc = 128;
                for (var y = 0; y < size; y++) b.Slice(o + y * Bps, size).Fill((byte)dc);
                break;
            }
            case BmageVp8Decoder.BTm:
                TrueMotion(b, o, size);
                break;
            case BmageVp8Decoder.BVe:
                for (var y = 0; y < size; y++) b.Slice(o - Bps, size).CopyTo(b.Slice(o + y * Bps, size));
                break;
            default: // horizontal
                for (var y = 0; y < size; y++) b.Slice(o + y * Bps, size).Fill(b[o + y * Bps - 1]);
                break;
        }
    }

    private static void TrueMotion(Span<byte> b, int o, int size)
    {
        int topLeft = b[o - Bps - 1];
        for (var y = 0; y < size; y++)
        {
            var left = b[o + y * Bps - 1] - topLeft;
            var row = o + y * Bps;
            for (var x = 0; x < size; x++) b[row + x] = BmageVp8Decoder.Clip8(b[o - Bps + x] + left);
        }
    }

    private static byte Avg3(int x, int y, int z) => (byte)((x + 2 * y + z + 2) >> 2);

    private static byte Avg2(int x, int y) => (byte)((x + y + 1) >> 1);

    /// <summary>Predicts a 4x4 block at <paramref name="o"/> from the eight pixels above it, the four to its left and the corner.</summary>
    public static void Luma4(int mode, Span<byte> b, int o)
    {
        int p = b[o - Bps - 1];
        int a0 = b[o - Bps], a1 = b[o - Bps + 1], a2 = b[o - Bps + 2], a3 = b[o - Bps + 3];
        int a4 = b[o - Bps + 4], a5 = b[o - Bps + 5], a6 = b[o - Bps + 6], a7 = b[o - Bps + 7];
        int l0 = b[o - 1], l1 = b[o + Bps - 1], l2 = b[o + 2 * Bps - 1], l3 = b[o + 3 * Bps - 1];
        Span<byte> r = stackalloc byte[16]; // r[row * 4 + column]
        switch (mode)
        {
            case BmageVp8Decoder.BDc:
                r.Fill((byte)((a0 + a1 + a2 + a3 + l0 + l1 + l2 + l3 + 4) >> 3));
                break;
            case BmageVp8Decoder.BTm:
                TrueMotion(b, o, 4);
                return;
            case BmageVp8Decoder.BVe:
            {
                byte c0 = Avg3(p, a0, a1), c1 = Avg3(a0, a1, a2), c2 = Avg3(a1, a2, a3), c3 = Avg3(a2, a3, a4);
                for (var y = 0; y < 4; y++) { r[y * 4] = c0; r[y * 4 + 1] = c1; r[y * 4 + 2] = c2; r[y * 4 + 3] = c3; }
                break;
            }
            case BmageVp8Decoder.BHe:
                r[..4].Fill(Avg3(p, l0, l1));
                r[4..8].Fill(Avg3(l0, l1, l2));
                r[8..12].Fill(Avg3(l1, l2, l3));
                r[12..].Fill(Avg3(l2, l3, l3));
                break;
            case BmageVp8Decoder.BLd:
                r[0] = Avg3(a0, a1, a2);
                r[1] = r[4] = Avg3(a1, a2, a3);
                r[2] = r[5] = r[8] = Avg3(a2, a3, a4);
                r[3] = r[6] = r[9] = r[12] = Avg3(a3, a4, a5);
                r[7] = r[10] = r[13] = Avg3(a4, a5, a6);
                r[11] = r[14] = Avg3(a5, a6, a7);
                r[15] = Avg3(a6, a7, a7);
                break;
            case BmageVp8Decoder.BRd:
                r[12] = Avg3(l3, l2, l1);
                r[13] = r[8] = Avg3(l2, l1, l0);
                r[14] = r[9] = r[4] = Avg3(l1, l0, p);
                r[15] = r[10] = r[5] = r[0] = Avg3(l0, p, a0);
                r[11] = r[6] = r[1] = Avg3(p, a0, a1);
                r[7] = r[2] = Avg3(a0, a1, a2);
                r[3] = Avg3(a1, a2, a3);
                break;
            case BmageVp8Decoder.BVr:
                r[12] = Avg3(l2, l1, l0);
                r[8] = Avg3(l1, l0, p);
                r[13] = r[4] = Avg3(l0, p, a0);
                r[9] = r[0] = Avg2(p, a0);
                r[14] = r[5] = Avg3(p, a0, a1);
                r[10] = r[1] = Avg2(a0, a1);
                r[15] = r[6] = Avg3(a0, a1, a2);
                r[11] = r[2] = Avg2(a1, a2);
                r[7] = Avg3(a1, a2, a3);
                r[3] = Avg2(a2, a3);
                break;
            case BmageVp8Decoder.BVl:
                r[0] = Avg2(a0, a1);
                r[4] = Avg3(a0, a1, a2);
                r[8] = r[1] = Avg2(a1, a2);
                r[5] = r[12] = Avg3(a1, a2, a3);
                r[9] = r[2] = Avg2(a2, a3);
                r[13] = r[6] = Avg3(a2, a3, a4);
                r[10] = r[3] = Avg2(a3, a4);
                r[14] = r[7] = Avg3(a3, a4, a5);
                r[11] = Avg3(a4, a5, a6);
                r[15] = Avg3(a5, a6, a7);
                break;
            case BmageVp8Decoder.BHd:
                r[12] = Avg2(l3, l2);
                r[13] = Avg3(l3, l2, l1);
                r[8] = r[14] = Avg2(l2, l1);
                r[9] = r[15] = Avg3(l2, l1, l0);
                r[10] = r[4] = Avg2(l1, l0);
                r[11] = r[5] = Avg3(l1, l0, p);
                r[6] = r[0] = Avg2(l0, p);
                r[7] = r[1] = Avg3(l0, p, a0);
                r[2] = Avg3(p, a0, a1);
                r[3] = Avg3(a0, a1, a2);
                break;
            default: // horizontal up
                r[0] = Avg2(l0, l1);
                r[1] = Avg3(l0, l1, l2);
                r[2] = r[4] = Avg2(l1, l2);
                r[3] = r[5] = Avg3(l1, l2, l3);
                r[6] = r[8] = Avg2(l2, l3);
                r[7] = r[9] = Avg3(l2, l3, l3);
                r[10] = r[11] = r[12] = r[13] = r[14] = r[15] = (byte)l3;
                break;
        }
        for (var y = 0; y < 4; y++) r.Slice(y * 4, 4).CopyTo(b[(o + y * Bps)..]);
    }
}
