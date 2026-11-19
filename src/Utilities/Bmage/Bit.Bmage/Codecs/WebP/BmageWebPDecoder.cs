using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Bit.Bmage;

/// <summary>
/// Managed WebP decoder: lossy (VP8) with or without an alpha channel, lossless (VP8L), the extended format's metadata,
/// and the first frame of an animation.
/// </summary>
public sealed class BmageWebPDecoder : IBmageImageDecoder
{
    private static readonly BmageImageFormat[] SupportedFormats = [BmageImageFormat.WebP];
    public IReadOnlyCollection<BmageImageFormat> Formats => SupportedFormats;

    private const int AlphaFlag = 0x10;
    private const int AnimationFlag = 0x02;

    public BmageImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        if (BmageImageFormats.Detect(data) != BmageImageFormat.WebP) return null;
        var chunks = BmageWebPContainer.EnumerateChunks(data);
        if (chunks.Count == 0) return null;
        var first = chunks[0];
        var payload = data.Slice(first.DataOffset, first.DataLength);
        int width, height;
        bool hasAlpha;
        switch (first.FourCc)
        {
            case "VP8 " when BmageVp8Decoder.ReadHeader(payload) is { } lossy:
                (width, height, hasAlpha) = (lossy.Width, lossy.Height, false);
                break;
            case "VP8L" when BmageVp8LDecoder.ReadHeader(payload) is { } lossless:
                (width, height, hasAlpha) = lossless;
                break;
            case "VP8X" when payload.Length >= 10:
                width = 1 + (payload[4] | payload[5] << 8 | payload[6] << 16);
                height = 1 + (payload[7] | payload[8] << 8 | payload[9] << 16);
                hasAlpha = (payload[0] & AlphaFlag) != 0;
                break;
            default:
                return null;
        }
        var orientation = BmageOrientation.Normal;
        foreach (var c in chunks)
            if (c.FourCc == "EXIF") { orientation = BmageExifReader.Read(data.Slice(c.DataOffset, c.DataLength))?.Orientation ?? BmageOrientation.Normal; break; }
        return new BmageImageInfo(width, height, BmageImageFormat.WebP, orientation, hasAlpha);
    }

    public ValueTask<BmageImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken cancellationToken = default)
        => new(DecodeCore(data, options, allocator, cancellationToken));

    public static BmageImageBuffer Decode(ReadOnlySpan<byte> data, BmageDecodeOptions? options = null, IBmagePixelAllocator? allocator = null, CancellationToken cancellationToken = default)
    {
        // The bitstream readers read the file in place, which a span cannot be kept for, so it is copied, once it is
        // known not to be over the limit.
        (options ?? BmageDecodeOptions.Default).Limits.ValidateEncodedSize(data.Length);
        return DecodeCore(data.ToArray(), options, allocator, cancellationToken);
    }

    private static BmageImageBuffer DecodeCore(ReadOnlyMemory<byte> file, BmageDecodeOptions? options, IBmagePixelAllocator? allocator, CancellationToken cancellationToken)
    {
        options ??= BmageDecodeOptions.Default;
        allocator ??= BmagePooledPixelAllocator.Shared;
        var data = file.Span;
        options.Limits.ValidateEncodedSize(data.Length);
        if (BmageImageFormats.Detect(data) != BmageImageFormat.WebP) throw new BmageImageDecodeException("Invalid WebP: missing RIFF/WEBP header.");
        var chunks = BmageWebPContainer.EnumerateChunks(data);
        if (chunks.Count == 0) throw new BmageImageDecodeException("Invalid WebP: no chunks.");

        BmageImageBuffer image;
        var first = chunks[0];
        if (first.FourCc == "VP8X")
        {
            var header = data.Slice(first.DataOffset, first.DataLength);
            if (header.Length < 10) throw new BmageImageDecodeException("Invalid WebP: truncated VP8X header.");
            var width = 1 + (header[4] | header[5] << 8 | header[6] << 16);
            var height = 1 + (header[7] | header[8] << 8 | header[9] << 16);
            options.Limits.Validate(width, height);
            image = (header[0] & AnimationFlag) != 0
                ? DecodeFirstFrame(file, chunks, width, height, options, allocator, cancellationToken)
                : DecodeStill(file, chunks, width, height, options, allocator, cancellationToken);
        }
        else image = DecodeStill(file, chunks, 0, 0, options, allocator, cancellationToken);

        try
        {
            if (options.ReadMetadata)
            {
                var md = BmageWebPContainer.ReadMetadata(data, options.FileName);
                image.Metadata = md;
                if (options.AutoOrient && md.Orientation is not (BmageOrientation.Normal or BmageOrientation.Unspecified))
                {
                    var oriented = new BmageOrientationOperation(md.Orientation).Apply(image, new BmageOperationContext(allocator, options.Limits, cancellationToken, canMutateSource: true, maxDegreeOfParallelism: options.MaxDegreeOfParallelism));
                    if (!ReferenceEquals(oriented, image)) image.Dispose();
                    image = oriented;
                    image.Metadata = md.WithOrientation(BmageOrientation.Normal);
                }
            }
            else image.Metadata = new BmageImageMetadata(sourceFormat: BmageImageFormat.WebP, fileName: options.FileName);
            return image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Decodes a still image: a lone 'VP8 ' or 'VP8L' chunk, or, in the extended format, the image chunk with the 'ALPH'
    /// chunk before it. <paramref name="canvasWidth"/> is 0 for the simple format, which has no canvas.
    /// </summary>
    private static BmageImageBuffer DecodeStill(ReadOnlyMemory<byte> data, List<BmageWebPContainer.Chunk> chunks, int canvasWidth, int canvasHeight, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken ct)
    {
        BmageWebPContainer.Chunk? alpha = null, bitstream = null;
        foreach (var c in chunks)
        {
            if (c.FourCc == "ALPH") alpha ??= c;
            else if (c.FourCc is "VP8 " or "VP8L") { bitstream = c; break; }
        }
        if (bitstream is not { } b) throw new BmageImageDecodeException("Invalid WebP: no image data.");
        var payload = data.Slice(b.DataOffset, b.DataLength);
        var alphaData = alpha is { } a ? data.Slice(a.DataOffset, a.DataLength) : default;
        var (width, height) = ReadFrameSize(b.FourCc, payload.Span);
        if (canvasWidth != 0 && (width != canvasWidth || height != canvasHeight))
            throw new BmageImageDecodeException("Invalid WebP: the image size does not match the canvas size.");
        options.Limits.Validate(width, height);

        var image = BmageImageBuffer.Create(width, height, allocator, clear: false, options.Limits);
        try
        {
            DecodeFrame(b.FourCc, payload, alpha is not null, alphaData, width, height, image, 0, 0, ct);
            return image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Decodes the first frame of an animation onto a transparent canvas. Later frames are not decoded: like GIF, an
    /// animated WebP becomes a still of its opening frame.
    /// </summary>
    private static BmageImageBuffer DecodeFirstFrame(ReadOnlyMemory<byte> data, List<BmageWebPContainer.Chunk> chunks, int canvasWidth, int canvasHeight, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken ct)
    {
        BmageWebPContainer.Chunk? frame = null;
        foreach (var c in chunks) if (c.FourCc == "ANMF") { frame = c; break; }
        if (frame is not { } f || f.DataLength < 16) throw new BmageImageDecodeException("Invalid WebP: animation without frames.");
        var header = data.Span.Slice(f.DataOffset, 16);
        var x = 2 * (header[0] | header[1] << 8 | header[2] << 16);
        var y = 2 * (header[3] | header[4] << 8 | header[5] << 16);
        var frameWidth = 1 + (header[6] | header[7] << 8 | header[8] << 16);
        var frameHeight = 1 + (header[9] | header[10] << 8 | header[11] << 16);
        if ((long)x + frameWidth > canvasWidth || (long)y + frameHeight > canvasHeight)
            throw new BmageImageDecodeException("Invalid WebP: the first frame lies outside the canvas.");

        // The frame's own chunks follow its 16 byte header, laid out like the top level ones.
        var frameData = data.Span.Slice(f.DataOffset + 16, f.DataLength - 16);
        BmageWebPContainer.Chunk? alpha = null, bitstream = null;
        var pos = 0;
        while (pos + 8 <= frameData.Length)
        {
            var fourcc = System.Text.Encoding.ASCII.GetString(frameData.Slice(pos, 4));
            var length = (long)BinaryPrimitives.ReadUInt32LittleEndian(frameData[(pos + 4)..]);
            if (pos + 8 + length > frameData.Length) break;
            var chunk = new BmageWebPContainer.Chunk(fourcc, f.DataOffset + 16 + pos, f.DataOffset + 16 + pos + 8, (int)length);
            if (fourcc == "ALPH") alpha ??= chunk;
            else if (fourcc is "VP8 " or "VP8L") { bitstream = chunk; break; }
            pos += 8 + (int)length + ((int)length & 1);
        }
        if (bitstream is not { } b) throw new BmageImageDecodeException("Invalid WebP: the first frame has no image data.");
        var payload = data.Slice(b.DataOffset, b.DataLength);
        var (width, height) = ReadFrameSize(b.FourCc, payload.Span);
        if (width != frameWidth || height != frameHeight) throw new BmageImageDecodeException("Invalid WebP: the frame size does not match its header.");

        var image = BmageImageBuffer.Create(canvasWidth, canvasHeight, allocator, clear: true, options.Limits);
        try
        {
            var alphaData = alpha is { } a ? data.Slice(a.DataOffset, a.DataLength) : default;
            DecodeFrame(b.FourCc, payload, alpha is not null, alphaData, width, height, image, x, y, ct);
            return image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
    }

    private static (int Width, int Height) ReadFrameSize(string fourcc, ReadOnlySpan<byte> payload)
    {
        if (fourcc == "VP8L")
            return BmageVp8LDecoder.ReadHeader(payload) is { } l ? (l.Width, l.Height) : throw new BmageImageDecodeException("Invalid WebP lossless header.");
        var size = BmageVp8Decoder.ReadHeader(payload) ?? throw new BmageImageDecodeException("Invalid WebP lossy header.");

        // Every macroblock's prediction modes are coded in the first partition, at well over half a bit each however
        // likely they are: a partition far too short for the frame it declares is refused here, before the frame is
        // allocated - a 358 byte file declaring 8909 x 4302 allocated 311 MB before it was found to be truncated.
        var firstPartition = Math.Min((payload[0] | payload[1] << 8 | payload[2] << 16) >> 5, payload.Length - 10);
        var macroblocks = (long)((size.Width + 15) >> 4) * ((size.Height + 15) >> 4);
        if (firstPartition * 16L < macroblocks) throw new BmageImageDecodeException("Truncated WebP lossy data.");
        return size;
    }

    /// <summary>Decodes one bitstream chunk into the rectangle of <paramref name="image"/> at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private static void DecodeFrame(string fourcc, ReadOnlyMemory<byte> payload, bool hasAlphaChunk, ReadOnlyMemory<byte> alphaChunk, int width, int height, BmageImageBuffer image, int x, int y, CancellationToken ct)
    {
        if (fourcc == "VP8L")
        {
            if (x == 0 && y == 0 && width == image.Width && height == image.Height)
            {
                // ARGB words in memory are B, G, R, A, so swapping red and blue in place gives the buffer's RGBA.
                var argb = MemoryMarshal.Cast<byte, uint>(image.Bytes);
                BmageVp8LDecoder.Decode(payload, width, height, argb, ct);
                for (var i = 0; i < argb.Length; i++) argb[i] = ArgbToRgba(argb[i]);
            }
            else
            {
                var argb = new uint[(long)width * height];
                BmageVp8LDecoder.Decode(payload, width, height, argb, ct);
                for (var r = 0; r < height; r++)
                {
                    var dst = MemoryMarshal.Cast<BmageRgba32, uint>(image.GetRow(y + r).Slice(x, width));
                    for (var c = 0; c < width; c++) dst[c] = ArgbToRgba(argb[r * width + c]);
                }
            }
            return;
        }

        var frame = BmageVp8Decoder.Decode(payload, ct);
        byte[]? alpha = null;
        if (hasAlphaChunk) alpha = DecodeAlpha(alphaChunk, width, height, ct);
        BmageVp8Yuv.ToRgba(frame, alpha, image, x, y, ct);
    }

    private static uint ArgbToRgba(uint v) => (v & 0xFF00FF00u) | ((v >> 16) & 0xFFu) | ((v & 0xFFu) << 16);

    /// <summary>Decodes an 'ALPH' chunk: raw or losslessly compressed values, then the spatial filter undone.</summary>
    internal static byte[] DecodeAlpha(ReadOnlyMemory<byte> chunk, int width, int height, CancellationToken ct)
    {
        if (chunk.Length < 1) throw new BmageImageDecodeException("Invalid WebP: empty alpha chunk.");
        var header = chunk.Span[0];
        var compression = header & 3;
        var filter = (header >> 2) & 3;
        var alpha = new byte[(long)width * height];
        switch (compression)
        {
            case 0:
                if (chunk.Length - 1 < alpha.Length) throw new BmageImageDecodeException("Truncated WebP alpha data.");
                chunk.Span.Slice(1, alpha.Length).CopyTo(alpha);
                break;
            case 1:
                BmageVp8LDecoder.DecodeAlpha(chunk[1..], width, height, alpha, ct);
                break;
            default:
                throw new BmageImageDecodeException($"Unsupported WebP alpha compression {compression}.");
        }
        if (filter != 0) Unfilter(alpha, width, height, filter);
        return alpha;
    }

    private static void Unfilter(byte[] alpha, int width, int height, int filter)
    {
        // The top left value has no predictor, the rest of the first row predicts from the left and the rest of the
        // first column from above, whatever the filter.
        for (var x = 1; x < width; x++) alpha[x] += alpha[x - 1];
        for (var y = 1; y < height; y++)
        {
            var row = y * width;
            alpha[row] += alpha[row - width];
            for (var x = 1; x < width; x++)
            {
                var i = row + x;
                int predictor = filter switch
                {
                    1 => alpha[i - 1],
                    2 => alpha[i - width],
                    _ => Math.Clamp(alpha[i - 1] + alpha[i - width] - alpha[i - width - 1], 0, 255),
                };
                alpha[i] = (byte)(alpha[i] + predictor);
            }
        }
    }
}
