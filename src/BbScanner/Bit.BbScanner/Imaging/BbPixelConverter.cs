using System.Runtime.CompilerServices;

namespace Bit.BbScanner;

/// <summary>
/// Converts packed colour pixels into the 8 bit luminance plane the decoders operate on.
/// </summary>
/// <remarks>
/// <para>
/// This is the hottest loop in the library: it touches every byte of every frame. The
/// implementation uses fixed point ITU-R BT.601 weights (<c>Y = (77R + 150G + 29B) &gt;&gt; 8</c>)
/// so no floating point conversion is needed, and processes four pixels per iteration to give
/// the JIT a wide, branch free body.
/// </para>
/// <para>
/// Downsampling happens here rather than in a later stage, because producing a smaller
/// luminance plane up front makes every subsequent stage proportionally cheaper.
/// </para>
/// </remarks>
public static class BbPixelConverter
{
    private const int RedWeight = 77;
    private const int GreenWeight = 150;
    private const int BlueWeight = 29;

    /// <summary>Number of bytes each pixel occupies in the given layout.</summary>
    /// <param name="format">Source pixel layout.</param>
    public static int BytesPerPixel(BbPixelFormat format) => format switch
    {
        BbPixelFormat.Rgba32 or BbPixelFormat.Bgra32 => 4,
        BbPixelFormat.Rgb24 => 3,
        BbPixelFormat.Gray8 => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    /// <summary>
    /// Converts a packed colour image into grayscale.
    /// </summary>
    /// <param name="source">Packed source pixels.</param>
    /// <param name="destination">Destination plane, at least <c>width * height</c> bytes.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="format">Source pixel layout.</param>
    /// <param name="sourceStride">Source row stride in bytes, or 0 to derive it from the width.</param>
    public static void ToGrayscale(
        ReadOnlySpan<byte> source,
        Span<byte> destination,
        int width,
        int height,
        BbPixelFormat format,
        int sourceStride = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var bpp = BytesPerPixel(format);
        if (sourceStride <= 0)
        {
            sourceStride = width * bpp;
        }

        if (source.Length < ((height - 1) * sourceStride) + (width * bpp))
        {
            throw new ArgumentException("Source is smaller than the declared image size.", nameof(source));
        }

        if (destination.Length < width * height)
        {
            throw new ArgumentException("Destination is smaller than the declared image size.", nameof(destination));
        }

        for (var y = 0; y < height; y++)
        {
            var row = source.Slice(y * sourceStride, width * bpp);
            var target = destination.Slice(y * width, width);
            switch (format)
            {
                case BbPixelFormat.Gray8:
                    row.CopyTo(target);
                    break;
                case BbPixelFormat.Rgba32:
                    ConvertRow32(row, target, redFirst: true);
                    break;
                case BbPixelFormat.Bgra32:
                    ConvertRow32(row, target, redFirst: false);
                    break;
                case BbPixelFormat.Rgb24:
                    ConvertRow24(row, target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }
    }

    /// <summary>
    /// Converts and downsamples in a single pass by taking every <paramref name="step"/>-th pixel
    /// of every <paramref name="step"/>-th row.
    /// </summary>
    /// <param name="source">Packed source pixels.</param>
    /// <param name="destination">Destination plane, at least <c>(width / step) * (height / step)</c> bytes.</param>
    /// <param name="width">Source width in pixels.</param>
    /// <param name="height">Source height in pixels.</param>
    /// <param name="format">Source pixel layout.</param>
    /// <param name="step">Subsampling factor; 1 means no downsampling.</param>
    /// <returns>The dimensions of the produced grayscale image.</returns>
    public static (int Width, int Height) ToGrayscaleDownsampled(
        ReadOnlySpan<byte> source,
        Span<byte> destination,
        int width,
        int height,
        BbPixelFormat format,
        int step)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(step, 1);
        if (step == 1)
        {
            ToGrayscale(source, destination, width, height, format);
            return (width, height);
        }

        var bpp = BytesPerPixel(format);
        var stride = width * bpp;
        var outWidth = width / step;
        var outHeight = height / step;

        if (outWidth == 0 || outHeight == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(step), "Downsampling would produce an empty image.");
        }

        if (destination.Length < outWidth * outHeight)
        {
            throw new ArgumentException("Destination is smaller than the downsampled image.", nameof(destination));
        }

        var (ri, gi, bi) = ChannelOffsets(format);

        for (var y = 0; y < outHeight; y++)
        {
            var row = source.Slice(y * step * stride, stride);
            var target = destination.Slice(y * outWidth, outWidth);
            if (format == BbPixelFormat.Gray8)
            {
                for (var x = 0; x < outWidth; x++)
                {
                    target[x] = row[x * step];
                }

                continue;
            }

            for (var x = 0; x < outWidth; x++)
            {
                var o = x * step * bpp;
                target[x] = (byte)(((row[o + ri] * RedWeight) + (row[o + gi] * GreenWeight) + (row[o + bi] * BlueWeight)) >> 8);
            }
        }

        return (outWidth, outHeight);
    }

    private static (int R, int G, int B) ChannelOffsets(BbPixelFormat format) => format switch
    {
        BbPixelFormat.Bgra32 => (2, 1, 0),
        _ => (0, 1, 2),
    };

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void ConvertRow32(ReadOnlySpan<byte> row, Span<byte> target, bool redFirst)
    {
        var width = target.Length;
        var wr = redFirst ? RedWeight : BlueWeight;
        var wb = redFirst ? BlueWeight : RedWeight;

        var x = 0;

        // Four pixels per iteration. The bounds checks are hoisted by slicing once, which lets
        // the JIT keep the whole body in registers.
        for (; x + 4 <= width; x += 4)
        {
            var o = x * 4;
            var p = row.Slice(o, 16);
            target[x] = (byte)(((p[0] * wr) + (p[1] * GreenWeight) + (p[2] * wb)) >> 8);
            target[x + 1] = (byte)(((p[4] * wr) + (p[5] * GreenWeight) + (p[6] * wb)) >> 8);
            target[x + 2] = (byte)(((p[8] * wr) + (p[9] * GreenWeight) + (p[10] * wb)) >> 8);
            target[x + 3] = (byte)(((p[12] * wr) + (p[13] * GreenWeight) + (p[14] * wb)) >> 8);
        }

        for (; x < width; x++)
        {
            var o = x * 4;
            target[x] = (byte)(((row[o] * wr) + (row[o + 1] * GreenWeight) + (row[o + 2] * wb)) >> 8);
        }
    }

    private static void ConvertRow24(ReadOnlySpan<byte> row, Span<byte> target)
    {
        for (var x = 0; x < target.Length; x++)
        {
            var o = x * 3;
            target[x] = (byte)(((row[o] * RedWeight) + (row[o + 1] * GreenWeight) + (row[o + 2] * BlueWeight)) >> 8);
        }
    }
}
