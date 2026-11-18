using System.Buffers.Binary;

namespace Bit.Bmage;

/// <summary>Managed BMP decoder: uncompressed 1/4/8-bit palette, 16/24/32-bit RGB and BI_BITFIELDS images, top-down or bottom-up.</summary>
public sealed class BmageBmpDecoder : IBmageImageDecoder
{
    private static readonly BmageImageFormat[] SupportedFormats = [BmageImageFormat.Bmp];
    public IReadOnlyCollection<BmageImageFormat> Formats => SupportedFormats;

    public BmageImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        if (data.Length < 26 || data[0] != 'B' || data[1] != 'M') return null;
        var headerSize = BinaryPrimitives.ReadInt32LittleEndian(data[14..]);
        if (headerSize == 12)
        {
            return new BmageImageInfo(BinaryPrimitives.ReadUInt16LittleEndian(data[18..]), Math.Abs((int)BinaryPrimitives.ReadInt16LittleEndian(data[20..])), BmageImageFormat.Bmp, HasAlpha: false);
        }
        var w = BinaryPrimitives.ReadInt32LittleEndian(data[18..]);
        var rawHeight = BinaryPrimitives.ReadInt32LittleEndian(data[22..]);
        if (rawHeight == int.MinValue) return null; // has no positive counterpart
        var h = Math.Abs(rawHeight);
        var bpp = data.Length >= 30 ? BinaryPrimitives.ReadUInt16LittleEndian(data[28..]) : 0;
        return w <= 0 || h <= 0 ? null : new BmageImageInfo(w, h, BmageImageFormat.Bmp, HasAlpha: bpp == 32);
    }

    public ValueTask<BmageImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken cancellationToken = default)
        => new(Decode(data.Span, options, allocator, cancellationToken));

    public static BmageImageBuffer Decode(ReadOnlySpan<byte> data, BmageDecodeOptions? options = null, IBmagePixelAllocator? allocator = null, CancellationToken cancellationToken = default)
    {
        options ??= BmageDecodeOptions.Default;
        allocator ??= BmagePooledPixelAllocator.Shared;
        options.Limits.ValidateEncodedSize(data.Length);
        if (data.Length < 26 || data[0] != 'B' || data[1] != 'M') throw new BmageImageDecodeException("Not a BMP file.");
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
            if (headerSize < 40 || headerSize > data.Length - 14) throw new BmageImageDecodeException("Invalid BMP header.");
            width = BinaryPrimitives.ReadInt32LittleEndian(data[18..]);
            var h = BinaryPrimitives.ReadInt32LittleEndian(data[22..]);
            if (h == int.MinValue) throw new BmageImageDecodeException("Invalid BMP height.");
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
            else if (compression != 0) throw new BmageImageDecodeException($"Unsupported BMP compression {compression}.");
        }
        if (width <= 0 || height <= 0) throw new BmageImageDecodeException("Invalid BMP dimensions.");
        options.Limits.Validate(width, height);
        if (bpp is not (1 or 4 or 8 or 16 or 24 or 32)) throw new BmageImageDecodeException($"Unsupported BMP bit depth {bpp}.");
        if (compression == 0 && bpp == 16) { maskR = 0x7C00; maskG = 0x03E0; maskB = 0x001F; }
        if (compression == 0 && bpp == 32) { maskR = 0x00FF0000; maskG = 0x0000FF00; maskB = 0x000000FF; maskA = 0; }
        if (maskR == 0 && maskG == 0 && maskB == 0 && bpp >= 16) { maskR = bpp == 16 ? 0x7C00u : 0x00FF0000u; maskG = bpp == 16 ? 0x03E0u : 0x0000FF00u; maskB = bpp == 16 ? 0x001Fu : 0x000000FFu; }

        BmageRgba32[]? palette = null;
        if (bpp <= 8)
        {
            var count = colorsUsed > 0 ? colorsUsed : 1 << bpp;
            palette = new BmageRgba32[1 << bpp];
            for (var i = 0; i < Math.Min(count, palette.Length); i++)
            {
                var o = paletteOffset + i * paletteEntrySize;
                if (o + 3 > data.Length) break;
                palette[i] = new BmageRgba32(data[o + 2], data[o + 1], data[o]);
            }
        }

        var stride = ((width * bpp + 31) / 32) * 4;
        if (pixelOffset < 0 || (long)pixelOffset + (long)stride * height > data.Length) throw new BmageImageDecodeException("Truncated BMP pixel data.");
        var image = BmageImageBuffer.Create(width, height, allocator, clear: false, options.Limits);
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
                        for (var x = 0; x < width; x++) dst[x] = new BmageRgba32(row[x * 3 + 2], row[x * 3 + 1], row[x * 3]);
                        break;
                    default:
                    {
                        for (var x = 0; x < width; x++)
                        {
                            var v = bpp == 16 ? BinaryPrimitives.ReadUInt16LittleEndian(row[(x * 2)..]) : BinaryPrimitives.ReadUInt32LittleEndian(row[(x * 4)..]);
                            var a = maskA == 0 ? (byte)255 : Extract(v, maskA, shA, scA);
                            if (a != 255) anyAlpha = true;
                            dst[x] = new BmageRgba32(Extract(v, maskR, shR, scR), Extract(v, maskG, shG, scG), Extract(v, maskB, shB, scB), a);
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
            image.Metadata = new BmageImageMetadata(sourceFormat: BmageImageFormat.Bmp, fileName: options.FileName);
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
