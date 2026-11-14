using System.Buffers;
using System.Drawing;

namespace Bit.Bmage.Operations.Filters;

/// <summary>How <see cref="AdaptiveThresholdFilter"/> sets the level each pixel is compared with.</summary>
public enum AdaptiveThresholdMethod
{
    /// <summary>
    /// Sauvola's method: the local mean, lowered where the neighbourhood has little contrast. Keeps text crisp on a page
    /// lit unevenly and leaves the plain paper white instead of speckled, which is what document scans need.
    /// </summary>
    Sauvola = 0,

    /// <summary>
    /// The local mean less a fixed offset. Faster and simpler; plain areas pick up noise more easily than with Sauvola's
    /// method, but fine lines in strong contrast survive better.
    /// </summary>
    Mean,
}

/// <summary>
/// Converts to pure black and white by comparing each pixel with its surroundings rather than with one level for the
/// whole image: a photographed page with a shadow across it, a receipt under a desk lamp, a barcode on a curved, glossy
/// label. Alpha is kept.
/// </summary>
/// <remarks>
/// <para>
/// A pixel turns black when its luminance is at or below a level computed from the square window of
/// <c>2 × Radius + 1</c> pixels around it. With <see cref="AdaptiveThresholdMethod.Sauvola"/> that level is
/// <c>mean × (1 + Bias × (deviation / 128 − 1))</c> on 0..255 luminance; with <see cref="AdaptiveThresholdMethod.Mean"/>
/// it is <c>mean − Bias × 255</c>. A larger bias asks a pixel to be darker than its surroundings by more, so less turns
/// black. The window should be somewhat larger than the strokes to keep: about the height of a line of text.
/// </para>
/// <para>
/// The window is clipped at the image's edges, and always measured over the whole image even when only a region is
/// thresholded, so a region's border pixels are judged as they would be in the full image. Window sums slide down the
/// rows and along each row, so the cost does not depend on the radius; the extra memory is one byte per pixel of the
/// image for its luminance and a few rows of sums per thread.
/// </para>
/// </remarks>
public sealed class AdaptiveThresholdFilter : RegionOperation
{
    /// <summary>The largest window radius, in pixels.</summary>
    public const float MaxRadius = 500;

    /// <param name="radius">Half the window's side in pixels, between 1 and 500: about the height of a line of text.</param>
    /// <param name="bias">
    /// How much darker than its surroundings a pixel must be to turn black, in [0,1]. For Sauvola's method this is its
    /// <c>k</c>, usually between 0.2 and 0.5; for the mean method it is the offset below the mean, as a share of the range.
    /// </param>
    /// <param name="method">How the local level is set.</param>
    /// <param name="region">The part of the image to threshold, or null for all of it.</param>
    public AdaptiveThresholdFilter(float radius = 16, float bias = 0.2f, AdaptiveThresholdMethod method = AdaptiveThresholdMethod.Sauvola, Rectangle? region = null)
        : base(region)
    {
        if (!float.IsFinite(radius) || radius < 1 || radius > MaxRadius)
            throw new ArgumentOutOfRangeException(nameof(radius), radius, $"The radius must be between 1 and {MaxRadius} pixels.");
        if (!float.IsFinite(bias) || bias < 0 || bias > 1)
            throw new ArgumentOutOfRangeException(nameof(bias), bias, "The bias must be between 0 and 1.");
        if (!Enum.IsDefined(method)) throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown method.");
        Radius = radius;
        Bias = bias;
        Method = method;
    }

    /// <summary>Half the window's side in pixels.</summary>
    public float Radius { get; }

    /// <summary>How much darker than its surroundings a pixel must be to turn black, in [0,1].</summary>
    public float Bias { get; }

    /// <summary>How the local level is set.</summary>
    public AdaptiveThresholdMethod Method { get; }

    public override string Name => "Adaptive threshold";

    /// <inheritdoc />
    public override IImageOperation ForScale(double scale)
        => scale == 1 ? this : new AdaptiveThresholdFilter(Math.Clamp((float)(Radius * scale), 1, MaxRadius), Bias, Method, ScaleRegion(Region, scale));

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var region = GetRegion(source.Size);
        if (region.Width <= 0 || region.Height <= 0) return GetMutableCopy(source, context);

        int width = source.Width, height = source.Height;
        var radius = (int)MathF.Round(Radius);
        // Luminance of every pixel, once: each row is read by the window of every row within the radius.
        var luma = ArrayPool<byte>.Shared.Rent(width * height);
        ImageBuffer? output = null;
        try
        {
            output = GetMutableCopy(source, context);
            ParallelRows.For(context, 0, height, width, (start, end, band) =>
            {
                for (var y = start; y < end; y++)
                {
                    if ((y & 31) == 0) band.ThrowIfCancellationRequested();
                    var row = source.GetRow(y);
                    var line = luma.AsSpan(y * width, width);
                    // Rec. 709 weights on the encoded values, as ThresholdFilter uses, in 8.8 fixed point: 54 + 183 + 19 = 256.
                    for (var x = 0; x < row.Length; x++)
                    {
                        var p = row[x];
                        line[x] = (byte)((p.R * 54 + p.G * 183 + p.B * 19 + 128) >> 8);
                    }
                }
            });

            var target = output;
            ParallelRows.For(context, region.Top, region.Bottom, (long)region.Width * 4, (start, end, band) =>
                ThresholdRows(luma, target, width, height, region, radius, start, end, band));
            context.ReportProgress(1);
            return target;
        }
        catch
        {
            if (output is not null && !ReferenceEquals(output, source)) output.Dispose();
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(luma);
        }
    }

    private void ThresholdRows(byte[] luma, ImageBuffer output, int width, int height, Rectangle region, int radius, int start, int end, OperationContext band)
    {
        // Column sums over the window's rows, then their running totals along the row, so any window's sum is two reads.
        var columns = ArrayPool<int>.Shared.Rent(width);
        var squares = ArrayPool<long>.Shared.Rent(width);
        var prefix = ArrayPool<long>.Shared.Rent(width + 1);
        var prefixSquares = ArrayPool<long>.Shared.Rent(width + 1);
        try
        {
            Array.Clear(columns, 0, width);
            Array.Clear(squares, 0, width);
            // The window of the band's first row: rows [start - radius, start + radius] that exist.
            int windowTop = Math.Max(0, start - radius), windowBottom = Math.Min(height - 1, start + radius);
            for (var y = windowTop; y <= windowBottom; y++) AddRow(luma.AsSpan(y * width, width), columns, squares, 1);

            var sauvola = Method == AdaptiveThresholdMethod.Sauvola;
            var bias = Bias;
            var offset = bias * 255;
            var rows = (end - start) * 1.0;
            for (var y = start; y < end; y++)
            {
                if ((y & 15) == 0) band.ThrowIfCancellationRequested();
                // Slide the window to this row's: drop the row that left at the top, add the one that came in at the bottom.
                var top = Math.Max(0, y - radius);
                var bottom = Math.Min(height - 1, y + radius);
                while (windowTop < top) AddRow(luma.AsSpan(windowTop++ * width, width), columns, squares, -1);
                while (windowBottom < bottom) AddRow(luma.AsSpan(++windowBottom * width, width), columns, squares, 1);
                var windowRows = bottom - top + 1;

                prefix[0] = 0;
                prefixSquares[0] = 0;
                for (var x = 0; x < width; x++)
                {
                    prefix[x + 1] = prefix[x] + columns[x];
                    prefixSquares[x + 1] = prefixSquares[x] + squares[x];
                }

                var line = luma.AsSpan(y * width, width);
                var pixels = output.GetRow(y);
                for (var x = region.Left; x < region.Right; x++)
                {
                    var left = Math.Max(0, x - radius);
                    var right = Math.Min(width - 1, x + radius);
                    var count = (double)(right - left + 1) * windowRows;
                    var sum = prefix[right + 1] - prefix[left];
                    var mean = sum / count;
                    double level;
                    if (sauvola)
                    {
                        var variance = (prefixSquares[right + 1] - prefixSquares[left]) / count - mean * mean;
                        var deviation = variance > 0 ? Math.Sqrt(variance) : 0;
                        level = mean * (1 + bias * (deviation / 128 - 1));
                    }
                    else
                    {
                        level = mean - offset;
                    }
                    var v = line[x] <= level ? (byte)0 : (byte)255;
                    pixels[x] = new Rgba32(v, v, v, pixels[x].A);
                }
                band.ReportProgress((y - start + 1) / rows);
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(columns);
            ArrayPool<long>.Shared.Return(squares);
            ArrayPool<long>.Shared.Return(prefix);
            ArrayPool<long>.Shared.Return(prefixSquares);
        }
    }

    private static void AddRow(ReadOnlySpan<byte> row, int[] columns, long[] squares, int sign)
    {
        for (var x = 0; x < row.Length; x++)
        {
            int v = row[x];
            columns[x] += sign * v;
            squares[x] += sign * v * v;
        }
    }
}
