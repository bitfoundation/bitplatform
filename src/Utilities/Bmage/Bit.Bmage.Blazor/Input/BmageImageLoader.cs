using System.Buffers.Binary;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Bit.Bmage;

/// <summary>
/// Loads images from any browser source into an <see cref="BmageImageBuffer"/>: file inputs, drag and drop, the clipboard,
/// the camera, remote URLs and raw bytes.
/// </summary>
public sealed class BmageImageLoader
{
    private readonly BmageImageProcessorFactory _processors;
    private readonly BmageBrowserImageInterop _interop;
    private readonly IBmageImageCapabilityProvider _capabilities;

    public BmageImageLoader(BmageImageProcessorFactory processors, BmageBrowserImageInterop interop, IBmageImageCapabilityProvider capabilities)
    {
        _processors = processors ?? throw new ArgumentNullException(nameof(processors));
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    /// <summary>Maximum encoded size accepted for a single image. Defaults to 64 MB.</summary>
    public long MaxSourceBytes { get; set; } = 64L * 1024 * 1024;

    /// <summary>Loads an image from any source. The caller owns the returned buffer.</summary>
    public async ValueTask<BmageImageBuffer> LoadAsync(BmageImageSource source, BmageDecodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var processor = await _processors.GetAsync(cancellationToken).ConfigureAwait(false);
        options ??= new BmageDecodeOptions { Limits = processor.Limits };
        if (options.FileName is null && source.FileName is not null) options = options with { FileName = source.FileName };

        if (source.IsRemote)
        {
            var (url, withCredentials) = source.GetRemoteRequest();
            return await LoadFromUrlAsync(processor, url, withCredentials, options, cancellationToken).ConfigureAwait(false);
        }

        using var buffer = await source.ReadAsync(MaxSourceBytes, cancellationToken).ConfigureAwait(false);
        if (buffer.Length == 0) throw new BmageImageDecodeException($"'{source.FileName ?? "The file"}' is empty.");
        return await processor.DecodeAsync(buffer.Memory, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Loads an image from a file input or drop zone.</summary>
    public ValueTask<BmageImageBuffer> LoadAsync(IBrowserFile file, BmageDecodeOptions? options = null, CancellationToken cancellationToken = default)
        => LoadAsync(BmageImageSource.FromBrowserFile(file), options, cancellationToken);

    /// <summary>Loads an image from raw encoded bytes.</summary>
    public ValueTask<BmageImageBuffer> LoadAsync(ReadOnlyMemory<byte> bytes, BmageDecodeOptions? options = null, CancellationToken cancellationToken = default)
        => LoadAsync(BmageImageSource.FromBytes(bytes), options, cancellationToken);

    /// <summary>
    /// Loads several images, reporting progress and collecting per-file failures instead of aborting on the first bad file.
    /// </summary>
    public async ValueTask<IReadOnlyList<BmageImageLoadResult>> LoadManyAsync(
        IEnumerable<BmageImageSource> sources,
        BmageDecodeOptions? options = null,
        IProgress<(int Completed, int Total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var list = sources.ToList();
        var results = new List<BmageImageLoadResult>(list.Count);
        for (var i = 0; i < list.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var image = await LoadAsync(list[i], options, cancellationToken).ConfigureAwait(false);
                results.Add(BmageImageLoadResult.Success(list[i], image));
            }
            catch (OperationCanceledException)
            {
                foreach (var r in results) r.Image?.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                results.Add(BmageImageLoadResult.Failure(list[i], ex));
            }
            progress?.Report((i + 1, list.Count));
        }
        return results;
    }

    /// <summary>
    /// Loads the image currently on the clipboard, or null when there is none. Requires a user gesture and clipboard
    /// permission in most browsers.
    /// </summary>
    public async ValueTask<BmageImageBuffer?> LoadFromClipboardAsync(BmageDecodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        var capabilities = await _capabilities.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!capabilities.SupportsClipboardRead)
            throw new BmageImageCapabilityException("This browser cannot read images from the clipboard. Offer a file picker or a paste target instead.");
        var processor = await _processors.GetAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var result = await _interop.InvokeForBytesAsync<ClipboardInfo>("readClipboardImage", cancellationToken, MaxSourceBytes).ConfigureAwait(false);
            // An image past the limit is refused by its size, before the browser reads it.
            if (result.Info is { TooLarge: true } refused)
                throw new BmageImageLimitExceededException($"The image on the clipboard is {refused.Size:N0} bytes, larger than the {MaxSourceBytes:N0} bytes allowed.");
            if (!result.HasValue || result.Bytes.Length == 0) return null;
            return await processor.DecodeAsync(result.Bytes.Memory, options ?? new BmageDecodeOptions { Limits = processor.Limits }, cancellationToken).ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            throw new BmageImageCapabilityException($"Reading the clipboard failed. The browser may have denied permission: {ex.Message}", ex);
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
    public async ValueTask<IAsyncDisposable> ListenForPasteAsync(Func<BmageImageBuffer, Task> onImage, Func<Exception, Task>? onError = null, BmageDecodeOptions? options = null)
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
        private readonly BmageImageLoader _loader;
        private readonly Func<BmageImageBuffer, Task> _onImage;
        private readonly Func<Exception, Task>? _onError;
        private readonly BmageDecodeOptions? _options;

        public PasteSubscription(BmageImageLoader loader, Func<BmageImageBuffer, Task> onImage, Func<Exception, Task>? onError, BmageDecodeOptions? options)
        {
            _loader = loader;
            _onImage = onImage;
            _onError = onError;
            _options = options;
            Reference = DotNetObjectReference.Create(this);
        }

        internal DotNetObjectReference<PasteSubscription> Reference { get; }

        public int Id { get; set; }

        /// <summary>
        /// Called with the number of a paste whose image the page holds. The image is streamed out of the page rather
        /// than passed to this call: over a Blazor Server circuit a call's arguments are one message, which the
        /// circuit refuses past 32 KB by default, ending it.
        /// </summary>
        [JSInvokable]
        public async Task OnPaste(int id)
        {
            try
            {
                BmageImageBuffer image;
                using (var pasted = await _loader._interop.InvokeForBytesAsync<ClipboardInfo>("takePasted", CancellationToken.None, id).ConfigureAwait(true))
                {
                    if (!pasted.HasValue || pasted.Bytes.Length == 0) return;
                    image = await _loader.LoadAsync(BmageImageSource.FromBytes(pasted.Bytes.Memory, contentType: pasted.Info!.MimeType), _options).ConfigureAwait(true);
                }

                await _onImage(image).ConfigureAwait(true);
            }
            catch (Exception ex) when (_onError is not null)
            {
                await _onError(ex).ConfigureAwait(true);
            }
        }

        [JSInvokable]
        public Task OnPasteRejected(long length)
            => _onError?.Invoke(new BmageImageLimitExceededException(
                $"The pasted image is {length:N0} bytes, larger than the {_loader.MaxSourceBytes:N0} bytes allowed.")) ?? Task.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            if (Id > 0)
            {
                try
                {
                    await _loader._interop.InvokeVoidAsync("stopListeningForPaste", CancellationToken.None, Id).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is JSDisconnectedException or JSException or BmageImageCapabilityException or ObjectDisposedException)
                {
                    // The page is going away, and its listener with it.
                }
                Id = 0;
            }
            Reference.Dispose();
        }
    }

    /// <summary>Fetches and decodes a remote image. Requires the remote server to permit cross-origin reads.</summary>
    private async ValueTask<BmageImageBuffer> LoadFromUrlAsync(BmageImageProcessor processor, string url, bool withCredentials, BmageDecodeOptions options, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            throw new ArgumentException($"'{url}' is not a valid URL.", nameof(url));
        if (uri.IsAbsoluteUri && uri.Scheme is not ("http" or "https" or "blob" or "data"))
            throw new ArgumentException($"The scheme '{uri.Scheme}' is not allowed for image URLs. Use http, https, blob or data.", nameof(url));
        try
        {
            using var result = await _interop.InvokeForBytesAsync<ClipboardInfo>("fetchImage", cancellationToken, url, withCredentials, MaxSourceBytes).ConfigureAwait(false);
            if (!result.HasValue)
                throw new BmageImageLimitExceededException($"'{url}' exceeds the {MaxSourceBytes} byte limit for a single image.");
            if (result.Bytes.Length == 0)
                throw new BmageImageDecodeException($"Fetching '{url}' returned no data.");
            return await processor.DecodeAsync(result.Bytes.Memory, options, cancellationToken).ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            throw new BmageImageDecodeException(
                $"Could not fetch '{url}'. The most common cause is a missing CORS header on the remote server; a browser will not let a page read pixels it cannot access. The browser reported: {ex.Message}", ex);
        }
    }

    private sealed class ClipboardInfo
    {
        [JsonPropertyName("mimeType")] public string? MimeType { get; set; }
        [JsonPropertyName("byteLength")] public long ByteLength { get; set; }

        /// <summary>Set when the clipboard's image was past the source limit and left unread; <see cref="Size"/> is then its length.</summary>
        [JsonPropertyName("tooLarge")] public bool TooLarge { get; set; }
        [JsonPropertyName("size")] public long Size { get; set; }
    }
}
