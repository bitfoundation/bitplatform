using Bit.BbScanner.TestKit;

namespace Bit.BbScanner.Samples.Core;

/// <summary>One generated symbol of a symbology.</summary>
/// <param name="Format">The symbology.</param>
/// <param name="Caption">The symbology's name, shown under the symbol.</param>
/// <param name="Text">What the decoder should read back: the encoded value, or the GS1 element string it makes.</param>
/// <param name="Render">Renders the symbol into a grayscale image, with its quiet zone.</param>
public sealed record SampleSymbol(BbBarcodeFormat Format, string Caption, string Text, Func<BbLuminanceBuffer> Render);

/// <summary>
/// A symbol of every symbology BbScanner reads, generated in memory by the test kit's encoders, so the pages need no
/// image files and a symbol can never drift from what the decoder expects.
/// </summary>
public static class SymbolCatalog
{
    public static IReadOnlyList<SampleSymbol> All { get; } =
    [
        Matrix(BbBarcodeFormat.QrCode, "QR Code", "https://bbscanner.bitplatform.dev",
            () => QrEncoder.Encode("https://bbscanner.bitplatform.dev", BbQrErrorCorrectionLevel.M, maskPattern: 2)),
        Matrix(BbBarcodeFormat.MicroQrCode, "Micro QR", "MICRO QR 42",
            () => MicroQrEncoder.Encode("MICRO QR 42")),
        Matrix(BbBarcodeFormat.DataMatrix, "Data Matrix", "BIT-BBSCANNER-DM",
            () => DataMatrixEncoder.Encode("BIT-BBSCANNER-DM")),
        Matrix(BbBarcodeFormat.Aztec, "Aztec", "M1DEMO/BBSCANNER EABC123 AMSJFKKL 0641",
            () => AztecEncoder.Encode("M1DEMO/BBSCANNER EABC123 AMSJFKKL 0641", compact: true)),
        Matrix(BbBarcodeFormat.Pdf417, "PDF417", "PDF FOUR ONE SEVEN",
            () => Pdf417Encoder.Encode("PDF FOUR ONE SEVEN", columns: 4, errorCorrectionLevel: 2, rowHeightModules: 4)),
        new(BbBarcodeFormat.MaxiCode, "MaxiCode", "MAXICODE DEMO PARCEL 1Z00004951",
            () => SyntheticImage.FromMaxiCode(MaxiCodeEncoder.Encode("MAXICODE DEMO PARCEL 1Z00004951"), moduleWidth: 6, quietZoneModules: 4)),
        Linear(BbBarcodeFormat.Code128, "Code 128", "BIT-BBSCANNER-128",
            () => LinearEncoders.Code128("BIT-BBSCANNER-128")),
        Linear(BbBarcodeFormat.Code39, "Code 39", "CODE39 DEMO",
            () => LinearEncoders.Code39("CODE39 DEMO")),
        Linear(BbBarcodeFormat.Code93, "Code 93", "CODE 93 SAMPLE",
            () => LinearEncoders.Code93("CODE 93 SAMPLE")),
        Linear(BbBarcodeFormat.Codabar, "Codabar", "31234000123457",
            () => LinearEncoders.Codabar("31234000123457")),
        Linear(BbBarcodeFormat.Ean13, "EAN-13", "4006381333931",
            () => LinearEncoders.Ean13("4006381333931")),
        Linear(BbBarcodeFormat.Ean8, "EAN-8", "96385074",
            () => LinearEncoders.Ean8("96385074")),
        Linear(BbBarcodeFormat.UpcA, "UPC-A", "036000291452",
            () => LinearEncoders.UpcA("036000291452")),
        Linear(BbBarcodeFormat.UpcE, "UPC-E", "01234565",
            () => LinearEncoders.UpcE("01234565")),
        Linear(BbBarcodeFormat.Itf, "ITF", "1234567890",
            () => LinearEncoders.Itf("1234567890")),
        // The DataBar family carries GS1 data, and reads back as the element string: application identifier 01
        // and the GTIN-14, then any further elements.
        Linear(BbBarcodeFormat.DataBar, "GS1 DataBar", "0109521234543213",
            () => LinearEncoders.DataBar("09521234543213")),
        Linear(BbBarcodeFormat.DataBarLimited, "GS1 DataBar Limited", "0115012345678907",
            () => LinearEncoders.DataBarLimited("15012345678907")),
        Linear(BbBarcodeFormat.DataBarExpanded, "GS1 DataBar Expanded", "010952123454321310ABC123",
            () => LinearEncoders.DataBarExpanded("010952123454321310ABC123")),
    ];

    private static SampleSymbol Matrix(BbBarcodeFormat format, string caption, string text, Func<BbBitMatrix> encode) =>
        new(format, caption, text, () => SyntheticImage.FromMatrix(encode(), scale: 4));

    private static SampleSymbol Linear(BbBarcodeFormat format, string caption, string text, Func<bool[]> encode) =>
        new(format, caption, text, () => SyntheticImage.FromLinear(encode(), scale: 3, height: 60));
}
