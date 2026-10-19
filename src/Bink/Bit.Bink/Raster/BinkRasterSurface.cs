
namespace Bit.Bink;

/// <summary>
/// A 32-bit RGBA pixel buffer with premultiplied alpha, the target of the pure .NET rasterizer.
/// </summary>
/// <remarks>
/// Alpha is premultiplied so source-over compositing is a multiply-add per channel. Colors are converted to and
/// from straight alpha at the boundaries (<see cref="Blend"/> input, <see cref="ToRgba32"/> output).
/// </remarks>
public sealed class BinkRasterSurface
{
    private readonly byte[] _pixels;

    /// <summary>Creates a transparent surface.</summary>
    /// <param name="width">Width in pixels (1 or more).</param>
    /// <param name="height">Height in pixels (1 or more).</param>
    public BinkRasterSurface(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        long bytes = (long)width * height * 4;
        if (bytes > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(width), $"A {width} by {height} surface needs {bytes} bytes, which exceeds the maximum array size.");
        Width = width;
        Height = height;
        _pixels = new byte[(int)bytes];
    }

    /// <summary>Width in pixels.</summary>
    public int Width { get; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; }

    /// <summary>The raw premultiplied RGBA bytes, row major.</summary>
    public Span<byte> Pixels => _pixels;

    /// <summary>The full surface rectangle.</summary>
    public BinkRect Bounds => new(0, 0, Width, Height);

    /// <summary>Fills the whole surface with a straight-alpha color, replacing what is there.</summary>
    public void Clear(BinkColor color)
    {
        if (color.A == 0)
        {
            Array.Clear(_pixels);
            return;
        }
        float a = color.A / 255f;
        byte r = (byte)MathF.Round(color.R * a);
        byte g = (byte)MathF.Round(color.G * a);
        byte b = (byte)MathF.Round(color.B * a);
        for (int i = 0; i < _pixels.Length; i += 4)
        {
            _pixels[i] = r;
            _pixels[i + 1] = g;
            _pixels[i + 2] = b;
            _pixels[i + 3] = color.A;
        }
    }

    /// <summary>
    /// Blends a span of coverage values with a color into one row.
    /// </summary>
    /// <param name="y">Row index.</param>
    /// <param name="x">Column of the first coverage value.</param>
    /// <param name="coverage">Coverage 0..1 per pixel.</param>
    /// <param name="color">Straight-alpha source color.</param>
    /// <param name="alpha">Extra alpha 0..1 (paint opacity).</param>
    /// <param name="blend">Blend mode.</param>
    public void Blend(int y, int x, ReadOnlySpan<float> coverage, BinkColor color, float alpha, BinkBlendMode blend)
    {
        if ((uint)y >= (uint)Height) return;
        float sourceAlpha = color.Alpha * Math.Clamp(alpha, 0f, 1f);
        if (sourceAlpha <= 0 && blend != BinkBlendMode.DestinationOut) return;

        float sr = color.R / 255f;
        float sg = color.G / 255f;
        float sb = color.B / 255f;
        int rowOffset = y * Width * 4;

        for (int i = 0; i < coverage.Length; i++)
        {
            int px = x + i;
            if ((uint)px >= (uint)Width) continue;
            float c = coverage[i];
            if (c <= 0.0005f) continue;
            if (c > 1f) c = 1f;
            float sa = sourceAlpha * c;
            if (sa <= 0.0005f && blend != BinkBlendMode.DestinationOut) continue;

            int offset = rowOffset + px * 4;
            float dr = _pixels[offset] / 255f;
            float dg = _pixels[offset + 1] / 255f;
            float db = _pixels[offset + 2] / 255f;
            float da = _pixels[offset + 3] / 255f;

            if (blend == BinkBlendMode.DestinationOut)
            {
                float keep = 1f - c * Math.Clamp(alpha, 0f, 1f) * color.Alpha;
                _pixels[offset] = ToByte(dr * keep);
                _pixels[offset + 1] = ToByte(dg * keep);
                _pixels[offset + 2] = ToByte(db * keep);
                _pixels[offset + 3] = ToByte(da * keep);
                continue;
            }

            // Blend against the backdrop color (straight alpha), then composite source-over.
            float br = sr, bg = sg, bb = sb;
            if (blend != BinkBlendMode.Normal && da > 0.0001f)
            {
                float bdr = dr / da, bdg = dg / da, bdb = db / da;
                (float mr, float mg, float mb) = blend switch
                {
                    BinkBlendMode.Multiply => (sr * bdr, sg * bdg, sb * bdb),
                    BinkBlendMode.Screen => (sr + bdr - sr * bdr, sg + bdg - sg * bdg, sb + bdb - sb * bdb),
                    _ => (sr, sg, sb),
                };
                // Where the backdrop is partly transparent the source keeps its own color proportionally.
                br = sr * (1f - da) + mr * da;
                bg = sg * (1f - da) + mg * da;
                bb = sb * (1f - da) + mb * da;
            }

            float inverse = 1f - sa;
            _pixels[offset] = ToByte(br * sa + dr * inverse);
            _pixels[offset + 1] = ToByte(bg * sa + dg * inverse);
            _pixels[offset + 2] = ToByte(bb * sa + db * inverse);
            _pixels[offset + 3] = ToByte(sa + da * inverse);
        }
    }

    /// <summary>Blends a single premultiplied pixel, used when drawing images.</summary>
    public void BlendPixel(int x, int y, float r, float g, float b, float a, BinkBlendMode blend)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height || a <= 0.0005f) return;
        int offset = (y * Width + x) * 4;
        float dr = _pixels[offset] / 255f;
        float dg = _pixels[offset + 1] / 255f;
        float db = _pixels[offset + 2] / 255f;
        float da = _pixels[offset + 3] / 255f;

        if (blend == BinkBlendMode.DestinationOut)
        {
            float keep = 1f - a;
            _pixels[offset] = ToByte(dr * keep);
            _pixels[offset + 1] = ToByte(dg * keep);
            _pixels[offset + 2] = ToByte(db * keep);
            _pixels[offset + 3] = ToByte(da * keep);
            return;
        }

        float inverse = 1f - a;
        _pixels[offset] = ToByte(r + dr * inverse);
        _pixels[offset + 1] = ToByte(g + dg * inverse);
        _pixels[offset + 2] = ToByte(b + db * inverse);
        _pixels[offset + 3] = ToByte(a + da * inverse);
    }

    /// <summary>Composites another surface of the same size onto this one.</summary>
    /// <param name="source">The surface to draw.</param>
    /// <param name="opacity">Group opacity 0..1.</param>
    /// <param name="blend">Blend mode.</param>
    /// <param name="clip">Region to composite.</param>
    public void Composite(BinkRasterSurface source, float opacity, BinkBlendMode blend, BinkRect clip)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Width != Width || source.Height != Height) throw new ArgumentException("Surfaces must have the same size to be composited.", nameof(source));
        opacity = Math.Clamp(opacity, 0f, 1f);
        if (opacity <= 0) return;

        int x0 = Math.Max((int)MathF.Floor(clip.Left), 0);
        int y0 = Math.Max((int)MathF.Floor(clip.Top), 0);
        int x1 = Math.Min((int)MathF.Ceiling(clip.Right), Width);
        int y1 = Math.Min((int)MathF.Ceiling(clip.Bottom), Height);

        for (int y = y0; y < y1; y++)
        {
            int row = y * Width * 4;
            for (int x = x0; x < x1; x++)
            {
                int offset = row + x * 4;
                float sa = source._pixels[offset + 3] / 255f * opacity;
                if (sa <= 0.0005f) continue;
                float sr = source._pixels[offset] / 255f * opacity;
                float sg = source._pixels[offset + 1] / 255f * opacity;
                float sb = source._pixels[offset + 2] / 255f * opacity;

                float dr = _pixels[offset] / 255f;
                float dg = _pixels[offset + 1] / 255f;
                float db = _pixels[offset + 2] / 255f;
                float da = _pixels[offset + 3] / 255f;

                if (blend == BinkBlendMode.DestinationOut)
                {
                    // The group's alpha removes what is beneath it, as Blend does for a single shape.
                    float keep = 1f - sa;
                    _pixels[offset] = ToByte(dr * keep);
                    _pixels[offset + 1] = ToByte(dg * keep);
                    _pixels[offset + 2] = ToByte(db * keep);
                    _pixels[offset + 3] = ToByte(da * keep);
                    continue;
                }

                if (blend != BinkBlendMode.Normal && da > 0.0001f)
                {
                    float ur = sr / sa, ug = sg / sa, ub = sb / sa;
                    float bdr = dr / da, bdg = dg / da, bdb = db / da;
                    (float mr, float mg, float mb) = blend switch
                    {
                        BinkBlendMode.Multiply => (ur * bdr, ug * bdg, ub * bdb),
                        BinkBlendMode.Screen => (ur + bdr - ur * bdr, ug + bdg - ug * bdg, ub + bdb - ub * bdb),
                        _ => (ur, ug, ub),
                    };
                    sr = (ur * (1f - da) + mr * da) * sa;
                    sg = (ug * (1f - da) + mg * da) * sa;
                    sb = (ub * (1f - da) + mb * da) * sa;
                }

                float inverse = 1f - sa;
                _pixels[offset] = ToByte(sr + dr * inverse);
                _pixels[offset + 1] = ToByte(sg + dg * inverse);
                _pixels[offset + 2] = ToByte(sb + db * inverse);
                _pixels[offset + 3] = ToByte(sa + da * inverse);
            }
        }
    }

    /// <summary>Returns the pixels as straight-alpha RGBA bytes, the layout PNG expects.</summary>
    public byte[] ToRgba32()
    {
        byte[] result = new byte[_pixels.Length];
        for (int i = 0; i < _pixels.Length; i += 4)
        {
            byte a = _pixels[i + 3];
            if (a == 0) continue;
            if (a == 255)
            {
                result[i] = _pixels[i];
                result[i + 1] = _pixels[i + 1];
                result[i + 2] = _pixels[i + 2];
                result[i + 3] = 255;
                continue;
            }
            float inverse = 255f / a;
            result[i] = ToByte(_pixels[i] * inverse / 255f);
            result[i + 1] = ToByte(_pixels[i + 1] * inverse / 255f);
            result[i + 2] = ToByte(_pixels[i + 2] * inverse / 255f);
            result[i + 3] = a;
        }
        return result;
    }

    /// <summary>Reads a straight-alpha pixel.</summary>
    public BinkColor GetPixel(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return BinkColor.Transparent;
        int offset = (y * Width + x) * 4;
        byte a = _pixels[offset + 3];
        if (a == 0) return BinkColor.Transparent;
        float inverse = 255f / a;
        return new BinkColor(ToByte(_pixels[offset] * inverse / 255f), ToByte(_pixels[offset + 1] * inverse / 255f), ToByte(_pixels[offset + 2] * inverse / 255f), a);
    }

    private static byte ToByte(float value) => (byte)Math.Clamp(MathF.Round(value * 255f), 0, 255);
}
