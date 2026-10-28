namespace Bit.BbScanner;

/// <summary>
/// A camera of the platform's own, which the scanner reads in place of the page's: in a Blazor Hybrid app, the device's
/// camera through its native API. <c>Bit.BbScanner.Maui</c> registers one for .NET MAUI.
/// </summary>
/// <remarks>
/// <para>
/// Registered as a service, it is used wherever .NET does not run in the page - everywhere but WebAssembly, where the
/// page's camera is the fastest there is. Its frames go from the platform straight to the decoder, in the app's own
/// process and at the camera's own resolution, without crossing into the page; the page only hosts the scanner's
/// overlays and controls, as anywhere else, over a preview the source draws itself where the scanner's video element is.
/// </para>
/// <para>
/// Where the source is not <see cref="IsSupported"/>, the scanner opens the page's camera instead, as it does with no
/// source registered at all.
/// </para>
/// </remarks>
public interface IBbCameraSource
{
    /// <summary>Whether a camera can be opened here at all: the platform has a camera API and the app is set up for it.</summary>
    bool IsSupported { get; }

    /// <summary>Whether the app is in the background, where the platform does not let it use the camera.</summary>
    bool IsInBackground { get; }

    /// <summary>What opening a camera would do about permission, without asking the user.</summary>
    Task<BbCameraPermission> GetPermissionAsync();

    /// <summary>The cameras that can be opened.</summary>
    Task<IReadOnlyList<BbCameraDevice>> GetCamerasAsync();

    /// <summary>
    /// Opens a camera, asking the user for permission first where the platform needs it, and shows its preview.
    /// </summary>
    /// <param name="request">Which camera, at about which size.</param>
    /// <param name="cancellationToken">Abandons the request; a camera that opens afterwards is closed again.</param>
    /// <returns>The open camera.</returns>
    /// <exception cref="BbScannerException">
    /// The camera could not be opened: <see cref="BbScannerErrorKind.PermissionDenied"/>,
    /// <see cref="BbScannerErrorKind.NoCamera"/>, <see cref="BbScannerErrorKind.CameraUnavailable"/> or
    /// <see cref="BbScannerErrorKind.NotSupported"/>.
    /// </exception>
    Task<IBbCameraSession> OpenAsync(BbCameraRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Which camera the scanner asks an <see cref="IBbCameraSource"/> for.</summary>
public sealed class BbCameraRequest
{
    /// <summary>A specific camera, from <see cref="IBbCameraSource.GetCamerasAsync"/>, or <see langword="null"/> to go by <see cref="Facing"/>.</summary>
    public string? DeviceId { get; init; }

    /// <summary>Which camera to prefer when <see cref="DeviceId"/> is not set.</summary>
    public BbCameraFacing Facing { get; init; } = BbCameraFacing.Rear;

    /// <summary>The frame width to aim for; the camera delivers what it has that is closest.</summary>
    public int Width { get; init; } = 1280;

    /// <summary>The frame height to aim for.</summary>
    public int Height { get; init; } = 720;
}

/// <summary>What an open camera is doing.</summary>
public enum BbCameraSessionState
{
    /// <summary>Delivering frames.</summary>
    Running = 0,

    /// <summary>
    /// Not delivering frames for now - the app is in the background, or the platform took the camera away from it for a
    /// while - and will again once it is back. The scanner keeps the camera meanwhile.
    /// </summary>
    Interrupted = 1,

    /// <summary>Gone for good: unplugged, taken by another app, or failed. Open it again to scan.</summary>
    Ended = 2,
}

/// <summary>
/// A camera an <see cref="IBbCameraSource"/> opened. Disposing it closes the camera and takes its preview down.
/// </summary>
/// <remarks>
/// Frames arrive on the platform's own thread and are kept one at a time: <see cref="ReadFrame"/> copies the latest,
/// and a frame that arrives before the scanner has read the last one replaces it. A slow decode therefore never builds a
/// queue of stale frames.
/// </remarks>
public interface IBbCameraSession : IAsyncDisposable
{
    /// <summary>
    /// The camera as it delivers frames now. Its size is the frames' upright size, which changes when the device turns;
    /// a change is a new instance, so a scanner can tell one by reference.
    /// </summary>
    BbCameraInfo Camera { get; }

    /// <summary>What the camera is doing.</summary>
    BbCameraSessionState State { get; }

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

    /// <summary>Sets the zoom, between <see cref="BbCameraInfo.ZoomMin"/> and <see cref="BbCameraInfo.ZoomMax"/>; returns whether the camera accepted it.</summary>
    Task<bool> SetZoomAsync(double zoom);

    /// <summary>Puts the camera's preview where the scanner's video element is in the page, or takes it down.</summary>
    void PlacePreview(BbPreviewPlacement placement);
}

/// <summary>
/// Where the scanner's video element is, for a native preview drawn behind the page: in CSS pixels of the page's
/// viewport, which a WebView lays out one to one with the platform's device independent units.
/// </summary>
/// <param name="X">The element's left edge.</param>
/// <param name="Y">The element's top edge.</param>
/// <param name="Width">The element's width.</param>
/// <param name="Height">The element's height.</param>
/// <param name="CornerRadius">The scanner's corner radius, so the preview is rounded as the scanner is.</param>
/// <param name="Visible">Whether the element is on screen at all.</param>
/// <param name="Backdrop">
/// The page's own background colour as CSS gives it: the page is made transparent around the scanner while a preview
/// shows through it, and what is behind the page is painted this colour instead.
/// </param>
/// <param name="ClipX">The left edge of the part of the viewport the element shows in.</param>
/// <param name="ClipY">The top edge of that part.</param>
/// <param name="ClipWidth">Its width.</param>
/// <param name="ClipHeight">
/// Its height. The viewport, cut down by every element above the scanner that clips what overflows it - a scrolling
/// panel the scanner is partly scrolled out of, say: the preview shows inside it only.
/// </param>
/// <remarks>The default placement says the page has taken the preview out altogether: put everything back.</remarks>
public readonly record struct BbPreviewPlacement(
    double X, double Y, double Width, double Height, double CornerRadius, bool Visible, string? Backdrop,
    double ClipX, double ClipY, double ClipWidth, double ClipHeight)
{
    /// <summary>Whether this is the placement that says the page has taken the preview out.</summary>
    public bool IsDetached => this == default;
}
