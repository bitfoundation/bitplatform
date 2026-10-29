using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Bit.Bmage.Geometry;
using Bit.Bmage.Memory;
using Bit.Bmage.Metadata;
using Bit.Bmage.Operations.Transforms;

namespace Bit.Bmage.Codecs.WebP;

/// <summary>
/// Managed WebP decoder: lossy (VP8) with or without an alpha channel, lossless (VP8L), the extended format's metadata,
/// and the first frame of an animation.
/// </summary>
public sealed class WebPDecoder : IImageDecoder
{
    private static readonly ImageFormat[] SupportedFormats = [ImageFormat.WebP];
    public IReadOnlyCollection<ImageFormat> Formats => SupportedFormats;

    private const int AlphaFlag = 0x10;
    private const int AnimationFlag = 0x02;

    public ImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        if (ImageFormats.Detect(data) != ImageFormat.WebP) return null;
        var chunks = WebPContainer.EnumerateChunks(data);
        if (chunks.Count == 0) return null;
        var first = chunks[0];
        var payload = data.Slice(first.DataOffset, first.DataLength);
        int width, height;
        bool hasAlpha;
        switch (first.FourCc)
        {
            case "VP8 " when Vp8Decoder.ReadHeader(payload) is { } lossy:
                (width, height, hasAlpha) = (lossy.Width, lossy.Height, false);
                break;
            case "VP8L" when Vp8LDecoder.ReadHeader(payload) is { } lossless:
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
        var orientation = Orientation.Normal;
        foreach (var c in chunks)
            if (c.FourCc == "EXIF") { orientation = ExifReader.Read(data.Slice(c.DataOffset, c.DataLength))?.Orientation ?? Orientation.Normal; break; }
        return new ImageInfo(width, height, ImageFormat.WebP, orientation, hasAlpha);
    }

    public ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken = default)
        => new(Decode(data.Span, options, allocator, cancellationToken));

    public static ImageBuffer Decode(ReadOnlySpan<byte> data, DecodeOptions? options = null, IPixelAllocator? allocator = null, CancellationToken cancellationToken = default)
    {
        options ??= DecodeOptions.Default;
        allocator ??= PooledPixelAllocator.Shared;
        options.Limits.ValidateEncodedSize(data.Length);
        if (ImageFormats.Detect(data) != ImageFormat.WebP) throw new ImageDecodeException("Invalid WebP: missing RIFF/WEBP header.");
        var chunks = WebPContainer.EnumerateChunks(data);
        if (chunks.Count == 0) throw new ImageDecodeException("Invalid WebP: no chunks.");

        ImageBuffer image;
        var first = chunks[0];
        if (first.FourCc == "VP8X")
        {
            var header = data.Slice(first.DataOffset, first.DataLength);
            if (header.Length < 10) throw new ImageDecodeException("Invalid WebP: truncated VP8X header.");
            var width = 1 + (header[4] | header[5] << 8 | header[6] << 16);
            var height = 1 + (header[7] | header[8] << 8 | header[9] << 16);
            options.Limits.Validate(width, height);
            image = (header[0] & AnimationFlag) != 0
                ? DecodeFirstFrame(data, chunks, width, height, options, allocator, cancellationToken)
                : DecodeStill(data, chunks, width, height, options, allocator, cancellationToken);
        }
        else image = DecodeStill(data, chunks, 0, 0, options, allocator, cancellationToken);

        try
        {
            if (options.ReadMetadata)
            {
                var md = WebPContainer.ReadMetadata(data, options.FileName);
                image.Metadata = md;
                if (options.AutoOrient && md.Orientation is not (Orientation.Normal or Orientation.Unspecified))
                {
                    var oriented = new OrientationOperation(md.Orientation).Apply(image, new Operations.OperationContext(allocator, options.Limits, cancellationToken, canMutateSource: true, maxDegreeOfParallelism: options.MaxDegreeOfParallelism));
                    if (!ReferenceEquals(oriented, image)) image.Dispose();
                    image = oriented;
                    image.Metadata = md.WithOrientation(Orientation.Normal);
                }
            }
            else image.Metadata = new ImageMetadata(sourceFormat: ImageFormat.WebP, fileName: options.FileName);
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
    private static ImageBuffer DecodeStill(ReadOnlySpan<byte> data, List<WebPContainer.Chunk> chunks, int canvasWidth, int canvasHeight, DecodeOptions options, IPixelAllocator allocator, CancellationToken ct)
    {
        WebPContainer.Chunk? alpha = null, bitstream = null;
        foreach (var c in chunks)
        {
            if (c.FourCc == "ALPH") alpha ??= c;
            else if (c.FourCc is "VP8 " or "VP8L") { bitstream = c; break; }
        }
        if (bitstream is not { } b) throw new ImageDecodeException("Invalid WebP: no image data.");
        var payload = data.Slice(b.DataOffset, b.DataLength);
        var alphaData = alpha is { } a ? data.Slice(a.DataOffset, a.DataLength) : default;
        var (width, height) = ReadFrameSize(b.FourCc, payload);
        if (canvasWidth != 0 && (width != canvasWidth || height != canvasHeight))
            throw new ImageDecodeException("Invalid WebP: the image size does not match the canvas size.");
        options.Limits.Validate(width, height);

        var image = ImageBuffer.Create(width, height, allocator, clear: false, options.Limits);
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
    private static ImageBuffer DecodeFirstFrame(ReadOnlySpan<byte> data, List<WebPContainer.Chunk> chunks, int canvasWidth, int canvasHeight, DecodeOptions options, IPixelAllocator allocator, CancellationToken ct)
    {
        WebPContainer.Chunk? frame = null;
        foreach (var c in chunks) if (c.FourCc == "ANMF") { frame = c; break; }
        if (frame is not { } f || f.DataLength < 16) throw new ImageDecodeException("Invalid WebP: animation without frames.");
        var header = data.Slice(f.DataOffset, 16);
        var x = 2 * (header[0] | header[1] << 8 | header[2] << 16);
        var y = 2 * (header[3] | header[4] << 8 | header[5] << 16);
        var frameWidth = 1 + (header[6] | header[7] << 8 | header[8] << 16);
        var frameHeight = 1 + (header[9] | header[10] << 8 | header[11] << 16);
        if ((long)x + frameWidth > canvasWidth || (long)y + frameHeight > canvasHeight)
            throw new ImageDecodeException("Invalid WebP: the first frame lies outside the canvas.");

        // The frame's own chunks follow its 16 byte header, laid out like the top level ones.
        var frameData = data.Slice(f.DataOffset + 16, f.DataLength - 16);
        WebPContainer.Chunk? alpha = null, bitstream = null;
        var pos = 0;
        while (pos + 8 <= frameData.Length)
        {
            var fourcc = System.Text.Encoding.ASCII.GetString(frameData.Slice(pos, 4));
            var length = (long)BinaryPrimitives.ReadUInt32LittleEndian(frameData[(pos + 4)..]);
            if (pos + 8 + length > frameData.Length) break;
            var chunk = new WebPContainer.Chunk(fourcc, f.DataOffset + 16 + pos, f.DataOffset + 16 + pos + 8, (int)length);
            if (fourcc == "ALPH") alpha ??= chunk;
            else if (fourcc is "VP8 " or "VP8L") { bitstream = chunk; break; }
            pos += 8 + (int)length + ((int)length & 1);
        }
        if (bitstream is not { } b) throw new ImageDecodeException("Invalid WebP: the first frame has no image data.");
        var payload = data.Slice(b.DataOffset, b.DataLength);
        var (width, height) = ReadFrameSize(b.FourCc, payload);
        if (width != frameWidth || height != frameHeight) throw new ImageDecodeException("Invalid WebP: the frame size does not match its header.");

        var image = ImageBuffer.Create(canvasWidth, canvasHeight, allocator, clear: true, options.Limits);
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
            return Vp8LDecoder.ReadHeader(payload) is { } l ? (l.Width, l.Height) : throw new ImageDecodeException("Invalid WebP lossless header.");
        return Vp8Decoder.ReadHeader(payload) ?? throw new ImageDecodeException("Invalid WebP lossy header.");
    }

    /// <summary>Decodes one bitstream chunk into the rectangle of <paramref name="image"/> at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private static void DecodeFrame(string fourcc, ReadOnlySpan<byte> payload, bool hasAlphaChunk, ReadOnlySpan<byte> alphaChunk, int width, int height, ImageBuffer image, int x, int y, CancellationToken ct)
    {
        if (fourcc == "VP8L")
        {
            if (x == 0 && y == 0 && width == image.Width && height == image.Height)
            {
                // ARGB words in memory are B, G, R, A, so swapping red and blue in place gives the buffer's RGBA.
                var argb = MemoryMarshal.Cast<byte, uint>(image.Bytes);
                Vp8LDecoder.Decode(payload, width, height, argb, ct);
                for (var i = 0; i < argb.Length; i++) argb[i] = ArgbToRgba(argb[i]);
            }
            else
            {
                var argb = new uint[(long)width * height];
                Vp8LDecoder.Decode(payload, width, height, argb, ct);
                for (var r = 0; r < height; r++)
                {
                    var dst = MemoryMarshal.Cast<Rgba32, uint>(image.GetRow(y + r).Slice(x, width));
                    for (var c = 0; c < width; c++) dst[c] = ArgbToRgba(argb[r * width + c]);
                }
            }
            return;
        }

        var frame = Vp8Decoder.Decode(payload, ct);
        byte[]? alpha = null;
        if (hasAlphaChunk) alpha = DecodeAlpha(alphaChunk, width, height, ct);
        Vp8Yuv.ToRgba(frame, alpha, image, x, y, ct);
    }

    private static uint ArgbToRgba(uint v) => (v & 0xFF00FF00u) | ((v >> 16) & 0xFFu) | ((v & 0xFFu) << 16);

    /// <summary>Decodes an 'ALPH' chunk: raw or losslessly compressed values, then the spatial filter undone.</summary>
    internal static byte[] DecodeAlpha(ReadOnlySpan<byte> chunk, int width, int height, CancellationToken ct)
    {
        if (chunk.Length < 1) throw new ImageDecodeException("Invalid WebP: empty alpha chunk.");
        var header = chunk[0];
        var compression = header & 3;
        var filter = (header >> 2) & 3;
        var alpha = new byte[(long)width * height];
        switch (compression)
        {
            case 0:
                if (chunk.Length - 1 < alpha.Length) throw new ImageDecodeException("Truncated WebP alpha data.");
                chunk.Slice(1, alpha.Length).CopyTo(alpha);
                break;
            case 1:
                Vp8LDecoder.DecodeAlpha(chunk[1..], width, height, alpha, ct);
                break;
            default:
                throw new ImageDecodeException($"Unsupported WebP alpha compression {compression}.");
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

/// <summary>Converts a VP8 frame's limited range BT.601 YUV 4:2:0 to RGBA, interpolating chroma bilinearly.</summary>
internal static class Vp8Yuv
{
    public static void ToRgba(Vp8Frame frame, byte[]? alpha, ImageBuffer image, int x0, int y0, CancellationToken ct)
    {
        var width = frame.Width;
        var height = frame.Height;
        var chromaWidth = (width + 1) >> 1;
        var chromaHeight = (height + 1) >> 1;
        var u = new int[chromaWidth];
        var v = new int[chromaWidth];
        for (var y = 0; y < height; y++)
        {
            if ((y & 63) == 0) ct.ThrowIfCancellationRequested();
            // Chroma samples sit between luma rows and columns, so every output pixel weighs its four nearest samples
            // 9:3:3:1. The vertical pass is done once per row at four times the precision.
            var near = y >> 1;
            var far = Math.Clamp((y & 1) != 0 ? near + 1 : near - 1, 0, chromaHeight - 1);
            var un = frame.U.AsSpan(near * frame.UVStride, chromaWidth);
            var uf = frame.U.AsSpan(far * frame.UVStride, chromaWidth);
            var vn = frame.V.AsSpan(near * frame.UVStride, chromaWidth);
            var vf = frame.V.AsSpan(far * frame.UVStride, chromaWidth);
            for (var c = 0; c < chromaWidth; c++)
            {
                u[c] = 3 * un[c] + uf[c];
                v[c] = 3 * vn[c] + vf[c];
            }

            var luma = frame.Y.AsSpan(y * frame.YStride, width);
            var dst = image.GetRow(y0 + y).Slice(x0, width);
            var a = alpha is null ? default : alpha.AsSpan(y * width, width);
            for (var x = 0; x < width; x++)
            {
                var cn = x >> 1;
                var cf = (x & 1) != 0 ? Math.Min(cn + 1, chromaWidth - 1) : Math.Max(cn - 1, 0);
                var uu = (3 * u[cn] + u[cf] + 8) >> 4;
                var vv = (3 * v[cn] + v[cf] + 8) >> 4;
                dst[x] = ToRgb(luma[x], uu, vv, alpha is null ? (byte)255 : a[x]);
            }
        }
    }

    // BT.601 with luma in 16..235 and chroma in 16..240, in 14 bit fixed point.
    internal static Rgba32 ToRgb(int y, int u, int v, byte a)
    {
        var luma = 19077 * (y - 16) + 8192;
        var r = (luma + 26149 * (v - 128)) >> 14;
        var g = (luma - 6419 * (u - 128) - 13320 * (v - 128)) >> 14;
        var b = (luma + 33050 * (u - 128)) >> 14;
        return new Rgba32(Vp8Decoder.Clip8(r), Vp8Decoder.Clip8(g), Vp8Decoder.Clip8(b), a);
    }
}
