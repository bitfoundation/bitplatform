using System.Buffers.Binary;

namespace Bit.Bmage;

/// <summary>
/// Managed ICO decoder: reads the largest image of an icon file, whether stored as PNG or as a Windows bitmap of 1, 4,
/// 8, 24 or 32 bits a pixel with its transparency mask.
/// </summary>
public sealed class BmageIcoDecoder : IBmageImageDecoder
{
    /// <summary>The most images an icon file may list.</summary>
    internal const int MaxEntries = 256;

    private static readonly BmageImageFormat[] SupportedFormats = [BmageImageFormat.Ico];

    public IReadOnlyCollection<BmageImageFormat> Formats => SupportedFormats;

    public BmageImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        if (BmageImageFormats.Detect(data) != BmageImageFormat.Ico) return null;
        try
        {
            var entry = Largest(data);
            return new BmageImageInfo(entry.Width, entry.Height, BmageImageFormat.Ico, HasAlpha: true);
        }
        catch (BmageImageDecodeException)
        {
            return null;
        }
    }

    public ValueTask<BmageImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken cancellationToken = default)
        => new(Decode(data.Span, options, allocator, cancellationToken));

    /// <summary>Decodes the largest image in the icon file.</summary>
    public static BmageImageBuffer Decode(ReadOnlySpan<byte> data, BmageDecodeOptions? options = null, IBmagePixelAllocator? allocator = null, CancellationToken cancellationToken = default)
    {
        options ??= BmageDecodeOptions.Default;
        allocator ??= BmagePooledPixelAllocator.Shared;
        options.Limits.ValidateEncodedSize(data.Length);
        if (BmageImageFormats.Detect(data) != BmageImageFormat.Ico) throw new BmageImageDecodeException("Not an ICO file.");
        var entry = Largest(data);
        var body = data.Slice(entry.Offset, entry.Length);
        if (BmageImageFormats.Detect(body) == BmageImageFormat.Png)
        {
            var png = BmagePngDecoder.Decode(body, options with { ReadMetadata = false }, allocator, cancellationToken);
            png.Metadata = new BmageImageMetadata(sourceFormat: BmageImageFormat.Ico, fileName: options.FileName);
            return png;
        }
        var image = DecodeBitmap(body, options, allocator, cancellationToken);
        image.Metadata = new BmageImageMetadata(sourceFormat: BmageImageFormat.Ico, fileName: options.FileName);
        return image;
    }

    internal readonly record struct Entry(int Width, int Height, int BitCount, int Offset, int Length);

    /// <summary>Every entry of the directory, checked to lie within the file.</summary>
    internal static List<Entry> ReadDirectory(ReadOnlySpan<byte> data)
    {
        if (data.Length < 6) throw new BmageImageDecodeException("Truncated ICO header.");
        int count = BinaryPrimitives.ReadUInt16LittleEndian(data[4..]);
        if (count == 0 || count > MaxEntries) throw new BmageImageDecodeException($"An ICO file lists between 1 and {MaxEntries} images, not {count}.");
        if (6 + 16 * count > data.Length) throw new BmageImageDecodeException("Truncated ICO directory.");
        var entries = new List<Entry>(count);
        for (var i = 0; i < count; i++)
        {
            var e = data.Slice(6 + 16 * i, 16);
            var length = BinaryPrimitives.ReadInt32LittleEndian(e[8..]);
            var offset = BinaryPrimitives.ReadInt32LittleEndian(e[12..]);
            if (length <= 0 || offset < 6 + 16 * count || offset > data.Length - length)
                throw new BmageImageDecodeException($"ICO image {i + 1} lies outside the file.");
            int width = e[0] == 0 ? 256 : e[0], height = e[1] == 0 ? 256 : e[1];
            var body = data.Slice(offset, length);
            // A PNG entry's own header gives its real size, which can exceed what the directory's bytes can say.
            if (BmageImageFormats.Detect(body) == BmageImageFormat.Png && body.Length >= 24)
            {
                width = BinaryPrimitives.ReadInt32BigEndian(body[16..]);
                height = BinaryPrimitives.ReadInt32BigEndian(body[20..]);
            }
            entries.Add(new Entry(width, height, BinaryPrimitives.ReadUInt16LittleEndian(e[6..]), offset, length));
        }
        return entries;
    }

    /// <summary>The entry with the most pixels, and of those the deepest.</summary>
    private static Entry Largest(ReadOnlySpan<byte> data)
    {
        var entries = ReadDirectory(data);
        return entries.OrderByDescending(e => (long)e.Width * e.Height).ThenByDescending(e => e.BitCount).First();
    }

    /// <summary>
    /// A Windows bitmap without its file header, as icons store them: the height counts the colour rows and the mask
    /// rows below them, bottom-up; a mask bit of one is transparent.
    /// </summary>
    private static BmageImageBuffer DecodeBitmap(ReadOnlySpan<byte> dib, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken cancellationToken)
    {
        if (dib.Length < 40) throw new BmageImageDecodeException("Truncated ICO bitmap header.");
        var headerSize = BinaryPrimitives.ReadInt32LittleEndian(dib);
        if (headerSize < 40 || headerSize > dib.Length) throw new BmageImageDecodeException("Invalid ICO bitmap header.");
        var width = BinaryPrimitives.ReadInt32LittleEndian(dib[4..]);
        var doubled = BinaryPrimitives.ReadInt32LittleEndian(dib[8..]);
        int bpp = BinaryPrimitives.ReadUInt16LittleEndian(dib[14..]);
        var compression = BinaryPrimitives.ReadInt32LittleEndian(dib[16..]);
        var colorsUsed = BinaryPrimitives.ReadInt32LittleEndian(dib[32..]);
        if (compression != 0) throw new BmageImageDecodeException($"Unsupported ICO bitmap compression {compression}.");
        if (width <= 0 || doubled <= 1 || width > 4096 || doubled > 8192) throw new BmageImageDecodeException("Invalid ICO bitmap dimensions.");
        var height = doubled / 2;
        options.Limits.Validate(width, height);
        if (bpp is not (1 or 4 or 8 or 24 or 32)) throw new BmageImageDecodeException($"Unsupported ICO bit depth {bpp}.");

        var paletteSize = bpp <= 8 ? (colorsUsed is > 0 and <= 256 ? colorsUsed : 1 << bpp) : 0;
        var paletteOffset = headerSize;
        var pixelOffset = paletteOffset + paletteSize * 4;
        var stride = (width * bpp + 31) / 32 * 4;
        var maskStride = (width + 31) / 32 * 4;
        var maskOffset = pixelOffset + (long)stride * height;
        // Some writers of 32-bit icons leave the mask out: alpha says it all.
        var hasMask = maskOffset + (long)maskStride * height <= dib.Length;
        if (maskOffset > dib.Length || (!hasMask && bpp != 32)) throw new BmageImageDecodeException("Truncated ICO bitmap.");

        var palette = new BmageRgba32[Math.Max(paletteSize, 1)];
        for (var i = 0; i < paletteSize; i++)
        {
            var o = paletteOffset + i * 4;
            palette[i] = new BmageRgba32(dib[o + 2], dib[o + 1], dib[o]);
        }

        var image = BmageImageBuffer.Create(width, height, allocator, clear: false, options.Limits);
        try
        {
            var anyAlpha = false;
            for (var y = 0; y < height; y++)
            {
                if ((y & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
                var row = dib.Slice(pixelOffset + y * stride, stride);
                var dst = image.GetRow(height - 1 - y);
                for (var x = 0; x < width; x++)
                {
                    BmageRgba32 p;
                    switch (bpp)
                    {
                        case 32:
                            p = new BmageRgba32(row[x * 4 + 2], row[x * 4 + 1], row[x * 4], row[x * 4 + 3]);
                            if (p.A != 0) anyAlpha = true;
                            break;
                        case 24:
                            p = new BmageRgba32(row[x * 3 + 2], row[x * 3 + 1], row[x * 3]);
                            break;
                        default:
                            var bit = x * bpp;
                            var index = (row[bit >> 3] >> (8 - bpp - (bit & 7))) & ((1 << bpp) - 1);
                            p = index < paletteSize ? palette[index] : BmageRgba32.Black;
                            break;
                    }
                    dst[x] = p;
                }
            }

            // The mask decides transparency unless 32-bit pixels carry alpha of their own.
            var useMask = hasMask && !(bpp == 32 && anyAlpha);
            for (var y = 0; y < height; y++)
            {
                var dst = image.GetRow(height - 1 - y);
                if (!useMask)
                {
                    if (bpp != 32) continue;
                    // A 32-bit icon with no alpha and no mask is opaque.
                    if (!anyAlpha) for (var x = 0; x < width; x++) dst[x] = dst[x].WithAlpha(255);
                    continue;
                }
                var mask = dib.Slice((int)maskOffset + y * maskStride, maskStride);
                for (var x = 0; x < width; x++)
                {
                    var transparent = (mask[x >> 3] >> (7 - (x & 7)) & 1) != 0;
                    dst[x] = transparent ? BmageRgba32.Transparent : dst[x].WithAlpha(255);
                }
            }
            return image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
    }
}
