namespace Bit.BlazorUI;

/// <summary>
/// Identifies where a PDF document is loaded from. A source is either a byte
/// buffer already in memory, or a URL the document can be fetched from.
/// </summary>
public sealed class BitPdfSource
{
    /// <summary>Raw document bytes, when the source is an in-memory buffer.</summary>
    public byte[]? Bytes { get; private init; }

    /// <summary>The URL to fetch the document from, when the source is remote.</summary>
    public string? Url { get; private init; }

    /// <summary>An optional display name (e.g. the original file name).</summary>
    public string? FileName { get; private init; }

    /// <summary>The password to open an encrypted document, if known up front.</summary>
    public string? Password { get; private init; }

    /// <summary>
    /// Extra HTTP request headers sent when the document is fetched from
    /// <see cref="Url"/> - an <c>Authorization</c> header, for instance. Ignored for
    /// an in-memory source.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Headers { get; private init; }

    /// <summary><c>true</c> when this source carries an in-memory byte buffer.</summary>
    public bool IsBytes => Bytes is not null;

    private BitPdfSource() { }

    private BitPdfSource With(Action<Builder> configure)
    {
        var builder = new Builder { Bytes = Bytes, Url = Url, FileName = FileName, Password = Password, Headers = Headers };
        configure(builder);
        return new BitPdfSource
        {
            Bytes = builder.Bytes,
            Url = builder.Url,
            FileName = builder.FileName,
            Password = builder.Password,
            Headers = builder.Headers,
        };
    }

    private sealed class Builder
    {
        public byte[]? Bytes { get; set; }
        public string? Url { get; set; }
        public string? FileName { get; set; }
        public string? Password { get; set; }
        public IReadOnlyDictionary<string, string>? Headers { get; set; }
    }

    /// <summary>Returns a copy of this source that carries <paramref name="password"/>.</summary>
    public BitPdfSource WithPassword(string? password) => With(b => b.Password = password);

    /// <summary>Returns a copy of this source that carries <paramref name="fileName"/>.</summary>
    public BitPdfSource WithFileName(string? fileName) => With(b => b.FileName = fileName);

    /// <summary>
    /// Returns a copy of this source whose fetch sends <paramref name="headers"/>.
    /// Only meaningful for a URL source.
    /// </summary>
    public BitPdfSource WithHeaders(IReadOnlyDictionary<string, string>? headers)
        => With(b => b.Headers = headers);

    /// <summary>Creates a source from an in-memory byte buffer.</summary>
    public static BitPdfSource FromBytes(byte[] bytes, string? fileName = null)
        => new() { Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes)), FileName = fileName };

    /// <summary>Creates a source that will be fetched from <paramref name="url"/>.</summary>
    public static BitPdfSource FromUrl(string url, string? fileName = null)
        => new() { Url = url ?? throw new ArgumentNullException(nameof(url)), FileName = fileName };

    /// <summary>
    /// Reads <paramref name="stream"/> to the end and creates an in-memory source
    /// from it - the shape an upload, a database blob or an embedded resource
    /// arrives in. The stream is read, not owned: the caller still disposes it.
    /// </summary>
    public static async Task<BitPdfSource> FromStreamAsync(Stream stream, string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // A seekable stream of known length is copied into a right-sized buffer; any
        // other one grows a MemoryStream, which is what a network stream needs.
        using var buffer = stream.CanSeek && stream.Length > 0
            ? new MemoryStream((int)Math.Min(stream.Length - stream.Position, int.MaxValue))
            : new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);

        return FromBytes(buffer.ToArray(), fileName);
    }
}
