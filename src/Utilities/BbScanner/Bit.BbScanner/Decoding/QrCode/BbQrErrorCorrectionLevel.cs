namespace Bit.BbScanner;

/// <summary>
/// The four QR Code error correction levels, in the numeric order the format information
/// field uses, which is deliberately not the order of increasing capability.
/// </summary>
public enum BbQrErrorCorrectionLevel
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

/// <summary>Helpers for <see cref="BbQrErrorCorrectionLevel"/>.</summary>
public static class BbQrErrorCorrectionLevelExtensions
{
    /// <summary>Returns the single letter name used by the specification and by tooling.</summary>
    /// <param name="level">The level.</param>
    public static string ToLetter(this BbQrErrorCorrectionLevel level) => level switch
    {
        BbQrErrorCorrectionLevel.L => "L",
        BbQrErrorCorrectionLevel.M => "M",
        BbQrErrorCorrectionLevel.Q => "Q",
        BbQrErrorCorrectionLevel.H => "H",
        _ => "?",
    };

    /// <summary>
    /// Returns the index of the level in the order the block tables are laid out,
    /// which is L, M, Q, H.
    /// </summary>
    /// <param name="level">The level.</param>
    public static int ToTableIndex(this BbQrErrorCorrectionLevel level) => level switch
    {
        BbQrErrorCorrectionLevel.L => 0,
        BbQrErrorCorrectionLevel.M => 1,
        BbQrErrorCorrectionLevel.Q => 2,
        _ => 3,
    };
}
