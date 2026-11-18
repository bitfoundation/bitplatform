namespace Bit.BbScanner;

/// <summary>One Data Matrix error correction block group.</summary>
/// <param name="Count">Number of blocks in the group.</param>
/// <param name="DataCodewords">Data codewords per block.</param>
public readonly record struct BbScannerDataMatrixEcBlockGroup(int Count, int DataCodewords);
