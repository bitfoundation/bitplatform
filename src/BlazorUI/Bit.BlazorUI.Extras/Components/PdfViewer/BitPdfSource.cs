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

    /// <summary><c>true</c> when this source carries an in-memory byte buffer.</summary>
    public bool IsBytes => Bytes is not null;

    private BitPdfSource() { }

    /// <summary>Returns a copy of this source that carries <paramref name="password"/>.</summary>
    public BitPdfSource WithPassword(string? password) => new()
    {
        Bytes = Bytes,
        Url = Url,
        FileName = FileName,
        Password = password,
    };

    /// <summary>Creates a source from an in-memory byte buffer.</summary>
    public static BitPdfSource FromBytes(byte[] bytes, string? fileName = null)
        => new() { Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes)), FileName = fileName };

    /// <summary>Creates a source that will be fetched from <paramref name="url"/>.</summary>
    public static BitPdfSource FromUrl(string url, string? fileName = null)
        => new() { Url = url ?? throw new ArgumentNullException(nameof(url)), FileName = fileName };
}
