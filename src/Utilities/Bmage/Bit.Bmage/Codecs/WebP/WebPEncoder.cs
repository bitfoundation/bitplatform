using System.Buffers.Binary;
using Bit.Bmage.Metadata;

namespace Bit.Bmage.Codecs.WebP;

/// <summary>
/// Managed WebP encoder. Lossy by default (VP8, with a losslessly compressed alpha channel when the image has
/// transparency); lossless (VP8L) when <see cref="ImageExportOptions.Lossless"/> is set.
/// </summary>
public sealed class WebPEncoder : IImageEncoder
{
    private static readonly ImageFormat[] SupportedFormats = [ImageFormat.WebP];
    public IReadOnlyCollection<ImageFormat> Formats => SupportedFormats;

    /// <summary>The largest width or height a lossy WebP can store.</summary>
    public const int MaxDimension = 16383;

    /// <summary>The largest width or height a lossless WebP can store, one more than a lossy one.</summary>
    public const int MaxLosslessDimension = 16384;

    public ValueTask<EncodedImage> EncodeAsync(ImageBuffer image, ImageExportOptions options, CancellationToken cancellationToken = default)
        => new(Encode(image, options, cancellationToken));

    public static EncodedImage Encode(ImageBuffer image, ImageExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= ImageExportOptions.WebP;
        var maxDimension = options.Lossless ? MaxLosslessDimension : MaxDimension;
        if (image.Width > maxDimension || image.Height > maxDimension)
            throw new ImageEncodeException($"{(options.Lossless ? "A lossless" : "A lossy")} WebP cannot store a {image.Width}x{image.Height} image: neither side may exceed {maxDimension} pixels. Resize it first, or choose another format.");

        var quality = Math.Clamp(options.Quality, 0.0, 1.0);
        var hasAlpha = image.HasTransparency();
        byte[] bytes;
        if (options.Lossless)
        {
            var vp8l = Vp8LEncoder.Encode(image, hasAlpha, cancellationToken);
            bytes = Riff([("VP8L", vp8l)]);
        }
        else
        {
            var vp8 = Vp8Encoder.Encode(image, quality, cancellationToken);
            if (!hasAlpha) bytes = Riff([("VP8 ", vp8)]);
            else
            {
                var alph = EncodeAlpha(image, cancellationToken);
                bytes = Riff([("VP8X", ExtendedHeader(image.Width, image.Height, alpha: true)), ("ALPH", alph), ("VP8 ", vp8)]);
            }
        }

        var metadata = image.Metadata?.Apply(options.Metadata, image.Width, image.Height);
        if (metadata is { IsEmpty: false }) bytes = WebPContainer.WriteMetadata(bytes, metadata);
        return new EncodedImage(bytes, ImageFormat.WebP, image.Width, image.Height, options.Lossless ? null : quality);
    }

    /// <summary>
    /// Codes the alpha channel losslessly, as a green-only lossless image, after the spatial filter estimated to make it
    /// smallest. Each try is a whole lossless encode, so the filter is picked as libwebp's default effort picks it: none
    /// for a mask of a few values, else the one the gradients favour, and, for an alpha of many values, where the guess
    /// is least sure, none too, keeping the smaller.
    /// </summary>
    private static byte[] EncodeAlpha(ImageBuffer image, CancellationToken ct)
    {
        var width = image.Width;
        var height = image.Height;
        var alpha = new byte[(long)width * height];
        Span<bool> used = stackalloc bool[256];
        var colors = 0;
        for (var y = 0; y < height; y++)
        {
            var row = image.GetRow(y);
            for (var x = 0; x < width; x++)
            {
                var a = row[x].A;
                alpha[y * width + x] = a;
                if (!used[a]) { used[a] = true; colors++; }
            }
        }

        var estimated = colors <= 16 ? 0 : EstimateBestFilter(alpha, width, height);
        byte[]? best = null;
        var filtered = new byte[alpha.Length];
        foreach (var filter in estimated == 0 || colors <= 192 ? [estimated] : (ReadOnlySpan<int>)[estimated, 0])
        {
            ct.ThrowIfCancellationRequested();
            Filter(alpha, filtered, width, height, filter);
            var stream = Vp8LEncoder.EncodeAlpha(filtered, width, height, ct);
            if (best is not null && stream.Length + 1 >= best.Length) continue;
            best = new byte[stream.Length + 1];
            best[0] = (byte)(filter << 2 | 1); // filter, lossless compression
            stream.CopyTo(best, 1);
        }
        return best!;
    }

    /// <summary>
    /// libwebp's estimate of the best alpha filter: over every other pixel of every other row, which of the coarse
    /// prediction errors (sixteenths of the range) each filter produces at all, the filter whose errors stay smallest
    /// winning.
    /// </summary>
    internal static int EstimateBestFilter(ReadOnlySpan<byte> alpha, int width, int height)
    {
        Span<bool> seen = stackalloc bool[4 * 16];
        for (var y = 2; y < height - 1; y += 2)
        {
            var row = y * width;
            int mean = alpha[row];
            for (var x = 2; x < width - 1; x += 2)
            {
                var i = row + x;
                int value = alpha[i], left = alpha[i - 1], up = alpha[i - width];
                var gradient = Math.Clamp(left + up - alpha[i - width - 1], 0, 255);
                seen[Math.Abs(value - mean) >> 4] = true;
                seen[16 + (Math.Abs(value - left) >> 4)] = true;
                seen[32 + (Math.Abs(value - up) >> 4)] = true;
                seen[48 + (Math.Abs(value - gradient) >> 4)] = true;
                mean = (3 * mean + value + 2) >> 2;
            }
        }
        var best = 0;
        var bestScore = int.MaxValue;
        for (var filter = 0; filter < 4; filter++)
        {
            var score = 0;
            for (var bin = 0; bin < 16; bin++)
                if (seen[filter * 16 + bin]) score += bin;
            if (score < bestScore) (best, bestScore) = (filter, score);
        }
        return best;
    }

    /// <summary>Applies one of the 'ALPH' chunk's spatial filters, the inverse of <see cref="WebPDecoder.DecodeAlpha"/>'s.</summary>
    internal static void Filter(ReadOnlySpan<byte> alpha, Span<byte> output, int width, int height, int filter)
    {
        if (filter == 0)
        {
            alpha.CopyTo(output);
            return;
        }
        output[0] = alpha[0];
        for (var x = 1; x < width; x++) output[x] = (byte)(alpha[x] - alpha[x - 1]);
        for (var y = 1; y < height; y++)
        {
            var row = y * width;
            output[row] = (byte)(alpha[row] - alpha[row - width]);
            for (var x = 1; x < width; x++)
            {
                var i = row + x;
                int predictor = filter switch
                {
                    1 => alpha[i - 1],
                    2 => alpha[i - width],
                    _ => Math.Clamp(alpha[i - 1] + alpha[i - width] - alpha[i - width - 1], 0, 255),
                };
                output[i] = (byte)(alpha[i] - predictor);
            }
        }
    }

    private static byte[] ExtendedHeader(int width, int height, bool alpha)
    {
        var header = new byte[10];
        header[0] = (byte)(alpha ? 0x10 : 0);
        var w = width - 1;
        var h = height - 1;
        header[4] = (byte)w; header[5] = (byte)(w >> 8); header[6] = (byte)(w >> 16);
        header[7] = (byte)h; header[8] = (byte)(h >> 8); header[9] = (byte)(h >> 16);
        return header;
    }

    private static byte[] Riff(ReadOnlySpan<(string FourCc, byte[] Data)> chunks)
    {
        long length = 4;
        foreach (var (_, data) in chunks) length += 8 + data.Length + (data.Length & 1);
        if (length > uint.MaxValue - 8) throw new ImageEncodeException("The WebP would exceed the 4 GB a RIFF file can hold.");
        var output = new byte[8 + length];
        "RIFF"u8.CopyTo(output);
        BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(4), (uint)length);
        "WEBP"u8.CopyTo(output.AsSpan(8));
        var pos = 12;
        foreach (var (fourcc, data) in chunks)
        {
            for (var i = 0; i < 4; i++) output[pos + i] = (byte)fourcc[i];
            BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(pos + 4), (uint)data.Length);
            data.CopyTo(output, pos + 8);
            pos += 8 + data.Length + (data.Length & 1);
        }
        return output;
    }
}
