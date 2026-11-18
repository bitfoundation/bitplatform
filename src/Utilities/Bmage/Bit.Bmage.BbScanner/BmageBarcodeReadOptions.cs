using Bit.BbScanner;

namespace Bit.Bmage;

/// <summary>How <see cref="BmageBarcodes.Read"/> looks for codes.</summary>
public sealed record BmageBarcodeReadOptions
{
    /// <summary>The decoders' options; null for <see cref="BbScannerOptions.ForStillImages"/>.</summary>
    public BbScannerOptions? Scanner { get; init; }

    /// <summary>The most codes to return.</summary>
    public int MaxSymbols
    {
        get;
        init => field = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(MaxSymbols), value, "At least one code must be asked for.");
    } = 16;

    /// <summary>
    /// What to try, in order, until one gives a code: the first entry is usually <see cref="BmageBarcodePreprocessing.None"/>,
    /// so an image that reads as it is costs one decode. Defaults to as it is, sharpened, then enlarged: cheapest first.
    /// </summary>
    public IReadOnlyList<BmageBarcodePreprocessing> Preprocessing { get; init; } =
        [BmageBarcodePreprocessing.None, BmageBarcodePreprocessing.Sharpen, BmageBarcodePreprocessing.Enlarge];

    /// <summary>The longer side <see cref="BmageBarcodePreprocessing.Enlarge"/> enlarges a smaller image to; larger images are skipped by it.</summary>
    public int MinimumSize
    {
        get;
        init => field = value is >= 64 and <= 4096 ? value : throw new ArgumentOutOfRangeException(nameof(MinimumSize), value, "The size must be between 64 and 4096 pixels.");
    } = 1200;
}
