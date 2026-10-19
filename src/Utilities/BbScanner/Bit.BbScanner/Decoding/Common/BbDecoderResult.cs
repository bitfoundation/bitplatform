namespace Bit.BbScanner;

/// <summary>
/// The output of a symbology decoder: the recovered payload plus whatever the symbol said
/// about itself.
/// </summary>
public sealed class BbDecoderResult
{
    /// <summary>Creates a result.</summary>
    /// <param name="rawBytes">The recovered payload bytes.</param>
    /// <param name="text">The payload interpreted as text.</param>
    public BbDecoderResult(byte[] rawBytes, string text)
    {
        RawBytes = rawBytes;
        Text = text;
    }

    /// <summary>The recovered payload bytes.</summary>
    public byte[] RawBytes { get; }

    /// <summary>The payload interpreted as text.</summary>
    public string Text { get; }

    /// <summary>Error correction level reported by the symbol, if any.</summary>
    public string? ErrorCorrectionLevel { get; init; }

    /// <summary>Symbol version or size, if reported.</summary>
    public string? SymbolVersion { get; init; }

    /// <summary>Number of codewords repaired by error correction.</summary>
    public int ErrorsCorrected { get; init; }

    /// <summary>ECI assignment number carried by the symbol.</summary>
    public int? Eci { get; init; }

    /// <summary>Index of this symbol within a structured append sequence.</summary>
    public int? StructuredAppendIndex { get; init; }

    /// <summary>Total number of symbols in the structured append sequence.</summary>
    public int? StructuredAppendCount { get; init; }

    /// <summary>Parity or file identifier that groups a structured append sequence.</summary>
    public int? StructuredAppendParity { get; init; }

    /// <summary>Number of rows, when the symbology reports one.</summary>
    public int? Rows { get; init; }

    /// <summary>Number of columns, when the symbology reports one.</summary>
    public int? Columns { get; init; }

    /// <summary><see langword="true"/> when the symbol advertises a GS1 application identifier stream.</summary>
    public bool IsGs1 { get; init; }

    /// <summary>Builds the public metadata object, or <see langword="null"/> when nothing is worth reporting.</summary>
    public BbBarcodeResultMetadata? ToMetadata()
    {
        if (ErrorCorrectionLevel is null &&
            SymbolVersion is null &&
            ErrorsCorrected == 0 &&
            Eci is null &&
            StructuredAppendIndex is null &&
            Rows is null &&
            Columns is null &&
            !IsGs1)
        {
            return null;
        }

        return new BbBarcodeResultMetadata
        {
            ErrorCorrectionLevel = ErrorCorrectionLevel,
            SymbolVersion = SymbolVersion,
            ErrorsCorrected = ErrorsCorrected,
            Eci = Eci,
            StructuredAppendIndex = StructuredAppendIndex,
            StructuredAppendCount = StructuredAppendCount,
            StructuredAppendParity = StructuredAppendParity,
            Rows = Rows,
            Columns = Columns,
            IsGs1 = IsGs1 ? true : null,
        };
    }
}
