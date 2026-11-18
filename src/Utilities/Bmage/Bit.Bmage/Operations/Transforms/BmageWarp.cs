using System.Buffers;
using System.Drawing;
using System.Numerics;
using System.Runtime.Intrinsics.Wasm;

namespace Bit.Bmage;

/// <summary>Shared inverse-mapping warp engine used by affine and perspective operations.</summary>
internal static class BmageWarp
{
    private static Vector4 Premultiplied(BmageRgba32 background)
    {
        const float inv = 1f / 255f;
        return new Vector4(
            background.R * inv * background.A * inv,
            background.G * inv * background.A * inv,
            background.B * inv * background.A * inv,
            background.A * inv);
    }

    /// <summary>
    /// Fills <paramref name="output"/> by mapping each destination pixel centre through <paramref name="inverse"/> to a source
    /// location and sampling with premultiplied alpha. Pixels mapping outside the source get <paramref name="background"/>.
    /// </summary>
    /// <remarks>
    /// The general path, for mappings that are not affine. Affine transforms should use <see cref="RunAffine"/>.
    /// </remarks>
    public static void Run(BmageImageBuffer source, BmageImageBuffer output, Func<Vector2, Vector2> inverse, BmageWarpSampling sampling, BmageRgba32 background, BmageOperationContext context)
    {
        int sw = source.Width, sh = source.Height;
        var bg = Premultiplied(background);

        // Every output pixel is mapped and sampled on its own, so bands of rows run side by side.
        BmageParallelRows.For(context, 0, output.Height, output.Width * 4L, (start, end, band) =>
        {
            var src = source.Pixels;
            for (var y = start; y < end; y++)
            {
                if (((y - start) & 15) == 0) { band.ThrowIfCancellationRequested(); band.ReportProgress(y / (double)output.Height); }
                var dst = output.GetRow(y);
                for (var x = 0; x < output.Width; x++)
                {
                    var p = inverse(new Vector2(x + 0.5f, y + 0.5f));
                    dst[x] = Store(Sample(src, sw, sh, p.X - 0.5f, p.Y - 0.5f, bg, sampling));
                }
            }
        });
        context.ReportProgress(1);
    }

    /// <summary>
    /// The affine fast path. Exploits the fact that an affine map advances by a constant vector along a scanline, so
    /// the source position costs a multiply-add per pixel instead of a delegate call and a full matrix transform, and
    /// the sampling mode is resolved once per image rather than branched on per pixel. The reassociated arithmetic can
    /// put a pixel on the other side of a rounding boundary, so results match <see cref="Run"/> to within one unit per
    /// channel rather than exactly.
    /// </summary>
    /// <remarks>
    /// The filtering samplers go on to <see cref="RunAffineTiled"/>, which on a 12 megapixel image rotated 37 degrees
    /// took bilinear from 286 ms to 248 ms and bicubic from 718 ms to 337 ms on desktop .NET. See
    /// <see cref="BmageWarpSampling"/> for the trade-off.
    /// </remarks>
    public static void RunAffine(BmageImageBuffer source, BmageImageBuffer output, Matrix3x2 inverse, BmageWarpSampling sampling, BmageRgba32 background, BmageOperationContext context)
    {
        if (sampling != BmageWarpSampling.NearestNeighbor)
        {
            RunAffineTiled(source, output, inverse, sampling, background, context);
            return;
        }
        int sw = source.Width, sh = source.Height;
        var bg = Premultiplied(background);
        // Derivative of the mapped position with respect to the destination column.
        var stepX = inverse.M11;
        var stepY = inverse.M12;
        var width = output.Width;

        BmageParallelRows.For(context, 0, output.Height, width, (start, end, band) =>
        {
            var src = source.Pixels;
            for (var y = start; y < end; y++)
            {
                if (((y - start) & 15) == 0) { band.ThrowIfCancellationRequested(); band.ReportProgress(y / (double)output.Height); }
                var dst = output.GetRow(y);
                // Recomputed per row rather than accumulated, so a long row cannot drift.
                var origin = Vector2.Transform(new Vector2(0.5f, y + 0.5f), inverse);
                var baseX = origin.X - 0.5f;
                var baseY = origin.Y - 0.5f;
                for (var x = 0; x < width; x++)
                {
                    var ix = (int)MathF.Floor(baseX + x * stepX + 0.5f);
                    var iy = (int)MathF.Floor(baseY + x * stepY + 0.5f);
                    dst[x] = Store((uint)ix < (uint)sw && (uint)iy < (uint)sh ? Load(src[iy * sw + ix]) : bg);
                }
            }
        });
        context.ReportProgress(1);
    }

    /// <summary>Output tiles are this many pixels square; the source patch each one reads stays small enough to be cheap.</summary>
    private const int Tile = 32;

    /// <summary>
    /// <see cref="RunAffine"/> for the filtering samplers, one output tile at a time. Each tile first premultiplies the
    /// source pixels its samples can reach into a patch, with the background where they fall outside the source, so
    /// every source pixel is converted about twice instead of once per tap: sixteen times for bicubic. The positions,
    /// weights and sums are computed exactly as before, so the pixels are the same.
    /// </summary>
    private static void RunAffineTiled(BmageImageBuffer source, BmageImageBuffer output, Matrix3x2 inverse, BmageWarpSampling sampling, BmageRgba32 background, BmageOperationContext context)
    {
        var height = output.Height;
        // Bands of whole rows of tiles: each tile reads the source and writes only its own pixels.
        var tileRows = (height + Tile - 1) / Tile;
        BmageParallelRows.For(context, 0, tileRows, (long)output.Width * Tile, (start, end, band) =>
            RunAffineTileRows(source, output, inverse, sampling, background, start, end, band));
        context.ReportProgress(1);
    }

    /// <summary>Warps the output tiles in rows of tiles <paramref name="start"/> to <paramref name="end"/>.</summary>
    private static void RunAffineTileRows(BmageImageBuffer source, BmageImageBuffer output, Matrix3x2 inverse, BmageWarpSampling sampling, BmageRgba32 background, int start, int end, BmageOperationContext context)
    {
        int sw = source.Width, sh = source.Height;
        var bg = Premultiplied(background);
        var stepX = inverse.M11;
        var stepY = inverse.M12;
        var width = output.Width;
        var height = output.Height;
        var bicubic = sampling == BmageWarpSampling.Bicubic;
        var pool = ArrayPool<Vector4>.Shared;
        var patch = pool.Rent(256);
        var origins = new Vector2[Tile];
        try
        {
            for (var ty = start * Tile; ty < Math.Min(height, end * Tile); ty += Tile)
            {
                context.ThrowIfCancellationRequested();
                context.ReportProgress(ty / (double)height);
                var rows = Math.Min(Tile, height - ty);
                for (var r = 0; r < rows; r++)
                {
                    // Recomputed per row rather than accumulated, so a long row cannot drift.
                    var origin = Vector2.Transform(new Vector2(0.5f, ty + r + 0.5f), inverse);
                    origins[r] = new Vector2(origin.X - 0.5f, origin.Y - 0.5f);
                }
                for (var tx = 0; tx < width; tx += Tile)
                {
                    var columns = Math.Min(Tile, width - tx);
                    // The tile's samples lie within the box its corner samples span: an affine map takes the tile's
                    // rectangle to a parallelogram. A sample whose taps would all miss the source is the background
                    // without reading anything, so the patch never needs to reach further than three pixels outside.
                    float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                    foreach (var r in (ReadOnlySpan<int>)[0, rows - 1])
                        foreach (var c in (ReadOnlySpan<int>)[tx, tx + columns - 1])
                        {
                            var sx = origins[r].X + c * stepX;
                            var sy = origins[r].Y + c * stepY;
                            minX = MathF.Min(minX, sx); maxX = MathF.Max(maxX, sx);
                            minY = MathF.Min(minY, sy); maxY = MathF.Max(maxY, sy);
                        }
                    // A non-finite matrix, or a corner far outside the source, cannot bound a patch in int: such a tile
                    // samples directly, as the whole warp used to.
                    const float far = 1 << 24;
                    if (!(minX > -far && maxX < far && minY > -far && maxY < far))
                    {
                        SampleTileDirectly(source, output, origins, tx, ty, columns, rows, stepX, stepY, bicubic, bg);
                        continue;
                    }
                    var px0 = Math.Max(-3, (int)MathF.Floor(minX) - 2);
                    var py0 = Math.Max(-3, (int)MathF.Floor(minY) - 2);
                    var px1 = Math.Min(sw + 3, (int)MathF.Floor(maxX) + 4);
                    var py1 = Math.Min(sh + 3, (int)MathF.Floor(maxY) + 4);
                    var pw = Math.Max(0, px1 - px0);
                    var ph = Math.Max(0, py1 - py0);
                    if ((long)pw * ph > 16L * rows * columns)
                    {
                        // A strong reduction spreads a tile's samples thinly over a large area; converting all of it
                        // would cost more than the taps themselves.
                        SampleTileDirectly(source, output, origins, tx, ty, columns, rows, stepX, stepY, bicubic, bg);
                        continue;
                    }
                    if ((long)pw * ph > patch.Length)
                    {
                        pool.Return(patch);
                        patch = pool.Rent(pw * ph);
                    }
                    FillPatch(source, patch, px0, py0, pw, ph, bg);

                    for (var r = 0; r < rows; r++)
                    {
                        var dst = output.GetRow(ty + r).Slice(tx, columns);
                        var baseX = origins[r].X;
                        var baseY = origins[r].Y;
                        if (bicubic)
                            for (var c = 0; c < columns; c++)
                                dst[c] = Store(SampleBicubic(patch, px0, py0, pw, sw, sh, baseX + (tx + c) * stepX, baseY + (tx + c) * stepY, bg));
                        else
                            for (var c = 0; c < columns; c++)
                                dst[c] = Store(SampleBilinear(patch, px0, py0, pw, sw, sh, baseX + (tx + c) * stepX, baseY + (tx + c) * stepY, bg));
                    }
                }
            }
        }
        finally
        {
            pool.Return(patch);
        }
    }

    /// <summary>One output tile sampled straight from the source, tap by tap.</summary>
    private static void SampleTileDirectly(BmageImageBuffer source, BmageImageBuffer output, Vector2[] origins, int tx, int ty, int columns, int rows, float stepX, float stepY, bool bicubic, Vector4 bg)
    {
        int sw = source.Width, sh = source.Height;
        var src = source.Pixels;
        for (var r = 0; r < rows; r++)
        {
            var dst = output.GetRow(ty + r).Slice(tx, columns);
            for (var c = 0; c < columns; c++)
            {
                var sx = origins[r].X + (tx + c) * stepX;
                var sy = origins[r].Y + (tx + c) * stepY;
                dst[c] = Store(bicubic ? SampleBicubic(src, sw, sh, sx, sy, bg) : SampleBilinear(src, sw, sh, sx, sy, bg));
            }
        }
    }

    /// <summary>The premultiplied pixels of a source rectangle, with the background wherever it lies outside the source.</summary>
    private static void FillPatch(BmageImageBuffer source, Vector4[] patch, int px0, int py0, int pw, int ph, Vector4 bg)
    {
        int sw = source.Width, sh = source.Height;
        var inside0 = Math.Clamp(-px0, 0, pw);
        var inside1 = Math.Clamp(sw - px0, inside0, pw);
        for (var y = 0; y < ph; y++)
        {
            var row = patch.AsSpan(y * pw, pw);
            var sy = py0 + y;
            if ((uint)sy >= (uint)sh)
            {
                row.Fill(bg);
                continue;
            }
            row[..inside0].Fill(bg);
            if (inside1 > inside0) BmagePixelRows.LoadPremultiplied(source.GetRow(sy).Slice(px0 + inside0, inside1 - inside0), row[inside0..inside1]);
            row[inside1..].Fill(bg);
        }
    }

    // The samplers below are the ones further down, reading taps from a patch rather than fetching and premultiplying them.

    private static Vector4 SampleBilinear(Vector4[] patch, int px0, int py0, int pw, int sw, int sh, float sx, float sy, in Vector4 bg)
    {
        if (sx <= -1f || sy <= -1f || sx >= sw || sy >= sh) return bg;
        var x0 = (int)MathF.Floor(sx);
        var y0 = (int)MathF.Floor(sy);
        var fx = sx - x0;
        var fy = sy - y0;
        var i = (y0 - py0) * pw + (x0 - px0);
        var c00 = patch[i];
        var c10 = patch[i + 1];
        var c01 = patch[i + pw];
        var c11 = patch[i + pw + 1];
        // Vectors rather than floats as factors: identical products, several times faster on the interpreter.
        var tx = new Vector4(fx);
        var top = c00 + (c10 - c00) * tx;
        var bottom = c01 + (c11 - c01) * tx;
        return top + (bottom - top) * new Vector4(fy);
    }

    private static Vector4 SampleBicubic(Vector4[] patch, int px0, int py0, int pw, int sw, int sh, float sx, float sy, in Vector4 bg)
    {
        if (sx <= -2f || sy <= -2f || sx >= sw + 1 || sy >= sh + 1) return bg;
        var x0 = (int)MathF.Floor(sx);
        var y0 = (int)MathF.Floor(sy);
        var fx = sx - x0;
        var fy = sy - y0;
        Vector4 wx0 = new(Cubic(fx + 1)), wx1 = new(Cubic(fx)), wx2 = new(Cubic(fx - 1)), wx3 = new(Cubic(fx - 2));
        Vector4 wy0 = new(Cubic(fy + 1)), wy1 = new(Cubic(fy)), wy2 = new(Cubic(fy - 1)), wy3 = new(Cubic(fy - 2));
        var i = (y0 - 1 - py0) * pw + (x0 - 1 - px0);
        var acc = Vector4.Zero;
        acc += BicubicRow(patch, i, wx0, wx1, wx2, wx3) * wy0;
        acc += BicubicRow(patch, i + pw, wx0, wx1, wx2, wx3) * wy1;
        acc += BicubicRow(patch, i + 2 * pw, wx0, wx1, wx2, wx3) * wy2;
        acc += BicubicRow(patch, i + 3 * pw, wx0, wx1, wx2, wx3) * wy3;
        return acc;
    }

    private static Vector4 BicubicRow(Vector4[] patch, int i, Vector4 w0, Vector4 w1, Vector4 w2, Vector4 w3)
    {
        var row = Vector4.Zero;
        row += patch[i] * w0;
        row += patch[i + 1] * w1;
        row += patch[i + 2] * w2;
        row += patch[i + 3] * w3;
        return row;
    }

    private static Vector4 Sample(ReadOnlySpan<BmageRgba32> src, int sw, int sh, float sx, float sy, in Vector4 bg, BmageWarpSampling sampling)
    {
        switch (sampling)
        {
            case BmageWarpSampling.NearestNeighbor:
            {
                var ix = (int)MathF.Floor(sx + 0.5f);
                var iy = (int)MathF.Floor(sy + 0.5f);
                return (uint)ix < (uint)sw && (uint)iy < (uint)sh ? Load(src[iy * sw + ix]) : bg;
            }
            case BmageWarpSampling.Bicubic:
                return SampleBicubic(src, sw, sh, sx, sy, bg);
            default:
                return SampleBilinear(src, sw, sh, sx, sy, bg);
        }
    }

    private static Vector4 Load(BmageRgba32 p)
    {
        const float inv = 1f / 255f;
        var a = p.A * inv;
        return new Vector4(p.R * inv * a, p.G * inv * a, p.B * inv * a, a);
    }

    private static BmageRgba32 Store(Vector4 p)
    {
        var a = p.W;
        if (a > 1e-5f)
        {
            var ia = 1f / a;
            p = new Vector4(p.X * ia, p.Y * ia, p.Z * ia, a);
        }
        else return BmageRgba32.Transparent;
        var v = Vector4.Clamp(p, Vector4.Zero, Vector4.One) * 255f + new Vector4(0.5f);
        return BmageRgba32.FromPacked(PackedSimd.IsSupported ? BmagePixelRows.PackBrowser(v) : BmagePixelRows.Pack(v));
    }

    private static Vector4 Fetch(ReadOnlySpan<BmageRgba32> src, int sw, int sh, int x, int y, in Vector4 bg)
        => (uint)x < (uint)sw && (uint)y < (uint)sh ? Load(src[y * sw + x]) : bg;

    private static Vector4 SampleBilinear(ReadOnlySpan<BmageRgba32> src, int sw, int sh, float sx, float sy, in Vector4 bg)
    {
        if (sx <= -1f || sy <= -1f || sx >= sw || sy >= sh) return bg;
        var x0 = (int)MathF.Floor(sx);
        var y0 = (int)MathF.Floor(sy);
        var fx = sx - x0;
        var fy = sy - y0;
        var c00 = Fetch(src, sw, sh, x0, y0, bg);
        var c10 = Fetch(src, sw, sh, x0 + 1, y0, bg);
        var c01 = Fetch(src, sw, sh, x0, y0 + 1, bg);
        var c11 = Fetch(src, sw, sh, x0 + 1, y0 + 1, bg);
        var tx = new Vector4(fx);
        var top = c00 + (c10 - c00) * tx;
        var bottom = c01 + (c11 - c01) * tx;
        return top + (bottom - top) * new Vector4(fy);
    }

    private static float Cubic(float x)
    {
        const float a = -0.5f;
        x = MathF.Abs(x);
        if (x < 1f) return ((a + 2f) * x - (a + 3f)) * x * x + 1f;
        if (x < 2f) return ((a * x - 5f * a) * x + 8f * a) * x - 4f * a;
        return 0f;
    }

    private static Vector4 SampleBicubic(ReadOnlySpan<BmageRgba32> src, int sw, int sh, float sx, float sy, in Vector4 bg)
    {
        if (sx <= -2f || sy <= -2f || sx >= sw + 1 || sy >= sh + 1) return bg;
        var x0 = (int)MathF.Floor(sx);
        var y0 = (int)MathF.Floor(sy);
        var fx = sx - x0;
        var fy = sy - y0;
        Span<Vector4> wx = stackalloc Vector4[4];
        Span<Vector4> wy = stackalloc Vector4[4];
        for (var i = 0; i < 4; i++)
        {
            wx[i] = new Vector4(Cubic(fx - (i - 1)));
            wy[i] = new Vector4(Cubic(fy - (i - 1)));
        }
        var acc = Vector4.Zero;
        for (var j = 0; j < 4; j++)
        {
            var row = Vector4.Zero;
            for (var i = 0; i < 4; i++) row += Fetch(src, sw, sh, x0 - 1 + i, y0 - 1 + j, bg) * wx[i];
            acc += row * wy[j];
        }
        return acc;
    }
}
