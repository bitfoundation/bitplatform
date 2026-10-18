using System.Text.Json.Serialization;
using Bit.Bmage.Capabilities;
using Bit.Bmage.Codecs;
using Bit.Bmage.Codecs.Browser;
using Bit.Bmage.Pipeline;
using Bit.Bmage.Interop;
using Bit.Bmage.Memory;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Bit.Bmage.Input;

/// <summary>
/// Loads images from any browser source into an <see cref="ImageBuffer"/>: file inputs, drag and drop, the clipboard,
/// the camera, remote URLs and raw bytes.
/// </summary>
public sealed class ImageLoader
{
    private readonly ImageProcessorFactory _processors;
    private readonly BrowserImageInterop _interop;
    private readonly IImageCapabilityProvider _capabilities;

    public ImageLoader(ImageProcessorFactory processors, BrowserImageInterop interop, IImageCapabilityProvider capabilities)
    {
        _processors = processors ?? throw new ArgumentNullException(nameof(processors));
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    /// <summary>Maximum encoded size accepted for a single image. Defaults to 64 MB.</summary>
    public long MaxSourceBytes { get; set; } = 64L * 1024 * 1024;

    /// <summary>Loads an image from any source. The caller owns the returned buffer.</summary>
    public async ValueTask<ImageBuffer> LoadAsync(ImageSource source, DecodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var processor = await _processors.GetAsync(cancellationToken).ConfigureAwait(false);
        options ??= new DecodeOptions();
        if (options.FileName is null && source.FileName is not null) options = options with { FileName = source.FileName };

        if (source.IsRemote)
        {
            var (url, withCredentials) = source.GetRemoteRequest();
            return await LoadFromUrlAsync(processor, url, withCredentials, options, cancellationToken).ConfigureAwait(false);
        }

        using var buffer = await source.ReadAsync(MaxSourceBytes, cancellationToken).ConfigureAwait(false);
        if (buffer.Length == 0) throw new ImageDecodeException($"'{source.FileName ?? "The file"}' is empty.");
        return await processor.DecodeAsync(buffer.Memory, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Loads an image from a file input or drop zone.</summary>
    public ValueTask<ImageBuffer> LoadAsync(IBrowserFile file, DecodeOptions? options = null, CancellationToken cancellationToken = default)
        => LoadAsync(ImageSource.FromBrowserFile(file), options, cancellationToken);

    /// <summary>Loads an image from raw encoded bytes.</summary>
    public ValueTask<ImageBuffer> LoadAsync(ReadOnlyMemory<byte> bytes, DecodeOptions? options = null, CancellationToken cancellationToken = default)
        => LoadAsync(ImageSource.FromBytes(bytes), options, cancellationToken);

    /// <summary>
    /// Loads several images, reporting progress and collecting per-file failures instead of aborting on the first bad file.
    /// </summary>
    public async ValueTask<IReadOnlyList<ImageLoadResult>> LoadManyAsync(
        IEnumerable<ImageSource> sources,
        DecodeOptions? options = null,
        IProgress<(int Completed, int Total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var list = sources.ToList();
        var results = new List<ImageLoadResult>(list.Count);
        for (var i = 0; i < list.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var image = await LoadAsync(list[i], options, cancellationToken).ConfigureAwait(false);
                results.Add(ImageLoadResult.Success(list[i], image));
            }
            catch (OperationCanceledException)
            {
                foreach (var r in results) r.Image?.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                results.Add(ImageLoadResult.Failure(list[i], ex));
            }
            progress?.Report((i + 1, list.Count));
        }
        return results;
    }

    /// <summary>
    /// Loads the image currently on the clipboard, or null when there is none. Requires a user gesture and clipboard
    /// permission in most browsers.
    /// </summary>
    public async ValueTask<ImageBuffer?> LoadFromClipboardAsync(DecodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        var capabilities = await _capabilities.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!capabilities.SupportsClipboardRead)
            throw new ImageCapabilityException("This browser cannot read images from the clipboard. Offer a file picker or a paste target instead.");
        var processor = await _processors.GetAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var result = await _interop.InvokeForBytesAsync<ClipboardInfo>("readClipboardImage", cancellationToken).ConfigureAwait(false);
            if (!result.HasValue || result.Bytes.Length == 0) return null;
            return await processor.DecodeAsync(result.Bytes.Memory, options ?? new DecodeOptions(), cancellationToken).ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            throw new ImageCapabilityException($"Reading the clipboard failed. The browser may have denied permission: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Calls <paramref name="onImage"/> with every image the user pastes into the page (Ctrl+V or Cmd+V). Unlike
    /// <see cref="LoadFromClipboardAsync"/> this needs no clipboard permission, because the browser hands a paste event
    /// its data directly. Pastes without an image, such as text into a field, are left alone. Dispose the result to
    /// stop listening.
    /// </summary>
    /// <param name="onImage">Receives the decoded image, which the callee owns.</param>
    /// <param name="onError">Receives decode failures and oversized pastes. When null they are not reported.</param>
    /// <param name="options">How pasted images are decoded.</param>
    public async ValueTask<IAsyncDisposable> ListenForPasteAsync(Func<ImageBuffer, Task> onImage, Func<Exception, Task>? onError = null, DecodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(onImage);
        var subscription = new PasteSubscription(this, onImage, onError, options);
        try
        {
            subscription.Id = await _interop.InvokeAsync<int>("listenForPaste", CancellationToken.None, subscription.Reference, MaxSourceBytes).ConfigureAwait(false);
        }
        catch
        {
            await subscription.DisposeAsync().ConfigureAwait(false);
            throw;
        }
        return subscription;
    }

    /// <summary>The target of the page's paste listener; JavaScript calls back into it with each pasted image.</summary>
    internal sealed class PasteSubscription : IAsyncDisposable
    {
        private readonly ImageLoader _loader;
        private readonly Func<ImageBuffer, Task> _onImage;
        private readonly Func<Exception, Task>? _onError;
        private readonly DecodeOptions? _options;

        public PasteSubscription(ImageLoader loader, Func<ImageBuffer, Task> onImage, Func<Exception, Task>? onError, DecodeOptions? options)
        {
            _loader = loader;
            _onImage = onImage;
            _onError = onError;
            _options = options;
            Reference = DotNetObjectReference.Create(this);
        }

        internal DotNetObjectReference<PasteSubscription> Reference { get; }

        public int Id { get; set; }

        [JSInvokable]
        public async Task OnPaste(byte[] bytes, string? mimeType)
        {
            try
            {
                var image = await _loader.LoadAsync(ImageSource.FromBytes(bytes, contentType: mimeType), _options).ConfigureAwait(true);
                await _onImage(image).ConfigureAwait(true);
            }
            catch (Exception ex) when (_onError is not null)
            {
                await _onError(ex).ConfigureAwait(true);
            }
        }

        [JSInvokable]
        public Task OnPasteRejected(long length)
            => _onError?.Invoke(new ImageLimitExceededException(
                $"The pasted image is {length:N0} bytes, larger than the {_loader.MaxSourceBytes:N0} bytes allowed.")) ?? Task.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            if (Id > 0)
            {
                try
                {
                    await _loader._interop.InvokeVoidAsync("stopListeningForPaste", CancellationToken.None, Id).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is JSDisconnectedException or JSException or ImageCapabilityException or ObjectDisposedException)
                {
                    // The page is going away, and its listener with it.
                }
                Id = 0;
            }
            Reference.Dispose();
        }
    }

    /// <summary>Fetches and decodes a remote image. Requires the remote server to permit cross-origin reads.</summary>
    private async ValueTask<ImageBuffer> LoadFromUrlAsync(ImageProcessor processor, string url, bool withCredentials, DecodeOptions options, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            throw new ArgumentException($"'{url}' is not a valid URL.", nameof(url));
        if (uri.IsAbsoluteUri && uri.Scheme is not ("http" or "https" or "blob" or "data"))
            throw new ArgumentException($"The scheme '{uri.Scheme}' is not allowed for image URLs. Use http, https, blob or data.", nameof(url));
        try
        {
            using var result = await _interop.InvokeForBytesAsync<ClipboardInfo>("fetchImage", cancellationToken, url, withCredentials).ConfigureAwait(false);
            if (!result.HasValue || result.Bytes.Length == 0)
                throw new ImageDecodeException($"Fetching '{url}' returned no data.");
            return await processor.DecodeAsync(result.Bytes.Memory, options, cancellationToken).ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            throw new ImageDecodeException(
                $"Could not fetch '{url}'. The most common cause is a missing CORS header on the remote server; a browser will not let a page read pixels it cannot access. The browser reported: {ex.Message}", ex);
        }
    }

    private sealed class ClipboardInfo
    {
        [JsonPropertyName("mimeType")] public string? MimeType { get; set; }
        [JsonPropertyName("byteLength")] public long ByteLength { get; set; }
    }
}

/// <summary>The outcome of loading one image in a batch.</summary>
public sealed class ImageLoadResult : IDisposable
{
    private ImageLoadResult(ImageSource source, ImageBuffer? image, Exception? error)
    {
        Source = source;
        Image = image;
        Error = error;
    }

    public static ImageLoadResult Success(ImageSource source, ImageBuffer image) => new(source, image, null);
    public static ImageLoadResult Failure(ImageSource source, Exception error) => new(source, null, error);

    public ImageSource Source { get; }

    /// <summary>The decoded image, or null when loading failed. Owned by the caller.</summary>
    public ImageBuffer? Image { get; }

    public Exception? Error { get; }
    public bool IsSuccess => Image is not null;
    public string? FileName => Source.FileName;

    public void Dispose() => Image?.Dispose();
}

/// <summary>
/// Builds an <see cref="ImageProcessor"/> wired to whichever codecs this environment actually supports. Browser codecs
/// are registered on top of the managed ones so they take precedence where available.
/// </summary>
public sealed class ImageProcessorFactory
{
    private readonly BrowserImageInterop _interop;
    private readonly IImageCapabilityProvider _capabilities;
    private readonly BitBmageOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private ImageProcessor? _processor;

    public ImageProcessorFactory(BrowserImageInterop interop, IImageCapabilityProvider capabilities, BitBmageOptions? options = null)
    {
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        _options = options ?? new BitBmageOptions();
    }

    /// <summary>
    /// Returns the processor for this environment, creating it on first use. In a browser this prefers the native
    /// codecs; elsewhere it falls back to the managed ones so headless and prerendered code still works.
    /// </summary>
    public async ValueTask<ImageProcessor> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_processor is { } cached) return cached;
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_processor is { } existing) return existing;
            var capabilities = await _capabilities.GetAsync(cancellationToken).ConfigureAwait(false);
            var registry = ImageCodecRegistry.CreateDefault();
            if (capabilities.Environment is ImageEnvironment.BrowserWebAssembly or ImageEnvironment.BrowserServer)
            {
                var decodable = capabilities.DecodableFormats.Where(f => f != ImageFormat.Svg).ToArray();
                var encodable = capabilities.EncodableFormats.Where(f => f != ImageFormat.Bmp).ToArray();
                if (decodable.Length > 0) registry.AddDecoder(new BrowserImageDecoder(_interop, decodable));
                if (encodable.Length > 0) registry.AddEncoder(new BrowserImageEncoder(_interop, encodable));
            }
            var processor = new ImageProcessor(registry, _options.Allocator ?? PooledPixelAllocator.Shared, _options.Limits);
            // Only cache once a real browser answered; a prerender pass must not pin the managed-only registry.
            if (capabilities.Environment != ImageEnvironment.Prerendering) _processor = processor;
            return processor;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Drops the cached processor so the next call re-detects capabilities.</summary>
    public void Invalidate() => _processor = null;
}
