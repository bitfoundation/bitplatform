using System.Buffers;
using System.Drawing;
using System.Numerics;
using Bit.Bmage.Geometry;

namespace Bit.Bmage.Operations.Filters;

/// <summary>Base class for operations that optionally act on a sub-region of the image (e.g. redacting part of a screenshot).</summary>
public abstract class RegionOperation : ImageOperation
{
    protected RegionOperation(Rectangle? region) => Region = region;

    /// <summary>The region affected, in input coordinates, or null for the whole image.</summary>
    public Rectangle? Region { get; }

    /// <summary>Resolves the effective region for an input size.</summary>
    protected Rectangle GetRegion(Size input)
    {
        var full = new Rectangle(Point.Empty, input);
        if (Region is not { } r) return full;
        r.Intersect(full);
        return r;
    }

    /// <summary>Scales the region for a proxy image.</summary>
    protected static Rectangle? ScaleRegion(Rectangle? region, double scale)
    {
        if (region is not { } r || scale == 1) return region;
        var x = (int)Math.Round(r.X * scale);
        var y = (int)Math.Round(r.Y * scale);
        return new Rectangle(x, y, Math.Max(1, (int)Math.Round(r.Right * scale) - x), Math.Max(1, (int)Math.Round(r.Bottom * scale) - y));
    }
}

/// <summary>
/// Gaussian blur approximated with three successive box blurs (O(n) per pixel, independent of radius), operating on
/// premultiplied alpha so transparent pixels do not darken edges.
/// </summary>
public sealed class BlurFilter : RegionOperation
{
    public BlurFilter(float radius, Rectangle? region = null) : base(region)
    {
        if (radius < 0 || float.IsNaN(radius)) throw new ArgumentOutOfRangeException(nameof(radius));
        Radius = radius;
    }

    /// <summary>Blur radius (standard deviation) in pixels.</summary>
    public float Radius { get; }
    public override string Name => "Blur";
    public override bool IsIdentity => Radius < 0.05f;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var target = GetMutableCopy(source, context);
        var region = GetRegion(source.Size);
        if (region.IsEmptyArea() || IsIdentity) return target;
        BoxBlur.GaussianInPlace(target, region, Radius, context);
        return target;
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new BlurFilter((float)(Radius * scale), ScaleRegion(Region, scale));
}

internal static class BoxBlur
{
    /// <summary>Box radii for three passes approximating a gaussian with the given sigma (Ivan Kutskir's method).</summary>
    public static int[] BoxesForGauss(float sigma, int n)
    {
        var wIdeal = Math.Sqrt(12 * sigma * sigma / n + 1);
        var wl = (int)Math.Floor(wIdeal);
        if (wl % 2 == 0) wl--;
        var wu = wl + 2;
        var mIdeal = (12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4.0 * wl - 4);
        var m = (int)Math.Round(mIdeal);
        var sizes = new int[n];
        for (var i = 0; i < n; i++) sizes[i] = ((i < m ? wl : wu) - 1) / 2;
        return sizes;
    }

    public static void GaussianInPlace(ImageBuffer buffer, Rectangle region, float sigma, OperationContext context)
    {
        var w = region.Width;
        var h = region.Height;
        var pool = ArrayPool<Vector4>.Shared;
        var a = pool.Rent(w * h);
        var b = pool.Rent(w * h);
        try
        {
            const float inv = 1f / 255f;
            for (var y = 0; y < h; y++)
            {
                var row = buffer.GetRow(region.Y + y).Slice(region.X, w);
                for (var x = 0; x < w; x++)
                {
                    var p = row[x];
                    var al = p.A * inv;
                    a[y * w + x] = new Vector4(p.R * inv * al, p.G * inv * al, p.B * inv * al, al);
                }
            }
            var boxes = BoxesForGauss(sigma, 3);
            var src = a; var dst = b;
            foreach (var r in boxes)
            {
                context.ThrowIfCancellationRequested();
                if (r <= 0) continue;
                BoxH(src, dst, w, h, r);
                BoxV(dst, src, w, h, r);
            }
            for (var y = 0; y < h; y++)
            {
                var row = buffer.GetRow(region.Y + y).Slice(region.X, w);
                for (var x = 0; x < w; x++)
                {
                    var p = src[y * w + x];
                    var al = p.W;
                    if (al > 1e-5f) { var ia = 1f / al; p = new Vector4(p.X * ia, p.Y * ia, p.Z * ia, al); }
                    else p = Vector4.Zero;
                    var v = Vector4.Clamp(p, Vector4.Zero, Vector4.One) * 255f + new Vector4(0.5f);
                    row[x] = new Rgba32((byte)v.X, (byte)v.Y, (byte)v.Z, (byte)v.W);
                }
            }
            context.ReportProgress(1);
        }
        finally
        {
            pool.Return(a);
            pool.Return(b);
        }
    }

    private static void BoxH(Vector4[] src, Vector4[] dst, int w, int h, int r)
    {
        var iarr = 1f / (r + r + 1);
        for (var y = 0; y < h; y++)
        {
            var row = y * w;
            var fv = src[row];
            var lv = src[row + w - 1];
            var val = (r + 1) * fv;
            for (var j = 0; j < r; j++) val += src[row + Math.Min(j, w - 1)];
            for (var x = 0; x < w; x++)
            {
                var add = x + r < w ? src[row + x + r] : lv;
                var sub = x - r - 1 >= 0 ? src[row + x - r - 1] : fv;
                val += add - sub;
                dst[row + x] = val * iarr;
            }
        }
    }

    private static void BoxV(Vector4[] src, Vector4[] dst, int w, int h, int r)
    {
        var iarr = 1f / (r + r + 1);
        var pool = ArrayPool<Vector4>.Shared;
        var val = pool.Rent(w);
        try
        {
            for (var x = 0; x < w; x++)
            {
                var fv = src[x];
                var v = (r + 1) * fv;
                for (var j = 0; j < r; j++) v += src[Math.Min(j, h - 1) * w + x];
                val[x] = v;
            }
            for (var y = 0; y < h; y++)
            {
                var addRow = Math.Min(y + r, h - 1) * w;
                var subRow = Math.Max(y - r - 1, 0) * w;
                var useFirst = y - r - 1 < 0;
                var dstRow = y * w;
                for (var x = 0; x < w; x++)
                {
                    var add = src[addRow + x];
                    var sub = useFirst ? src[x] : src[subRow + x];
                    var v = val[x] + add - sub;
                    val[x] = v;
                    dst[dstRow + x] = v * iarr;
                }
            }
        }
        finally { pool.Return(val); }
    }
}

/// <summary>Unsharp-mask sharpening: adds the difference between the image and a blurred copy.</summary>
public sealed class SharpenFilter : RegionOperation
{
    public SharpenFilter(float amount = 0.5f, float radius = 1f, float threshold = 0f, Rectangle? region = null) : base(region)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        Amount = amount; Radius = radius; Threshold = Math.Clamp(threshold, 0f, 1f);
    }

    /// <summary>Strength of the effect. 0 = none, 1 = strong.</summary>
    public float Amount { get; }
    /// <summary>Radius of the blur used to find edges.</summary>
    public float Radius { get; }
    /// <summary>Minimum difference (0..1) before sharpening is applied; suppresses noise amplification.</summary>
    public float Threshold { get; }
    public override string Name => "Sharpen";
    public override bool IsIdentity => Amount == 0;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var region = GetRegion(source.Size);
        var target = GetMutableCopy(source, context);
        if (region.IsEmptyArea() || IsIdentity) return target;
        using var blurred = source.CopyRegion(region, context.Allocator);
        BoxBlur.GaussianInPlace(blurred, blurred.Bounds, Radius, context);
        var threshold = (int)(Threshold * 255);
        var amount = Amount;
        for (var y = 0; y < region.Height; y++)
        {
            if ((y & 31) == 0) context.ThrowIfCancellationRequested();
            var dst = target.GetRow(region.Y + y).Slice(region.X, region.Width);
            var blr = blurred.GetRow(y);
            for (var x = 0; x < region.Width; x++)
            {
                var p = dst[x];
                var b = blr[x];
                dst[x] = new Rgba32(Sharpen(p.R, b.R, amount, threshold), Sharpen(p.G, b.G, amount, threshold), Sharpen(p.B, b.B, amount, threshold), p.A);
            }
        }
        return target;
    }

    private static byte Sharpen(byte v, byte blurred, float amount, int threshold)
    {
        var diff = v - blurred;
        if (Math.Abs(diff) < threshold) return v;
        return Rgba32.ClampToByte(v + diff * amount);
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new SharpenFilter(Amount, (float)Math.Max(0.5, Radius * scale), Threshold, ScaleRegion(Region, scale));
}

/// <summary>Replaces blocks of pixels with their average colour (mosaic). Commonly used for redaction.</summary>
public sealed class PixelateFilter : RegionOperation
{
    public PixelateFilter(int blockSize, Rectangle? region = null) : base(region)
    {
        if (blockSize < 1) throw new ArgumentOutOfRangeException(nameof(blockSize));
        BlockSize = blockSize;
    }

    public int BlockSize { get; }
    public override string Name => "Pixelate";
    public override bool IsIdentity => BlockSize == 1;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var target = GetMutableCopy(source, context);
        var region = GetRegion(source.Size);
        if (region.IsEmptyArea() || IsIdentity) return target;
        var bs = BlockSize;
        for (var by = region.Top; by < region.Bottom; by += bs)
        {
            context.ThrowIfCancellationRequested();
            var bh = Math.Min(bs, region.Bottom - by);
            for (var bx = region.Left; bx < region.Right; bx += bs)
            {
                var bw = Math.Min(bs, region.Right - bx);
                long r = 0, g = 0, b = 0, a = 0;
                for (var y = 0; y < bh; y++)
                {
                    var row = target.GetRow(by + y).Slice(bx, bw);
                    foreach (var p in row) { r += p.R * p.A; g += p.G * p.A; b += p.B * p.A; a += p.A; }
                }
                var n = bw * bh;
                Rgba32 avg = a == 0 ? Rgba32.Transparent : new Rgba32((byte)(r / a), (byte)(g / a), (byte)(b / a), (byte)(a / n));
                for (var y = 0; y < bh; y++) target.GetRow(by + y).Slice(bx, bw).Fill(avg);
            }
        }
        return target;
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new PixelateFilter(Math.Max(1, (int)Math.Round(BlockSize * scale)), ScaleRegion(Region, scale));
}

/// <summary>Adds pseudo-random noise. Deterministic for a given seed.</summary>
public sealed class NoiseFilter : RegionOperation
{
    public NoiseFilter(float amount, bool monochrome = false, int seed = 0, Rectangle? region = null) : base(region)
    {
        Amount = Math.Clamp(amount, 0f, 1f); Monochrome = monochrome; Seed = seed;
    }

    /// <summary>Noise strength 0..1.</summary>
    public float Amount { get; }
    public bool Monochrome { get; }
    public int Seed { get; }
    public override string Name => "Noise";
    public override bool IsIdentity => Amount == 0;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var target = GetMutableCopy(source, context);
        var region = GetRegion(source.Size);
        if (region.IsEmptyArea() || IsIdentity) return target;
        var range = (int)(Amount * 255);
        var state = Seed == 0 ? 0x9E3779B9u : (uint)Seed;
        for (var y = region.Top; y < region.Bottom; y++)
        {
            if ((y & 31) == 0) context.ThrowIfCancellationRequested();
            var row = target.GetRow(y).Slice(region.Left, region.Width);
            for (var x = 0; x < row.Length; x++)
            {
                var p = row[x];
                int nr, ng, nb;
                if (Monochrome) { nr = ng = nb = Next(ref state, range); }
                else { nr = Next(ref state, range); ng = Next(ref state, range); nb = Next(ref state, range); }
                row[x] = new Rgba32(Rgba32.ClampToByte(p.R + nr), Rgba32.ClampToByte(p.G + ng), Rgba32.ClampToByte(p.B + nb), p.A);
            }
        }
        return target;
    }

    private static int Next(ref uint state, int range)
    {
        // xorshift32
        state ^= state << 13; state ^= state >> 17; state ^= state << 5;
        if (range == 0) return 0;
        return (int)(state % (uint)(range * 2 + 1)) - range;
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new NoiseFilter(Amount, Monochrome, Seed, ScaleRegion(Region, scale));
}

/// <summary>Median filter for noise reduction. Radius 1 = 3×3 window, 2 = 5×5. Cost grows with the square of the radius.</summary>
public sealed class MedianFilter : RegionOperation
{
    public MedianFilter(int radius = 1, Rectangle? region = null) : base(region)
    {
        if (radius is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be between 1 and 5.");
        Radius = radius;
    }

    public int Radius { get; }
    public override string Name => "Noise reduction";

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var region = GetRegion(source.Size);
        var output = context.CanMutateSource ? source : source.Clone(context.Allocator);
        if (region.IsEmptyArea()) return output;
        using var input = source.CopyRegion(region, context.Allocator);
        var r = Radius;
        var window = (2 * r + 1) * (2 * r + 1);
        Span<byte> rs = stackalloc byte[window];
        Span<byte> gs = stackalloc byte[window];
        Span<byte> bs = stackalloc byte[window];
        int w = input.Width, h = input.Height;
        var src = input.Pixels;
        for (var y = 0; y < h; y++)
        {
            if ((y & 15) == 0) { context.ThrowIfCancellationRequested(); context.ReportProgress(y / (double)h); }
            var dst = output.GetRow(region.Y + y).Slice(region.X, w);
            for (var x = 0; x < w; x++)
            {
                var n = 0;
                for (var dy = -r; dy <= r; dy++)
                {
                    var sy = Math.Clamp(y + dy, 0, h - 1);
                    for (var dx = -r; dx <= r; dx++)
                    {
                        var sx = Math.Clamp(x + dx, 0, w - 1);
                        var p = src[sy * w + sx];
                        rs[n] = p.R; gs[n] = p.G; bs[n] = p.B; n++;
                    }
                }
                rs.Sort(); gs.Sort(); bs.Sort();
                var mid = window / 2;
                dst[x] = new Rgba32(rs[mid], gs[mid], bs[mid], src[y * w + x].A);
            }
        }
        context.ReportProgress(1);
        return output;
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new MedianFilter(Math.Clamp((int)Math.Round(Radius * scale), 1, 5), ScaleRegion(Region, scale));
}

/// <summary>Applies an arbitrary convolution kernel (e.g. edge detection, emboss).</summary>
public sealed class ConvolutionFilter : RegionOperation
{
    private readonly float[] _kernel;

    public ConvolutionFilter(float[] kernel, int kernelWidth, int kernelHeight, float divisor = 1f, float bias = 0f, Rectangle? region = null, string? name = null) : base(region)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        if (kernelWidth < 1 || kernelHeight < 1 || kernelWidth % 2 == 0 || kernelHeight % 2 == 0) throw new ArgumentException("Kernel dimensions must be odd and positive.");
        if (kernel.Length != kernelWidth * kernelHeight) throw new ArgumentException("Kernel length does not match dimensions.", nameof(kernel));
        if (divisor == 0) throw new ArgumentOutOfRangeException(nameof(divisor));
        _kernel = (float[])kernel.Clone();
        KernelWidth = kernelWidth; KernelHeight = kernelHeight; Divisor = divisor; Bias = bias;
        _name = name ?? "Convolution";
    }

    private readonly string _name;
    public IReadOnlyList<float> Kernel => _kernel;
    public int KernelWidth { get; }
    public int KernelHeight { get; }
    public float Divisor { get; }
    /// <summary>Constant added to each output channel, in [0,1] units.</summary>
    public float Bias { get; }
    public override string Name => _name;

    /// <summary>3×3 emboss kernel.</summary>
    public static ConvolutionFilter Emboss(Rectangle? region = null) => new([-2, -1, 0, -1, 1, 1, 0, 1, 2], 3, 3, 1f, 0f, region, "Emboss");

    /// <summary>3×3 Laplacian edge detection kernel.</summary>
    public static ConvolutionFilter EdgeDetect(Rectangle? region = null) => new([-1, -1, -1, -1, 8, -1, -1, -1, -1], 3, 3, 1f, 0f, region, "Edge detect");

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var region = GetRegion(source.Size);
        var output = context.CanMutateSource ? source : source.Clone(context.Allocator);
        if (region.IsEmptyArea()) return output;
        using var input = source.CopyRegion(region, context.Allocator);
        int w = input.Width, h = input.Height, kw = KernelWidth, kh = KernelHeight, rx = kw / 2, ry = kh / 2;
        var src = input.Pixels;
        var inv = 1f / Divisor;
        var bias = Bias * 255f;
        for (var y = 0; y < h; y++)
        {
            if ((y & 15) == 0) { context.ThrowIfCancellationRequested(); context.ReportProgress(y / (double)h); }
            var dst = output.GetRow(region.Y + y).Slice(region.X, w);
            for (var x = 0; x < w; x++)
            {
                float r = 0, g = 0, b = 0;
                for (var ky = 0; ky < kh; ky++)
                {
                    var sy = Math.Clamp(y + ky - ry, 0, h - 1);
                    for (var kx = 0; kx < kw; kx++)
                    {
                        var sx = Math.Clamp(x + kx - rx, 0, w - 1);
                        var k = _kernel[ky * kw + kx];
                        var p = src[sy * w + sx];
                        r += p.R * k; g += p.G * k; b += p.B * k;
                    }
                }
                dst[x] = new Rgba32(Rgba32.ClampToByte(r * inv + bias), Rgba32.ClampToByte(g * inv + bias), Rgba32.ClampToByte(b * inv + bias), src[y * w + x].A);
            }
        }
        context.ReportProgress(1);
        return output;
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new ConvolutionFilter(_kernel, KernelWidth, KernelHeight, Divisor, Bias, ScaleRegion(Region, scale), _name);
}

/// <summary>Darkens (or tints) the corners of the image with a smooth radial falloff.</summary>
public sealed class VignetteFilter : ImageOperation
{
    public VignetteFilter(float amount = 0.5f, float radius = 0.75f, float softness = 0.5f, Rgba32? color = null)
    {
        Amount = Math.Clamp(amount, 0f, 1f);
        Radius = Math.Clamp(radius, 0.05f, 2f);
        Softness = Math.Clamp(softness, 0.01f, 1f);
        Color = color ?? Rgba32.Black;
    }

    /// <summary>Strength 0..1.</summary>
    public float Amount { get; }
    /// <summary>Distance from centre (as a fraction of the half diagonal) where the effect reaches full strength.</summary>
    public float Radius { get; }
    /// <summary>Width of the transition band as a fraction of the radius.</summary>
    public float Softness { get; }
    public Rgba32 Color { get; }
    public override string Name => "Vignette";
    public override bool IsIdentity => Amount == 0;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var target = GetMutableCopy(source, context);
        if (IsIdentity) return target;
        int w = target.Width, h = target.Height;
        var cx = w / 2f; var cy = h / 2f;
        var halfDiag = MathF.Sqrt(cx * cx + cy * cy);
        var start = Radius * (1f - Softness) * halfDiag;
        var end = Radius * halfDiag;
        var invRange = 1f / MathF.Max(1e-3f, end - start);
        var cr = Color.R; var cg = Color.G; var cb = Color.B;
        for (var y = 0; y < h; y++)
        {
            if ((y & 31) == 0) context.ThrowIfCancellationRequested();
            var row = target.GetRow(y);
            var dy = y + 0.5f - cy;
            for (var x = 0; x < w; x++)
            {
                var dx = x + 0.5f - cx;
                var d = MathF.Sqrt(dx * dx + dy * dy);
                var t = Math.Clamp((d - start) * invRange, 0f, 1f);
                t = t * t * (3f - 2f * t) * Amount;
                if (t <= 0f) continue;
                var p = row[x];
                row[x] = new Rgba32((byte)(p.R + (cr - p.R) * t), (byte)(p.G + (cg - p.G) * t), (byte)(p.B + (cb - p.B) * t), p.A);
            }
        }
        return target;
    }
}
