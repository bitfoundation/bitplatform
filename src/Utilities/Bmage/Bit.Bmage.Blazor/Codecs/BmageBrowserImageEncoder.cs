using Microsoft.JSInterop;

namespace Bit.Bmage;

/// <summary>
/// Encodes with the browser's own encoders, which is the only way to produce WebP and AVIF in the browser.
/// </summary>
/// <remarks>
/// The browser discards metadata when encoding, so EXIF, ICC and XMP are re-attached in C# afterwards according to the
/// export policy. When the browser cannot write the requested format it silently substitutes PNG; this encoder detects
/// that and raises <see cref="BmageImageCapabilityException"/> rather than returning something the caller did not ask for.
/// </remarks>
public sealed class BmageBrowserImageEncoder : IBmageImageEncoder
{
    private readonly BmageBrowserImageInterop _interop;
    private readonly BmageImageFormat[] _formats;

    public BmageBrowserImageEncoder(BmageBrowserImageInterop interop, IEnumerable<BmageImageFormat>? formats = null)
    {
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
        _formats = formats?.ToArray() ?? [BmageImageFormat.Png, BmageImageFormat.Jpeg, BmageImageFormat.WebP, BmageImageFormat.Avif];
    }

    public IReadOnlyCollection<BmageImageFormat> Formats => _formats;

    public async ValueTask<BmageEncodedImage> EncodeAsync(BmageImageBuffer image, BmageImageExportOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= BmageImageExportOptions.Png;
        if (!_formats.Contains(options.Format))
            throw new BmageImageCapabilityException($"The browser encoder is not configured for {options.Format}.");
        // A canvas only writes lossy WebP, so a lossless one, the only lossless choice there is, is the managed encoder's.
        if (options.EncodesLosslessly) return BmageWebPEncoder.Encode(image, options, cancellationToken);
        // Nor does it write an indexed PNG.
        if (options.Format == BmageImageFormat.Png && options.MaxColors is not null) return BmagePngEncoder.Encode(image, options, cancellationToken);

        // Formats without alpha need an explicit backdrop; otherwise browsers composite over black. The browser draws it
        // behind the pixels, which leaves opaque ones untouched, so there is no need to scan for transparency first.
        var background = options.Format.SupportsAlpha() ? null : options.Background.ToCss();
        var quality = Math.Clamp(options.Quality, 0.01, 1.0);

        BmageBrowserBytes<BmageBrowserImageInfo> result;
        try
        {
            if (OperatingSystem.IsBrowser())
            {
                var encoded = await BmageBrowserPixels.EncodeAsync(_interop, image, options.Format.GetMimeType(), quality, background, cancellationToken).ConfigureAwait(false);
                if (encoded.Length == 0) throw new BmageImageEncodeException($"The browser produced no output for {options.Format}.");
                return new BmageEncodedImage(ApplyMetadata(encoded, image, options), options.Format, image.Width, image.Height, options.Format.IsLossy() ? quality : null);
            }
            result = await _interop.InvokeForBytesAsync<BmageBrowserImageInfo>(
                "encode", cancellationToken, image.ToArray(), image.Width, image.Height, options.Format.GetMimeType(), quality, background).ConfigureAwait(false);
        }
        catch (JSException ex) when (ex.Message.Contains("unsupported-format:", StringComparison.Ordinal))
        {
            // The message is "unsupported-format:<requested>:<produced>", possibly followed by a JavaScript stack trace.
            var marker = ex.Message[ex.Message.IndexOf("unsupported-format:", StringComparison.Ordinal)..];
            var parts = marker.Split('\n', 2)[0].Split(':');
            var actual = parts.Length > 2 ? parts[2].Trim() : null;
            throw new BmageImageCapabilityException(
                $"This browser cannot encode {options.Format}; it produced {actual ?? "another format"} instead. " +
                "Check BmageImageCapabilities.CanEncode before exporting and fall back to a supported format.");
        }
        catch (JSException ex)
        {
            throw new BmageImageEncodeException($"The browser failed to encode this image as {options.Format}: {ex.Message}", ex);
        }

        using (result)
        {
            if (!result.HasValue || result.Bytes.Length == 0)
                throw new BmageImageEncodeException($"The browser produced no output for {options.Format}.");
            var bytes = result.Bytes.ToArray();
            bytes = ApplyMetadata(bytes, image, options);
            return new BmageEncodedImage(bytes, options.Format, image.Width, image.Height, options.Format.IsLossy() ? quality : null);
        }
    }

    private static byte[] ApplyMetadata(byte[] bytes, BmageImageBuffer image, BmageImageExportOptions options)
    {
        if (options.Metadata == BmageMetadataPolicy.Remove || image.Metadata is null) return bytes;
        var metadata = image.Metadata.Apply(options.Metadata, image.Width, image.Height);
        if (metadata.IsEmpty) return bytes;
        try
        {
            return options.Format switch
            {
                BmageImageFormat.Jpeg => BmageJpegContainer.WriteMetadata(bytes, metadata),
                BmageImageFormat.Png => BmagePngContainer.WriteMetadata(bytes, metadata),
                BmageImageFormat.WebP => BmageWebPContainer.WriteMetadata(bytes, metadata),
                // AVIF metadata boxes are not written by this library; the image is returned without them.
                _ => bytes,
            };
        }
        catch (BmageImageException)
        {
            // Never lose a correctly encoded image because metadata could not be attached.
            return bytes;
        }
    }
}
