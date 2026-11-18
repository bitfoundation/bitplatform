using System.Buffers.Binary;

namespace Bit.Bmage;

/// <summary>
/// Managed ICO encoder. Each image is stored as PNG, which every browser and Windows since Vista read, so transparency
/// and colours are kept exactly. An icon image is at most 256 pixels either way.
/// </summary>
/// <example>
/// <code>
/// using var logo = await loader.LoadAsync(file);
/// var favicon = BmageIcoEncoder.CreateFavicon(logo);  // 16, 32 and 48 pixels in one file
/// await File.WriteAllBytesAsync("favicon.ico", favicon.Data);
/// </code>
/// </example>
public sealed class BmageIcoEncoder : IBmageImageEncoder
{
    /// <summary>The largest side an icon image may have.</summary>
    public const int MaxSize = 256;

    /// <summary>The sizes <see cref="CreateFavicon"/> makes when none are given.</summary>
    public static IReadOnlyList<int> DefaultFaviconSizes { get; } = [16, 32, 48];

    private static readonly BmageImageFormat[] SupportedFormats = [BmageImageFormat.Ico];

    public IReadOnlyCollection<BmageImageFormat> Formats => SupportedFormats;

    /// <summary>
    /// Writes <paramref name="image"/> as a one-image icon file. An image larger than 256 pixels either way is first
    /// scaled down, keeping its shape, to fit inside 256 x 256; <see cref="CreateFavicon"/> makes square icons.
    /// </summary>
    public ValueTask<BmageEncodedImage> EncodeAsync(BmageImageBuffer image, BmageImageExportOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Width <= MaxSize && image.Height <= MaxSize) return new(Encode([image], options, cancellationToken));
        using var fitted = new BmageResizeOperation(MaxSize, MaxSize, BmageResizeMode.Max, BmageResamplingFilter.Auto, BmageAnchor.Center, BmageRgba32.Transparent)
            .Apply(image, new BmageOperationContext(cancellationToken: cancellationToken));
        return new(Encode([fitted], options, cancellationToken));
    }

    /// <summary>Writes the images, one entry each, into an icon file; each is at most 256 pixels either way.</summary>
    /// <exception cref="ArgumentException">No images were given, more than 256, or one is larger than 256 pixels.</exception>
    public static BmageEncodedImage Encode(IReadOnlyList<BmageImageBuffer> images, BmageImageExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(images);
        if (images.Count is 0 or > BmageIcoDecoder.MaxEntries) throw new ArgumentException($"An icon holds between 1 and {BmageIcoDecoder.MaxEntries} images.", nameof(images));
        foreach (var image in images)
        {
            ArgumentNullException.ThrowIfNull(image, nameof(images));
            if (image.Width > MaxSize || image.Height > MaxSize)
                throw new ArgumentException($"An icon image is at most {MaxSize} pixels either way, not {image.Width} x {image.Height}: resize it first, or use CreateFavicon.", nameof(images));
        }
        // Icons carry no metadata, and their PNGs need none; nor a palette, which would lose the exact colours.
        var png = (options ?? new BmageImageExportOptions()) with { Format = BmageImageFormat.Png, Metadata = BmageMetadataPolicy.Remove, MaxFileSize = null, MaxColors = null };
        var bodies = new byte[images.Count][];
        for (var i = 0; i < images.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bodies[i] = BmagePngEncoder.Encode(images[i], png, cancellationToken).Data;
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
        return new BmageEncodedImage(ms.ToArray(), BmageImageFormat.Ico, largest.Width, largest.Height);
    }

    /// <summary>
    /// Makes a favicon from <paramref name="source"/>: the image fitted inside a square of each size, centred on
    /// transparency, all in one icon file.
    /// </summary>
    /// <param name="source">The logo or image.</param>
    /// <param name="sizes">The square sizes to include, each between 1 and 256; null for 16, 32 and 48.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    public static BmageEncodedImage CreateFavicon(BmageImageBuffer source, IReadOnlyList<int>? sizes = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        sizes ??= DefaultFaviconSizes;
        if (sizes.Count == 0) throw new ArgumentException("At least one size is needed.", nameof(sizes));
        var images = new List<BmageImageBuffer>(sizes.Count);
        try
        {
            var context = new BmageOperationContext(cancellationToken: cancellationToken);
            foreach (var size in sizes.Distinct().Order())
            {
                if (size is < 1 or > MaxSize) throw new ArgumentOutOfRangeException(nameof(sizes), size, $"Each size is between 1 and {MaxSize} pixels.");
                images.Add(new BmageResizeOperation(size, size, BmageResizeMode.Contain, BmageResamplingFilter.Auto, BmageAnchor.Center, BmageRgba32.Transparent).Apply(source, context));
            }
            return Encode(images, cancellationToken: cancellationToken);
        }
        finally
        {
            foreach (var image in images) image.Dispose();
        }
    }
}
