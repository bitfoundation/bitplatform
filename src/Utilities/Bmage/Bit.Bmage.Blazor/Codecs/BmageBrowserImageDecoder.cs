using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Bit.Bmage;

/// <summary>
/// Decodes images with the browser's own decoders, which cover every format the browser can display, including WebP and
/// AVIF, and are hardware accelerated. Preferred over the managed decoders when running in a browser.
/// </summary>
/// <remarks>
/// The browser applies EXIF orientation while decoding, so the pixels arrive upright. Metadata is then parsed from the
/// original bytes in C#, because the browser discards it.
/// </remarks>
public sealed class BmageBrowserImageDecoder : IBmageImageDecoder
{
    private readonly BmageBrowserImageInterop _interop;
    private readonly BmageImageFormat[] _formats;

    public BmageBrowserImageDecoder(BmageBrowserImageInterop interop, IEnumerable<BmageImageFormat>? formats = null)
    {
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
        _formats = formats?.ToArray() ?? [BmageImageFormat.Png, BmageImageFormat.Jpeg, BmageImageFormat.WebP, BmageImageFormat.Avif, BmageImageFormat.Gif, BmageImageFormat.Bmp];
    }

    public IReadOnlyCollection<BmageImageFormat> Formats => _formats;

    /// <summary>
    /// Header inspection is done in managed code because it needs no browser round trip. Returns null for formats the
    /// managed readers do not understand; call <see cref="IdentifyAsync"/> for those.
    /// </summary>
    public BmageImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        var format = BmageImageFormats.Detect(data);
        return format switch
        {
            BmageImageFormat.Png => new BmagePngDecoder().Identify(data),
            BmageImageFormat.Jpeg => new BmageJpegDecoder().Identify(data),
            BmageImageFormat.Bmp => new BmageBmpDecoder().Identify(data),
            BmageImageFormat.Gif => new BmageGifDecoder().Identify(data),
            BmageImageFormat.WebP => new BmageWebPDecoder().Identify(data),
            BmageImageFormat.Avif => new BmageAvifDecoder().Identify(data),
            // The largest image's size, which the pixel limit is checked against; an icon whose directory is invalid
            // gets none, so a pixel limit refuses it before the browser sees it.
            BmageImageFormat.Ico => new BmageIcoDecoder().Identify(data),
            BmageImageFormat.Tiff => IdentifyTiff(data),
            _ => null,
        };
    }

    /// <summary>The size and orientation of a TIFF's first image, the one the browsers that read TIFF show.</summary>
    private static BmageImageInfo? IdentifyTiff(ReadOnlySpan<byte> data)
    {
        var width = BmageExifReader.ReadPrimaryNumber(data, BmageExifTag.ImageWidth);
        var height = BmageExifReader.ReadPrimaryNumber(data, BmageExifTag.ImageLength);
        if (width is not (> 0 and <= int.MaxValue) || height is not (> 0 and <= int.MaxValue)) return null;
        var orientation = BmageExifReader.ReadPrimaryNumber(data, BmageExifTag.Orientation) is uint o and >= 1 and <= 8 ? (BmageOrientation)o : BmageOrientation.Normal;
        return new BmageImageInfo((int)width.Value, (int)height.Value, BmageImageFormat.Tiff, orientation);
    }

    /// <summary>Reads the dimensions using the browser, for a format passed to the constructor that no managed header reader covers.</summary>
    public async ValueTask<BmageImageInfo?> IdentifyAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (Identify(data.Span) is { } managed) return managed;
        var format = BmageImageFormats.Detect(data.Span);
        try
        {
            var info = await _interop.InvokeAsync<BmageBrowserImageInfo>("identify", cancellationToken, data.ToArray(), format.GetMimeType()).ConfigureAwait(false);
            return info is { Width: > 0 } ? new BmageImageInfo(info.Width, info.Height, format) : null;
        }
        catch (JSException)
        {
            return null;
        }
    }

    // The request's properties are only ever set in .NET and read by System.Text.Json, so without this the trimmer
    // drops their getters and the browser decodes with none of the request's hints in trimmed (release) builds.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DecodeRequest))]
    public async ValueTask<BmageImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        allocator ??= BmagePooledPixelAllocator.Shared;
        options.Limits.ValidateEncodedSize(data.Length);
        var format = BmageImageFormats.Detect(data.Span);
        if (format == BmageImageFormat.Unknown)
            throw new BmageImageDecodeException("The data does not look like a supported image format.");
        if (format == BmageImageFormat.Svg)
            throw new BmageImageDecodeException(BmageImageFormats.SvgRefused);

        // The browser can decode straight to a smaller bitmap, which is what keeps a 48 megapixel phone photo from
        // ever existing at full size on a memory constrained device. It only preserves the aspect ratio when told a
        // single axis, so pass the header dimensions along and let the browser side pick the constraining one. Reading
        // them is cheap: no format's header reader copies its metadata out.
        var header = Identify(data.Span);
        var maxPixels = options.Limits.MaxPixels < long.MaxValue ? options.Limits.MaxPixels : (long?)null;
        var resizeRequired = maxPixels is not null && CheckDecodedSize(data.Span, format, header, options);
        if (OperatingSystem.IsBrowser())
        {
            BmageImageBuffer decoded;
            try
            {
                decoded = await BmageBrowserPixels.DecodeAsync(_interop, data, format.GetMimeType(), options.AutoOrient,
                    options.MaxSize?.Width ?? 0, options.MaxSize?.Height ?? 0, maxPixels ?? 0,
                    header?.DisplaySize.Width ?? 0, header?.DisplaySize.Height ?? 0, resizeRequired, options.Limits, allocator, cancellationToken).ConfigureAwait(false);
            }
            catch (JSException ex)
            {
                throw DecodeFailed(format, ex);
            }
            try
            {
                decoded.Metadata = ReadMetadata(data, format, options);
                return AsStored(decoded, data, format, options, allocator, cancellationToken);
            }
            catch
            {
                decoded.Dispose();
                throw;
            }
        }

        var request = new DecodeRequest
        {
            MimeType = format.GetMimeType(),
            AutoOrient = options.AutoOrient,
            MaxWidth = options.MaxSize?.Width,
            MaxHeight = options.MaxSize?.Height,
            MaxPixels = maxPixels,
            SourceWidth = header?.DisplaySize.Width,
            SourceHeight = header?.DisplaySize.Height,
            ResizeRequired = resizeRequired,
        };

        BmageBrowserBytes<BmageBrowserImageInfo> result;
        try
        {
            result = await _interop.InvokeForBytesAsync<BmageBrowserImageInfo>("decode", cancellationToken, data.ToArray(), request).ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            throw DecodeFailed(format, ex);
        }

        using (result)
        {
            if (!result.HasValue || result.Info is not { Width: > 0, Height: > 0 } info)
                throw new BmageImageDecodeException("The browser returned no image data.");
            options.Limits.Validate(info.Width, info.Height);
            var expected = (long)info.Width * info.Height * 4;
            if (result.Bytes.Length != expected)
                throw new BmageImageDecodeException($"The browser returned {result.Bytes.Length} pixel bytes for a {info.Width}x{info.Height} image, expected {expected}.");

            var image = BmageImageBuffer.Create(info.Width, info.Height, allocator, clear: false, options.Limits);
            try
            {
                result.Bytes.Span.CopyTo(image.Bytes);
                image.Metadata = ReadMetadata(data, format, options);
                return AsStored(image, data, format, options, allocator, cancellationToken);
            }
            catch
            {
                image.Dispose();
                throw;
            }
        }
    }

    /// <summary>
    /// Turns an image the browser decoded upright back to how it is stored, when <see cref="BmageDecodeOptions.AutoOrient"/> is
    /// off: the browser applies the orientation however it is asked, so the pixels would otherwise be upright while the
    /// metadata kept the orientation, and anything that applied it again would turn them twice. The managed decoders
    /// leave the pixels as stored, and this gives the same image.
    /// </summary>
    private static BmageImageBuffer AsStored(BmageImageBuffer decoded, ReadOnlyMemory<byte> data, BmageImageFormat format, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken cancellationToken)
    {
        if (options.AutoOrient) return decoded;

        // The orientation the browser applied, from the file even when its metadata was not asked for.
        var metadata = options.ReadMetadata ? decoded.Metadata : ReadMetadata(data, format, options with { ReadMetadata = true });
        var orientation = metadata?.Orientation ?? BmageOrientation.Normal;
        if (orientation == BmageOrientation.Normal) return decoded;

        var context = new BmageOperationContext(allocator, options.Limits, cancellationToken, canMutateSource: true, maxDegreeOfParallelism: options.MaxDegreeOfParallelism);
        var stored = new BmageOrientationOperation(orientation.Inverse()).Apply(decoded, context);
        if (!ReferenceEquals(stored, decoded)) decoded.Dispose();
        return stored;
    }

    /// <summary>
    /// Refuses an image the browser would decode far past the pixel limit. Shrinking to the limit happens only once the
    /// browser has decoded, so this is decided from the file before the browser sees it; four times the limit still
    /// leaves a large photo room to shrink. The size is the one the browser decodes at: a GIF's first frame can grow it
    /// past the logical screen, and an AVIF is decoded whole before its clean aperture crops it. A file whose size cannot
    /// be read is refused as well, since the browser's own reader might find a far larger one in it.
    /// </summary>
    /// <returns>
    /// True when the image passes only because the browser is to decode it smaller: decoded whole, it would be past
    /// four times the limit.
    /// </returns>
    private static bool CheckDecodedSize(ReadOnlySpan<byte> data, BmageImageFormat format, BmageImageInfo? header, BmageDecodeOptions options)
    {
        var decoded = format switch
        {
            BmageImageFormat.Gif => BmageGifDecoder.BrowserCanvasSize(data),
            BmageImageFormat.Avif => BmageAvifDecoder.UncroppedSize(data),
            _ => header is { Width: > 0, Height: > 0 } ? (header.DisplaySize.Width, header.DisplaySize.Height) : null,
        };
        if (decoded is not { } size)
            throw new BmageImageDecodeException($"The dimensions of this {format} image could not be read, so it cannot be checked against the pixel limit before the browser decodes it.");
        var scalable = options.Limits.MaxPixels > long.MaxValue / 4 ? long.MaxValue : options.Limits.MaxPixels * 4;
        if (DecodedPixels(size, header, options) > scalable)
            throw new BmageImageLimitExceededException($"Image dimensions {size.Width}x{size.Height} exceed four times the configured limit of {options.Limits.MaxPixels} pixels.");
        return (double)size.Width * size.Height > scalable;
    }

    /// <summary>
    /// The pixels the browser decodes an image of the given upright size to. As resizeHint in bit-bmage.ts decides, told
    /// one axis of <see cref="BmageDecodeOptions.MaxSize"/> it decodes straight to that; told both, it needs the header's
    /// size to pick the axis that constrains, and gets no hint when that already fits. Otherwise it decodes at full size.
    /// </summary>
    private static double DecodedPixels((int Width, int Height) size, BmageImageInfo? header, BmageDecodeOptions options)
    {
        int maxWidth = options.MaxSize?.Width ?? 0, maxHeight = options.MaxSize?.Height ?? 0;
        double scale;
        if (maxWidth > 0 && maxHeight > 0)
        {
            if (header is not { Width: > 0, Height: > 0 }) return (double)size.Width * size.Height;
            var source = header.DisplaySize;
            if (source.Width <= maxWidth && source.Height <= maxHeight) return (double)size.Width * size.Height;
            scale = (double)maxWidth / source.Width <= (double)maxHeight / source.Height ? (double)maxWidth / size.Width : (double)maxHeight / size.Height;
        }
        else if (maxWidth > 0) scale = (double)maxWidth / size.Width;
        else if (maxHeight > 0) scale = (double)maxHeight / size.Height;
        else return (double)size.Width * size.Height;
        return Math.Ceiling(size.Width * scale) * Math.Ceiling(size.Height * scale);
    }

    private static BmageImageDecodeException DecodeFailed(BmageImageFormat format, JSException ex)
        => new($"The browser could not decode this {format} image. It may be corrupt, or the format may not be supported here. The browser reported: {ex.Message}", ex);

    /// <summary>
    /// Parses metadata from the original file. The browser strips it during decode, and it must survive to the export
    /// step so a caller asking for <see cref="BmageMetadataPolicy.Preserve"/> actually gets their copyright and camera data.
    /// </summary>
    private static BmageImageMetadata ReadMetadata(ReadOnlyMemory<byte> data, BmageImageFormat format, BmageDecodeOptions options)
    {
        var metadata = ReadFileMetadata(data, format, options);
        if (format != BmageImageFormat.Avif || options.AutoOrient) return metadata;

        // The browser turns an AVIF upright by its container's irot and imir, not by an Exif orientation in it, so that
        // is the orientation the pixels are turned back from and the one recorded, the way the managed decoder does.
        var orientation = BmageAvifDecoder.ContainerOrientation(data.Span);
        return metadata.Exif is null
            ? orientation == BmageOrientation.Normal ? metadata : new BmageImageMetadata(BmageExifData.FromOrientation(orientation), metadata.IccProfile, metadata.Xmp, BmageImageFormat.Avif, metadata.FileName)
            : metadata.WithOrientation(orientation);
    }

    private static BmageImageMetadata ReadFileMetadata(ReadOnlyMemory<byte> data, BmageImageFormat format, BmageDecodeOptions options)
    {
        if (!options.ReadMetadata) return new BmageImageMetadata(sourceFormat: format, fileName: options.FileName);
        try
        {
            var metadata = format switch
            {
                BmageImageFormat.Jpeg => BmageJpegContainer.ReadMetadata(data.Span, options.FileName),
                BmageImageFormat.Png => BmagePngContainer.ReadMetadata(data.Span, options.FileName),
                BmageImageFormat.WebP => BmageWebPContainer.ReadMetadata(data.Span, options.FileName),
                BmageImageFormat.Avif => BmageAvifDecoder.ReadMetadata(data, options.FileName),
                _ => new BmageImageMetadata(sourceFormat: format, fileName: options.FileName),
            };
            // The browser already applied the orientation, so record that the pixels are upright.
            return options.AutoOrient ? metadata.WithOrientation(BmageOrientation.Normal) : metadata;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Malformed metadata must never prevent an image from loading.
            return new BmageImageMetadata(sourceFormat: format, fileName: options.FileName);
        }
    }

    private sealed class DecodeRequest
    {
        [JsonPropertyName("mimeType")] public string? MimeType { get; set; }
        [JsonPropertyName("autoOrient")] public bool AutoOrient { get; set; }
        [JsonPropertyName("maxWidth")] public int? MaxWidth { get; set; }
        [JsonPropertyName("maxHeight")] public int? MaxHeight { get; set; }
        [JsonPropertyName("maxPixels")] public long? MaxPixels { get; set; }

        /// <summary>
        /// Dimensions from the file header, as they will appear after the browser applies EXIF orientation. Lets the
        /// browser choose an aspect-preserving decode-time resize; omitted when no managed header reader applies.
        /// </summary>
        [JsonPropertyName("sourceWidth")] public int? SourceWidth { get; set; }
        [JsonPropertyName("sourceHeight")] public int? SourceHeight { get; set; }

        /// <summary>
        /// The image passed the size check only because the browser is to decode it smaller, so a browser that cannot
        /// resize while decoding refuses it rather than decoding it whole.
        /// </summary>
        [JsonPropertyName("resizeRequired")] public bool ResizeRequired { get; set; }
    }
}
