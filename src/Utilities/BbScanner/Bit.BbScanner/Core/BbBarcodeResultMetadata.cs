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

    /// <summary>
    /// Position of this symbol within a structured append sequence, as the symbology's specification numbers it:
    /// from 0 for QR Code (0 to 15) and PDF417 (the Macro PDF417 segment index, 0 to 99 998), from 1 for Data Matrix
    /// (1 to 16). <see cref="BbStructuredAppendAssembler"/> puts a sequence back together whichever way it counts.
    /// </summary>
    public int? StructuredAppendIndex { get; init; }

    /// <summary>
    /// Total number of symbols in the structured append sequence. A Macro PDF417 segment carries it only when its
    /// encoder wrote the optional segment count field; the last segment is then known by
    /// <see cref="StructuredAppendIsLast"/>.
    /// </summary>
    public int? StructuredAppendCount { get; init; }

    /// <summary>
    /// What groups a QR Code or Data Matrix structured append sequence: QR Code's parity byte - the exclusive or of
    /// every byte of the whole message - or Data Matrix's two byte file identification.
    /// </summary>
    public int? StructuredAppendParity { get; init; }

    /// <summary>
    /// The Macro PDF417 file ID that every segment of one file carries, each of its codewords written as the three
    /// digits it stands for; <see langword="null"/> for the other symbologies, whose sequences are grouped by
    /// <see cref="StructuredAppendParity"/>.
    /// </summary>
    public string? StructuredAppendFileId { get; init; }

    /// <summary>
    /// <see langword="true"/> when this symbol is the last of its structured append sequence: for Macro PDF417 the
    /// segment that closes its control block with the terminator, for QR Code and Data Matrix the one whose
    /// position is the count.
    /// </summary>
    public bool? StructuredAppendIsLast { get; init; }

    /// <summary>Number of PDF417 rows, or number of rows in the sampled matrix.</summary>
    public int? Rows { get; init; }

    /// <summary>Number of PDF417 data columns, or number of columns in the sampled matrix.</summary>
    public int? Columns { get; init; }

    /// <summary><see langword="true"/> when the symbol advertises a GS1 application identifier stream.</summary>
    public bool? IsGs1 { get; init; }

    /// <summary>
    /// The digits of an EAN-2 or EAN-5 add-on printed beside an EAN-13, UPC-A, UPC-E or EAN-8:
    /// a magazine's issue number, a book's price. <see cref="BbBarcodeResult.Text"/> carries the
    /// main symbol alone.
    /// </summary>
    public string? Extension { get; init; }
}
