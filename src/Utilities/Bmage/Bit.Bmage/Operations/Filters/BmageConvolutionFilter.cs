using System.Drawing;

namespace Bit.Bmage;

/// <summary>Applies an arbitrary convolution kernel (e.g. edge detection, emboss).</summary>
public sealed class BmageConvolutionFilter : BmageRegionOperation
{
    private readonly float[] _kernel;

    public BmageConvolutionFilter(float[] kernel, int kernelWidth, int kernelHeight, float divisor = 1f, float bias = 0f, Rectangle? region = null, string? name = null) : base(region)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        if (kernelWidth < 1 || kernelHeight < 1 || kernelWidth % 2 == 0 || kernelHeight % 2 == 0) throw new ArgumentException("Kernel dimensions must be odd and positive.");
        // In long: two large dimensions would overflow int and pass for a short kernel.
        if (kernel.Length != (long)kernelWidth * kernelHeight) throw new ArgumentException("Kernel length does not match dimensions.", nameof(kernel));
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
    public static BmageConvolutionFilter Emboss(Rectangle? region = null) => new([-2, -1, 0, -1, 1, 1, 0, 1, 2], 3, 3, 1f, 0f, region, "Emboss");

    /// <summary>3×3 Laplacian edge detection kernel.</summary>
    public static BmageConvolutionFilter EdgeDetect(Rectangle? region = null) => new([-1, -1, -1, -1, 8, -1, -1, -1, -1], 3, 3, 1f, 0f, region, "Edge detect");

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        var region = GetRegion(source.Size);
        var output = context.CanMutateSource ? source : source.Clone(context.Allocator);
        if (region.IsEmptyArea()) return output;
        using var input = source.CopyRegion(region, context.Allocator);
        int w = input.Width, h = input.Height, kw = KernelWidth, kh = KernelHeight, rx = kw / 2, ry = kh / 2;
        var inv = 1f / Divisor;
        var bias = Bias * 255f;
        // Every output pixel reads the copy of the input, never the output, so bands of rows run side by side.
        BmageParallelRows.For(context, 0, h, (long)w * kw * kh, (start, end, band) =>
        {
            var src = input.Pixels;
            for (var y = start; y < end; y++)
            {
                if (((y - start) & 15) == 0) { band.ThrowIfCancellationRequested(); band.ReportProgress(y / (double)h); }
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
                    dst[x] = new BmageRgba32(BmageRgba32.ClampToByte(r * inv + bias), BmageRgba32.ClampToByte(g * inv + bias), BmageRgba32.ClampToByte(b * inv + bias), src[y * w + x].A);
                }
            }
        });
        context.ReportProgress(1);
        return output;
    }

    public override IBmageImageOperation ForScale(double scale) => scale == 1 ? this : new BmageConvolutionFilter(_kernel, KernelWidth, KernelHeight, Divisor, Bias, ScaleRegion(Region, scale), _name);
}
