using System.Buffers;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Bmage;

/// <summary>
/// Where an image comes from. Covers every practical browser source so developers never have to convert to base64 or
/// write their own file plumbing.
/// </summary>
/// <example>
/// <code>
/// // From a file input
/// var source = BmageImageSource.FromBrowserFile(file);
/// // From a URL (subject to CORS)
/// var source = BmageImageSource.FromUrl("https://example.com/photo.jpg");
/// using var image = await loader.LoadAsync(source);
/// </code>
/// </example>
public abstract class BmageImageSource
{
    /// <summary>The original file name, when the source has one.</summary>
    public string? FileName { get; protected init; }

    /// <summary>The declared content type, when known. Never trusted for format detection; magic bytes win.</summary>
    public string? ContentType { get; protected init; }

    /// <summary>Size in bytes when known before reading.</summary>
    public long? Length { get; protected init; }

    /// <summary>Reads the encoded bytes. The returned buffer is pooled; dispose it when done.</summary>
    public abstract ValueTask<BmagePooledBuffer> ReadAsync(long maxBytes, CancellationToken cancellationToken = default);

    /// <summary>Wraps an <see cref="IBrowserFile"/> from an <c>InputFile</c> component or a drop zone.</summary>
    public static BmageImageSource FromBrowserFile(IBrowserFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return new BrowserFileSource(file);
    }

    /// <summary>Wraps raw encoded bytes already in memory.</summary>
    public static BmageImageSource FromBytes(ReadOnlyMemory<byte> bytes, string? fileName = null, string? contentType = null)
        => new MemorySource(bytes, fileName, contentType);

    /// <summary>Wraps a stream. The stream is read once and is not disposed by Bmage.</summary>
    public static BmageImageSource FromStream(Stream stream, string? fileName = null, string? contentType = null, long? length = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return new StreamSource(stream, fileName, contentType, length);
    }

    /// <summary>
    /// References a remote URL. The browser fetches it, so the server must allow cross-origin reads; otherwise the load
    /// fails with a clear message rather than a tainted canvas.
    /// </summary>
    public static BmageImageSource FromUrl(string url, bool withCredentials = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        return new UrlSource(url, withCredentials);
    }

    /// <summary>Parses a <c>data:</c> URL. Provided for compatibility; prefer the byte or file sources.</summary>
    public static BmageImageSource FromDataUrl(string dataUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataUrl);
        const string prefix = "data:";
        if (!dataUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Not a data URL.", nameof(dataUrl));
        var comma = dataUrl.IndexOf(',');
        if (comma < 0) throw new ArgumentException("Malformed data URL: no comma separator.", nameof(dataUrl));
        var header = dataUrl[prefix.Length..comma];
        var payload = dataUrl[(comma + 1)..];
        var isBase64 = header.EndsWith(";base64", StringComparison.OrdinalIgnoreCase);
        var contentType = isBase64 ? header[..^7] : header;
        byte[] bytes;
        try
        {
            bytes = isBase64 ? Convert.FromBase64String(payload) : System.Text.Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("The data URL payload is not valid base64.", nameof(dataUrl), ex);
        }
        return new MemorySource(bytes, null, string.IsNullOrWhiteSpace(contentType) ? null : contentType);
    }

    private sealed class BrowserFileSource : BmageImageSource
    {
        private readonly IBrowserFile _file;

        public BrowserFileSource(IBrowserFile file)
        {
            _file = file;
            FileName = file.Name;
            ContentType = file.ContentType;
            Length = file.Size;
        }

        public override async ValueTask<BmagePooledBuffer> ReadAsync(long maxBytes, CancellationToken cancellationToken = default)
        {
            if (_file.Size > maxBytes)
                throw new BmageImageLimitExceededException($"'{_file.Name}' is {_file.Size} bytes, which exceeds the {maxBytes} byte limit for a single image.");
            await using var stream = _file.OpenReadStream(maxBytes, cancellationToken);
            return await BmagePooledBuffer.ReadAllAsync(stream, (int)_file.Size, maxBytes, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class MemorySource : BmageImageSource
    {
        private readonly ReadOnlyMemory<byte> _bytes;

        public MemorySource(ReadOnlyMemory<byte> bytes, string? fileName, string? contentType)
        {
            _bytes = bytes;
            FileName = fileName;
            ContentType = contentType;
            Length = bytes.Length;
        }

        public override ValueTask<BmagePooledBuffer> ReadAsync(long maxBytes, CancellationToken cancellationToken = default)
        {
            if (_bytes.Length > maxBytes)
                throw new BmageImageLimitExceededException($"The image is {_bytes.Length} bytes, which exceeds the {maxBytes} byte limit.");
            return new ValueTask<BmagePooledBuffer>(BmagePooledBuffer.CopyOf(_bytes.Span));
        }
    }

    private sealed class StreamSource : BmageImageSource
    {
        private readonly Stream _stream;

        public StreamSource(Stream stream, string? fileName, string? contentType, long? length)
        {
            _stream = stream;
            FileName = fileName;
            ContentType = contentType;
            Length = length ?? (stream.CanSeek ? stream.Length : null);
        }

        public override async ValueTask<BmagePooledBuffer> ReadAsync(long maxBytes, CancellationToken cancellationToken = default)
            => await BmagePooledBuffer.ReadAllAsync(_stream, Length is { } l and <= int.MaxValue ? (int)l : 0, maxBytes, cancellationToken).ConfigureAwait(false);
    }

    private sealed class UrlSource : BmageImageSource
    {
        public UrlSource(string url, bool withCredentials)
        {
            Url = url;
            WithCredentials = withCredentials;
            FileName = TryGetFileName(url);
        }

        public string Url { get; }
        public bool WithCredentials { get; }

        private static string? TryGetFileName(string url)
        {
            try
            {
                var path = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url;
                var name = System.IO.Path.GetFileName(path);
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
            catch (ArgumentException) { return null; }
        }

        public override ValueTask<BmagePooledBuffer> ReadAsync(long maxBytes, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("A URL source is fetched by the browser. Use BmageImageLoader.LoadAsync, which routes it through JavaScript interop.");
    }

    /// <summary>True when this source must be fetched by the browser rather than read in .NET.</summary>
    internal bool IsRemote => this is UrlSource;

    internal (string Url, bool WithCredentials) GetRemoteRequest()
        => this is UrlSource u ? (u.Url, u.WithCredentials) : throw new InvalidOperationException("Not a remote source.");
}
