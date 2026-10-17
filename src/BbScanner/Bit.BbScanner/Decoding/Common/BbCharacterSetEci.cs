using System.Text;

namespace Bit.BbScanner;

/// <summary>
/// Maps ECI (Extended Channel Interpretation) assignment numbers to text encodings.
/// </summary>
/// <remarks>
/// <para>
/// Only the encodings that ship with the base class library are mapped unconditionally, because
/// pulling in the legacy code page provider would add a dependency and roughly a megabyte to a
/// WebAssembly payload for encodings that rarely appear in the wild. The East Asian code pages
/// are probed once: an application that registers <c>CodePagesEncodingProvider</c> before the
/// first decode gets Shift JIS, GB 2312, Big5 and the rest for free.
/// </para>
/// <para>
/// Unmapped ECI values fall back to ISO-8859-1, which is the behaviour the QR and Data Matrix
/// specifications prescribe for an unsupported interpretation and, unlike a wrong multi-byte
/// decoding, loses nothing: the text is the bytes. The value is still reported through
/// <see cref="BbBarcodeResultMetadata.Eci"/> so an application can re-interpret the raw bytes.
/// </para>
/// </remarks>
public static class BbCharacterSetEci
{
    /// <summary>ISO-8859-1, the default interpretation for QR Code and Data Matrix.</summary>
    public static Encoding Latin1 { get; } = Encoding.Latin1;

    /// <summary>Shift JIS, when the application has registered a code page provider; otherwise <see langword="null"/>.</summary>
    public static Encoding? ShiftJis { get; } = Probe(932);

    /// <summary>GB 2312 / GBK, when the application has registered a code page provider; otherwise <see langword="null"/>.</summary>
    public static Encoding? Gb2312 { get; } = Probe(936);

    private static readonly Encoding? Big5 = Probe(950);
    private static readonly Encoding? Korean = Probe(949);
    private static readonly Encoding? Gb18030 = Probe(54936);

    /// <summary>Returns the encoding for an ECI assignment number.</summary>
    /// <param name="eci">The ECI assignment number.</param>
    public static Encoding FromEci(int eci) => eci switch
    {
        0 or 1 or 2 or 3 => Latin1,
        20 => ShiftJis ?? Latin1,
        25 => Encoding.BigEndianUnicode,
        26 => Encoding.UTF8,
        27 or 170 => Encoding.ASCII,
        28 => Big5 ?? Latin1,
        29 or 31 => Gb2312 ?? Latin1,
        30 => Korean ?? Latin1,
        32 => Gb18030 ?? Gb2312 ?? Latin1,
        _ => Latin1,
    };

    /// <summary>
    /// Looks a code page up without assuming it exists: the base class library throws for any
    /// page it does not carry, and whether a provider has been registered is the host
    /// application's business.
    /// </summary>
    private static Encoding? Probe(int codePage)
    {
        try
        {
            return Encoding.GetEncoding(codePage);
        }
        catch (Exception ex) when (ex is NotSupportedException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Guesses the encoding of a payload that carried no ECI, by checking whether it is valid UTF-8.
    /// </summary>
    /// <param name="bytes">The raw payload.</param>
    /// <remarks>
    /// Many real world encoders emit UTF-8 without declaring ECI 26. Strict UTF-8 validation is a
    /// reliable discriminator: multi-byte sequences that decode cleanly are almost never
    /// coincidental in ISO-8859-1 text.
    /// </remarks>
    public static Encoding GuessEncoding(ReadOnlySpan<byte> bytes)
    {
        var hasHighBytes = false;
        foreach (var b in bytes)
        {
            if (b >= 0x80)
            {
                hasHighBytes = true;
                break;
            }
        }

        if (!hasHighBytes)
        {
            return Latin1;
        }

        try
        {
            var strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            strict.GetString(bytes);
            return strict;
        }
        catch (DecoderFallbackException)
        {
            return Latin1;
        }
    }
}
