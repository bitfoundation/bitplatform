using System.Buffers;
using System.Drawing;
using System.Numerics;
using Bit.Bmage.Geometry;

namespace Bit.Bmage.Operations.Filters;

/// <summary>
/// Blurs along one direction, as a camera moving during the exposure does: a sense of speed behind a car, a panned
/// shot, a streak of light. Each pixel becomes the average of the pixels along a line through it.
/// </summary>
/// <remarks>
/// <para>
/// The line is <see cref="Distance"/> pixels long and centred on the pixel. It is sampled one pixel apart along its
/// steeper axis and interpolated across the other, in premultiplied alpha so transparent pixels do not darken edges, and
/// its two end samples are weighted by how much of them the length covers, so dragging the distance changes the blur
/// smoothly. Beyond the image the edge pixels repeat, so an opaque photo stays opaque to its borders.
/// </para>
/// <para>
/// The cost grows with the distance: about one sample per pixel of the line for every output pixel. Rows are read once
/// into a cache of the few the line spans, in column stripes, so memory stays at a few rows' worth whatever the image
/// size, and the stripes run side by side with identical pixels.
/// </para>
/// </remarks>
public sealed class MotionBlurFilter : RegionOperation
{
    /// <summary>The longest blur, in pixels.</summary>
    public const float MaxDistance = 1000;

    /// <param name="distance">The length of the streak in pixels, up to 1000.</param>
    /// <param name="angle">The direction of motion in degrees: 0 horizontal, 90 vertical, 45 rising to the right.</param>
    /// <param name="region">The part of the image to blur, or null for all of it.</param>
    public MotionBlurFilter(float distance, float angle = 0, Rectangle? region = null) : base(region)
    {
        if (!float.IsFinite(distance) || distance < 0 || distance > MaxDistance)
            throw new ArgumentOutOfRangeException(nameof(distance), distance, $"The distance must be between 0 and {MaxDistance} pixels.");
        if (!float.IsFinite(angle)) throw new ArgumentOutOfRangeException(nameof(angle), "The angle must be a finite number of degrees.");
        Distance = distance;
        // A line has no direction, so angles half a turn apart blur the same way; (-90, 90] keeps one of them.
        var a = angle % 180;
        if (a <= -90) a += 180;
        else if (a > 90) a -= 180;
        Angle = a;
    }

    /// <summary>The length of the streak in pixels.</summary>
    public float Distance { get; }

    /// <summary>The direction of motion in degrees, normalised to (-90, 90].</summary>
    public float Angle { get; }

    public override string Name => "Motion blur";

    public override bool IsIdentity => Distance < 1;

    /// <summary>Output columns per stripe: enough to keep the loops long, few enough to keep the row cache small.</summary>
    private const int StripeWidth = 256;

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var region = GetRegion(source.Size);
        if (region.IsEmptyArea() || IsIdentity) return GetMutableCopy(source, context);

        var kernel = new Kernel(Distance, Angle);
        // The output is written while the source is still read for rows below, so it never overwrites the source.
        var output = context.Allocate(source.Width, source.Height, clear: false);
        output.Metadata = source.Metadata;
        try
        {
            source.CopyTo(output);
            var stripes = (region.Width + StripeWidth - 1) / StripeWidth;
            ParallelRows.For(context, 0, stripes, (long)StripeWidth * region.Height * kernel.Taps, (start, end, band) =>
            {
                for (var stripe = start; stripe < end; stripe++)
                {
                    var x0 = region.X + stripe * StripeWidth;
                    BlurStripe(source, output, kernel, x0, Math.Min(region.Right, x0 + StripeWidth), region.Top, region.Bottom, band);
                    band.ReportProgress((stripe + 1) / (double)stripes);
                }
            });
            context.ReportProgress(1);
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    /// <summary>The samples along the line: where each lies and how much it counts.</summary>
    private sealed class Kernel
    {
        public Kernel(float distance, float angle)
        {
            var radians = angle * MathF.PI / 180;
            var dx = MathF.Cos(radians);
            // Image rows grow downwards, so a positive angle rises to the right.
            var dy = -MathF.Sin(radians);
            Horizontal = MathF.Abs(dx) >= MathF.Abs(dy);
            // The line advances one pixel along its steeper axis per sample, and by this much along the other.
            Slope = Horizontal ? dy / dx : dx / dy;
            // The line covers [-half, half] along the steeper axis, and sample k stands for [k - 0.5, k + 0.5] of it: the
            // samples it covers fully count once, and the pair at its ends by the part of them it reaches.
            var half = distance / 2 * (Horizontal ? MathF.Abs(dx) : MathF.Abs(dy));
            var beyondCentre = half - 0.5f;
            var whole = Math.Max(0, (int)MathF.Floor(beyondCentre));
            var end = beyondCentre > 0 ? beyondCentre - MathF.Floor(beyondCentre) : 0;
            Reach = end > 1e-4f ? whole + 1 : whole;
            Weights = new float[2 * Reach + 1];
            for (var k = -Reach; k <= Reach; k++) Weights[k + Reach] = Math.Abs(k) <= whole ? 1 : end;
            var total = Weights.Sum();
            for (var i = 0; i < Weights.Length; i++) Weights[i] /= total;
            // How far the line reaches across its shallower axis, rounded out, plus the pixel interpolation reads beyond it.
            Across = (int)MathF.Ceiling(Reach * MathF.Abs(Slope)) + 1;
        }

        /// <summary>True when the line is closer to horizontal: samples step along rows, and are interpolated between them.</summary>
        public bool Horizontal { get; }

        /// <summary>Movement across the steeper axis per sample.</summary>
        public float Slope { get; }

        /// <summary>Samples either side of the centre.</summary>
        public int Reach { get; }

        /// <summary>The weight of each sample, from <c>-Reach</c> to <c>Reach</c>, summing to one.</summary>
        public float[] Weights { get; }

        /// <summary>Pixels the samples reach across the shallower axis.</summary>
        public int Across { get; }

        public int Taps => Weights.Length;

        /// <summary>Rows above and below an output row the samples read.</summary>
        public int RowReach => Horizontal ? Across : Reach;

        /// <summary>Columns left and right of an output column the samples read.</summary>
        public int ColumnReach => Horizontal ? Reach : Across;
    }

    /// <summary>Blurs output columns <paramref name="x0"/> to <paramref name="x1"/> of rows <paramref name="top"/> to <paramref name="bottom"/>.</summary>
    private static void BlurStripe(ImageBuffer source, ImageBuffer output, Kernel kernel, int x0, int x1, int top, int bottom, OperationContext context)
    {
        int width = source.Width, height = source.Height;
        // The source columns the stripe's samples read, edges repeated beyond the image.
        var c0 = Math.Max(0, x0 - kernel.ColumnReach);
        var c1 = Math.Min(width, x1 + kernel.ColumnReach + 1);
        var columns = c1 - c0;
        var stripe = x1 - x0;
        var rowReach = kernel.RowReach;
        var cacheRows = 2 * rowReach + 1;
        var pool = ArrayPool<Vector4>.Shared;
        var cache = pool.Rent(cacheRows * columns);
        var accumulator = pool.Rent(stripe);
        // Which source row each cache slot holds; -1 for none.
        var held = ArrayPool<int>.Shared.Rent(cacheRows);
        held.AsSpan(0, cacheRows).Fill(-1);
        try
        {
            Span<Vector4> Row(int y)
            {
                y = Math.Clamp(y, 0, height - 1);
                var slot = y % cacheRows;
                var row = cache.AsSpan(slot * columns, columns);
                if (held[slot] != y)
                {
                    PixelRows.LoadPremultiplied(source.GetRow(y).Slice(c0, columns), row);
                    held[slot] = y;
                }
                return row;
            }

            var weights = kernel.Weights;
            var reach = kernel.Reach;
            var slope = kernel.Slope;
            for (var y = top; y < bottom; y++)
            {
                if (((y - top) & 15) == 0) context.ThrowIfCancellationRequested();
                var acc = accumulator.AsSpan(0, stripe);
                acc.Clear();
                for (var k = -reach; k <= reach; k++)
                {
                    var w = weights[k + reach];
                    if (kernel.Horizontal)
                    {
                        // Every output pixel of the row reads the same two rows for this sample, shifted by k columns.
                        var position = y + k * slope;
                        var below = (int)MathF.Floor(position);
                        var t = position - below;
                        var a = Row(below);
                        var b = Row(below + 1);
                        var wa = new Vector4(w * (1 - t));
                        var wb = new Vector4(w * t);
                        for (var x = 0; x < stripe; x++)
                        {
                            var c = Math.Clamp(x0 + x + k, 0, width - 1) - c0;
                            acc[x] += a[c] * wa + b[c] * wb;
                        }
                    }
                    else
                    {
                        // One row for this sample, read a fractional number of columns aside.
                        var row = Row(y + k);
                        var shift = k * slope;
                        var left = (int)MathF.Floor(shift);
                        var t = shift - left;
                        var wa = new Vector4(w * (1 - t));
                        var wb = new Vector4(w * t);
                        for (var x = 0; x < stripe; x++)
                        {
                            var ca = Math.Clamp(x0 + x + left, 0, width - 1) - c0;
                            var cb = Math.Clamp(x0 + x + left + 1, 0, width - 1) - c0;
                            acc[x] += row[ca] * wa + row[cb] * wb;
                        }
                    }
                }
                PixelRows.StoreUnpremultiplied(acc, output.GetRow(y).Slice(x0, stripe));
            }
        }
        finally
        {
            pool.Return(cache);
            pool.Return(accumulator);
            ArrayPool<int>.Shared.Return(held);
        }
    }

    public override IImageOperation ForScale(double scale)
        => scale == 1 ? this : new MotionBlurFilter((float)Math.Min(MaxDistance, Distance * scale), Angle, ScaleRegion(Region, scale));

    public override string ToString() => $"MotionBlur({Distance:0.##}px, {Angle:0.##}°)";
}

/// <summary>
/// Keeps only the fine detail of an image: each channel becomes mid grey plus its difference from a blurred copy.
/// Blended over the image with <see cref="Drawing.BlendMode.Overlay"/> or <see cref="Drawing.BlendMode.SoftLight"/> it
/// sharpens; on its own it shows edges and texture, and finds blurred areas.
/// </summary>
public sealed class HighPassFilter : RegionOperation
{
    /// <param name="radius">The radius of the blur subtracted, in pixels: the size of the detail kept.</param>
    /// <param name="region">The part of the image to filter, or null for all of it.</param>
    public HighPassFilter(float radius = 3f, Rectangle? region = null) : base(region)
    {
        if (!float.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius), "The radius must be a positive number of pixels.");
        Radius = radius;
    }

    /// <summary>The radius (standard deviation) of the blur subtracted, in pixels.</summary>
    public float Radius { get; }

    public override string Name => "High pass";

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var region = GetRegion(source.Size);
        var target = GetMutableCopy(source, context);
        if (region.IsEmptyArea()) return target;
        // The blur hands over each blurred row while the original row is still in place, as sharpening uses it.
        BoxBlur.Gaussian(target, region, Radius, context, (y, blurred) =>
        {
            var row = target.GetRow(region.Y + y).Slice(region.X, region.Width);
            for (var x = 0; x < row.Length; x++)
            {
                var p = row[x];
                var b = blurred[x];
                row[x] = new Rgba32(Rgba32.ClampToByte(128 + p.R - b.R), Rgba32.ClampToByte(128 + p.G - b.G), Rgba32.ClampToByte(128 + p.B - b.B), p.A);
            }
        });
        return target;
    }

    public override IImageOperation ForScale(double scale) => scale == 1 ? this : new HighPassFilter((float)Math.Max(0.5, Radius * scale), ScaleRegion(Region, scale));
}
