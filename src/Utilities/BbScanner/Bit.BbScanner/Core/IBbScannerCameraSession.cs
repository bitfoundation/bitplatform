namespace Bit.BbScanner;

/// <summary>
/// A camera an <see cref="IBbScannerCameraSource"/> opened. Disposing it closes the camera and takes its preview down.
/// </summary>
/// <remarks>
/// Frames arrive on the platform's own thread and are kept one at a time: <see cref="ReadFrame"/> copies the latest,
/// and a frame that arrives before the scanner has read the last one replaces it. A slow decode therefore never builds a
/// queue of stale frames.
/// </remarks>
public interface IBbScannerCameraSession : IAsyncDisposable
{
    /// <summary>
    /// The camera as it delivers frames now. Its size is the frames' upright size, which changes when the device turns;
    /// a change is a new instance, so a scanner can tell one by reference.
    /// </summary>
    BbScannerCameraInfo Camera { get; }

    /// <summary>What the camera is doing.</summary>
    BbScannerCameraSessionState State { get; }

    /// <summary>How many frames have arrived since the camera opened; it changes with every new one.</summary>
    long FrameCount { get; }

    /// <summary>
    /// Copies the latest frame's luminance, a byte a pixel, into <paramref name="destination"/>, scaled to
    /// <paramref name="width"/> by <paramref name="height"/>, which keep the frame's aspect ratio.
    /// </summary>
    /// <returns>The <see cref="FrameCount"/> of the frame copied, or zero when no frame has arrived yet.</returns>
    long ReadFrame(Span<byte> destination, int width, int height);

    /// <summary>Turns the torch on or off; returns whether the camera accepted it.</summary>
    Task<bool> SetTorchAsync(bool on);

    /// <summary>Sets the zoom, between <see cref="BbScannerCameraInfo.ZoomMin"/> and <see cref="BbScannerCameraInfo.ZoomMax"/>; returns whether the camera accepted it.</summary>
    Task<bool> SetZoomAsync(double zoom);

    /// <summary>Puts the camera's preview where the scanner's video element is in the page, or takes it down.</summary>
    void PlacePreview(BbScannerPreviewPlacement placement);
}
