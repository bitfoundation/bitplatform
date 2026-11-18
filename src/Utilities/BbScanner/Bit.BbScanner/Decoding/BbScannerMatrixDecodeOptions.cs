namespace Bit.BbScanner;

/// <summary>Per-attempt tuning handed to a <see cref="IBbScannerMatrixDecoder"/>.</summary>
/// <param name="TryHarder">
/// When <see langword="true"/> the decoder may spend significantly more time, for example by
/// trying rotated variants or relaxing detector tolerances. The pipeline sets this for still
/// images and clears it for live camera frames.
/// </param>
/// <param name="AllowInverted">When <see langword="true"/> the decoder also tries a light-on-dark symbol.</param>
public readonly record struct BbScannerMatrixDecodeOptions(bool TryHarder, bool AllowInverted)
{
    /// <summary>The defaults used for live camera frames: fast, no inverted retry.</summary>
    public static BbScannerMatrixDecodeOptions Live => new(TryHarder: false, AllowInverted: false);

    /// <summary>The defaults used for still images: exhaustive.</summary>
    public static BbScannerMatrixDecodeOptions Thorough => new(TryHarder: true, AllowInverted: true);
}
