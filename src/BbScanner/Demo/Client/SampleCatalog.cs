using Bit.BbScanner.TestKit;

namespace Bit.BbScanner.Demo.Client;

/// <summary>One generated symbol, ready to render.</summary>
/// <param name="Format">The symbology.</param>
/// <param name="Caption">Label shown under the symbol.</param>
/// <param name="Value">The encoded value.</param>
/// <param name="Matrix">The module grid for a two dimensional symbology.</param>
/// <param name="Modules">The module array for a linear symbology.</param>
public sealed record BarcodeSample(
    BbBarcodeFormat Format,
    string Caption,
    string Value,
    BbBitMatrix? Matrix,
    bool[]? Modules);

/// <summary>
/// Generates a scannable example of every supported symbology.
/// </summary>
/// <remarks>
/// The demo generates its own barcodes rather than shipping images, so the samples are always
/// consistent with the decoder and a visitor can point a real phone camera at the page to check
/// that the library reads symbols it did not itself render.
/// </remarks>
public static class SampleCatalog
{
    private static List<BarcodeSample>? _cached;

    /// <summary>Builds, once, the full set of samples.</summary>
    public static IReadOnlyList<BarcodeSample> All => _cached ??= Build();

    /// <summary>Returns the samples for a set of formats.</summary>
    /// <param name="formats">The symbologies to include.</param>
    public static IEnumerable<BarcodeSample> ForFormats(BbBarcodeFormat formats) =>
        All.Where(s => (s.Format & formats) != 0);

    private static List<BarcodeSample> Build() =>
    [
        new(BbBarcodeFormat.QrCode, "QR Code", "https://bbscanner.bitplatform.dev",
            QrEncoder.Encode("https://bbscanner.bitplatform.dev", BbQrErrorCorrectionLevel.M, maskPattern: 2), null),

        new(BbBarcodeFormat.QrCode, "QR Code (numeric)", "1234567890123456789012345",
            QrEncoder.Encode("1234567890123456789012345", BbQrErrorCorrectionLevel.Q, maskPattern: 5), null),

        new(BbBarcodeFormat.DataMatrix, "Data Matrix", "BIT-BBSCANNER-DM",
            DataMatrixEncoder.Encode("BIT-BBSCANNER-DM"), null),

        new(BbBarcodeFormat.DataMatrix, "Data Matrix (numeric)", "20260905123456",
            DataMatrixEncoder.Encode("20260905123456"), null),

        new(BbBarcodeFormat.Code128, "Code 128", "BIT-BBSCANNER-128",
            null, LinearEncoders.Code128("BIT-BBSCANNER-128")),

        new(BbBarcodeFormat.Code128, "Code 128 (numeric)", "20260905",
            null, LinearEncoders.Code128("20260905")),

        new(BbBarcodeFormat.Code39, "Code 39", "CODE39 DEMO",
            null, LinearEncoders.Code39("CODE39 DEMO")),

        new(BbBarcodeFormat.Ean13, "EAN-13", Ean13Value,
            null, LinearEncoders.Ean13(Ean13Value)),

        new(BbBarcodeFormat.Ean8, "EAN-8", Ean8Value,
            null, LinearEncoders.Ean8(Ean8Value)),

        new(BbBarcodeFormat.UpcA, "UPC-A", UpcAValue,
            null, LinearEncoders.UpcA(UpcAValue)),

        new(BbBarcodeFormat.UpcE, "UPC-E", "01234565",
            null, LinearEncoders.UpcE("01234565")),

        new(BbBarcodeFormat.Itf, "ITF (Interleaved 2 of 5)", "1234567890",
            null, LinearEncoders.Itf("1234567890")),

        // Rendered with the placeholder symbol character table, so this one is only readable by
        // this library, not by a conforming scanner. See the PDF417 note on the samples page.
        new(BbBarcodeFormat.Pdf417, "PDF417 (placeholder table)", "PDF FOUR ONE SEVEN",
            Pdf417Encoder.Encode("PDF FOUR ONE SEVEN", columns: 4, errorCorrectionLevel: 2, rowHeightModules: 4),
            null),
    ];

    private static string Ean13Value { get; } = Complete("400638133393");

    private static string Ean8Value { get; } = Complete("9638507");

    private static string UpcAValue { get; } = CompleteUpcA("03600029145");

    private static string Complete(string withoutCheckDigit) =>
        withoutCheckDigit + LinearEncoders.EanCheckDigit(withoutCheckDigit);

    private static string CompleteUpcA(string withoutCheckDigit) =>
        withoutCheckDigit + LinearEncoders.EanCheckDigit("0" + withoutCheckDigit);
}
