namespace Bit.BbScanner;

/// <summary>Scratch lists shared between detections so that a frame allocates nothing.</summary>
internal sealed class BbScannerDetectorScratch
{
    public List<BbScannerScanPoint> Samples { get; } = new(512);

    public List<BbScannerScanPoint> Hull { get; } = new(64);

    public List<BbScannerScanPoint> EdgeSamples { get; } = new(128);
}
