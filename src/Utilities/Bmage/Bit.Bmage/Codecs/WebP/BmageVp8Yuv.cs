namespace Bit.Bmage;

/// <summary>Converts a VP8 frame's limited range BT.601 YUV 4:2:0 to RGBA, interpolating chroma bilinearly.</summary>
internal static class BmageVp8Yuv
{
    public static void ToRgba(BmageVp8Frame frame, byte[]? alpha, BmageImageBuffer image, int x0, int y0, CancellationToken ct)
    {
        var width = frame.Width;
        var height = frame.Height;
        var chromaWidth = (width + 1) >> 1;
        var chromaHeight = (height + 1) >> 1;
        var u = new int[chromaWidth];
        var v = new int[chromaWidth];
        for (var y = 0; y < height; y++)
        {
            if ((y & 63) == 0) ct.ThrowIfCancellationRequested();
            // Chroma samples sit between luma rows and columns, so every output pixel weighs its four nearest samples
            // 9:3:3:1. The vertical pass is done once per row at four times the precision.
            var near = y >> 1;
            var far = Math.Clamp((y & 1) != 0 ? near + 1 : near - 1, 0, chromaHeight - 1);
            var un = frame.U.AsSpan(near * frame.UVStride, chromaWidth);
            var uf = frame.U.AsSpan(far * frame.UVStride, chromaWidth);
            var vn = frame.V.AsSpan(near * frame.UVStride, chromaWidth);
            var vf = frame.V.AsSpan(far * frame.UVStride, chromaWidth);
            for (var c = 0; c < chromaWidth; c++)
            {
                u[c] = 3 * un[c] + uf[c];
                v[c] = 3 * vn[c] + vf[c];
            }

            var luma = frame.Y.AsSpan(y * frame.YStride, width);
            var dst = image.GetRow(y0 + y).Slice(x0, width);
            var a = alpha is null ? default : alpha.AsSpan(y * width, width);
            for (var x = 0; x < width; x++)
            {
                var cn = x >> 1;
                var cf = (x & 1) != 0 ? Math.Min(cn + 1, chromaWidth - 1) : Math.Max(cn - 1, 0);
                var uu = (3 * u[cn] + u[cf] + 8) >> 4;
                var vv = (3 * v[cn] + v[cf] + 8) >> 4;
                dst[x] = ToRgb(luma[x], uu, vv, alpha is null ? (byte)255 : a[x]);
            }
        }
    }

    // BT.601 with luma in 16..235 and chroma in 16..240, in 14 bit fixed point.
    internal static BmageRgba32 ToRgb(int y, int u, int v, byte a)
    {
        var luma = 19077 * (y - 16) + 8192;
        var r = (luma + 26149 * (v - 128)) >> 14;
        var g = (luma - 6419 * (u - 128) - 13320 * (v - 128)) >> 14;
        var b = (luma + 33050 * (u - 128)) >> 14;
        return new BmageRgba32(BmageVp8Decoder.Clip8(r), BmageVp8Decoder.Clip8(g), BmageVp8Decoder.Clip8(b), a);
    }
}
