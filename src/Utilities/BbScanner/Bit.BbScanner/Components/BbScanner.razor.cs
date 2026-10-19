using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace Bit.BbScanner;

/// <summary>
/// A camera barcode scanner.
/// </summary>
/// <remarks>
/// <para>
/// The component owns the camera, the frame loop and one <see cref="BbBarcodeDecoder"/>. Frames
/// never enter the Blazor render tree: the video element paints itself, the decoder reads pixels
/// out of a buffer JavaScript writes into directly, and the component only re-renders when
/// something a user can see actually changes. A scanner running at twenty frames a second
/// typically renders a handful of times a minute.
/// </para>
/// <para>
/// Continuous camera scanning requires Blazor WebAssembly, because it depends on the browser
/// interop that lets JavaScript write into managed memory. On other render modes the component
/// reports <see cref="BbScannerErrorKind.NotSupported"/> rather than silently degrading.
/// </para>
/// </remarks>
public sealed partial class BbScanner : ComponentBase, IAsyncDisposable
{
    /// <summary>Returned by the frame grab when the camera track has ended.</summary>
    private const int FrameTrackEnded = -2;

    /// <summary>Returned by the frame grab when the buffer is too small for a frame.</summary>
    private const int FrameBufferTooSmall = -1;

    /// <summary>
    /// How long the highlight outlives the last frame its symbol was decoded in: long enough to
    /// ride out a frame or two that misses a symbol still in view, short enough that the box does
    /// not linger over whatever the camera is pointed at next.
    /// </summary>
    private const long HighlightHoldMilliseconds = 600;

    /// <summary>
    /// How far, as a fraction of the view, a symbol held in view has to move before the highlight
    /// follows it. A held symbol jitters by a pixel or two from frame to frame, and re-rendering
    /// for that would cost a render a frame for nothing a user can see.
    /// </summary>
    private const double HighlightTolerance = 0.015;

    /// <summary>How long a still image notice, such as "No barcode found", stays up.</summary>
    private static readonly TimeSpan NoticeDuration = TimeSpan.FromSeconds(4);

    /// <summary>
    /// How many frames in a row the decoder may fail on before the scanner gives up. One odd frame
    /// is not worth the camera; a decoder that throws on every frame has a fault that will not go
    /// away by itself, and two seconds of it at the default frame rate is enough to know.
    /// </summary>
    private const int MaxConsecutiveDecodeFailures = 30;

    private readonly string _sessionId = $"bs-{Guid.NewGuid():N}";
    private readonly string _videoElementId = $"bit-bbscanner-video-{Guid.NewGuid():N}";
    private readonly string _fileInputId = $"bit-bbscanner-file-{Guid.NewGuid():N}";

    // The component, not the decoder, owns duplicate suppression: the decoder is replaced whenever
    // the formats or options change, and a symbol already reported must stay reported across that.
    // Owning it also lets every decoded frame reach the highlight, suppressed or not.
    private readonly BbDuplicateFilter _duplicateFilter = new(TimeSpan.FromSeconds(2));

    private BbBarcodeDecoder? _decoder;
    private string? _decoderSignature;
    private Exception? _optionsError;
    private BbScanDiagnostics _lastDiagnostics;
    private long _retiredFramesDecoded;
    private long _retiredFramesWithSymbol;
    private int _consecutiveDecodeFailures;
    private CancellationTokenSource? _loopCancellation;
    private PeriodicTimer? _timer;
    private byte[] _frameBuffer = [];
    private List<BbCameraDevice> _cameras = [];
    private BbCameraInfo? _camera;
    private BbScanRegion _region = BbScanRegion.Full;

    private BbScannerStatus _status = BbScannerStatus.Idle;
    private string? _message;
    private string? _notice;
    private int _noticeVersion;
    private int _imageRequest;
    private string? _announcement;
    private string? _highlight;
    private (double Left, double Top, double Width, double Height) _highlightBox;
    private long _symbolSeenAt;
    private string? _activeDeviceId;
    private string? _lastRequestedDeviceId;
    private BbCameraFacing _lastRequestedFacing;
    private bool _restartRequested;
    private bool _mirrored;
    private bool _torchOn;
    private double _zoom;
    private int _processingWidth;
    private int _processingHeight;
    private bool _renderPending = true;
    private long _lastDiagnosticsAt;
    private int _startVersion;
    private int _changeToken;
    private bool _disposed;

    /// <summary>The symbologies to look for.</summary>
    [Parameter]
    public BbBarcodeFormat Formats { get; set; } = BbBarcodeFormat.All;

    /// <summary>
    /// The part of the frame to search, in normalized coordinates. The default searches the whole
    /// frame; a centred square is the usual choice for an aiming user interface. Still images
    /// scanned through the file picker are always searched in full.
    /// </summary>
    [Parameter]
    public BbScanRegion Region { get; set; } = BbScanRegion.Full;

    /// <summary>
    /// Advanced tuning. When supplied, <see cref="Formats"/>, <see cref="Region"/> and an
    /// explicitly set <see cref="DuplicateSuppressionWindow"/> are still applied on top of it,
    /// so the common cases do not need this at all.
    /// </summary>
    [Parameter]
    public BbScannerOptions? Options { get; set; }

    /// <summary>How many frames a second to decode. Higher costs battery for little benefit above about twenty.</summary>
    [Parameter]
    public int TargetFrameRate { get; set; } = 15;

    /// <summary>Camera width to ask the browser for. The browser is free to negotiate something else.</summary>
    [Parameter]
    public int RequestedWidth { get; set; } = 1280;

    /// <summary>Camera height to ask the browser for.</summary>
    [Parameter]
    public int RequestedHeight { get; set; } = 720;

    /// <summary>
    /// Longest edge, in pixels, of the image the decoder actually sees. Frames are scaled down to
    /// this by the browser before they cross into managed code, which is the single most
    /// effective performance control the component exposes.
    /// </summary>
    [Parameter]
    public int MaxProcessingSize { get; set; } = 640;

    /// <summary>Which camera to prefer when <see cref="DeviceId"/> is not set.</summary>
    [Parameter]
    public BbCameraFacing Facing { get; set; } = BbCameraFacing.Rear;

    /// <summary>
    /// A specific camera to open, from <see cref="GetCamerasAsync"/>. Changing it while the
    /// scanner is running switches cameras.
    /// </summary>
    [Parameter]
    public string? DeviceId { get; set; }

    /// <summary>Start the camera as soon as the component is ready.</summary>
    [Parameter]
    public bool AutoStart { get; set; } = true;

    /// <summary>
    /// How long the same value is suppressed after being reported. When not set, the value from
    /// <see cref="Options"/> applies, and failing that two seconds.
    /// </summary>
    [Parameter]
    public TimeSpan? DuplicateSuppressionWindow { get; set; }

    /// <summary>Show the built-in control bar.</summary>
    [Parameter]
    public bool ShowControls { get; set; } = true;

    /// <summary>Show the built-in status bar.</summary>
    [Parameter]
    public bool ShowStatusBar { get; set; } = true;

    /// <summary>Draw the corner reticle. Ignored when <see cref="Overlay"/> is supplied.</summary>
    [Parameter]
    public bool ShowReticle { get; set; } = true;

    /// <summary>Dim the area outside <see cref="Region"/>.</summary>
    [Parameter]
    public bool ShowMask { get; set; } = true;

    /// <summary>Outline a decoded symbol where it was found in the frame.</summary>
    [Parameter]
    public bool HighlightResults { get; set; } = true;

    /// <summary>Offer the built-in still image scanning control.</summary>
    [Parameter]
    public bool EnableFileScanning { get; set; } = true;

    /// <summary>
    /// Stretch to the size of the parent instead of keeping the camera's aspect ratio. The video
    /// is cropped to fill; overlays and highlights are mapped onto the visible part.
    /// </summary>
    [Parameter]
    public bool Fill { get; set; }

    /// <summary>Accessible name for the scanner region.</summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Barcode scanner";

    /// <summary>Extra CSS classes for the root element.</summary>
    [Parameter]
    public string? CssClass { get; set; }

    /// <summary>Replaces the built-in reticle entirely.</summary>
    [Parameter]
    public RenderFragment<BbScanner>? Overlay { get; set; }

    /// <summary>Replaces the rendering of status and error messages.</summary>
    [Parameter]
    public RenderFragment<string>? MessageTemplate { get; set; }

    /// <summary>Extra controls appended to the built-in control bar.</summary>
    [Parameter]
    public RenderFragment? ControlsContent { get; set; }

    /// <summary>Arbitrary content rendered on top of the scanner.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Raised for every accepted scan, after duplicate suppression.</summary>
    [Parameter]
    public EventCallback<BbBarcodeResult> OnScan { get; set; }

    /// <summary>Raised when the scanner fails to start or the camera is lost.</summary>
    [Parameter]
    public EventCallback<BbScannerException> OnError { get; set; }

    /// <summary>Raised whenever <see cref="Status"/> changes.</summary>
    [Parameter]
    public EventCallback<BbScannerStatus> OnStatusChanged { get; set; }

    /// <summary>Raised when a camera is opened, with what the browser negotiated.</summary>
    [Parameter]
    public EventCallback<BbCameraInfo> OnCameraChanged { get; set; }

    /// <summary>
    /// Raised at most twice a second with the timings of the most recent frame. Subscribing has
    /// no cost when nothing is listening.
    /// </summary>
    [Parameter]
    public EventCallback<BbScanDiagnostics> OnDiagnostics { get; set; }

    /// <summary>Any other attributes are applied to the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Current lifecycle state.</summary>
    public BbScannerStatus Status => _status;

    /// <summary>What the browser negotiated for the open camera, or <see langword="null"/>.</summary>
    public BbCameraInfo? Camera => _camera;

    /// <summary>Cameras discovered by the last call to <see cref="GetCamerasAsync"/>.</summary>
    public IReadOnlyList<BbCameraDevice> Cameras => _cameras;

    /// <summary>Timings from the most recently decoded frame.</summary>
    public BbScanDiagnostics Diagnostics => _lastDiagnostics;

    /// <summary>Number of camera frames decoded since the scanner was created.</summary>
    public long FramesDecoded => _retiredFramesDecoded + (_decoder?.FramesDecoded ?? 0);

    /// <summary>Number of camera frames that contained a symbol, before duplicate suppression.</summary>
    public long FramesWithSymbol => _retiredFramesWithSymbol + (_decoder?.FramesWithSymbol ?? 0);

    /// <summary>Whether the torch is currently on.</summary>
    public bool IsTorchOn => _torchOn;

    /// <summary>Current zoom value, or zero when zoom is not supported.</summary>
    public double Zoom => _zoom;

    /// <summary>Size of the frames handed to the decoder, or zero before the camera has opened.</summary>
    public (int Width, int Height) ProcessingSize => (_processingWidth, _processingHeight);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // A parameter changed, so whatever it controls has to be drawn again.
        RequestRender();

        _region = Region.Normalized();
        _duplicateFilter.Window = EffectiveDuplicateWindow();

        if (_decoder is not null)
        {
            if (DecoderSignature() != _decoderSignature)
            {
                // The decoder builds its symbology list and scratch state up front, so a change
                // to anything but the region and the suppression window needs a new one.
                ReplaceDecoder();
            }
            else
            {
                // The rest is live-editable, so a running scanner picks up parameter changes
                // without dropping the camera.
                _decoder.Options.Region = _region;
            }
        }

        if (_timer is not null)
        {
            _timer.Period = FrameInterval();
        }

        if (_camera is not null && _status is BbScannerStatus.Scanning or BbScannerStatus.Paused)
        {
            var expectedLongest = Math.Max(_processingWidth, _processingHeight);
            var wanted = Math.Min(Math.Max(120, MaxProcessingSize), Math.Max(_camera.Width, _camera.Height));
            if (Math.Abs(expectedLongest - wanted) > 1)
            {
                ConfigureProcessingSize(_camera.Width, _camera.Height);
            }
        }

        // A different camera was asked for: switch after this render. This is checked while the
        // camera is still opening too, because a request made during the permission prompt would
        // otherwise be dropped and never revisited.
        if (_status is BbScannerStatus.Starting or BbScannerStatus.Scanning or BbScannerStatus.Paused &&
            (DeviceId != _lastRequestedDeviceId || (DeviceId is null && Facing != _lastRequestedFacing)))
        {
            _restartRequested = true;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        if (!OperatingSystem.IsBrowser())
        {
            // Prerendering is expected and harmless: the component activates once the
            // WebAssembly runtime takes over. An interactive render mode that is not the
            // browser, though, will never activate, and saying so is better than a Start button
            // that quietly does nothing.
#if NET9_0_OR_GREATER
            if (firstRender && RendererInfo.IsInteractive)
            {
                await FailAsync(new BbScannerException(
                    BbScannerErrorKind.NotSupported,
                    "Camera scanning needs Blazor WebAssembly; this component is running on the "
                    + $"{RendererInfo.Name} renderer, which cannot reach the browser's camera API.")).ConfigureAwait(true);
            }
#else
            // .NET 8 has no RendererInfo, but it needs none: a static prerender never calls
            // OnAfterRender, so reaching this point outside the browser means an interactive
            // renderer that is not WebAssembly.
            if (firstRender)
            {
                await FailAsync(new BbScannerException(
                    BbScannerErrorKind.NotSupported,
                    "Camera scanning needs Blazor WebAssembly; this component is running on an "
                    + "interactive renderer that cannot reach the browser's camera API.")).ConfigureAwait(true);
            }
#endif

            return;
        }

        if (!firstRender)
        {
            if (_optionsError is { } optionsError)
            {
                // Options that became invalid while the camera was running: the decoder could not
                // be rebuilt, so the camera has nothing to feed and is released.
                _optionsError = null;
                _restartRequested = false;
                _startVersion++;
                StopFrameLoop();
                if (BbScannerJsInterop.IsImported)
                {
                    BbScannerJsInterop.Stop(_sessionId);
                }

                await FailAsync(InvalidOptions(optionsError)).ConfigureAwait(true);
                return;
            }

            if (_restartRequested)
            {
                _restartRequested = false;
                await StartAsync().ConfigureAwait(true);
            }

            return;
        }

        if (!await EnsureModuleAsync().ConfigureAwait(true) || _disposed)
        {
            return;
        }

        if (!BbScannerJsInterop.IsCameraSupported())
        {
            await FailAsync(new BbScannerException(
                BbScannerErrorKind.NotSupported,
                BbScannerJsInterop.IsSecureContext()
                    ? "This browser does not expose a camera API to the page."
                    : "The camera API needs a secure context; serve the application over HTTPS or from localhost.")).ConfigureAwait(true);
            return;
        }

        await RefreshCamerasAsync().ConfigureAwait(true);

        if (AutoStart && !_disposed && _status == BbScannerStatus.Idle)
        {
            await StartAsync().ConfigureAwait(true);
        }
    }

    /// <summary>Enumerates the available cameras.</summary>
    /// <remarks>
    /// Browsers withhold camera labels until permission has been granted once, so calling this
    /// before <see cref="StartAsync"/> tends to return generic names.
    /// </remarks>
    public async Task<IReadOnlyList<BbCameraDevice>> GetCamerasAsync()
    {
        if (!_disposed && await EnsureModuleAsync().ConfigureAwait(true))
        {
            await RefreshCamerasAsync().ConfigureAwait(true);
        }

        return _cameras;
    }

    /// <summary>Opens the camera and starts decoding.</summary>
    /// <remarks>
    /// Calling this while a previous start is still waiting for the permission prompt supersedes
    /// that start: only the most recent request ever ends up owning the camera.
    /// </remarks>
    /// <param name="deviceId">A specific camera, or <see langword="null"/> to use <see cref="DeviceId"/> and <see cref="Facing"/>.</param>
    public async Task StartAsync(string? deviceId = null)
    {
        if (!OperatingSystem.IsBrowser() || _disposed)
        {
            return;
        }

        if (!await EnsureModuleAsync().ConfigureAwait(true) || _disposed)
        {
            return;
        }

        var version = ++_startVersion;
        StopFrameLoop();

        _highlight = null;
        _lastRequestedDeviceId = DeviceId;
        _lastRequestedFacing = Facing;

        // The options are validated before the camera is opened, not after: a configuration
        // mistake must not leave the camera light on behind an exception.
        if (_decoder is null)
        {
            try
            {
                _decoder = CreateDecoder();
            }
            catch (ArgumentException ex)
            {
                // A restart while scanning still owns the previous camera.
                BbScannerJsInterop.Stop(_sessionId);
                await FailAsync(InvalidOptions(ex)).ConfigureAwait(true);
                return;
            }
        }

        await SetStatusAsync(BbScannerStatus.Starting, "Requesting camera access...").ConfigureAwait(true);
        if (IsStale(version))
        {
            return;
        }

        var facing = Facing switch
        {
            BbCameraFacing.Rear => "environment",
            BbCameraFacing.Front => "user",
            _ => null,
        };

        string json;
        try
        {
            json = await BbScannerJsInterop.StartAsync(
                _sessionId,
                _videoElementId,
                deviceId ?? DeviceId,
                RequestedWidth,
                RequestedHeight,
                facing).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            if (!IsStale(version))
            {
                // The browser may have attached a stream before failing, so release the session
                // rather than trusting the rejection to have cleaned up after itself.
                BbScannerJsInterop.Stop(_sessionId);
                await FailAsync(BbScannerException.FromBrowserError(ex)).ConfigureAwait(true);
            }

            return;
        }

        if (IsStale(version))
        {
            // A newer start, a stop or disposal won the race; JavaScript has already released
            // this stream.
            return;
        }

        var camera = JsonSerializer.Deserialize(json, BbScannerJsonContext.Default.BbCameraInfo);
        if (camera is null || camera.Width <= 0 || camera.Height <= 0)
        {
            BbScannerJsInterop.Stop(_sessionId);
            await FailAsync(new BbScannerException(BbScannerErrorKind.Unknown, "The camera opened but reported no video size.")).ConfigureAwait(true);
            return;
        }

        _camera = camera;
        _changeToken = BbScannerJsInterop.PollChanges(_sessionId);
        _activeDeviceId = camera.DeviceId;
        _mirrored = camera.FacingMode == "user";
        _torchOn = camera.TorchOn;
        _zoom = camera.Zoom;

        ConfigureProcessingSize(camera.Width, camera.Height);

        _duplicateFilter.Reset();
        _consecutiveDecodeFailures = 0;

        // Labels only become readable once permission has been granted, so refresh them now.
        await RefreshCamerasAsync().ConfigureAwait(true);
        if (IsStale(version))
        {
            return;
        }

        await SetStatusAsync(BbScannerStatus.Scanning, null).ConfigureAwait(true);
        if (IsStale(version))
        {
            return;
        }

        await InvokeCallbackAsync(OnCameraChanged, camera).ConfigureAwait(true);
        if (IsStale(version))
        {
            return;
        }

        _loopCancellation = new CancellationTokenSource();
        _ = RunFrameLoopAsync(_loopCancellation.Token, version);
    }

    /// <summary>Stops decoding and releases the camera.</summary>
    public async Task StopAsync()
    {
        _startVersion++;
        StopFrameLoop();

        if (OperatingSystem.IsBrowser() && BbScannerJsInterop.IsImported)
        {
            BbScannerJsInterop.Stop(_sessionId);
        }

        ClearCameraState();
        if (!_disposed)
        {
            await SetStatusAsync(BbScannerStatus.Stopped, null).ConfigureAwait(true);
        }
    }

    /// <summary>Stops decoding but keeps the camera open, so resuming is instant.</summary>
    public void Pause()
    {
        if (_status == BbScannerStatus.Scanning)
        {
            _ = SetStatusAsync(BbScannerStatus.Paused, null);
        }
    }

    /// <summary>Resumes decoding after <see cref="Pause"/>.</summary>
    public void Resume()
    {
        if (_status == BbScannerStatus.Paused)
        {
            _duplicateFilter.Reset();
            _ = SetStatusAsync(BbScannerStatus.Scanning, null);
        }
    }

    /// <summary>Switches to another camera without tearing the component down.</summary>
    /// <param name="deviceId">The camera to open.</param>
    public Task SwitchCameraAsync(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        return StartAsync(deviceId);
    }

    /// <summary>Turns the torch on or off where the camera supports it.</summary>
    /// <param name="on">Whether the torch should be lit.</param>
    /// <returns><see langword="true"/> when the browser accepted the request.</returns>
    public async Task<bool> SetTorchAsync(bool on)
    {
        if (!OperatingSystem.IsBrowser() || _disposed || _camera?.SupportsTorch != true || !BbScannerJsInterop.IsImported)
        {
            return false;
        }

        var accepted = await BbScannerJsInterop.SetTorchAsync(_sessionId, on).ConfigureAwait(true);
        if (_disposed)
        {
            return accepted;
        }

        if (accepted)
        {
            _torchOn = on;
        }

        // Render either way: a refused request has to put the control back where it was.
        RequestRender();
        StateHasChanged();
        return accepted;
    }

    /// <summary>Sets the zoom where the camera supports it.</summary>
    /// <param name="zoom">A value between <see cref="BbCameraInfo.ZoomMin"/> and <see cref="BbCameraInfo.ZoomMax"/>.</param>
    /// <returns><see langword="true"/> when the browser accepted the request.</returns>
    public async Task<bool> SetZoomAsync(double zoom)
    {
        if (!OperatingSystem.IsBrowser() || _disposed || _camera?.SupportsZoom != true || !BbScannerJsInterop.IsImported)
        {
            return false;
        }

        if (double.IsNaN(zoom))
        {
            return false;
        }

        zoom = Math.Clamp(zoom, _camera.ZoomMin, _camera.ZoomMax);
        var accepted = await BbScannerJsInterop.SetZoomAsync(_sessionId, zoom).ConfigureAwait(true);
        if (_disposed)
        {
            return accepted;
        }

        if (accepted)
        {
            _zoom = zoom;
        }

        RequestRender();
        StateHasChanged();
        return accepted;
    }

    /// <summary>Opens the file picker so the user can scan a still image.</summary>
    public async Task OpenFilePickerAsync()
    {
        if (!OperatingSystem.IsBrowser() || _disposed || !EnableFileScanning)
        {
            return;
        }

        if (await EnsureModuleAsync().ConfigureAwait(true) && !_disposed)
        {
            BbScannerJsInterop.Click(_fileInputId);
        }
    }

    /// <summary>
    /// Decodes an image already loaded into a pixel buffer, for applications that source images
    /// themselves rather than through the built-in picker. The whole image is searched with the
    /// still image settings, using the symbologies and options of this scanner.
    /// </summary>
    /// <param name="pixels">Packed pixels.</param>
    /// <param name="width">Image width.</param>
    /// <param name="height">Image height.</param>
    /// <param name="format">Pixel layout.</param>
    public BbBarcodeResult? DecodeImage(ReadOnlySpan<byte> pixels, int width, int height, BbPixelFormat format)
    {
        using var decoder = new BbBarcodeDecoder(BuildStillImageOptions());
        return decoder.Decode(pixels, width, height, format, suppressDuplicates: false);
    }

    private async Task OnFileSelectedAsync(ChangeEventArgs args)
    {
        if (!OperatingSystem.IsBrowser() || _disposed || !BbScannerJsInterop.IsImported)
        {
            return;
        }

        // A newer selection supersedes this one, whichever of the two finishes preparing first;
        // each is prepared under its own request, so neither can read the other's pixels.
        var request = ++_imageRequest;

        // The still image path speaks through a notice of its own rather than the status message,
        // so it never hides why the scanner is starting or why it faulted, and never leaves its
        // own outcome pinned over a running camera.
        SetNotice("Reading image...");

        int packed;
        try
        {
            // Large photographs are scaled down in the browser: a 12 megapixel image carries no
            // more barcode information than a 2 megapixel one and costs six times as much to scan.
            packed = await BbScannerJsInterop.PrepareImageFromInputAsync(_sessionId, _fileInputId, request, 1600)
                .ConfigureAwait(true);
        }
        catch (Exception)
        {
            // An unreadable file is a message for the user, not a fault of the running camera.
            if (!_disposed && request == _imageRequest)
            {
                SetNotice("The selected file could not be read as an image.", transient: true);
            }

            return;
        }

        if (_disposed)
        {
            return;
        }

        if (request != _imageRequest)
        {
            BbScannerJsInterop.ReleasePrepared(_sessionId, request);
            return;
        }

        if (packed <= 0)
        {
            SetNotice(null);
            return;
        }

        var width = packed >> 16;
        var height = packed & 0xFFFF;
        var required = width * height * 4;
        var buffer = new byte[required];

        var written = BbScannerJsInterop.ReadPrepared(_sessionId, request, buffer);
        if (written < required)
        {
            SetNotice("The image could not be read.", transient: true);
            return;
        }

        BbBarcodeResult? result;
        try
        {
            using var stillDecoder = new BbBarcodeDecoder(BuildStillImageOptions());
            result = stillDecoder.Decode(buffer.AsSpan(0, written), width, height, BbPixelFormat.Rgba32, suppressDuplicates: false);
        }
        catch (Exception ex)
        {
            SetNotice("The image could not be decoded.", transient: true);
            await DispatchExceptionAsync(ex).ConfigureAwait(true);
            return;
        }

        if (result is null)
        {
            SetNotice("No barcode found in that image.", transient: true);
        }
        else
        {
            SetNotice(null);
        }

        if (result is not null)
        {
            _announcement = $"{result.Format}: {result.Text}";
            RequestRender();
            StateHasChanged();
            await InvokeCallbackAsync(OnScan, result).ConfigureAwait(true);
        }
    }

    private async Task RunFrameLoopAsync(CancellationToken cancellationToken, int version)
    {
        if (!OperatingSystem.IsBrowser())
        {
            return;
        }

        using var timer = new PeriodicTimer(FrameInterval());
        _timer = timer;

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(true))
            {
                if (IsStale(version))
                {
                    return;
                }

                if (_status is not (BbScannerStatus.Scanning or BbScannerStatus.Paused))
                {
                    continue;
                }

                // A paused scanner decodes nothing but still owns the camera, so whatever the
                // browser changes underneath it - a rotation, a resize, a camera that went away -
                // is picked up now rather than discovered on resume.
                await ApplyBrowserChangesAsync().ConfigureAwait(true);
                if (IsStale(version))
                {
                    return;
                }

                ExpireHighlight(Environment.TickCount64);

                if (_status == BbScannerStatus.Paused)
                {
                    if (BbScannerJsInterop.IsTrackEnded(_sessionId))
                    {
                        await LoseCameraAsync().ConfigureAwait(true);
                        return;
                    }

                    continue;
                }

                // The browser reports zero when the video has not produced a new frame since the
                // last grab, so an idle camera costs one interop call and nothing else. This is
                // also the backpressure mechanism: a slow decode simply misses ticks and the
                // next grab returns the newest frame rather than a queued stale one.
                var written = BbScannerJsInterop.GrabFrame(_sessionId, _frameBuffer);
                if (written == 0)
                {
                    continue;
                }

                if (written == FrameTrackEnded)
                {
                    await LoseCameraAsync().ConfigureAwait(true);
                    return;
                }

                if (written == FrameBufferTooSmall)
                {
                    // The frame buffer and the canvas are sized together, so this cannot happen
                    // without an invariant having been broken. Retrying would spin at the frame
                    // rate reporting nothing, so it is treated as a fault.
                    BbScannerJsInterop.Stop(_sessionId);
                    await FailAsync(new BbScannerException(
                        BbScannerErrorKind.Unknown,
                        "The frame buffer no longer matches the camera's frame size.")).ConfigureAwait(true);
                    return;
                }

                var decoder = _decoder;
                if (decoder is null || written < _processingWidth * _processingHeight * 4)
                {
                    continue;
                }

                // Every decoded frame is returned, duplicates included, so the highlight can
                // follow a symbol held in view; suppression is applied below, before reporting.
                BbBarcodeResult? result;
                try
                {
                    result = decoder.Decode(
                        _frameBuffer.AsSpan(0, written),
                        _processingWidth,
                        _processingHeight,
                        BbPixelFormat.Rgba32,
                        suppressDuplicates: false);
                    _consecutiveDecodeFailures = 0;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // One frame the decoder trips over is skipped; the next frame is a different
                    // image. Only a decoder that fails frame after frame ends the session.
                    if (++_consecutiveDecodeFailures < MaxConsecutiveDecodeFailures)
                    {
                        continue;
                    }

                    BbScannerJsInterop.Stop(_sessionId);
                    await FailAsync(new BbScannerException(
                        BbScannerErrorKind.Unknown,
                        $"The decoder failed on {MaxConsecutiveDecodeFailures} frames in a row.",
                        ex)).ConfigureAwait(true);
                    return;
                }

                _lastDiagnostics = decoder.LastFrame;
                await ReportDiagnosticsAsync().ConfigureAwait(true);

                if (result is null || IsStale(version))
                {
                    continue;
                }

                var now = Environment.TickCount64;
                _symbolSeenAt = now;
                FollowHighlight(result);

                if (!_duplicateFilter.ShouldReport(result.Text, result.Format, now))
                {
                    continue;
                }

                _announcement = $"{result.Format}: {result.Text}";
                RequestRender();
                StateHasChanged();

                await InvokeCallbackAsync(OnScan, result).ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on stop and on disposal.
        }
        catch (Exception ex)
        {
            // Nothing else should escape, but a dead loop with a live camera would be the worst
            // outcome, so surface it and release the camera. This runs on a fire-and-forget
            // task, so it must not throw on its way out either.
            try
            {
                if (!IsStale(version))
                {
                    if (BbScannerJsInterop.IsImported)
                    {
                        BbScannerJsInterop.Stop(_sessionId);
                    }

                    await FailAsync(new BbScannerException(BbScannerErrorKind.Unknown, "The frame loop failed.", ex)).ConfigureAwait(true);
                }
            }
            catch (Exception failure) when (failure is not OperationCanceledException)
            {
                // The renderer is already gone; there is nowhere left to report this.
            }
        }
        finally
        {
            if (ReferenceEquals(_timer, timer))
            {
                _timer = null;
            }

            // Whatever ended this loop, its cancellation source is finished with.
            if (!IsStale(version))
            {
                StopFrameLoop();
            }
        }
    }

    /// <summary>
    /// Picks up the two things the browser can change underneath a running scanner: the size of
    /// the video element, and the resolution the camera track renegotiated.
    /// </summary>
    /// <remarks>
    /// A track that changes resolution, which mobile browsers do when the device is rotated,
    /// would otherwise keep being drawn into a canvas of the old shape. The frames handed to the
    /// decoder would be anamorphically squashed, finder patterns would stop being square, and
    /// the scanner would go on reporting "Scanning" while reading nothing at all.
    /// </remarks>
    private async Task ApplyBrowserChangesAsync()
    {
        if (!OperatingSystem.IsBrowser() || !BbScannerJsInterop.IsImported)
        {
            return;
        }

        var token = BbScannerJsInterop.PollChanges(_sessionId);
        if (token == _changeToken)
        {
            return;
        }

        var videoChanged = (token >> 8) != (_changeToken >> 8);
        _changeToken = token;

        if (!videoChanged)
        {
            // Only the element was resized. That moves the overlays, which are expressed
            // relative to the visible part of the frame, but nothing else.
            if (Fill)
            {
                RequestRender();
                StateHasChanged();
            }

            return;
        }

        var json = BbScannerJsInterop.Describe(_sessionId);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        BbCameraInfo? camera;
        try
        {
            camera = JsonSerializer.Deserialize(json, BbScannerJsonContext.Default.BbCameraInfo);
        }
        catch (JsonException)
        {
            return;
        }

        if (camera is null || camera.Width <= 0 || camera.Height <= 0)
        {
            return;
        }

        var resized = _camera is null || camera.Width != _camera.Width || camera.Height != _camera.Height;
        _camera = camera;
        _mirrored = camera.FacingMode == "user";

        if (resized)
        {
            ConfigureProcessingSize(camera.Width, camera.Height);
        }

        RequestRender();
        StateHasChanged();

        if (resized)
        {
            await InvokeCallbackAsync(OnCameraChanged, camera).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// The camera went away: unplugged, taken over by another application, or the permission was
    /// revoked. Releases what is left and reports it.
    /// </summary>
    private async Task LoseCameraAsync()
    {
        if (OperatingSystem.IsBrowser())
        {
            BbScannerJsInterop.Stop(_sessionId);
        }

        await FailAsync(new BbScannerException(
            BbScannerErrorKind.CameraUnavailable,
            "The camera stopped delivering frames; it may have been disconnected or claimed by another application.")).ConfigureAwait(true);
    }

    private async Task ReportDiagnosticsAsync()
    {
        if (!OnDiagnostics.HasDelegate)
        {
            return;
        }

        var now = Environment.TickCount64;
        if (now - _lastDiagnosticsAt < 500)
        {
            return;
        }

        _lastDiagnosticsAt = now;
        await InvokeCallbackAsync(OnDiagnostics, _lastDiagnostics).ConfigureAwait(true);
    }

    private TimeSpan FrameInterval() => TimeSpan.FromMilliseconds(1000.0 / Math.Clamp(TargetFrameRate, 1, 60));

    private bool IsStale(int version) => _disposed || version != _startVersion;

    private void StopFrameLoop()
    {
        var cancellation = _loopCancellation;
        _loopCancellation = null;
        if (cancellation is null)
        {
            return;
        }

        cancellation.Cancel();
        cancellation.Dispose();
    }

    private void ClearCameraState()
    {
        _highlight = null;
        _camera = null;
        _torchOn = false;
        _zoom = 0;
    }

    private void ConfigureProcessingSize(int cameraWidth, int cameraHeight)
    {
        var longest = Math.Max(cameraWidth, cameraHeight);
        var limit = Math.Max(120, MaxProcessingSize);
        var scale = longest > limit ? (double)limit / longest : 1.0;

        _processingWidth = Math.Max(2, (int)Math.Round(cameraWidth * scale) & ~1);
        _processingHeight = Math.Max(2, (int)Math.Round(cameraHeight * scale) & ~1);

        var required = _processingWidth * _processingHeight * 4;
        if (_frameBuffer.Length < required)
        {
            // The buffer lives for the whole session; frames are copied into it in place.
            _frameBuffer = new byte[required];
        }

        // Configure creates the session if it is missing, so a call after disposal would
        // resurrect one that nothing will ever release.
        if (OperatingSystem.IsBrowser() && BbScannerJsInterop.IsImported && !_disposed)
        {
            BbScannerJsInterop.Configure(_sessionId, _processingWidth, _processingHeight);
        }
    }

    /// <summary>
    /// Loads the JavaScript module, reporting a load failure through the normal error path.
    /// </summary>
    /// <remarks>
    /// Every entry point that reaches JavaScript goes through here or through
    /// <see cref="BbScannerJsInterop.IsImported"/> first. Calling a binding on a module that has not
    /// been imported aborts the WebAssembly runtime outright - it is an assertion, not an
    /// exception - which leaves the whole application dead rather than just the scanner.
    /// </remarks>
    private async Task<bool> EnsureModuleAsync()
    {
        if (!OperatingSystem.IsBrowser() || _disposed)
        {
            return false;
        }

        if (BbScannerJsInterop.IsImported)
        {
            return true;
        }

        try
        {
            await BbScannerJsInterop.EnsureImportedAsync().ConfigureAwait(true);
            return !_disposed;
        }
        catch (Exception ex)
        {
            if (!_disposed)
            {
                await FailAsync(new BbScannerException(
                    BbScannerErrorKind.NotSupported,
                    "The Bit.BbScanner JavaScript module could not be loaded. Check that static web assets are served.",
                    ex)).ConfigureAwait(true);
            }

            return false;
        }
    }

    /// <summary>Builds a decoder from the current parameters; throws when the options are invalid.</summary>
    private BbBarcodeDecoder CreateDecoder()
    {
        var decoder = new BbBarcodeDecoder(BuildOptions(Options?.Clone() ?? new BbScannerOptions()));
        _decoderSignature = DecoderSignature();
        return decoder;
    }

    /// <summary>
    /// Swaps in a decoder built from new parameters. Options that fail validation leave no decoder
    /// at all, so that nothing goes on decoding with the settings the application moved away from:
    /// a running scanner is faulted after this render, and an idle one reports it on start.
    /// </summary>
    private void ReplaceDecoder()
    {
        BbBarcodeDecoder next;
        try
        {
            next = CreateDecoder();
        }
        catch (ArgumentException ex)
        {
            RetireDecoder();
            if (_status is BbScannerStatus.Starting or BbScannerStatus.Scanning or BbScannerStatus.Paused)
            {
                _optionsError = ex;
            }

            return;
        }

        RetireDecoder();
        _decoder = next;
    }

    /// <summary>
    /// Disposes the decoder, keeping its frame counts: they describe the scanner, not one decoder,
    /// and must not drop back to zero because a format was toggled.
    /// </summary>
    private void RetireDecoder()
    {
        if (_decoder is null)
        {
            return;
        }

        _retiredFramesDecoded += _decoder.FramesDecoded;
        _retiredFramesWithSymbol += _decoder.FramesWithSymbol;
        _decoder.Dispose();
        _decoder = null;
    }

    private static BbScannerException InvalidOptions(Exception exception)
        => new(BbScannerErrorKind.Unknown, $"The scanner options are invalid: {exception.Message}", exception);

    /// <summary>Everything the decoder bakes in at construction, so a change can be detected.</summary>
    private string DecoderSignature()
    {
        var options = Options;
        var itfLengths = options?.ItfLengths is { } lengths ? string.Join(',', lengths) : "";
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{(int)Formats}|{options?.MaxScanLines}|{options?.DownsampleFactor}|{options?.TryHarder}|{options?.AllowInverted}|{options?.TryReversedRows}|{options?.TryVerticalLines}|{options?.Code39CheckDigit}|{options?.Code39ExtendedMode}|{options?.ItfLengths is null}|{itfLengths}");
    }

    private BbScannerOptions BuildOptions(BbScannerOptions baseOptions)
    {
        baseOptions.Formats = Formats;
        baseOptions.Region = _region;
        baseOptions.DuplicateSuppressionWindow = EffectiveDuplicateWindow();
        return baseOptions;
    }

    /// <summary>
    /// The suppression window in force: the parameter when the application set one, otherwise
    /// whatever <see cref="Options"/> carries, otherwise the library default. A negative value
    /// is treated as zero rather than left to throw inside the decoder.
    /// </summary>
    private TimeSpan EffectiveDuplicateWindow()
    {
        var window = DuplicateSuppressionWindow ?? Options?.DuplicateSuppressionWindow ?? TimeSpan.FromSeconds(2);
        return window < TimeSpan.Zero ? TimeSpan.Zero : window;
    }

    /// <summary>
    /// Still images keep the application's symbology options but flip the latency trade-offs:
    /// exhaustive detection, inverted symbols, every scan line, the whole image.
    /// </summary>
    private BbScannerOptions BuildStillImageOptions()
    {
        var options = Options?.Clone() ?? new BbScannerOptions();
        var still = BbScannerOptions.ForStillImages();
        options.Formats = Formats;
        options.Region = BbScanRegion.Full;
        options.MaxScanLines = Math.Max(options.MaxScanLines, still.MaxScanLines);
        options.TryHarder = true;
        options.AllowInverted = true;
        options.TryVerticalLines = true;
        options.DuplicateSuppressionWindow = TimeSpan.Zero;
        return options;
    }

    private async Task RefreshCamerasAsync()
    {
        if (!OperatingSystem.IsBrowser() || _disposed || !BbScannerJsInterop.IsImported)
        {
            return;
        }

        try
        {
            var json = await BbScannerJsInterop.ListCamerasAsync().ConfigureAwait(true);
            var devices = JsonSerializer.Deserialize(json, BbScannerJsonContext.Default.BbCameraDeviceArray);
            _cameras = devices is null ? [] : [.. devices];
            RequestRender();
        }
        catch
        {
            // Enumeration is best effort: a browser that refuses it still supports scanning with
            // the default camera.
            _cameras = [];
        }
    }

    private async Task OnCameraSelectedAsync(ChangeEventArgs args)
    {
        var deviceId = args.Value?.ToString();
        if (!string.IsNullOrEmpty(deviceId) && deviceId != _activeDeviceId)
        {
            await SwitchCameraAsync(deviceId).ConfigureAwait(true);
        }

        // The selection has to fall back to the camera that is actually open if the switch
        // failed, so this renders whatever the outcome was.
        RequestRender();
        StateHasChanged();
    }

    private async Task OnZoomChangedAsync(ChangeEventArgs args)
    {
        if (double.TryParse(args.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var zoom))
        {
            await SetZoomAsync(zoom).ConfigureAwait(true);
        }
    }

    private Task ToggleTorchAsync() => SetTorchAsync(!_torchOn);

    private Task TogglePauseAsync()
    {
        if (_status == BbScannerStatus.Paused)
        {
            Resume();
        }
        else
        {
            Pause();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Outlines a decoded symbol, and keeps outlining it while it stays in view, moving with it.
    /// Called for every frame that decodes, whether or not the result is reported.
    /// </summary>
    private void FollowHighlight(BbBarcodeResult result)
    {
        if (!HighlightResults || _processingWidth == 0 || _processingHeight == 0)
        {
            return;
        }

        var box = result.BoundingBox;
        var left = box.X / _processingWidth;
        var top = box.Y / _processingHeight;
        var width = Math.Max(box.Width, 4) / _processingWidth;
        var height = Math.Max(box.Height, 4) / _processingHeight;

        var view = FrameToView(left, top, width, height);
        if (_highlight is not null &&
            Math.Abs(view.Left - _highlightBox.Left) < HighlightTolerance &&
            Math.Abs(view.Top - _highlightBox.Top) < HighlightTolerance &&
            Math.Abs(view.Width - _highlightBox.Width) < HighlightTolerance &&
            Math.Abs(view.Height - _highlightBox.Height) < HighlightTolerance)
        {
            return;
        }

        _highlightBox = view;
        _highlight = string.Create(
            CultureInfo.InvariantCulture,
            $"--bit-bbscanner-hit-left:{view.Left * 100:0.##}%;--bit-bbscanner-hit-top:{view.Top * 100:0.##}%;--bit-bbscanner-hit-width:{view.Width * 100:0.##}%;--bit-bbscanner-hit-height:{view.Height * 100:0.##}%");
        RequestRender();
        StateHasChanged();
    }

    /// <summary>Takes the highlight down once its symbol has not been decoded for a while.</summary>
    private void ExpireHighlight(long now)
    {
        if (_highlight is null || now - _symbolSeenAt <= HighlightHoldMilliseconds)
        {
            return;
        }

        _highlight = null;
        RequestRender();
        StateHasChanged();
    }

    /// <summary>
    /// Maps a normalized rectangle in frame coordinates onto the visible part of the video
    /// element: the box keeps the camera's aspect ratio unless <see cref="Fill"/> crops it, and a
    /// front camera is shown mirrored.
    /// </summary>
    private (double Left, double Top, double Width, double Height) FrameToView(double left, double top, double width, double height)
    {
        if (_mirrored)
        {
            left = 1 - left - width;
        }

        if (!Fill || _camera is null || _camera.Width <= 0 || _camera.Height <= 0 ||
            !OperatingSystem.IsBrowser() || !BbScannerJsInterop.IsImported)
        {
            return (left, top, width, height);
        }

        var packed = BbScannerJsInterop.GetViewSize(_sessionId);
        var viewWidth = packed >> 16;
        var viewHeight = packed & 0xFFFF;
        if (viewWidth <= 0 || viewHeight <= 0)
        {
            return (left, top, width, height);
        }

        // object-fit: cover keeps the frame's aspect ratio and crops whichever axis overflows.
        var frameAspect = (double)_camera.Width / _camera.Height;
        var viewAspect = (double)viewWidth / viewHeight;
        if (viewAspect > frameAspect)
        {
            // The frame is scaled to the view's width; rows at the top and bottom are cropped.
            var visible = frameAspect / viewAspect;
            var crop = (1 - visible) / 2;
            return (left, (top - crop) / visible, width, height / visible);
        }
        else
        {
            var visible = viewAspect / frameAspect;
            var crop = (1 - visible) / 2;
            return ((left - crop) / visible, top, width / visible, height);
        }
    }

    /// <summary>
    /// The scan region as the camera's frames see it: a square region is only square once it is
    /// known what shape of frame it is square in. Before a camera has opened, the region is drawn
    /// as given.
    /// </summary>
    private BbScanRegion FrameRegion()
        => _camera is { Width: > 0, Height: > 0 } ? _region.ForFrame(_camera.Width, _camera.Height) : _region;

    private string MaskClipPath()
    {
        var region = FrameRegion();
        var (left, top, width, height) = FrameToView(region.X, region.Y, region.Width, region.Height);
        var l = Math.Clamp(left, 0, 1) * 100;
        var t = Math.Clamp(top, 0, 1) * 100;
        var r = Math.Clamp(left + width, 0, 1) * 100;
        var b = Math.Clamp(top + height, 0, 1) * 100;

        // An even-odd polygon: the outer ring is the frame, the inner ring is the hole.
        return string.Create(
            CultureInfo.InvariantCulture,
            $"polygon(evenodd, 0% 0%, 100% 0%, 100% 100%, 0% 100%, 0% 0%, {l:0.##}% {t:0.##}%, {r:0.##}% {t:0.##}%, {r:0.##}% {b:0.##}%, {l:0.##}% {b:0.##}%, {l:0.##}% {t:0.##}%)");
    }

    private string ReticleStyle()
    {
        // Physical rather than logical properties: the box maps camera pixels, and the camera
        // frame does not flip in a right-to-left document, so the mask, the reticle and the
        // highlight must all agree on where "left" is.
        var region = FrameRegion();
        var (left, top, width, height) = FrameToView(region.X, region.Y, region.Width, region.Height);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"left:{left * 100:0.##}%;top:{top * 100:0.##}%;width:{width * 100:0.##}%;height:{height * 100:0.##}%");
    }

    /// <summary>
    /// The root element's classes: the component's own, the application's <see cref="CssClass"/>,
    /// and any class supplied through an unmatched attribute, which would otherwise replace them.
    /// </summary>
    private string RootClass()
    {
        var own = string.IsNullOrWhiteSpace(CssClass) ? "bit-bbscanner" : $"bit-bbscanner {CssClass}";
        return AdditionalAttributes?.TryGetValue("class", out var extra) == true &&
               extra is string text && !string.IsNullOrWhiteSpace(text)
            ? $"{own} {text}"
            : own;
    }

    private string? RootStyle()
    {
        string? aspect = null;
        if (!Fill && _camera is { Width: > 0, Height: > 0 })
        {
            // Keep the box at the camera's aspect ratio so that nothing is cropped and overlays
            // line up with the frame exactly.
            aspect = string.Create(CultureInfo.InvariantCulture, $"--bit-bbscanner-aspect:{_camera.Width}/{_camera.Height}");
        }

        var extra = AdditionalAttributes?.TryGetValue("style", out var value) == true ? value?.ToString() : null;
        if (string.IsNullOrWhiteSpace(extra))
        {
            return aspect;
        }

        return aspect is null ? extra : $"{extra.TrimEnd().TrimEnd(';')};{aspect}";
    }

    /// <summary>
    /// Formats a number for an HTML attribute. Attribute values are rendered with the current
    /// culture, and a browser reads <c>step="0,1"</c> as invalid and silently falls back to a
    /// step of one, so numeric attributes always go through the invariant culture.
    /// </summary>
    private static string Invariant(double value) => value.ToString(CultureInfo.InvariantCulture);

    private string StatusText() => _status switch
    {
        BbScannerStatus.Idle => "Ready",
        BbScannerStatus.Starting => "Starting camera",
        BbScannerStatus.Scanning => _camera is null ? "Scanning" : $"Scanning · {_camera.Width}×{_camera.Height}",
        BbScannerStatus.Paused => "Paused",
        BbScannerStatus.Stopped => "Stopped",
        _ => "Error",
    };

    private Task SetStatusAsync(BbScannerStatus status, string? message)
    {
        if (_status == status && _message == message)
        {
            return Task.CompletedTask;
        }

        var changed = _status != status;
        _status = status;
        _message = message;
        if (changed)
        {
            // A new state speaks for itself; a still image notice from the old one is stale.
            ClearNotice();
        }

        RequestRender();
        StateHasChanged();
        return changed ? InvokeCallbackAsync(OnStatusChanged, status) : Task.CompletedTask;
    }

    /// <summary>The message on screen: a still image notice while there is one, otherwise the status message.</summary>
    private string? DisplayedMessage => _notice ?? _message;

    /// <summary>
    /// Shows a still image notice over the status message. A transient one, an outcome rather
    /// than progress, takes itself down after <see cref="NoticeDuration"/>.
    /// </summary>
    private void SetNotice(string? notice, bool transient = false)
    {
        var version = ++_noticeVersion;
        if (_notice != notice)
        {
            _notice = notice;
            RequestRender();
            StateHasChanged();
        }

        if (transient && notice is not null)
        {
            _ = ExpireNoticeAsync(version);
        }
    }

    private void ClearNotice()
    {
        _noticeVersion++;
        _notice = null;
    }

    private async Task ExpireNoticeAsync(int version)
    {
        await Task.Delay(NoticeDuration).ConfigureAwait(true);
        if (_disposed || version != _noticeVersion)
        {
            return;
        }

        _notice = null;
        RequestRender();
        StateHasChanged();
    }

    private async Task FailAsync(BbScannerException exception)
    {
        // A faulted scanner owns no camera, so nothing may keep describing one: leaving
        // _camera set would go on offering a torch button and a zoom slider for a dead track.
        ClearCameraState();
        ClearNotice();

        _status = BbScannerStatus.Faulted;
        _message = exception.Message;
        RequestRender();
        StateHasChanged();
        await InvokeCallbackAsync(OnStatusChanged, BbScannerStatus.Faulted).ConfigureAwait(true);
        await InvokeCallbackAsync(OnError, exception).ConfigureAwait(true);
    }

    /// <summary>
    /// Invokes an application callback and routes anything it throws to the renderer, the way
    /// Blazor treats exceptions from its own event handlers, instead of letting it vanish inside
    /// a fire-and-forget task or tear down the frame loop.
    /// </summary>
    private async Task InvokeCallbackAsync<T>(EventCallback<T> callback, T argument)
    {
        if (!callback.HasDelegate)
        {
            return;
        }

        try
        {
            await callback.InvokeAsync(argument).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex).ConfigureAwait(true);
        }
    }

    private void RequestRender() => _renderPending = true;

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        // Frames arrive many times a second; only state a user can see is allowed to re-render.
        if (!_renderPending)
        {
            return false;
        }

        _renderPending = false;
        return true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _startVersion++;
        StopFrameLoop();

        if (OperatingSystem.IsBrowser() && BbScannerJsInterop.IsImported)
        {
            try
            {
                BbScannerJsInterop.DisposeSession(_sessionId);
            }
            catch (Exception ex) when (ex is JSException or InvalidOperationException)
            {
                // The page may already be tearing down; there is nothing left to release.
            }
        }

        RetireDecoder();
        _frameBuffer = [];
        _camera = null;
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
