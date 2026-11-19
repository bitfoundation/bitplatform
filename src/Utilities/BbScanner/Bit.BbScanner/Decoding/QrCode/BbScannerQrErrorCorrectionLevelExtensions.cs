namespace Bit.BbScanner;

/// <summary>Helpers for <see cref="BbScannerQrErrorCorrectionLevel"/>.</summary>
public static class BbScannerQrErrorCorrectionLevelExtensions
{
    /// <summary>Returns the single letter name used by the specification and by tooling.</summary>
    /// <param name="level">The level.</param>
    public static string ToLetter(this BbScannerQrErrorCorrectionLevel level) => level switch
    {
        BbScannerQrErrorCorrectionLevel.L => "L",
        BbScannerQrErrorCorrectionLevel.M => "M",
        BbScannerQrErrorCorrectionLevel.Q => "Q",
        BbScannerQrErrorCorrectionLevel.H => "H",
        _ => "?",
    };

    /// <summary>
    /// Returns the index of the level in the order the block tables are laid out,
    /// which is L, M, Q, H.
    /// </summary>
    /// <param name="level">The level.</param>
    public static int ToTableIndex(this BbScannerQrErrorCorrectionLevel level) => level switch
    {
        BbScannerQrErrorCorrectionLevel.L => 0,
        BbScannerQrErrorCorrectionLevel.M => 1,
        BbScannerQrErrorCorrectionLevel.Q => 2,
        _ => 3,
    };
}
