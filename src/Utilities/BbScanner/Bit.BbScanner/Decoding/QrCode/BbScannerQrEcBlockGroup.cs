namespace Bit.BbScanner;

/// <summary>
/// One error correction block group: <paramref name="Count"/> blocks that each carry
/// <paramref name="DataCodewords"/> data codewords.
/// </summary>
/// <param name="Count">Number of blocks in the group.</param>
/// <param name="DataCodewords">Data codewords per block.</param>
public readonly record struct BbScannerQrEcBlockGroup(int Count, int DataCodewords);
