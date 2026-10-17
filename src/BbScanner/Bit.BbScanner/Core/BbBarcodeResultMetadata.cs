namespace Bit.BbScanner;

/// <summary>
/// Optional, symbology specific information produced alongside a decoded value.
/// Every member is <see langword="null"/> when the symbology does not carry it.
/// </summary>
public sealed class BbBarcodeResultMetadata
{
    /// <summary>Error correction level, for example <c>"L"</c>, <c>"M"</c>, <c>"Q"</c> or <c>"H"</c> for QR codes.</summary>
    public string? ErrorCorrectionLevel { get; init; }

    /// <summary>Symbol version or size, for example the QR version number or the Data Matrix symbol size.</summary>
    public string? SymbolVersion { get; init; }

    /// <summary>Number of codewords the Reed-Solomon stage had to repair. Useful as a print-quality signal.</summary>
    public int? ErrorsCorrected { get; init; }

    /// <summary>ECI assignment number carried by the symbol, when present.</summary>
    public int? Eci { get; init; }

    /// <summary>Index of this symbol within a structured append sequence.</summary>
    public int? StructuredAppendIndex { get; init; }

    /// <summary>Total number of symbols in the structured append sequence.</summary>
    public int? StructuredAppendCount { get; init; }

    /// <summary>Parity or file identifier that groups a structured append sequence.</summary>
    public int? StructuredAppendParity { get; init; }

    /// <summary>Number of PDF417 rows, or number of rows in the sampled matrix.</summary>
    public int? Rows { get; init; }

    /// <summary>Number of PDF417 data columns, or number of columns in the sampled matrix.</summary>
    public int? Columns { get; init; }

    /// <summary><see langword="true"/> when the symbol advertises a GS1 application identifier stream.</summary>
    public bool? IsGs1 { get; init; }
}
