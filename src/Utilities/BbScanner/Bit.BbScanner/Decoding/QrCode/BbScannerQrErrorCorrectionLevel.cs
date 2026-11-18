namespace Bit.BbScanner;

/// <summary>
/// The four QR Code error correction levels, in the numeric order the format information
/// field uses, which is deliberately not the order of increasing capability.
/// </summary>
public enum BbScannerQrErrorCorrectionLevel
{
    /// <summary>Recovers roughly 7% of the codewords.</summary>
    M = 0,

    /// <summary>Recovers roughly 15% of the codewords.</summary>
    L = 1,

    /// <summary>Recovers roughly 25% of the codewords.</summary>
    H = 2,

    /// <summary>Recovers roughly 30% of the codewords.</summary>
    Q = 3,
}
