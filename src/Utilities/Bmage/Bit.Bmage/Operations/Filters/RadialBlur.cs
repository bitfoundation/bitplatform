using System.Buffers;
using System.Drawing;
using System.Numerics;

namespace Bit.Bmage.Operations.Filters;

/// <summary>Which way <see cref="RadialBlurFilter"/> streaks the image.</summary>
public enum RadialBlurMode
{
    /// <summary>Along lines through the centre, as zooming the lens during the exposure does: a rush towards the subject.</summary>
    Zoom = 0,

    /// <summary>Along circles around the centre, as turning the camera does: a spinning wheel, a swirl.</summary>
    Spin,
}

/// <summary>
/// Blurs towards or around a centre point, leaving the centre sharp and streaking more the further a pixel is from
/// it: the zoom burst behind a product shot, the spin of a wheel.
/// </summary>
/// <remarks>
/// <para>
/// Each pixel becomes the average of the pixels along its streak, in premultiplied alpha so transparent pixels do not
/// darken edges; beyond the image the edge pixels repeat. With <see cref="RadialBlurMode.Zoom"/> the streak runs from
/// the pixel towards the centre for <see cref="Amount"/> of the way (at most 95% of it), its samples spaced in proportion
/// to their distance from the centre; with <see cref="RadialBlurMode.Spin"/> it is an arc of <c>Amount × 45</c> degrees
/// centred on the pixel, its samples spaced evenly by angle.
/// </para>
/// <para>
/// A streak of 4<sup>k</sup> evenly spaced samples is the same as k passes that each average four samples, their spacing
/// growing fourfold from pass to pass, so the blur runs as about log4 of the longest streak's length in pixels of such
/// passes, at most <see cref="MaxPasses"/>, rather than as a sample for every pixel of every streak. The samples are
/// bilinear, and the passes keep their sums at 16 bits a channel, so the extra softening and rounding they add stay well
/// below what the streak itself does.
/// </para>
/// <para>
/// The centre is a fraction of the image's width and height and the streak a fraction of the distance, so the
/// operation is the same at any resolution and replays on a proxy as it will on the original. It takes two working
/// copies of the image at 16 bits a channel while it runs, four times the image's own size, which
/// <see cref="Pipeline.ImagePipeline.EstimateMemory"/> counts; a large image is best blurred at the size it will be shown.
/// </para>
/// </remarks>
public sealed class RadialBlurFilter : RegionOperation
{
    /// <summary>The most passes, enough for a streak of 4096 samples.</summary>
    public const int MaxPasses = 6;

    /// <summary>Samples each pass averages: a streak of 4<sup>k</sup> samples is k passes.</summary>
    private const int Radix = 4;

    /// <summary>How close to the centre a zoom streak may reach, as a share of the way.</summary>
    private const float MaxZoomReach = 0.95f;

    /// <param name="amount">How strong the blur is, in [0,1]: the share of the way to the centre a zoom streak runs, or the arc a spin covers as a share of 45 degrees.</param>
    /// <param name="mode">Zoom or spin.</param>
    /// <param name="center">The centre as fractions of the width and height, from (0,0) top left to (1,1) bottom right; null for the middle.</param>
    /// <param name="region">The part of the image to blur, or null for all of it.</param>
    public RadialBlurFilter(float amount, RadialBlurMode mode = RadialBlurMode.Zoom, PointF? center = null, Rectangle? region = null) : base(region)
    {
        if (!float.IsFinite(amount) || amount < 0 || amount > 1)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "The amount must be between 0 and 1.");
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown mode.");
        var c = center ?? new PointF(0.5f, 0.5f);
        if (!float.IsFinite(c.X) || !float.IsFinite(c.Y) || c.X < -1 || c.X > 2 || c.Y < -1 || c.Y > 2)
            throw new ArgumentOutOfRangeException(nameof(center), c, "The centre must lie within one image size of the image, as fractions between -1 and 2.");
        Amount = amount;
        Mode = mode;
        Center = c;
    }

    /// <summary>How strong the blur is, in [0,1].</summary>
    public float Amount { get; }

    /// <summary>Zoom or spin.</summary>
    public RadialBlurMode Mode { get; }

    /// <summary>The centre as fractions of the width and height.</summary>
    public PointF Center { get; }

    public override string Name => Mode == RadialBlurMode.Zoom ? "Zoom blur" : "Spin blur";

    public override bool IsIdentity => Amount <= 0;

    /// <inheritdoc />
    public override IImageOperation ForScale(double scale)
        => scale == 1 || Region is null ? this : new RadialBlurFilter(Amount, Mode, Center, ScaleRegion(Region, scale));

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var region = GetRegion(source.Size);
        if (region.Width <= 0 || region.Height <= 0 || IsIdentity) return GetMutableCopy(source, context);

        int width = source.Width, height = source.Height;
        float cx = Center.X * width, cy = Center.Y * height;
        // The farthest any pixel of the region is from the centre, and so the longest streak, in pixels.
        var farthest = 0f;
        foreach (var (x, y) in new[] { (region.Left, region.Top), (region.Right, region.Top), (region.Left, region.Bottom), (region.Right, region.Bottom) })
            farthest = MathF.Max(farthest, MathF.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)));
        var spin = Mode == RadialBlurMode.Spin;
        // The streak's span: an angle for a spin, the logarithm of the scale it shrinks to for a zoom.
        var span = spin ? Amount * MathF.PI / 4 : -MathF.Log(1 - Math.Min(Amount, MaxZoomReach));
        var longest = spin ? farthest * span : farthest * (1 - MathF.Exp(-span));
        // Enough passes that the samples along the longest streak are at most a pixel apart.
        var passes = Math.Clamp((int)MathF.Ceiling(MathF.Log(Math.Max(2, longest + 1)) / MathF.Log(Radix)), 1, MaxPasses);
        var taps = 1;
        for (var i = 0; i < passes; i++) taps *= Radix;
        // Between neighbouring samples: the span split into taps - 1 equal steps.
        var step = span / (taps - 1);

        // Premultiplied, 16 bits a channel (0..65535 for 0..1), so averaging pass after pass loses next to nothing.
        var length = width * height * 4;
        var a = ArrayPool<ushort>.Shared.Rent(length);
        var b = ArrayPool<ushort>.Shared.Rent(length);
        try
        {
            ParallelRows.For(context, 0, height, width, (start, end, band) =>
            {
                for (var y = start; y < end; y++)
                {
                    if ((y & 31) == 0) band.ThrowIfCancellationRequested();
                    var row = source.GetRow(y);
                    var o = y * width * 4;
                    for (var x = 0; x < row.Length; x++, o += 4)
                    {
                        var p = row[x];
                        var alpha = p.A * 257;
                        a[o] = (ushort)((p.R * alpha + 127) / 255);
                        a[o + 1] = (ushort)((p.G * alpha + 127) / 255);
                        a[o + 2] = (ushort)((p.B * alpha + 127) / 255);
                        a[o + 3] = (ushort)alpha;
                    }
                }
            });
            // Outside the region every pass leaves the pixels as they were, in both copies.
            Array.Copy(a, b, length);

            var progressStep = 1.0 / passes;
            var spacing = step;
            for (var pass = 0; pass < passes; pass++, spacing *= Radix)
            {
                context.ThrowIfCancellationRequested();
                // This pass's samples, Radix of them, spacing apart along the streak: a rotation for each (centred on the
                // pixel, so the arc stays centred), or a scale towards the centre (the first being the pixel itself).
                var transforms = new Matrix3x2[Radix];
                for (var j = 0; j < Radix; j++)
                {
                    if (spin)
                    {
                        var (sin, cos) = MathF.SinCos((j - (Radix - 1) / 2f) * spacing);
                        transforms[j] = new Matrix3x2(cos, sin, -sin, cos, 0, 0);
                    }
                    else
                    {
                        transforms[j] = Matrix3x2.CreateScale(MathF.Exp(-j * spacing));
                    }
                }
                var (read, write) = (pass & 1) == 0 ? (a, b) : (b, a);
                ParallelRows.For(context, region.Top, region.Bottom, (long)region.Width * 4 * Radix, (start, end, band) =>
                {
                    for (var y = start; y < end; y++)
                    {
                        if ((y & 31) == 0) band.ThrowIfCancellationRequested();
                        var py = y + 0.5f - cy;
                        for (var x = region.Left; x < region.Right; x++)
                        {
                            var px = x + 0.5f - cx;
                            var sum = spin ? Vector4.Zero : Load(read, (y * width + x) * 4);
                            for (var j = spin ? 0 : 1; j < Radix; j++)
                            {
                                var m = transforms[j];
                                sum += Sample(read, width, height, cx + px * m.M11 + py * m.M21 - 0.5f, cy + px * m.M12 + py * m.M22 - 0.5f);
                            }
                            var w = (y * width + x) * 4;
                            write[w] = (ushort)(sum.X * (1f / Radix) + 0.5f);
                            write[w + 1] = (ushort)(sum.Y * (1f / Radix) + 0.5f);
                            write[w + 2] = (ushort)(sum.Z * (1f / Radix) + 0.5f);
                            write[w + 3] = (ushort)(sum.W * (1f / Radix) + 0.5f);
                        }
                    }
                });
                context.ReportProgress((pass + 1) * progressStep * 0.95);
            }

            var result = (passes & 1) == 0 ? a : b;
            var output = GetMutableCopy(source, context);
            try
            {
                WriteBack(output, result, width, region, context);
                return output;
            }
            catch
            {
                if (!ReferenceEquals(output, source)) output.Dispose();
                throw;
            }
        }
        finally
        {
            ArrayPool<ushort>.Shared.Return(a);
            ArrayPool<ushort>.Shared.Return(b);
        }
    }

    /// <summary>Writes the blurred region, premultiplied at 16 bits a channel, back into <paramref name="output"/> as straight 8-bit pixels.</summary>
    private static void WriteBack(ImageBuffer output, ushort[] result, int width, Rectangle region, OperationContext context)
    {
        ParallelRows.For(context, region.Top, region.Bottom, region.Width, (start, end, band) =>
        {
            for (var y = start; y < end; y++)
            {
                var row = output.GetRow(y);
                for (var x = region.Left; x < region.Right; x++)
                {
                    var o = (y * width + x) * 4;
                    int alpha = result[o + 3];
                    if (alpha == 0)
                    {
                        row[x] = Rgba32.Transparent;
                        continue;
                    }
                    row[x] = new Rgba32(
                        (byte)Math.Min(255, (result[o] * 255 + alpha / 2) / alpha),
                        (byte)Math.Min(255, (result[o + 1] * 255 + alpha / 2) / alpha),
                        (byte)Math.Min(255, (result[o + 2] * 255 + alpha / 2) / alpha),
                        (byte)((alpha + 128) / 257));
                }
            }
        });
        context.ReportProgress(1);
    }

    private static Vector4 Load(ushort[] pixels, int o) => new(pixels[o], pixels[o + 1], pixels[o + 2], pixels[o + 3]);

    /// <summary>The bilinear sample at pixel coordinates (x, y), with the edges repeating beyond the image.</summary>
    private static Vector4 Sample(ushort[] pixels, int width, int height, float x, float y)
    {
        x = Math.Clamp(x, 0, width - 1);
        y = Math.Clamp(y, 0, height - 1);
        int x0 = (int)x, y0 = (int)y;
        int x1 = Math.Min(x0 + 1, width - 1), y1 = Math.Min(y0 + 1, height - 1);
        float fx = x - x0, fy = y - y0;
        int top = y0 * width, bottom = y1 * width;
        var t = Vector4.Lerp(Load(pixels, (top + x0) * 4), Load(pixels, (top + x1) * 4), fx);
        var u = Vector4.Lerp(Load(pixels, (bottom + x0) * 4), Load(pixels, (bottom + x1) * 4), fx);
        return Vector4.Lerp(t, u, fy);
    }
}
