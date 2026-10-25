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

    /// <summary>
    /// Rasterises a two dimensional sample, <paramref name="scale"/> pixels a module: square modules, or MaxiCode's
    /// hexagons round its bullseye.
    /// </summary>
    /// <param name="sample">A sample with a <see cref="BarcodeSample.Matrix"/>.</param>
    /// <param name="scale">Pixels a module.</param>
    public static BbLuminanceBuffer RenderMatrix(BarcodeSample sample, int scale) =>
        sample.Format == BbBarcodeFormat.MaxiCode
            ? SyntheticImage.FromMaxiCode(sample.Matrix!, moduleWidth: scale, quietZoneModules: 4)
            : SyntheticImage.FromMatrix(sample.Matrix!, scale: scale);

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

        new(BbBarcodeFormat.Code93, "Code 93", "CODE 93 SAMPLE",
            null, LinearEncoders.Code93("CODE 93 SAMPLE")),

        new(BbBarcodeFormat.Codabar, "Codabar", "31234000123457",
            null, LinearEncoders.Codabar("31234000123457")),

        // DataBar carries a GTIN-14 and reads back as the GS1 element string: application identifier 01, then the GTIN.
        new(BbBarcodeFormat.DataBar, "GS1 DataBar", "0109521234543213",
            null, LinearEncoders.DataBar("09521234543213")),

        // DataBar Limited holds a GTIN-14 starting with 0 or 1, in a symbol small enough for a pharmacy pack.
        new(BbBarcodeFormat.DataBarLimited, "GS1 DataBar Limited", "0115012345678907",
            null, LinearEncoders.DataBarLimited("15012345678907")),

        // DataBar Expanded holds any GS1 element string: here a GTIN and a batch number.
        new(BbBarcodeFormat.DataBarExpanded, "GS1 DataBar Expanded", "010952123454321310ABC123",
            null, LinearEncoders.DataBarExpanded("010952123454321310ABC123")),

        new(BbBarcodeFormat.Aztec, "Aztec (compact)", "M1DEMO/BBSCANNER EABC123 AMSJFKKL 0641",
            AztecEncoder.Encode("M1DEMO/BBSCANNER EABC123 AMSJFKKL 0641", compact: true), null),

        new(BbBarcodeFormat.Aztec, "Aztec (full range)", "Aztec Code, full range, read at any angle: bbscanner.bitplatform.dev",
            AztecEncoder.Encode("Aztec Code, full range, read at any angle: bbscanner.bitplatform.dev", compact: false), null),

        new(BbBarcodeFormat.Pdf417, "PDF417", "PDF FOUR ONE SEVEN",
            Pdf417Encoder.Encode("PDF FOUR ONE SEVEN", columns: 4, errorCorrectionLevel: 2, rowHeightModules: 4),
            null),

        // MaxiCode: the parcel sorting symbol, hexagons round a bullseye, always the same size.
        new(BbBarcodeFormat.MaxiCode, "MaxiCode", "MAXICODE DEMO PARCEL 1Z00004951",
            MaxiCodeEncoder.Encode("MAXICODE DEMO PARCEL 1Z00004951"), null),

        // Micro QR: one finder pattern, for a short value in a small space.
        new(BbBarcodeFormat.MicroQrCode, "Micro QR", "MICRO QR 42",
            MicroQrEncoder.Encode("MICRO QR 42"), null),
    ];

    private static string Ean13Value { get; } = Complete("400638133393");

    private static string Ean8Value { get; } = Complete("9638507");

    private static string UpcAValue { get; } = CompleteUpcA("03600029145");

    private static string Complete(string withoutCheckDigit) =>
        withoutCheckDigit + LinearEncoders.EanCheckDigit(withoutCheckDigit);

    private static string CompleteUpcA(string withoutCheckDigit) =>
        withoutCheckDigit + LinearEncoders.EanCheckDigit("0" + withoutCheckDigit);
}
