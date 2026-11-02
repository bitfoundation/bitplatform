namespace Bit.Bmage.Codecs.Avif;

/// <summary>The colour description of an image: an 'nclx' colr property, or the AV1 sequence header's.</summary>
internal readonly record struct AvifNclx(int ColorPrimaries, int TransferCharacteristics, int MatrixCoefficients, bool FullRange);

/// <summary>
/// Converts decoded AV1 planes to 8-bit RGBA (ITU-T H.273 matrices, limited or full range), upsampling subsampled
/// chroma with the same triangle filter the JPEG decoder uses, and fills the alpha channel from an auxiliary image.
/// </summary>
internal static class AvifColor
{

    /// <summary>
    /// Writes the image's colour into <paramref name="dst"/> at (<paramref name="dstX"/>, <paramref name="dstY"/>),
    /// cropped to the buffer, with alpha 255. <paramref name="nearestChroma"/> repeats chroma samples instead of
    /// interpolating them, which is what libheif does; the tests use it to compare reconstruction exactly.
    /// </summary>
    public static void WriteRgb(Av1Image image, AvifNclx color, ImageBuffer dst, int dstX, int dstY, bool nearestChroma = false)
    {
        var w = Math.Min(image.Width, dst.Width - dstX);
        var h = Math.Min(image.Height, dst.Height - dstY);
        if (w <= 0 || h <= 0) return;
        var bitDepth = image.BitDepth;
        var maxCode = (1 << bitDepth) - 1;
        double yScale, cScale;
        int yOff, cOff;
        if (color.FullRange)
        {
            yOff = 0;
            yScale = 255.0 / maxCode;
            cOff = 1 << (bitDepth - 1);
            cScale = 255.0 / maxCode;
        }
        else
        {
            yOff = 16 << (bitDepth - 8);
            yScale = 255.0 / (219 << (bitDepth - 8));
            cOff = 128 << (bitDepth - 8);
            cScale = 255.0 / (224 << (bitDepth - 8));
        }
        var yPlane = image.Planes[0];
        if (image.MonoChrome || image.Planes.Length < 3)
        {
            var ys = (long)Math.Round(yScale * 65536);
            for (var y = 0; y < h; y++)
            {
                var src = yPlane.Data.AsSpan(y * yPlane.Stride, w);
                var row = dst.GetRow(dstY + y).Slice(dstX, w);
                for (var x = 0; x < w; x++)
                {
                    var v = Clamp8(((src[x] - yOff) * ys + 32768) >> 16);
                    row[x] = new Rgba32(v, v, v, 255);
                }
            }
            return;
        }

        var matrix = color.MatrixCoefficients;
        // Coefficients in 16.16 fixed point, already scaled to the 8-bit output range. Chroma arrives multiplied by 16
        // from the upsampler, so its products are shifted by 20.
        long yS, crR, cbG, crG, cbB;
        var identity = matrix == 0;
        var ycgco = matrix == 8;
        if (identity || ycgco)
        {
            // GBR and YCgCo carry every channel in the luma range.
            yS = (long)Math.Round(yScale * 65536);
            crR = cbG = crG = cbB = yS;
        }
        else
        {
            var (kr, kb) = KrKb(matrix, color.ColorPrimaries);
            var kg = 1 - kr - kb;
            yS = (long)Math.Round(yScale * 65536);
            crR = (long)Math.Round(cScale * 2 * (1 - kr) * 65536);
            cbB = (long)Math.Round(cScale * 2 * (1 - kb) * 65536);
            cbG = (long)Math.Round(cScale * 2 * kb * (1 - kb) / kg * 65536);
            crG = (long)Math.Round(cScale * 2 * kr * (1 - kr) / kg * 65536);
        }

        var uPlane = image.Planes[1];
        var vPlane = image.Planes[2];
        var subX = image.SubX;
        var subY = image.SubY;
        var chromaW = (image.Width + subX) >> subX;
        var chromaH = (image.Height + subY) >> subY;
        var uRow = new int[chromaW];
        var vRow = new int[chromaW];
        var uOut = new int[w];
        var vOut = new int[w];
        // Identity and YCgCo use the luma offset for every plane when the range is limited.
        var chromaOffset16 = (identity ? yOff : cOff) * 16;
        for (var y = 0; y < h; y++)
        {
            // Vertical: weights 3/4 and 1/4 (times 4), towards the neighbouring chroma row on the side of this row.
            if (subY == 1)
            {
                var c0 = Math.Min(y >> 1, chromaH - 1);
                var c1 = nearestChroma ? c0 : (y & 1) == 0 ? Math.Max(c0 - 1, 0) : Math.Min(c0 + 1, chromaH - 1);
                BlendRows(uPlane, c0, c1, chromaW, uRow);
                BlendRows(vPlane, c0, c1, chromaW, vRow);
            }
            else
            {
                CopyRow(uPlane, y, chromaW, uRow);
                CopyRow(vPlane, y, chromaW, vRow);
            }
            if (subX == 1)
            {
                for (var x = 0; x < w; x++)
                {
                    var c0 = Math.Min(x >> 1, chromaW - 1);
                    var c1 = nearestChroma ? c0 : (x & 1) == 0 ? Math.Max(c0 - 1, 0) : Math.Min(c0 + 1, chromaW - 1);
                    uOut[x] = 3 * uRow[c0] + uRow[c1];
                    vOut[x] = 3 * vRow[c0] + vRow[c1];
                }
            }
            else
            {
                for (var x = 0; x < w; x++)
                {
                    uOut[x] = uRow[x] * 4;
                    vOut[x] = vRow[x] * 4;
                }
            }

            var ySrc = yPlane.Data.AsSpan(y * yPlane.Stride, w);
            var row = dst.GetRow(dstY + y).Slice(dstX, w);
            for (var x = 0; x < w; x++)
            {
                long u = uOut[x] - chromaOffset16;
                long v = vOut[x] - chromaOffset16;
                byte r, g, b;
                if (identity)
                {
                    var yy = (long)(ySrc[x] - yOff) << 4;
                    g = Clamp8((yy * yS + (1 << 19)) >> 20);
                    b = Clamp8((u * yS + (1 << 19)) >> 20);
                    r = Clamp8((v * yS + (1 << 19)) >> 20);
                }
                else if (ycgco)
                {
                    // Y, Cg, Co: t = Y - Cg, G = Y + Cg, B = t - Co, R = t + Co.
                    var yy = (long)(ySrc[x] - yOff) << 4;
                    var t = yy - u;
                    g = Clamp8(((yy + u) * yS + (1 << 19)) >> 20);
                    b = Clamp8(((t - v) * yS + (1 << 19)) >> 20);
                    r = Clamp8(((t + v) * yS + (1 << 19)) >> 20);
                }
                else
                {
                    var yy = ((long)(ySrc[x] - yOff) * yS) << 4;
                    r = Clamp8((yy + v * crR + (1 << 19)) >> 20);
                    g = Clamp8((yy - u * cbG - v * crG + (1 << 19)) >> 20);
                    b = Clamp8((yy + u * cbB + (1 << 19)) >> 20);
                }
                row[x] = new Rgba32(r, g, b, 255);
            }
        }
    }

    /// <summary>
    /// Writes the alpha channel from a monochrome auxiliary image (its luma plane). Alpha is read as full range whatever
    /// the auxiliary image's range flag says, as libavif, libheif and the browsers read it.
    /// </summary>
    public static void WriteAlpha(Av1Image alpha, ImageBuffer dst, int dstX, int dstY)
    {
        var w = Math.Min(alpha.Width, dst.Width - dstX);
        var h = Math.Min(alpha.Height, dst.Height - dstY);
        if (w <= 0 || h <= 0) return;
        var maxCode = (1 << alpha.BitDepth) - 1;
        var plane = alpha.Planes[0];
        for (var y = 0; y < h; y++)
        {
            var src = plane.Data.AsSpan(y * plane.Stride, w);
            var row = dst.GetRowBytes(dstY + y).Slice(dstX * 4, w * 4);
            for (var x = 0; x < w; x++) row[x * 4 + 3] = (byte)((src[x] * 255 + maxCode / 2) / maxCode);
        }
    }

    /// <summary>Undoes premultiplied alpha ('prem'), so the buffer holds straight colour like every other decoder's.</summary>
    public static void Unpremultiply(ImageBuffer image)
    {
        var bytes = image.Bytes;
        for (var i = 0; i < bytes.Length; i += 4)
        {
            int a = bytes[i + 3];
            if (a is 0 or 255) continue;
            bytes[i] = (byte)Math.Min(255, (bytes[i] * 255 + a / 2) / a);
            bytes[i + 1] = (byte)Math.Min(255, (bytes[i + 1] * 255 + a / 2) / a);
            bytes[i + 2] = (byte)Math.Min(255, (bytes[i + 2] * 255 + a / 2) / a);
        }
    }

    private static void BlendRows(Av1Plane plane, int c0, int c1, int width, int[] dst)
    {
        var r0 = c0 * plane.Stride;
        var r1 = c1 * plane.Stride;
        var data = plane.Data;
        for (var x = 0; x < width; x++) dst[x] = 3 * data[r0 + x] + data[r1 + x];
    }

    private static void CopyRow(Av1Plane plane, int y, int width, int[] dst)
    {
        var r = y * plane.Stride;
        var data = plane.Data;
        for (var x = 0; x < width; x++) dst[x] = data[r + x] * 4;
    }

    private static byte Clamp8(long v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);

    /// <summary>The luma weights of the matrix coefficients, H.273 table 4. Unspecified and unknown matrices read as BT.601.</summary>
    private static (double Kr, double Kb) KrKb(int matrix, int primaries) => matrix switch
    {
        1 => (0.2126, 0.0722),
        4 => (0.30, 0.11),
        5 or 6 => (0.299, 0.114),
        7 => (0.212, 0.087),
        9 or 10 => (0.2627, 0.0593),
        12 or 13 => FromPrimaries(primaries),
        _ => (0.299, 0.114),
    };

    /// <summary>Chromaticity-derived luma weights (H.273 equations 39 to 44), for matrix coefficients 12 and 13.</summary>
    private static (double Kr, double Kb) FromPrimaries(int primaries)
    {
        // x, y of red, green, blue and white.
        double[] p = primaries switch
        {
            4 => [0.67, 0.33, 0.21, 0.71, 0.14, 0.08, 0.310, 0.316],
            5 => [0.64, 0.33, 0.29, 0.60, 0.15, 0.06, 0.3127, 0.3290],
            6 or 7 => [0.630, 0.340, 0.310, 0.595, 0.155, 0.070, 0.3127, 0.3290],
            9 => [0.708, 0.292, 0.170, 0.797, 0.131, 0.046, 0.3127, 0.3290],
            11 => [0.680, 0.320, 0.265, 0.690, 0.150, 0.060, 0.314, 0.351],
            12 => [0.680, 0.320, 0.265, 0.690, 0.150, 0.060, 0.3127, 0.3290],
            _ => [0.64, 0.33, 0.30, 0.60, 0.15, 0.06, 0.3127, 0.3290],
        };
        double xr = p[0], yr = p[1], xg = p[2], yg = p[3], xb = p[4], yb = p[5], xw = p[6], yw = p[7];
        double zr = 1 - xr - yr, zg = 1 - xg - yg, zb = 1 - xb - yb, zw = 1 - xw - yw;
        var denom = yw * (xr * (yg * zb - yb * zg) + xg * (yb * zr - yr * zb) + xb * (yr * zg - yg * zr));
        var kr = yr * (xw * (yg * zb - yb * zg) + yw * (xb * zg - xg * zb) + zw * (xg * yb - xb * yg)) / denom;
        var kb = yb * (xw * (yr * zg - yg * zr) + yw * (xg * zr - xr * zg) + zw * (xr * yg - xg * yr)) / denom;
        return (kr, kb);
    }
}
