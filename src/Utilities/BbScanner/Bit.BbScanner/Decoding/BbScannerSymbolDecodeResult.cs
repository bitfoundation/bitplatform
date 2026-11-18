namespace Bit.BbScanner;

/// <summary>
/// What a single symbology decoder returns when it recognises a symbol.
/// </summary>
/// <param name="Payload">The recovered payload and symbol metadata.</param>
/// <param name="Format">The symbology that produced the payload.</param>
/// <param name="Points">Corner or endpoint locations in image coordinates.</param>
public sealed record BbScannerSymbolDecodeResult(BbScannerDecoderResult Payload, BbScannerBarcodeFormat Format, BbScannerScanPoint[] Points);
