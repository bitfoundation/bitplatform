using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Bit.Bmage.Capabilities;
using Bit.Bmage.Geometry;
using Bit.Bmage.Interop;
using Bit.Bmage.Memory;
using Bit.Bmage.Metadata;
using Microsoft.JSInterop;

namespace Bit.Bmage.Codecs.Browser;

/// <summary>Dimensions reported by the browser after decoding.</summary>
public sealed class BrowserImageInfo
{
    [JsonPropertyName("width")] public int Width { get; set; }
    [JsonPropertyName("height")] public int Height { get; set; }
    [JsonPropertyName("mimeType")] public string? MimeType { get; set; }
    [JsonPropertyName("byteLength")] public long ByteLength { get; set; }
}

/// <summary>
/// Decodes images with the browser's own decoders, which cover every format the browser can display, including WebP and
/// AVIF, and are hardware accelerated. Preferred over the managed decoders when running in a browser.
/// </summary>
/// <remarks>
/// The browser applies EXIF orientation while decoding, so the pixels arrive upright. Metadata is then parsed from the
/// original bytes in C#, because the browser discards it.
/// </remarks>
public sealed class BrowserImageDecoder : IImageDecoder
{
    private readonly BrowserImageInterop _interop;
    private readonly ImageFormat[] _formats;

    public BrowserImageDecoder(BrowserImageInterop interop, IEnumerable<ImageFormat>? formats = null)
    {
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
        _formats = formats?.ToArray() ?? [ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.WebP, ImageFormat.Avif, ImageFormat.Gif, ImageFormat.Bmp];
    }

    public IReadOnlyCollection<ImageFormat> Formats => _formats;

    /// <summary>
    /// Header inspection is done in managed code because it needs no browser round trip. Returns null for formats the
    /// managed readers do not understand; call <see cref="IdentifyAsync"/> for those.
    /// </summary>
    public ImageInfo? Identify(ReadOnlySpan<byte> data)
    {
        var format = ImageFormats.Detect(data);
        return format switch
        {
            ImageFormat.Png => new Png.PngDecoder().Identify(data),
            ImageFormat.Jpeg => new Jpeg.JpegDecoder().Identify(data),
            ImageFormat.Bmp => new Bmp.BmpDecoder().Identify(data),
            ImageFormat.Gif => new Gif.GifDecoder().Identify(data),
            ImageFormat.WebP => new WebP.WebPDecoder().Identify(data),
            ImageFormat.Avif => new Avif.AvifDecoder().Identify(data),
            ImageFormat.Tiff => IdentifyTiff(data),
            _ => null,
        };
    }

    /// <summary>The size and orientation of a TIFF's first image, the one the browsers that read TIFF show.</summary>
    private static ImageInfo? IdentifyTiff(ReadOnlySpan<byte> data)
    {
        var width = ExifReader.ReadPrimaryNumber(data, ExifTag.ImageWidth);
        var height = ExifReader.ReadPrimaryNumber(data, ExifTag.ImageLength);
        if (width is not (> 0 and <= int.MaxValue) || height is not (> 0 and <= int.MaxValue)) return null;
        var orientation = ExifReader.ReadPrimaryNumber(data, ExifTag.Orientation) is uint o and >= 1 and <= 8 ? (Orientation)o : Orientation.Normal;
        return new ImageInfo((int)width.Value, (int)height.Value, ImageFormat.Tiff, orientation);
    }

    /// <summary>Reads the dimensions using the browser, for a format passed to the constructor that no managed header reader covers.</summary>
    public async ValueTask<ImageInfo?> IdentifyAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (Identify(data.Span) is { } managed) return managed;
        var format = ImageFormats.Detect(data.Span);
        try
        {
            var info = await _interop.InvokeAsync<BrowserImageInfo>("identify", cancellationToken, data.ToArray(), format.GetMimeType()).ConfigureAwait(false);
            return info is { Width: > 0 } ? new ImageInfo(info.Width, info.Height, format) : null;
        }
        catch (JSException)
        {
            return null;
        }
    }

    // The request's properties are only ever set in .NET and read by System.Text.Json, so without this the trimmer
    // drops their getters and the browser decodes with none of the request's hints in trimmed (release) builds.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DecodeRequest))]
    public async ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        allocator ??= PooledPixelAllocator.Shared;
        options.Limits.ValidateEncodedSize(data.Length);
        var format = ImageFormats.Detect(data.Span);
        if (format == ImageFormat.Unknown)
            throw new ImageDecodeException("The data does not look like a supported image format.");
        if (format == ImageFormat.Svg)
            throw new ImageDecodeException(ImageFormats.SvgRefused);

        // The browser can decode straight to a smaller bitmap, which is what keeps a 48 megapixel phone photo from
        // ever existing at full size on a memory constrained device. It only preserves the aspect ratio when told a
        // single axis, so pass the header dimensions along and let the browser side pick the constraining one. Reading
        // them is cheap: no format's header reader copies its metadata out.
        var header = Identify(data.Span);
        var maxPixels = options.Limits.MaxPixels < long.MaxValue ? options.Limits.MaxPixels : (long?)null;
        var resizeRequired = maxPixels is not null && CheckDecodedSize(data.Span, format, header, options);
        if (OperatingSystem.IsBrowser())
        {
            ImageBuffer decoded;
            try
            {
                decoded = await BrowserPixels.DecodeAsync(_interop, data, format.GetMimeType(), options.AutoOrient,
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
                return decoded;
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

        BrowserBytes<BrowserImageInfo> result;
        try
        {
            result = await _interop.InvokeForBytesAsync<BrowserImageInfo>("decode", cancellationToken, data.ToArray(), request).ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            throw DecodeFailed(format, ex);
        }

        using (result)
        {
            if (!result.HasValue || result.Info is not { Width: > 0, Height: > 0 } info)
                throw new ImageDecodeException("The browser returned no image data.");
            options.Limits.Validate(info.Width, info.Height);
            var expected = (long)info.Width * info.Height * 4;
            if (result.Bytes.Length != expected)
                throw new ImageDecodeException($"The browser returned {result.Bytes.Length} pixel bytes for a {info.Width}x{info.Height} image, expected {expected}.");

            var image = ImageBuffer.Create(info.Width, info.Height, allocator, clear: false, options.Limits);
            try
            {
                result.Bytes.Span.CopyTo(image.Bytes);
                image.Metadata = ReadMetadata(data, format, options);
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
    private static bool CheckDecodedSize(ReadOnlySpan<byte> data, ImageFormat format, ImageInfo? header, DecodeOptions options)
    {
        var decoded = format switch
        {
            ImageFormat.Gif => Gif.GifDecoder.BrowserCanvasSize(data),
            ImageFormat.Avif => Avif.AvifDecoder.UncroppedSize(data),
            _ => header is { Width: > 0, Height: > 0 } ? (header.DisplaySize.Width, header.DisplaySize.Height) : null,
        };
        if (decoded is not { } size)
            throw new ImageDecodeException($"The dimensions of this {format} image could not be read, so it cannot be checked against the pixel limit before the browser decodes it.");
        var scalable = options.Limits.MaxPixels > long.MaxValue / 4 ? long.MaxValue : options.Limits.MaxPixels * 4;
        if (DecodedPixels(size, header, options) > scalable)
            throw new ImageLimitExceededException($"Image dimensions {size.Width}x{size.Height} exceed four times the configured limit of {options.Limits.MaxPixels} pixels.");
        return (double)size.Width * size.Height > scalable;
    }

    /// <summary>
    /// The pixels the browser decodes an image of the given upright size to. As resizeHint in bit-bmage.ts decides, told
    /// one axis of <see cref="DecodeOptions.MaxSize"/> it decodes straight to that; told both, it needs the header's
    /// size to pick the axis that constrains, and gets no hint when that already fits. Otherwise it decodes at full size.
    /// </summary>
    private static double DecodedPixels((int Width, int Height) size, ImageInfo? header, DecodeOptions options)
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

    private static ImageDecodeException DecodeFailed(ImageFormat format, JSException ex)
        => new($"The browser could not decode this {format} image. It may be corrupt, or the format may not be supported here. The browser reported: {ex.Message}", ex);

    /// <summary>
    /// Parses metadata from the original file. The browser strips it during decode, and it must survive to the export
    /// step so a caller asking for <see cref="MetadataPolicy.Preserve"/> actually gets their copyright and camera data.
    /// </summary>
    private static ImageMetadata ReadMetadata(ReadOnlyMemory<byte> data, ImageFormat format, DecodeOptions options)
    {
        if (!options.ReadMetadata) return new ImageMetadata(sourceFormat: format, fileName: options.FileName);
        try
        {
            var metadata = format switch
            {
                ImageFormat.Jpeg => JpegContainer.ReadMetadata(data.Span, options.FileName),
                ImageFormat.Png => PngContainer.ReadMetadata(data.Span, options.FileName),
                ImageFormat.WebP => WebPContainer.ReadMetadata(data.Span, options.FileName),
                ImageFormat.Avif => Avif.AvifDecoder.ReadMetadata(data, options.FileName),
                _ => new ImageMetadata(sourceFormat: format, fileName: options.FileName),
            };
            // The browser already applied the orientation, so record that the pixels are upright.
            return options.AutoOrient ? metadata.WithOrientation(Orientation.Normal) : metadata;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Malformed metadata must never prevent an image from loading.
            return new ImageMetadata(sourceFormat: format, fileName: options.FileName);
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

/// <summary>
/// Encodes with the browser's own encoders, which is the only way to produce WebP and AVIF in the browser.
/// </summary>
/// <remarks>
/// The browser discards metadata when encoding, so EXIF, ICC and XMP are re-attached in C# afterwards according to the
/// export policy. When the browser cannot write the requested format it silently substitutes PNG; this encoder detects
/// that and raises <see cref="ImageCapabilityException"/> rather than returning something the caller did not ask for.
/// </remarks>
public sealed class BrowserImageEncoder : IImageEncoder
{
    private readonly BrowserImageInterop _interop;
    private readonly ImageFormat[] _formats;

    public BrowserImageEncoder(BrowserImageInterop interop, IEnumerable<ImageFormat>? formats = null)
    {
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
        _formats = formats?.ToArray() ?? [ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.WebP, ImageFormat.Avif];
    }

    public IReadOnlyCollection<ImageFormat> Formats => _formats;

    public async ValueTask<EncodedImage> EncodeAsync(ImageBuffer image, ImageExportOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= ImageExportOptions.Png;
        if (!_formats.Contains(options.Format))
            throw new ImageCapabilityException($"The browser encoder is not configured for {options.Format}.");
        // A canvas only writes lossy WebP, so a lossless one, the only lossless choice there is, is the managed encoder's.
        if (options.EncodesLosslessly) return WebP.WebPEncoder.Encode(image, options, cancellationToken);

        // Formats without alpha need an explicit backdrop; otherwise browsers composite over black. The browser draws it
        // behind the pixels, which leaves opaque ones untouched, so there is no need to scan for transparency first.
        var background = options.Format.SupportsAlpha() ? null : options.Background.ToCss();
        var quality = Math.Clamp(options.Quality, 0.01, 1.0);

        BrowserBytes<BrowserImageInfo> result;
        try
        {
            if (OperatingSystem.IsBrowser())
            {
                var encoded = await BrowserPixels.EncodeAsync(_interop, image, options.Format.GetMimeType(), quality, background, cancellationToken).ConfigureAwait(false);
                if (encoded.Length == 0) throw new ImageEncodeException($"The browser produced no output for {options.Format}.");
                return new EncodedImage(ApplyMetadata(encoded, image, options), options.Format, image.Width, image.Height, options.Format.IsLossy() ? quality : null);
            }
            result = await _interop.InvokeForBytesAsync<BrowserImageInfo>(
                "encode", cancellationToken, image.ToArray(), image.Width, image.Height, options.Format.GetMimeType(), quality, background).ConfigureAwait(false);
        }
        catch (JSException ex) when (ex.Message.Contains("unsupported-format:", StringComparison.Ordinal))
        {
            // The message is "unsupported-format:<requested>:<produced>", possibly followed by a JavaScript stack trace.
            var marker = ex.Message[ex.Message.IndexOf("unsupported-format:", StringComparison.Ordinal)..];
            var parts = marker.Split('\n', 2)[0].Split(':');
            var actual = parts.Length > 2 ? parts[2].Trim() : null;
            throw new ImageCapabilityException(
                $"This browser cannot encode {options.Format}; it produced {actual ?? "another format"} instead. " +
                "Check ImageCapabilities.CanEncode before exporting and fall back to a supported format.");
        }
        catch (JSException ex)
        {
            throw new ImageEncodeException($"The browser failed to encode this image as {options.Format}: {ex.Message}", ex);
        }

        using (result)
        {
            if (!result.HasValue || result.Bytes.Length == 0)
                throw new ImageEncodeException($"The browser produced no output for {options.Format}.");
            var bytes = result.Bytes.ToArray();
            bytes = ApplyMetadata(bytes, image, options);
            return new EncodedImage(bytes, options.Format, image.Width, image.Height, options.Format.IsLossy() ? quality : null);
        }
    }

    private static byte[] ApplyMetadata(byte[] bytes, ImageBuffer image, ImageExportOptions options)
    {
        if (options.Metadata == MetadataPolicy.Remove || image.Metadata is null) return bytes;
        var metadata = image.Metadata.Apply(options.Metadata, image.Width, image.Height);
        if (metadata.IsEmpty) return bytes;
        try
        {
            return options.Format switch
            {
                ImageFormat.Jpeg => JpegContainer.WriteMetadata(bytes, metadata),
                ImageFormat.Png => PngContainer.WriteMetadata(bytes, metadata),
                ImageFormat.WebP => WebPContainer.WriteMetadata(bytes, metadata),
                // AVIF metadata boxes are not written by this library; the image is returned without them.
                _ => bytes,
            };
        }
        catch (ImageException)
        {
            // Never lose a correctly encoded image because metadata could not be attached.
            return bytes;
        }
    }
}
