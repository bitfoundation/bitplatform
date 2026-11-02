namespace Bit.BbScanner;

/// <summary>What the scanner does once it has reported a scan.</summary>
public enum BbAfterScan
{
    /// <summary>Keep scanning: the next symbol is reported as soon as it is read.</summary>
    Continue = 0,

    /// <summary>
    /// Stop decoding but keep the camera open, so a <see cref="BbScanner.Resume"/> - from a
    /// "scan another" button, say - is instant. The view stays live on the scanned symbol.
    /// </summary>
    Pause = 1,

    /// <summary>Release the camera, for a scanner that reads one code and is done.</summary>
    Stop = 2,
}
