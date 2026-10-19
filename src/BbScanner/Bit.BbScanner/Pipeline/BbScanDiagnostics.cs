namespace Bit.BbScanner;

/// <summary>
/// Timings and counters from the most recently decoded frame.
/// </summary>
/// <remarks>
/// Exposed because tuning a scanner is otherwise guesswork: the split between grayscale
/// conversion, binarisation, matrix detection and linear scanning is what tells an application
/// whether to lower the resolution, narrow the scan region or drop a symbology.
/// </remarks>
public readonly record struct BbScanDiagnostics
{
    /// <summary>Width in pixels of the image the decoders actually saw, after cropping and downsampling.</summary>
    public int DecodedWidth { get; init; }

    /// <summary>Height in pixels of the image the decoders actually saw.</summary>
    public int DecodedHeight { get; init; }

    /// <summary>Total time spent on the frame.</summary>
    public TimeSpan Total { get; init; }

    /// <summary>Time spent converting packed pixels to the luminance plane.</summary>
    public TimeSpan Grayscale { get; init; }

    /// <summary>Time spent binarising for the matrix symbologies.</summary>
    public TimeSpan Binarize { get; init; }

    /// <summary>Time spent in matrix symbology detection and decoding.</summary>
    public TimeSpan Matrix { get; init; }

    /// <summary>Time spent scanning rows for linear symbologies.</summary>
    public TimeSpan Linear { get; init; }

    /// <summary>How many scan lines were examined.</summary>
    public int RowsScanned { get; init; }

    /// <summary><see langword="true"/> when the frame produced a result.</summary>
    public bool Decoded { get; init; }

    /// <summary>Frames per second the measured total time corresponds to.</summary>
    public double EquivalentFramesPerSecond => Total > TimeSpan.Zero ? 1.0 / Total.TotalSeconds : 0;
}
