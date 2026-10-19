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

    /// <summary>PDF417 (ISO/IEC 15438).</summary>
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

    /// <summary>All linear (one dimensional) symbologies.</summary>
    AllLinear = Code128 | Code39 | Ean13 | Ean8 | UpcA | UpcE | Itf,

    /// <summary>All matrix (two dimensional) symbologies.</summary>
    AllMatrix = QrCode | DataMatrix | Pdf417,

    /// <summary>Every supported symbology.</summary>
    All = AllLinear | AllMatrix,
}
