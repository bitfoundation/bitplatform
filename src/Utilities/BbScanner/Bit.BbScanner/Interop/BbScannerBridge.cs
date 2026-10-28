using Microsoft.JSInterop;

namespace Bit.BbScanner;

/// <summary>
/// The scanner's way into its JavaScript module, wherever .NET runs relative to the page.
/// </summary>
/// <remarks>
/// <para>
/// The camera is always the page's: only the browser - or the WebView of a Blazor Hybrid app - can open it. What
/// differs between the hosting models is how far .NET is from it. On WebAssembly .NET runs in the page, and
/// <see cref="BbScannerWasmBridge"/> calls the module through <c>[JSImport]</c>, handing a frame over through a memory
/// view without copying it twice. Under Blazor Server .NET is across a network, and in a BlazorWebView behind the
/// WebView's message channel; <see cref="BbScannerJsRuntimeBridge"/> calls the same module through
/// <see cref="IJSRuntime"/> there, and pixels come back in slices, as byte arrays.
/// </para>
/// <para>
/// Every member is asynchronous so that one component serves both: on WebAssembly the calls complete synchronously
/// and the <see cref="ValueTask"/>s cost nothing.
/// </para>
/// </remarks>
internal abstract class BbScannerBridge : IAsyncDisposable
{
    /// <summary>The low 24 bits of a poll: the change versions of the cameras, the video size and the view size.</summary>
    internal const int PollChanges = 0xFFFFFF;

    /// <summary>Poll flag: the page is hidden.</summary>
    internal const int PollHidden = 1 << 24;

    /// <summary>Poll flag: the camera track has ended.</summary>
    internal const int PollEnded = 1 << 25;

    /// <summary>Poll flag: the camera that ended was interrupted - by the page being hidden, or by the watchdog - rather than lost.</summary>
    internal const int PollInterrupted = 1 << 26;

    /// <summary>Poll flag: the camera has delivered a frame not grabbed yet.</summary>
    internal const int PollFrame = 1 << 27;

    /// <summary>Poll flag: the poll grabbed that frame already, as the luminance of the region it was given, ready to read.</summary>
    internal const int PollGrabbed = 1 << 28;

    /// <summary>Capability flag: the page exposes the camera API.</summary>
    internal const int CapabilityCamera = 1;

    /// <summary>Capability flag: the page is a secure context.</summary>
    internal const int CapabilitySecureContext = 2;

    /// <summary>Returned by the frame grab when the camera track has ended.</summary>
    internal const int FrameTrackEnded = -2;

    /// <summary>Returned by the frame grab, and by reading a prepared image, when the buffer is too small.</summary>
    internal const int FrameBufferTooSmall = -1;

    /// <summary>
    /// The bridge for where the component runs: the browser itself; a process that reaches it through
    /// <paramref name="jsRuntime"/> - across a network, or in the same process behind a WebView when
    /// <paramref name="inWebView"/>; or, there, the platform's own camera when <paramref name="cameraSource"/> supplies
    /// one.
    /// </summary>
    public static BbScannerBridge Create(IJSRuntime jsRuntime, IBbCameraSource? cameraSource, bool inWebView)
    {
        if (OperatingSystem.IsBrowser())
        {
            return new BbScannerWasmBridge();
        }

        var page = new BbScannerJsRuntimeBridge(jsRuntime, local: inWebView);
        return cameraSource is { IsSupported: true } ? new BbScannerNativeBridge(page, cameraSource) : page;
    }

    /// <summary>
    /// Whether pixels cross a process or a network to reach .NET. Frames then arrive as the luminance of the searched
    /// region only, and prepared images as luminance, so that a fraction of the bytes make the trip.
    /// </summary>
    public abstract bool IsRemote { get; }

    /// <summary>
    /// Whether frames are decoded on a thread of their own rather than on the component's dispatcher - which in a
    /// Blazor Hybrid app is the app's UI thread, where a decode would hold up everything the app draws.
    /// </summary>
    public virtual bool DecodesOffThread => false;

    /// <summary>Whether the module is loaded, so that the other members are safe to call.</summary>
    public abstract bool IsLoaded { get; }

    /// <summary>Loads the module. Throws when it cannot be loaded.</summary>
    public abstract ValueTask LoadAsync();

    /// <summary>What the page offers: <see cref="CapabilityCamera"/> and <see cref="CapabilitySecureContext"/>.</summary>
    public abstract ValueTask<int> GetCapabilitiesAsync();

    /// <summary>The camera permission's state as the Permissions API reports it, or an empty string.</summary>
    public abstract ValueTask<string> QueryCameraPermissionAsync();

    /// <summary>The video inputs, as the JSON of a <see cref="BbCameraDevice"/> array.</summary>
    public abstract ValueTask<string> ListCamerasAsync();

    /// <summary>Opens a camera into the video element; returns the JSON of a <see cref="BbCameraInfo"/>.</summary>
    public abstract ValueTask<string> StartAsync(string sessionId, string videoElementId, string? deviceId, int requestedWidth, int requestedHeight, string? facingMode);

    /// <summary>Sets the size frames are read at.</summary>
    public abstract ValueTask ConfigureAsync(string sessionId, int width, int height);

    /// <summary>
    /// The change versions and the <c>Poll</c> flags, in one number. Given the region the frame loop is about to grab,
    /// a bridge that pays a round trip a call may grab a waiting frame's region along with the poll, for
    /// <see cref="GrabFrameAsync"/> to read without asking for it again.
    /// </summary>
    public abstract ValueTask<int> PollAsync(string sessionId, (int X, int Y, int Width, int Height)? grab = null);

    /// <summary>
    /// Copies the latest frame into <paramref name="buffer"/>, and returns how many bytes were written: zero when there
    /// is no new frame, or <see cref="FrameTrackEnded"/> or <see cref="FrameBufferTooSmall"/>. Where
    /// <see cref="IsRemote"/>, the frame is the luminance of <paramref name="region"/> only; otherwise it is the whole
    /// frame, as luminance or as RGBA.
    /// </summary>
    public abstract ValueTask<int> GrabFrameAsync(string sessionId, byte[] buffer, (int X, int Y, int Width, int Height) region);

    /// <summary>The open camera, as the JSON of a <see cref="BbCameraInfo"/>, or an empty string.</summary>
    public abstract ValueTask<string> DescribeAsync(string sessionId);

    /// <summary>The rendered size of the video element, packed as <c>(width &lt;&lt; 16) | height</c>.</summary>
    public abstract ValueTask<int> GetViewSizeAsync(string sessionId);

    /// <summary>Turns the torch on or off; returns whether the request was accepted.</summary>
    public abstract ValueTask<bool> SetTorchAsync(string sessionId, bool on);

    /// <summary>Sets the zoom; returns whether the request was accepted.</summary>
    public abstract ValueTask<bool> SetZoomAsync(string sessionId, double value);

    /// <summary>Closes the camera; the session survives for a restart.</summary>
    public abstract ValueTask StopAsync(string sessionId);

    /// <summary>Closes the camera and forgets the session.</summary>
    public abstract ValueTask DisposeSessionAsync(string sessionId);

    /// <summary>Vibrates, beeps or both; the kinds are the values of <see cref="BbScanFeedback"/>.</summary>
    public abstract ValueTask FeedbackAsync(int kinds);

    /// <summary>Makes the root element a drop target for images, and its pick buttons open the file input.</summary>
    public abstract ValueTask AttachRootAsync(string sessionId, string rootElementId, string inputElementId);

    /// <summary>Clicks an element.</summary>
    public abstract ValueTask ClickAsync(string elementId);

    /// <summary>Decodes the file chosen in the file input; returns the packed dimensions, zero or a refusal code.</summary>
    public abstract ValueTask<int> PrepareImageFromInputAsync(string sessionId, string inputElementId, int requestId, int maxDimension, double maxBytes);

    /// <summary>Decodes an encoded image the application handed over; returns the packed dimensions, zero or a refusal code.</summary>
    public abstract ValueTask<int> PrepareImageAsync(string sessionId, int requestId, ReadOnlyMemory<byte> image, string? contentType, int maxDimension, double maxBytes);

    /// <summary>Captures the camera's current frame at full size; returns the packed dimensions, or zero.</summary>
    public abstract ValueTask<int> PrepareVideoFrameAsync(string sessionId, int requestId, int maxDimension);

    /// <summary>
    /// Reads an image prepared under <paramref name="requestId"/> into managed memory and releases it, or returns
    /// <see langword="null"/> when it is gone or short.
    /// </summary>
    public abstract ValueTask<(byte[] Pixels, BbPixelFormat Format)?> ReadPreparedAsync(string sessionId, int requestId, int width, int height);

    /// <summary>Forgets an image prepared under <paramref name="requestId"/> without reading it.</summary>
    public abstract ValueTask ReleasePreparedAsync(string sessionId, int requestId);

    /// <summary>
    /// Whether an exception is the way to the page failing rather than a fault of the scanner: a script error, or, where
    /// the page is remote, a connection that dropped or a call that timed out. Releasing the camera ignores these.
    /// </summary>
    public abstract bool IsInteropFailure(Exception exception);

    /// <summary>
    /// Whether an exception is a call into the page that timed out or could not be delivered for now - a Blazor Server
    /// circuit reconnecting - which the frame loop rides out by trying again on its next tick.
    /// </summary>
    public virtual bool IsTransient(Exception exception) => false;

    /// <summary>Whether an exception says the page is gone for good: a Blazor Server circuit that will not come back.</summary>
    public virtual bool IsDisconnected(Exception exception) => false;

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync() => default;
}
