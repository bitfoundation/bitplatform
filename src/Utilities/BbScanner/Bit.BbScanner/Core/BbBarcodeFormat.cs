namespace Bit.BbScanner;

/// <summary>
/// Barcode symbologies supported by the decoding pipeline.
/// </summary>
/// <remarks>
/// The type is a bit flag so that a set of formats can be represented by a single
/// 32 bit value. This keeps the hot path of the decoder allocation free: enabling or
/// disabling a symbology is a single bitwise test rather than a collection lookup.
/// </remarks>
[Flags]
public enum BbBarcodeFormat
{
    /// <summary>No format.</summary>
    None = 0,

    /// <summary>QR Code (ISO/IEC 18004), model 2, versions 1-40.</summary>
    QrCode = 1 << 0,

    /// <summary>Data Matrix ECC 200 (ISO/IEC 16022).</summary>
    DataMatrix = 1 << 1,

    /// <summary>PDF417 (ISO/IEC 15438), including compact (truncated) PDF417.</summary>
    Pdf417 = 1 << 2,

    /// <summary>Code 128 (ISO/IEC 15417), including GS1-128.</summary>
    Code128 = 1 << 3,

    /// <summary>Code 39 (ISO/IEC 16388).</summary>
    Code39 = 1 << 4,

    /// <summary>EAN-13.</summary>
    Ean13 = 1 << 5,

    /// <summary>EAN-8.</summary>
    Ean8 = 1 << 6,

    /// <summary>UPC-A.</summary>
    UpcA = 1 << 7,

    /// <summary>UPC-E.</summary>
    UpcE = 1 << 8,

    /// <summary>Interleaved 2 of 5.</summary>
    Itf = 1 << 9,

    /// <summary>Codabar, also known as NW-7: libraries, blood banks and courier waybills.</summary>
    Codabar = 1 << 10,

    /// <summary>Code 93, including its full ASCII form; both check characters are verified.</summary>
    Code93 = 1 << 11,

    /// <summary>
    /// Aztec Code (ISO/IEC 24778), compact and full range: boarding passes, rail and transit tickets.
    /// </summary>
    Aztec = 1 << 12,

    /// <summary>
    /// GS1 DataBar Omnidirectional and Truncated (ISO/IEC 24724), and their two-row forms Stacked Omnidirectional and
    /// Stacked: a GTIN-14 on loose produce and small retail items.
    /// </summary>
    DataBar = 1 << 13,

    /// <summary>
    /// GS1 DataBar Limited (ISO/IEC 24724): a GTIN-14 starting with 0 or 1 in a symbol small enough for pharmaceuticals
    /// and cosmetics.
    /// </summary>
    DataBarLimited = 1 << 14,

    /// <summary>
    /// GS1 DataBar Expanded and Expanded Stacked (ISO/IEC 24724): any GS1 element string - a GTIN with a weight, a
    /// price, a batch or a best before date - on variable measure fresh foods and coupons.
    /// </summary>
    DataBarExpanded = 1 << 15,

    /// <summary>
    /// MaxiCode (ISO/IEC 16023): the fixed size hexagonal symbol round a bullseye on parcel labels, modes 2 to 5 - the
    /// structured carrier message, standard and enhanced error correction.
    /// </summary>
    MaxiCode = 1 << 16,

    /// <summary>
    /// Micro QR (ISO/IEC 18004), M1 to M4: QR Code's small sibling with one finder pattern, 11 to 17 modules square, on
    /// parts and components too small for a QR symbol.
    /// </summary>
    MicroQrCode = 1 << 17,

    /// <summary>All linear (one dimensional) symbologies.</summary>
    AllLinear = Code128 | Code39 | Ean13 | Ean8 | UpcA | UpcE | Itf | Codabar | Code93 | DataBar | DataBarLimited | DataBarExpanded,

    /// <summary>All matrix (two dimensional) symbologies.</summary>
    AllMatrix = QrCode | DataMatrix | Pdf417 | Aztec | MaxiCode | MicroQrCode,

    /// <summary>Every supported symbology.</summary>
    All = AllLinear | AllMatrix,

    /// <summary>
    /// The symbologies on most products, tickets and labels - QR Code, Data Matrix, Code 128, Code 39, EAN, UPC and
    /// ITF - and what a scanner looks for unless told otherwise. PDF417, Aztec, MaxiCode, Micro QR, Codabar, Code 93
    /// and the GS1 DataBar family are asked for by name: each costs every frame time of its own, and Codabar, which has
    /// no check character, is a source of misreads wherever it is not expected.
    /// </summary>
    Common = QrCode | DataMatrix | Code128 | Code39 | Ean13 | Ean8 | UpcA | UpcE | Itf,
}
