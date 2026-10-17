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
            ImageFormat.WebP => WebPContainer.ReadSize(data) is { } size ? new ImageInfo(size.Width, size.Height, ImageFormat.WebP) : null,
            _ => null,
        };
    }

    /// <summary>Reads the dimensions using the browser, for formats with no managed header reader (such as AVIF).</summary>
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

    public async ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, DecodeOptions options, IPixelAllocator allocator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        allocator ??= PooledPixelAllocator.Shared;
        options.Limits.ValidateEncodedSize(data.Length);
        var format = ImageFormats.Detect(data.Span);
        if (format == ImageFormat.Unknown)
            throw new ImageDecodeException("The data does not look like a supported image format.");
        if (format == ImageFormat.Svg)
            throw new ImageDecodeException("SVG input is not decoded by Bmage because rasterising untrusted SVG can execute embedded scripts and fetch remote resources. Rasterise it yourself and pass the pixels instead.");

        // The browser can decode straight to a smaller bitmap, which is what keeps a 48 megapixel phone photo from
        // ever existing at full size on a memory constrained device. It only preserves the aspect ratio when told a
        // single axis, so pass the header dimensions along and let the browser side pick the constraining one.
        var header = options.MaxSize is not null ? Identify(data.Span) : null;
        var request = new DecodeRequest
        {
            MimeType = format.GetMimeType(),
            AutoOrient = options.AutoOrient,
            MaxWidth = options.MaxSize?.Width,
            MaxHeight = options.MaxSize?.Height,
            MaxPixels = options.Limits.MaxPixels < long.MaxValue ? options.Limits.MaxPixels : null,
            SourceWidth = header?.DisplaySize.Width,
            SourceHeight = header?.DisplaySize.Height,
        };

        BrowserBytes<BrowserImageInfo> result;
        try
        {
            result = await _interop.InvokeForBytesAsync<BrowserImageInfo>("decode", cancellationToken, data.ToArray(), request).ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            throw new ImageDecodeException($"The browser could not decode this {format} image. It may be corrupt, or the format may not be supported here. The browser reported: {ex.Message}", ex);
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
                image.Metadata = ReadMetadata(data.Span, format, options);
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
    /// Parses metadata from the original file. The browser strips it during decode, and it must survive to the export
    /// step so a caller asking for <see cref="MetadataPolicy.Preserve"/> actually gets their copyright and camera data.
    /// </summary>
    private static ImageMetadata ReadMetadata(ReadOnlySpan<byte> data, ImageFormat format, DecodeOptions options)
    {
        if (!options.ReadMetadata) return new ImageMetadata(sourceFormat: format, fileName: options.FileName);
        try
        {
            var metadata = format switch
            {
                ImageFormat.Jpeg => JpegContainer.ReadMetadata(data, options.FileName),
                ImageFormat.Png => PngContainer.ReadMetadata(data, options.FileName),
                ImageFormat.WebP => WebPContainer.ReadMetadata(data, options.FileName),
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

        // Formats without alpha need an explicit backdrop; otherwise browsers composite over black.
        var background = options.Format.SupportsAlpha() || !image.HasTransparency() ? null : options.Background.ToCss();
        var quality = Math.Clamp(options.Quality, 0.01, 1.0);

        BrowserBytes<BrowserImageInfo> result;
        try
        {
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
