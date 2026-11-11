using System.Buffers.Binary;
using Bit.Bmage.Codecs.Png;
using Bit.Bmage.Geometry;
using Bit.Bmage.Memory;
using Bit.Bmage.Metadata;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Transforms;

namespace Bit.Bmage.Codecs.Ico;

/// <summary>
/// Managed ICO decoder: reads the largest image of an icon file, whether stored as PNG or as a Windows bitmap of 1, 4,
/// 8, 24 or 32 bits a pixel with its transparency mask.
/// </summary>
public sealed class IcoDecoder : IImageDecoder
{
    /// <summary>The most images an icon file may list.</summary>
    internal const int MaxEntries = 256;

    private static readonly ImageFormat[] SupportedFormats = [ImageFormat.Ico];

    public IReadOnlyCollection<ImageFormat> Formats => SupportedFormats;

    public ImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        if (ImageFormats.Detect(data) != ImageFormat.Ico) return null;
        try
        {
            var entry = Largest(data);
            return new ImageInfo(entry.Width, entry.Height, ImageFormat.Ico, HasAlpha: true);
        }
        catch (ImageDecodeException)
        {
            return null;
        }
    }

    public ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken = default)
        => new(Decode(data.Span, options, allocator, cancellationToken));

    /// <summary>Decodes the largest image in the icon file.</summary>
    public static ImageBuffer Decode(ReadOnlySpan<byte> data, DecodeOptions? options = null, IPixelAllocator? allocator = null, CancellationToken cancellationToken = default)
    {
        options ??= DecodeOptions.Default;
        allocator ??= PooledPixelAllocator.Shared;
        options.Limits.ValidateEncodedSize(data.Length);
        if (ImageFormats.Detect(data) != ImageFormat.Ico) throw new ImageDecodeException("Not an ICO file.");
        var entry = Largest(data);
        var body = data.Slice(entry.Offset, entry.Length);
        if (ImageFormats.Detect(body) == ImageFormat.Png)
        {
            var png = PngDecoder.Decode(body, options with { ReadMetadata = false }, allocator, cancellationToken);
            png.Metadata = new ImageMetadata(sourceFormat: ImageFormat.Ico, fileName: options.FileName);
            return png;
        }
        var image = DecodeBitmap(body, options, allocator, cancellationToken);
        image.Metadata = new ImageMetadata(sourceFormat: ImageFormat.Ico, fileName: options.FileName);
        return image;
    }

    internal readonly record struct Entry(int Width, int Height, int BitCount, int Offset, int Length);

    /// <summary>Every entry of the directory, checked to lie within the file.</summary>
    internal static List<Entry> ReadDirectory(ReadOnlySpan<byte> data)
    {
        if (data.Length < 6) throw new ImageDecodeException("Truncated ICO header.");
        int count = BinaryPrimitives.ReadUInt16LittleEndian(data[4..]);
        if (count == 0 || count > MaxEntries) throw new ImageDecodeException($"An ICO file lists between 1 and {MaxEntries} images, not {count}.");
        if (6 + 16 * count > data.Length) throw new ImageDecodeException("Truncated ICO directory.");
        var entries = new List<Entry>(count);
        for (var i = 0; i < count; i++)
        {
            var e = data.Slice(6 + 16 * i, 16);
            var length = BinaryPrimitives.ReadInt32LittleEndian(e[8..]);
            var offset = BinaryPrimitives.ReadInt32LittleEndian(e[12..]);
            if (length <= 0 || offset < 6 + 16 * count || offset > data.Length - length)
                throw new ImageDecodeException($"ICO image {i + 1} lies outside the file.");
            int width = e[0] == 0 ? 256 : e[0], height = e[1] == 0 ? 256 : e[1];
            var body = data.Slice(offset, length);
            // A PNG entry's own header gives its real size, which can exceed what the directory's bytes can say.
            if (ImageFormats.Detect(body) == ImageFormat.Png && body.Length >= 24)
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
    private static ImageBuffer DecodeBitmap(ReadOnlySpan<byte> dib, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken)
    {
        if (dib.Length < 40) throw new ImageDecodeException("Truncated ICO bitmap header.");
        var headerSize = BinaryPrimitives.ReadInt32LittleEndian(dib);
        if (headerSize < 40 || headerSize > dib.Length) throw new ImageDecodeException("Invalid ICO bitmap header.");
        var width = BinaryPrimitives.ReadInt32LittleEndian(dib[4..]);
        var doubled = BinaryPrimitives.ReadInt32LittleEndian(dib[8..]);
        int bpp = BinaryPrimitives.ReadUInt16LittleEndian(dib[14..]);
        var compression = BinaryPrimitives.ReadInt32LittleEndian(dib[16..]);
        var colorsUsed = BinaryPrimitives.ReadInt32LittleEndian(dib[32..]);
        if (compression != 0) throw new ImageDecodeException($"Unsupported ICO bitmap compression {compression}.");
        if (width <= 0 || doubled <= 1 || width > 4096 || doubled > 8192) throw new ImageDecodeException("Invalid ICO bitmap dimensions.");
        var height = doubled / 2;
        options.Limits.Validate(width, height);
        if (bpp is not (1 or 4 or 8 or 24 or 32)) throw new ImageDecodeException($"Unsupported ICO bit depth {bpp}.");

        var paletteSize = bpp <= 8 ? (colorsUsed is > 0 and <= 256 ? colorsUsed : 1 << bpp) : 0;
        var paletteOffset = headerSize;
        var pixelOffset = paletteOffset + paletteSize * 4;
        var stride = (width * bpp + 31) / 32 * 4;
        var maskStride = (width + 31) / 32 * 4;
        var maskOffset = pixelOffset + (long)stride * height;
        // Some writers of 32-bit icons leave the mask out: alpha says it all.
        var hasMask = maskOffset + (long)maskStride * height <= dib.Length;
        if (maskOffset > dib.Length || (!hasMask && bpp != 32)) throw new ImageDecodeException("Truncated ICO bitmap.");

        var palette = new Rgba32[Math.Max(paletteSize, 1)];
        for (var i = 0; i < paletteSize; i++)
        {
            var o = paletteOffset + i * 4;
            palette[i] = new Rgba32(dib[o + 2], dib[o + 1], dib[o]);
        }

        var image = ImageBuffer.Create(width, height, allocator, clear: false, options.Limits);
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
                    Rgba32 p;
                    switch (bpp)
                    {
                        case 32:
                            p = new Rgba32(row[x * 4 + 2], row[x * 4 + 1], row[x * 4], row[x * 4 + 3]);
                            if (p.A != 0) anyAlpha = true;
                            break;
                        case 24:
                            p = new Rgba32(row[x * 3 + 2], row[x * 3 + 1], row[x * 3]);
                            break;
                        default:
                            var bit = x * bpp;
                            var index = (row[bit >> 3] >> (8 - bpp - (bit & 7))) & ((1 << bpp) - 1);
                            p = index < paletteSize ? palette[index] : Rgba32.Black;
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
                    dst[x] = transparent ? Rgba32.Transparent : dst[x].WithAlpha(255);
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

/// <summary>
/// Managed ICO encoder. Each image is stored as PNG, which every browser and Windows since Vista read, so transparency
/// and colours are kept exactly. An icon image is at most 256 pixels either way.
/// </summary>
/// <example>
/// <code>
/// using var logo = await loader.LoadAsync(file);
/// var favicon = IcoEncoder.CreateFavicon(logo);  // 16, 32 and 48 pixels in one file
/// await File.WriteAllBytesAsync("favicon.ico", favicon.Data);
/// </code>
/// </example>
public sealed class IcoEncoder : IImageEncoder
{
    /// <summary>The largest side an icon image may have.</summary>
    public const int MaxSize = 256;

    /// <summary>The sizes <see cref="CreateFavicon"/> makes when none are given.</summary>
    public static IReadOnlyList<int> DefaultFaviconSizes { get; } = [16, 32, 48];

    private static readonly ImageFormat[] SupportedFormats = [ImageFormat.Ico];

    public IReadOnlyCollection<ImageFormat> Formats => SupportedFormats;

    /// <summary>
    /// Writes <paramref name="image"/> as a one-image icon file. An image larger than 256 pixels either way is first
    /// scaled down, keeping its shape, to fit inside 256 x 256; <see cref="CreateFavicon"/> makes square icons.
    /// </summary>
    public ValueTask<EncodedImage> EncodeAsync(ImageBuffer image, ImageExportOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Width <= MaxSize && image.Height <= MaxSize) return new(Encode([image], options, cancellationToken));
        using var fitted = new ResizeOperation(MaxSize, MaxSize, ResizeMode.Max, ResamplingFilter.Auto, Anchor.Center, Rgba32.Transparent)
            .Apply(image, new OperationContext(cancellationToken: cancellationToken));
        return new(Encode([fitted], options, cancellationToken));
    }

    /// <summary>Writes the images, one entry each, into an icon file; each is at most 256 pixels either way.</summary>
    /// <exception cref="ArgumentException">No images were given, more than 256, or one is larger than 256 pixels.</exception>
    public static EncodedImage Encode(IReadOnlyList<ImageBuffer> images, ImageExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(images);
        if (images.Count is 0 or > IcoDecoder.MaxEntries) throw new ArgumentException($"An icon holds between 1 and {IcoDecoder.MaxEntries} images.", nameof(images));
        foreach (var image in images)
        {
            ArgumentNullException.ThrowIfNull(image, nameof(images));
            if (image.Width > MaxSize || image.Height > MaxSize)
                throw new ArgumentException($"An icon image is at most {MaxSize} pixels either way, not {image.Width} x {image.Height}: resize it first, or use CreateFavicon.", nameof(images));
        }
        // Icons carry no metadata, and their PNGs need none.
        var png = (options ?? new ImageExportOptions()) with { Format = ImageFormat.Png, Metadata = MetadataPolicy.Remove, MaxFileSize = null };
        var bodies = new byte[images.Count][];
        for (var i = 0; i < images.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bodies[i] = PngEncoder.Encode(images[i], png, cancellationToken).Data;
        }

        var ms = new MemoryStream();
        Span<byte> header = stackalloc byte[6];
        BinaryPrimitives.WriteUInt16LittleEndian(header[2..], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(header[4..], (ushort)images.Count);
        ms.Write(header);
        var offset = 6 + 16 * images.Count;
        Span<byte> entry = stackalloc byte[16];
        for (var i = 0; i < images.Count; i++)
        {
            entry.Clear();
            entry[0] = (byte)(images[i].Width == MaxSize ? 0 : images[i].Width);
            entry[1] = (byte)(images[i].Height == MaxSize ? 0 : images[i].Height);
            BinaryPrimitives.WriteUInt16LittleEndian(entry[4..], 1); // colour planes
            BinaryPrimitives.WriteUInt16LittleEndian(entry[6..], 32); // bits a pixel
            BinaryPrimitives.WriteInt32LittleEndian(entry[8..], bodies[i].Length);
            BinaryPrimitives.WriteInt32LittleEndian(entry[12..], offset);
            ms.Write(entry);
            offset += bodies[i].Length;
        }
        foreach (var body in bodies) ms.Write(body);
        var largest = images.MaxBy(i => (long)i.Width * i.Height)!;
        return new EncodedImage(ms.ToArray(), ImageFormat.Ico, largest.Width, largest.Height);
    }

    /// <summary>
    /// Makes a favicon from <paramref name="source"/>: the image fitted inside a square of each size, centred on
    /// transparency, all in one icon file.
    /// </summary>
    /// <param name="source">The logo or image.</param>
    /// <param name="sizes">The square sizes to include, each between 1 and 256; null for 16, 32 and 48.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    public static EncodedImage CreateFavicon(ImageBuffer source, IReadOnlyList<int>? sizes = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        sizes ??= DefaultFaviconSizes;
        if (sizes.Count == 0) throw new ArgumentException("At least one size is needed.", nameof(sizes));
        var images = new List<ImageBuffer>(sizes.Count);
        try
        {
            var context = new OperationContext(cancellationToken: cancellationToken);
            foreach (var size in sizes.Distinct().Order())
            {
                if (size is < 1 or > MaxSize) throw new ArgumentOutOfRangeException(nameof(sizes), size, $"Each size is between 1 and {MaxSize} pixels.");
                images.Add(new ResizeOperation(size, size, ResizeMode.Contain, ResamplingFilter.Auto, Anchor.Center, Rgba32.Transparent).Apply(source, context));
            }
            return Encode(images, cancellationToken: cancellationToken);
        }
        finally
        {
            foreach (var image in images) image.Dispose();
        }
    }
}
