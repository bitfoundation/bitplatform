namespace Bit.BbScanner;

/// <summary>How far an incomplete structured append sequence has got.</summary>
/// <param name="Format">The symbology of the sequence.</param>
/// <param name="Received">How many of its symbols have been read.</param>
/// <param name="Count">How many symbols it has; <see langword="null"/> for a Macro PDF417 file whose segments do not say and whose last has not been read.</param>
/// <param name="Missing">The places still to be read, numbered as <see cref="BbScannerBarcodeResultMetadata.StructuredAppendIndex"/> numbers them; empty while <paramref name="Count"/> is unknown.</param>
public sealed record BbScannerStructuredAppendProgress(BbScannerBarcodeFormat Format, int Received, int? Count, IReadOnlyList<int> Missing);
