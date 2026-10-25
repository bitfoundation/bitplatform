using System.Buffers.Binary;
using Bit.Bmage.Memory;
using Bit.Bmage.Metadata;

namespace Bit.Bmage.Codecs.Bmp;

/// <summary>Managed BMP decoder: uncompressed 1/4/8-bit palette, 16/24/32-bit RGB and BI_BITFIELDS images, top-down or bottom-up.</summary>
public sealed class BmpDecoder : IImageDecoder
{
    private static readonly ImageFormat[] SupportedFormats = [ImageFormat.Bmp];
    public IReadOnlyCollection<ImageFormat> Formats => SupportedFormats;

    public ImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        if (data.Length < 26 || data[0] != 'B' || data[1] != 'M') return null;
        var headerSize = BinaryPrimitives.ReadInt32LittleEndian(data[14..]);
        if (headerSize == 12)
        {
            return new ImageInfo(BinaryPrimitives.ReadUInt16LittleEndian(data[18..]), Math.Abs((int)BinaryPrimitives.ReadInt16LittleEndian(data[20..])), ImageFormat.Bmp, HasAlpha: false);
        }
        var w = BinaryPrimitives.ReadInt32LittleEndian(data[18..]);
        var rawHeight = BinaryPrimitives.ReadInt32LittleEndian(data[22..]);
        if (rawHeight == int.MinValue) return null; // has no positive counterpart
        var h = Math.Abs(rawHeight);
        var bpp = data.Length >= 30 ? BinaryPrimitives.ReadUInt16LittleEndian(data[28..]) : 0;
        return w <= 0 || h <= 0 ? null : new ImageInfo(w, h, ImageFormat.Bmp, HasAlpha: bpp == 32);
    }

    public ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken = default)
        => new(Decode(data.Span, options, allocator, cancellationToken));

    public static ImageBuffer Decode(ReadOnlySpan<byte> data, DecodeOptions? options = null, IPixelAllocator? allocator = null, CancellationToken cancellationToken = default)
    {
        options ??= DecodeOptions.Default;
        allocator ??= PooledPixelAllocator.Shared;
        options.Limits.ValidateEncodedSize(data.Length);
        if (data.Length < 26 || data[0] != 'B' || data[1] != 'M') throw new ImageDecodeException("Not a BMP file.");
        var pixelOffset = BinaryPrimitives.ReadInt32LittleEndian(data[10..]);
        var headerSize = BinaryPrimitives.ReadInt32LittleEndian(data[14..]);
        int width, height, bpp, compression = 0, colorsUsed = 0;
        bool topDown;
        uint maskR = 0, maskG = 0, maskB = 0, maskA = 0;
        int paletteOffset, paletteEntrySize;
        if (headerSize == 12)
        {
            width = BinaryPrimitives.ReadUInt16LittleEndian(data[18..]);
            var h = BinaryPrimitives.ReadInt16LittleEndian(data[20..]);
            height = Math.Abs((int)h); topDown = h < 0;
            bpp = BinaryPrimitives.ReadUInt16LittleEndian(data[24..]);
            paletteOffset = 14 + 12; paletteEntrySize = 3;
        }
        else
        {
            // Compared by subtraction so a huge header size cannot overflow past the check.
            if (headerSize < 40 || headerSize > data.Length - 14) throw new ImageDecodeException("Invalid BMP header.");
            width = BinaryPrimitives.ReadInt32LittleEndian(data[18..]);
            var h = BinaryPrimitives.ReadInt32LittleEndian(data[22..]);
            if (h == int.MinValue) throw new ImageDecodeException("Invalid BMP height.");
            height = Math.Abs(h); topDown = h < 0;
            bpp = BinaryPrimitives.ReadUInt16LittleEndian(data[28..]);
            compression = BinaryPrimitives.ReadInt32LittleEndian(data[30..]);
            colorsUsed = BinaryPrimitives.ReadInt32LittleEndian(data[46..]);
            paletteOffset = 14 + headerSize; paletteEntrySize = 4;
            if (compression == 3 || compression == 6)
            {
                if (headerSize >= 52)
                {
                    maskR = BinaryPrimitives.ReadUInt32LittleEndian(data[54..]);
                    maskG = BinaryPrimitives.ReadUInt32LittleEndian(data[58..]);
                    maskB = BinaryPrimitives.ReadUInt32LittleEndian(data[62..]);
                    if (headerSize >= 56) maskA = BinaryPrimitives.ReadUInt32LittleEndian(data[66..]);
                }
                else if (data.Length >= 14 + 40 + 12)
                {
                    maskR = BinaryPrimitives.ReadUInt32LittleEndian(data[54..]);
                    maskG = BinaryPrimitives.ReadUInt32LittleEndian(data[58..]);
                    maskB = BinaryPrimitives.ReadUInt32LittleEndian(data[62..]);
                    paletteOffset += 12;
                }
            }
            else if (compression != 0) throw new ImageDecodeException($"Unsupported BMP compression {compression}.");
        }
        if (width <= 0 || height <= 0) throw new ImageDecodeException("Invalid BMP dimensions.");
        options.Limits.Validate(width, height);
        if (bpp is not (1 or 4 or 8 or 16 or 24 or 32)) throw new ImageDecodeException($"Unsupported BMP bit depth {bpp}.");
        if (compression == 0 && bpp == 16) { maskR = 0x7C00; maskG = 0x03E0; maskB = 0x001F; }
        if (compression == 0 && bpp == 32) { maskR = 0x00FF0000; maskG = 0x0000FF00; maskB = 0x000000FF; maskA = 0; }
        if (maskR == 0 && maskG == 0 && maskB == 0 && bpp >= 16) { maskR = bpp == 16 ? 0x7C00u : 0x00FF0000u; maskG = bpp == 16 ? 0x03E0u : 0x0000FF00u; maskB = bpp == 16 ? 0x001Fu : 0x000000FFu; }

        Rgba32[]? palette = null;
        if (bpp <= 8)
        {
            var count = colorsUsed > 0 ? colorsUsed : 1 << bpp;
            palette = new Rgba32[1 << bpp];
            for (var i = 0; i < Math.Min(count, palette.Length); i++)
            {
                var o = paletteOffset + i * paletteEntrySize;
                if (o + 3 > data.Length) break;
                palette[i] = new Rgba32(data[o + 2], data[o + 1], data[o]);
            }
        }

        var stride = ((width * bpp + 31) / 32) * 4;
        if (pixelOffset < 0 || (long)pixelOffset + (long)stride * height > data.Length) throw new ImageDecodeException("Truncated BMP pixel data.");
        var image = ImageBuffer.Create(width, height, allocator, clear: false, options.Limits);
        try
        {
            var (shR, scR) = MaskInfo(maskR); var (shG, scG) = MaskInfo(maskG); var (shB, scB) = MaskInfo(maskB); var (shA, scA) = MaskInfo(maskA);
            var anyAlpha = false;
            for (var y = 0; y < height; y++)
            {
                if ((y & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
                var row = data.Slice(pixelOffset + y * stride, stride);
                var dst = image.GetRow(topDown ? y : height - 1 - y);
                switch (bpp)
                {
                    case 1 or 4 or 8:
                        for (var x = 0; x < width; x++)
                        {
                            var bit = x * bpp;
                            var idx = (row[bit >> 3] >> (8 - bpp - (bit & 7))) & ((1 << bpp) - 1);
                            dst[x] = palette![idx];
                        }
                        break;
                    case 24:
                        for (var x = 0; x < width; x++) dst[x] = new Rgba32(row[x * 3 + 2], row[x * 3 + 1], row[x * 3]);
                        break;
                    default:
                    {
                        for (var x = 0; x < width; x++)
                        {
                            var v = bpp == 16 ? BinaryPrimitives.ReadUInt16LittleEndian(row[(x * 2)..]) : BinaryPrimitives.ReadUInt32LittleEndian(row[(x * 4)..]);
                            var a = maskA == 0 ? (byte)255 : Extract(v, maskA, shA, scA);
                            if (a != 255) anyAlpha = true;
                            dst[x] = new Rgba32(Extract(v, maskR, shR, scR), Extract(v, maskG, shG, scG), Extract(v, maskB, shB, scB), a);
                        }
                        break;
                    }
                }
            }
            // Some writers store 32-bit BMPs with an all-zero alpha channel meaning "opaque".
            if (maskA != 0 && !anyAlpha) { /* alpha all 255: nothing to do */ }
            else if (maskA != 0)
            {
                var allZero = true;
                var px = image.Pixels;
                for (var i = 0; i < px.Length; i++) if (px[i].A != 0) { allZero = false; break; }
                if (allZero) for (var i = 0; i < px.Length; i++) px[i] = px[i].WithAlpha(255);
            }
            image.Metadata = new ImageMetadata(sourceFormat: ImageFormat.Bmp, fileName: options.FileName);
            return image;
        }
        catch { image.Dispose(); throw; }
    }

    private static (int Shift, float Scale) MaskInfo(uint mask)
    {
        if (mask == 0) return (0, 0);
        var shift = System.Numerics.BitOperations.TrailingZeroCount(mask);
        var bits = System.Numerics.BitOperations.PopCount(mask);
        return (shift, 255f / ((1 << bits) - 1));
    }

    private static byte Extract(uint v, uint mask, int shift, float scale)
        => mask == 0 ? (byte)0 : (byte)(((v & mask) >> shift) * scale + 0.5f);
}

/// <summary>Managed BMP encoder writing 24-bit (opaque) or 32-bit BGRA (with alpha, BITMAPV4HEADER) images.</summary>
public sealed class BmpEncoder : IImageEncoder
{
    private static readonly ImageFormat[] SupportedFormats = [ImageFormat.Bmp];
    public IReadOnlyCollection<ImageFormat> Formats => SupportedFormats;

    public ValueTask<EncodedImage> EncodeAsync(ImageBuffer image, ImageExportOptions options, CancellationToken cancellationToken = default)
        => new(Encode(image, cancellationToken));

    public static EncodedImage Encode(ImageBuffer image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        var alpha = image.HasTransparency();
        int w = image.Width, h = image.Height;
        var bpp = alpha ? 32 : 24;
        var stride = ((w * bpp + 31) / 32) * 4;
        var headerSize = alpha ? 108 : 40;
        var pixelOffset = 14 + headerSize;
        var total = pixelOffset + stride * h;
        var buf = new byte[total];
        buf[0] = (byte)'B'; buf[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(2), total);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(10), pixelOffset);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(14), headerSize);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(18), w);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(22), h);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(26), 1);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(28), (short)bpp);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(30), alpha ? 3 : 0);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(34), stride * h);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(38), 2835);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(42), 2835);
        if (alpha)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(54), 0x00FF0000);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(58), 0x0000FF00);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(62), 0x000000FF);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(66), 0xFF000000);
            buf[70] = (byte)'B'; buf[71] = (byte)'G'; buf[72] = (byte)'R'; buf[73] = (byte)'s';
        }
        for (var y = 0; y < h; y++)
        {
            if ((y & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            var row = image.GetRow(h - 1 - y);
            var o = pixelOffset + y * stride;
            if (alpha)
                for (var x = 0; x < w; x++) { var p = row[x]; buf[o + x * 4] = p.B; buf[o + x * 4 + 1] = p.G; buf[o + x * 4 + 2] = p.R; buf[o + x * 4 + 3] = p.A; }
            else
                for (var x = 0; x < w; x++) { var p = row[x]; buf[o + x * 3] = p.B; buf[o + x * 3 + 1] = p.G; buf[o + x * 3 + 2] = p.R; }
        }
        return new EncodedImage(buf, ImageFormat.Bmp, w, h);
    }
}
