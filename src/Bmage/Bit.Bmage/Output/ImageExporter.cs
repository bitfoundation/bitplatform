using Bit.Bmage.Capabilities;
using Bit.Bmage.Codecs;
using Bit.Bmage.Input;
using Bit.Bmage.Interop;
using Microsoft.JSInterop;

namespace Bit.Bmage.Output;

/// <summary>
/// Gets processed images out of the browser: download, save dialog, clipboard, object URL or plain bytes.
/// </summary>
public sealed class ImageExporter : IAsyncDisposable
{
    private readonly ImageProcessorFactory _processors;
    private readonly BrowserImageInterop _interop;
    private readonly IImageCapabilityProvider _capabilities;
    private readonly List<string> _objectUrls = [];

    public ImageExporter(ImageProcessorFactory processors, BrowserImageInterop interop, IImageCapabilityProvider capabilities)
    {
        _processors = processors ?? throw new ArgumentNullException(nameof(processors));
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    /// <summary>Encodes an image with the best available codec for the requested format.</summary>
    public async ValueTask<EncodedImage> EncodeAsync(ImageBuffer image, ImageExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        var processor = await _processors.GetAsync(cancellationToken).ConfigureAwait(false);
        return await processor.EncodeAsync(image, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Encodes using the first format this browser actually supports, so a WebP or AVIF request degrades predictably
    /// instead of failing. The chosen format is reported on the result.
    /// </summary>
    public async ValueTask<EncodedImage> EncodeWithFallbackAsync(ImageBuffer image, IReadOnlyList<ImageFormat> preferredFormats, ImageExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(preferredFormats);
        if (preferredFormats.Count == 0) throw new ArgumentException("At least one format is required.", nameof(preferredFormats));
        var capabilities = await _capabilities.GetAsync(cancellationToken).ConfigureAwait(false);
        var format = capabilities.ChooseEncodableFormat(preferredFormats, ImageFormat.Jpeg);
        var effective = (options ?? ImageExportOptions.Png) with { Format = format };
        return await EncodeAsync(image, effective, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Encodes and triggers a browser download.</summary>
    public async ValueTask DownloadAsync(ImageBuffer image, string fileName, ImageExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        var encoded = await EncodeAsync(image, options, cancellationToken).ConfigureAwait(false);
        await DownloadAsync(encoded, fileName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Triggers a browser download of already encoded bytes.</summary>
    public async ValueTask DownloadAsync(EncodedImage encoded, string? fileName = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        var name = EnsureExtension(fileName ?? "image", encoded);
        await _interop.InvokeVoidAsync("download", cancellationToken, encoded.Data, name, encoded.MimeType).ConfigureAwait(false);
    }

    /// <summary>
    /// Saves through the File System Access API when available, falling back to a download. Returns false when the user
    /// cancelled the save dialog.
    /// </summary>
    public async ValueTask<bool> SaveAsAsync(EncodedImage encoded, string? fileName = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        var name = EnsureExtension(fileName ?? "image", encoded);
        return await _interop.InvokeAsync<bool>("saveFile", cancellationToken, encoded.Data, name, encoded.MimeType, encoded.Extension).ConfigureAwait(false);
    }

    /// <summary>Copies an encoded image to the clipboard. Most browsers only allow PNG.</summary>
    public async ValueTask CopyToClipboardAsync(EncodedImage encoded, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        var capabilities = await _capabilities.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!capabilities.SupportsClipboardWrite)
            throw new ImageCapabilityException("This browser cannot write images to the clipboard. Offer a download instead.");
        try
        {
            await _interop.InvokeVoidAsync("writeClipboardImage", cancellationToken, encoded.Data, encoded.MimeType).ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            throw new ImageCapabilityException(
                encoded.Format == ImageFormat.Png
                    ? $"Writing to the clipboard failed: {ex.Message}"
                    : $"Most browsers only accept PNG on the clipboard, not {encoded.Format}. Encode as PNG first. The browser reported: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates an object URL for an encoded image, suitable for an <c>img</c> src. The URL is released when this
    /// exporter is disposed, or explicitly with <see cref="ReleaseObjectUrlAsync"/>.
    /// </summary>
    public async ValueTask<string> CreateObjectUrlAsync(EncodedImage encoded, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        var url = await _interop.InvokeAsync<string>("createObjectUrl", cancellationToken, encoded.Data, encoded.MimeType).ConfigureAwait(false);
        lock (_objectUrls) _objectUrls.Add(url);
        return url;
    }

    /// <summary>Releases an object URL created by this exporter.</summary>
    public async ValueTask ReleaseObjectUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(url)) return;
        lock (_objectUrls) _objectUrls.Remove(url);
        try
        {
            await _interop.InvokeVoidAsync("revokeObjectUrl", cancellationToken, url).ConfigureAwait(false);
        }
        catch (JSException) { /* page already navigated */ }
        catch (JSDisconnectedException) { /* circuit closed */ }
    }

    private static string EnsureExtension(string fileName, EncodedImage encoded)
    {
        var expected = encoded.Extension;
        if (fileName.EndsWith(expected, StringComparison.OrdinalIgnoreCase)) return fileName;
        var existing = System.IO.Path.GetExtension(fileName);
        // Replace a wrong extension, otherwise append the right one.
        return string.IsNullOrEmpty(existing) ? fileName + expected : System.IO.Path.ChangeExtension(fileName, expected.TrimStart('.'));
    }

    /// <summary>Releases every object URL this exporter created.</summary>
    public async ValueTask DisposeAsync()
    {
        string[] urls;
        lock (_objectUrls)
        {
            urls = [.. _objectUrls];
            _objectUrls.Clear();
        }
        foreach (var url in urls)
        {
            try
            {
                await _interop.InvokeVoidAsync("revokeObjectUrl", CancellationToken.None, url).ConfigureAwait(false);
            }
            catch (JSException) { /* page gone */ }
            catch (JSDisconnectedException) { /* circuit closed */ }
            catch (ObjectDisposedException) { /* runtime shutting down */ }
        }
    }
}
