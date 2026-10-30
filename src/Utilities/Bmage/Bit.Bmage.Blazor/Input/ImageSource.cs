using System.Buffers;
using Bit.Bmage.Codecs;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Bmage.Input;

/// <summary>
/// Where an image comes from. Covers every practical browser source so developers never have to convert to base64 or
/// write their own file plumbing.
/// </summary>
/// <example>
/// <code>
/// // From a file input
/// var source = ImageSource.FromBrowserFile(file);
/// // From a URL (subject to CORS)
/// var source = ImageSource.FromUrl("https://example.com/photo.jpg");
/// using var image = await loader.LoadAsync(source);
/// </code>
/// </example>
public abstract class ImageSource
{
    /// <summary>The original file name, when the source has one.</summary>
    public string? FileName { get; protected init; }

    /// <summary>The declared content type, when known. Never trusted for format detection; magic bytes win.</summary>
    public string? ContentType { get; protected init; }

    /// <summary>Size in bytes when known before reading.</summary>
    public long? Length { get; protected init; }

    /// <summary>Reads the encoded bytes. The returned buffer is pooled; dispose it when done.</summary>
    public abstract ValueTask<PooledBuffer> ReadAsync(long maxBytes, CancellationToken cancellationToken = default);

    /// <summary>Wraps an <see cref="IBrowserFile"/> from an <c>InputFile</c> component or a drop zone.</summary>
    public static ImageSource FromBrowserFile(IBrowserFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return new BrowserFileSource(file);
    }

    /// <summary>Wraps raw encoded bytes already in memory.</summary>
    public static ImageSource FromBytes(ReadOnlyMemory<byte> bytes, string? fileName = null, string? contentType = null)
        => new MemorySource(bytes, fileName, contentType);

    /// <summary>Wraps a stream. The stream is read once and is not disposed by Bmage.</summary>
    public static ImageSource FromStream(Stream stream, string? fileName = null, string? contentType = null, long? length = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return new StreamSource(stream, fileName, contentType, length);
    }

    /// <summary>
    /// References a remote URL. The browser fetches it, so the server must allow cross-origin reads; otherwise the load
    /// fails with a clear message rather than a tainted canvas.
    /// </summary>
    public static ImageSource FromUrl(string url, bool withCredentials = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        return new UrlSource(url, withCredentials);
    }

    /// <summary>Parses a <c>data:</c> URL. Provided for compatibility; prefer the byte or file sources.</summary>
    public static ImageSource FromDataUrl(string dataUrl)
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

    private sealed class BrowserFileSource : ImageSource
    {
        private readonly IBrowserFile _file;

        public BrowserFileSource(IBrowserFile file)
        {
            _file = file;
            FileName = file.Name;
            ContentType = file.ContentType;
            Length = file.Size;
        }

        public override async ValueTask<PooledBuffer> ReadAsync(long maxBytes, CancellationToken cancellationToken = default)
        {
            if (_file.Size > maxBytes)
                throw new ImageLimitExceededException($"'{_file.Name}' is {_file.Size} bytes, which exceeds the {maxBytes} byte limit for a single image.");
            await using var stream = _file.OpenReadStream(maxBytes, cancellationToken);
            return await PooledBuffer.ReadAllAsync(stream, (int)_file.Size, maxBytes, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class MemorySource : ImageSource
    {
        private readonly ReadOnlyMemory<byte> _bytes;

        public MemorySource(ReadOnlyMemory<byte> bytes, string? fileName, string? contentType)
        {
            _bytes = bytes;
            FileName = fileName;
            ContentType = contentType;
            Length = bytes.Length;
        }

        public override ValueTask<PooledBuffer> ReadAsync(long maxBytes, CancellationToken cancellationToken = default)
        {
            if (_bytes.Length > maxBytes)
                throw new ImageLimitExceededException($"The image is {_bytes.Length} bytes, which exceeds the {maxBytes} byte limit.");
            return new ValueTask<PooledBuffer>(PooledBuffer.CopyOf(_bytes.Span));
        }
    }

    private sealed class StreamSource : ImageSource
    {
        private readonly Stream _stream;

        public StreamSource(Stream stream, string? fileName, string? contentType, long? length)
        {
            _stream = stream;
            FileName = fileName;
            ContentType = contentType;
            Length = length ?? (stream.CanSeek ? stream.Length : null);
        }

        public override async ValueTask<PooledBuffer> ReadAsync(long maxBytes, CancellationToken cancellationToken = default)
            => await PooledBuffer.ReadAllAsync(_stream, Length is { } l and <= int.MaxValue ? (int)l : 0, maxBytes, cancellationToken).ConfigureAwait(false);
    }

    private sealed class UrlSource : ImageSource
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

        public override ValueTask<PooledBuffer> ReadAsync(long maxBytes, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("A URL source is fetched by the browser. Use ImageLoader.LoadAsync, which routes it through JavaScript interop.");
    }

    /// <summary>True when this source must be fetched by the browser rather than read in .NET.</summary>
    internal bool IsRemote => this is UrlSource;

    internal (string Url, bool WithCredentials) GetRemoteRequest()
        => this is UrlSource u ? (u.Url, u.WithCredentials) : throw new InvalidOperationException("Not a remote source.");
}

/// <summary>A pooled byte buffer holding encoded image data. Dispose to return it to the pool.</summary>
public sealed class PooledBuffer : IDisposable
{
    private byte[]? _array;

    private PooledBuffer(byte[] array, int length)
    {
        _array = array;
        Length = length;
    }

    /// <summary>Number of valid bytes.</summary>
    public int Length { get; }

    /// <summary>The valid bytes.</summary>
    public ReadOnlyMemory<byte> Memory => _array is null ? ReadOnlyMemory<byte>.Empty : _array.AsMemory(0, Length);

    /// <summary>The valid bytes as a span.</summary>
    public ReadOnlySpan<byte> Span => _array is null ? ReadOnlySpan<byte>.Empty : _array.AsSpan(0, Length);

    /// <summary>Copies existing bytes into a pooled buffer.</summary>
    public static PooledBuffer CopyOf(ReadOnlySpan<byte> bytes)
    {
        var array = ArrayPool<byte>.Shared.Rent(Math.Max(1, bytes.Length));
        bytes.CopyTo(array);
        return new PooledBuffer(array, bytes.Length);
    }

    /// <summary>Reads a stream fully into a pooled buffer, refusing to exceed <paramref name="maxBytes"/>.</summary>
    public static async ValueTask<PooledBuffer> ReadAllAsync(Stream stream, int sizeHint, long maxBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var capacity = Math.Max(4096, sizeHint > 0 ? sizeHint : 64 * 1024);
        var array = ArrayPool<byte>.Shared.Rent(capacity);
        var length = 0;
        try
        {
            while (true)
            {
                if (length == array.Length)
                {
                    if (array.Length >= maxBytes)
                        throw new ImageLimitExceededException($"The image exceeds the {maxBytes} byte limit.");
                    var bigger = ArrayPool<byte>.Shared.Rent(Math.Min((long)array.Length * 2, maxBytes + 1) is var n && n > int.MaxValue ? int.MaxValue : (int)n);
                    array.AsSpan(0, length).CopyTo(bigger);
                    ArrayPool<byte>.Shared.Return(array);
                    array = bigger;
                }
                var read = await stream.ReadAsync(array.AsMemory(length), cancellationToken).ConfigureAwait(false);
                if (read == 0) break;
                length += read;
                if (length > maxBytes)
                    throw new ImageLimitExceededException($"The image exceeds the {maxBytes} byte limit.");
            }
            return new PooledBuffer(array, length);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(array);
            throw;
        }
    }

    public void Dispose()
    {
        var array = Interlocked.Exchange(ref _array, null);
        if (array is not null) ArrayPool<byte>.Shared.Return(array);
    }
}
