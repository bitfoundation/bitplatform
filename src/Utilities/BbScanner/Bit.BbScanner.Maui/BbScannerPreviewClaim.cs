namespace Bit.BbScanner;

/// <summary>
/// A camera's hold on a <see cref="BbScannerHost"/>'s preview: the host draws the placements of the latest claim only,
/// so a camera closing late never takes down the preview of the one that replaced it.
/// </summary>
internal sealed class BbScannerPreviewClaim(BbScannerHost host)
{
    public BbScannerHost Host { get; } = host;

    /// <summary>The view the camera draws its preview into.</summary>
    public BbScannerCameraPreview Preview => Host.Preview;

    public void Place(BbScannerPreviewPlacement placement) => Host.Place(this, placement);

    public void Release() => Host.Release(this);
}
