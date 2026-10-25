using System.Buffers;

namespace Bit.BbScanner;

/// <summary>
/// Samples a matrix symbol out of a binarised image through a <see cref="BbPerspectiveTransform"/>.
/// </summary>
public static class BbGridSampler
{
    /// <summary>
    /// Reads a <paramref name="dimensionX"/> by <paramref name="dimensionY"/> module grid from
    /// <paramref name="image"/>.
    /// </summary>
    /// <param name="image">The binarised image.</param>
    /// <param name="dimensionX">Number of modules across.</param>
    /// <param name="dimensionY">Number of modules down.</param>
    /// <param name="transform">Maps module coordinates to image coordinates.</param>
    /// <returns>The sampled matrix, or <see langword="null"/> when the grid falls outside the image.</returns>
    public static BbBitMatrix? Sample(BbBitMatrix image, int dimensionX, int dimensionY, BbPerspectiveTransform transform)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(transform);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dimensionX);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dimensionY);

        var buffer = ArrayPool<float>.Shared.Rent(dimensionX * 2);
        try
        {
            var points = buffer.AsSpan(0, dimensionX * 2);
            var result = new BbBitMatrix(dimensionX, dimensionY);
            var succeeded = true;

            for (var y = 0; y < dimensionY; y++)
            {
                var moduleCenterY = y + 0.5f;
                for (var x = 0; x < dimensionX; x++)
                {
                    points[x * 2] = x + 0.5f;
                    points[(x * 2) + 1] = moduleCenterY;
                }

                transform.TransformPoints(points);

                // A single out of bounds sample means the detector produced a quadrilateral that
                // does not fit the image, so the whole attempt is abandoned rather than silently
                // producing a matrix with fabricated modules.
                for (var x = 0; x < dimensionX; x++)
                {
                    var fx = points[x * 2];
                    var fy = points[(x * 2) + 1];

                    // A degenerate transform produces NaN, which truncates to zero and would
                    // otherwise sample the top-left pixel for every module.
                    if (!float.IsFinite(fx) || !float.IsFinite(fy))
                    {
                        succeeded = false;
                        break;
                    }

                    var px = (int)fx;
                    var py = (int)fy;
                    if ((uint)px >= (uint)image.Width || (uint)py >= (uint)image.Height)
                    {
                        succeeded = false;
                        break;
                    }

                    if (image[px, py])
                    {
                        result[x, y] = true;
                    }
                }

                if (!succeeded)
                {
                    break;
                }
            }

            if (!succeeded)
            {
                result.Dispose();
                return null;
            }

            return result;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Reads a <paramref name="dimensionX"/> by <paramref name="dimensionY"/> module grid from the luminance plane a
    /// binarised image was made from, for a symbol too small for the binarised image to have kept it.
    /// </summary>
    /// <remarks>
    /// At two pixels a module, turned, a module covers parts of four pixels and none whole, and a binariser deciding
    /// each pixel on its own loses modules a decoder can still tell apart: interpolated at its centre, a dark module
    /// is darker than the light ones around it even when no pixel of it is darker than the block threshold. So each
    /// module's centre is interpolated, and a module is dark when it is darker than halfway between the average of the
    /// seven by seven modules around it - which follows the light falling unevenly across the symbol - and the middle
    /// of the whole grid's range, which keeps a module inside a large solid area, a finder pattern's centre, from being
    /// decided by noise.
    /// </remarks>
    /// <param name="luminance">The luminance plane.</param>
    /// <param name="dimensionX">Number of modules across.</param>
    /// <param name="dimensionY">Number of modules down.</param>
    /// <param name="transform">Maps module coordinates to image coordinates.</param>
    /// <param name="inverted">Whether the symbol is light on dark.</param>
    /// <returns>The sampled matrix, or <see langword="null"/> when the grid falls outside the image.</returns>
    internal static BbBitMatrix? SampleLuminance(
        in BbLuminanceView luminance, int dimensionX, int dimensionY, BbPerspectiveTransform transform, bool inverted)
    {
        var count = dimensionX * dimensionY;
        var points = ArrayPool<float>.Shared.Rent(dimensionX * 2);
        var samples = ArrayPool<float>.Shared.Rent(count);

        // The running sums of the samples, one row and column of zeros ahead, for the average of any block in four reads.
        var sums = ArrayPool<float>.Shared.Rent((dimensionX + 1) * (dimensionY + 1));
        try
        {
            float lowest = 255, highest = 0;
            for (var y = 0; y < dimensionY; y++)
            {
                for (var x = 0; x < dimensionX; x++)
                {
                    points[x * 2] = x + 0.5f;
                    points[(x * 2) + 1] = y + 0.5f;
                }

                transform.TransformPoints(points.AsSpan(0, dimensionX * 2));
                for (var x = 0; x < dimensionX; x++)
                {
                    var (fx, fy) = (points[x * 2] - 0.5f, points[(x * 2) + 1] - 0.5f);
                    if (!float.IsFinite(fx) || !float.IsFinite(fy) || fx < 0 || fy < 0 || fx > luminance.Width - 1 || fy > luminance.Height - 1)
                    {
                        return null;
                    }

                    var value = luminance.Interpolate(fx, fy);
                    samples[(y * dimensionX) + x] = value;
                    lowest = MathF.Min(lowest, value);
                    highest = MathF.Max(highest, value);
                }
            }

            var width = dimensionX + 1;
            Array.Clear(sums, 0, width * (dimensionY + 1));
            for (var y = 0; y < dimensionY; y++)
            {
                var row = 0f;
                for (var x = 0; x < dimensionX; x++)
                {
                    row += samples[(y * dimensionX) + x];
                    sums[((y + 1) * width) + x + 1] = sums[(y * width) + x + 1] + row;
                }
            }

            var middle = (lowest + highest) / 2;
            var result = new BbBitMatrix(dimensionX, dimensionY);
            for (var y = 0; y < dimensionY; y++)
            {
                var (top, bottom) = (Math.Max(0, y - 3), Math.Min(dimensionY, y + 4));
                for (var x = 0; x < dimensionX; x++)
                {
                    var (left, right) = (Math.Max(0, x - 3), Math.Min(dimensionX, x + 4));
                    var block = sums[(bottom * width) + right] - sums[(top * width) + right] - sums[(bottom * width) + left] + sums[(top * width) + left];
                    var threshold = ((block / ((bottom - top) * (right - left))) + middle) / 2;
                    if (samples[(y * dimensionX) + x] < threshold != inverted)
                    {
                        result[x, y] = true;
                    }
                }
            }

            return result;
        }
        finally
        {
            // What the samples and their sums hold is the image's, and the pool hands them on to anyone.
            ArrayPool<float>.Shared.Return(points);
            ArrayPool<float>.Shared.Return(samples, clearArray: true);
            ArrayPool<float>.Shared.Return(sums, clearArray: true);
        }
    }
}
