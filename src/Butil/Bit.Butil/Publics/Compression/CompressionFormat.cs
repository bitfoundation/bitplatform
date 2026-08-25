namespace Bit.Butil;

/// <summary>
/// The codecs <see cref="Compression"/> understands, matching the strings the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/CompressionStream/CompressionStream">CompressionStream constructor</see>
/// accepts.
/// </summary>
public enum CompressionFormat
{
    /// <summary>
    /// GZIP (RFC 1952). The interoperable default - what .NET's <c>GZipStream</c> and HTTP's
    /// <c>Content-Encoding: gzip</c> produce.
    /// </summary>
    Gzip,

    /// <summary>
    /// Zlib-wrapped DEFLATE (RFC 1950) - what .NET's <c>ZLibStream</c> produces. Note that .NET's
    /// <c>DeflateStream</c> is the <em>raw</em> variant, not this one.
    /// </summary>
    Deflate,

    /// <summary>
    /// Raw DEFLATE (RFC 1951) with no wrapper - the one that matches .NET's <c>DeflateStream</c>.
    /// </summary>
    DeflateRaw,
}
