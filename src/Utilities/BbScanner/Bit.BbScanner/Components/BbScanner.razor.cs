using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

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
/// It runs under every interactive render mode. On Blazor WebAssembly the frame loop and the decoder
/// run in the browser, and JavaScript writes each frame straight into managed memory. Under Blazor
/// Server and Blazor Hybrid the camera is still the page's, but .NET runs elsewhere: the page cuts each
/// frame down to the luminance of the searched region and streams it across, and the decoder runs
/// where .NET does - on the server, or natively in the app. Prerendered, it renders its markup and
/// stays inert until it becomes interactive.
/// </para>
/// </remarks>
public sealed partial class BbScanner : ComponentBase, IAsyncDisposable
{
    /// <summary>
    /// The longest edge a still image is decoded at. A twelve megapixel photograph carries no more
    /// barcode detail than a two megapixel one and costs six times as much to scan.
    /// </summary>
    private const int StillImageMaxDimension = 1600;

    /// <summary>The longest edge a captured camera frame is decoded at: a 1080p camera's own resolution.</summary>
    private const int CaptureMaxDimension = 1920;

    /// <summary>
    /// Set on the request numbers of programmatic scans, which share the session's prepared images
    /// with the built-in picker's, so that the two can never collide.
    /// </summary>
    private const int ScanRequestBit = 1 << 30;

    /// <summary>Returned by image preparation for a file over <see cref="MaxImageFileSize"/>.</summary>
    private const int ImageTooLarge = -3;

    /// <summary>Returned by image preparation for a file that declares a type other than an image.</summary>
    private const int ImageNotAnImage = -4;

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
    private readonly string _rootElementId = $"bit-bbscanner-{Guid.NewGuid():N}";
    private readonly string _videoElementId = $"bit-bbscanner-video-{Guid.NewGuid():N}";
    private readonly string _fileInputId = $"bit-bbscanner-file-{Guid.NewGuid():N}";

    // The component, not the decoder, owns duplicate suppression: the decoder is replaced whenever
    // the formats or options change, and a symbol already reported must stay reported across that.
    // Owning it also lets every decoded frame reach the highlight, suppressed or not.
    private readonly BbDuplicateFilter _duplicateFilter = new(TimeSpan.FromSeconds(2));

    /// <summary>The way into the page's JavaScript, made for where the component runs on its first use.</summary>
    private BbScannerBridge? _bridge;

    /// <summary>
    /// The decoder of the frame the loop has in hand, from its grab until it is decoded - its pixels arriving on other
    /// threads, or decoding on one of its own - or <see langword="null"/>. Meanwhile that frame has the frame buffer and
    /// the decoder to itself: no other frame is grabbed, and what a stop, a new size or a new decoder would do to either
    /// is left to <see cref="EndFrame"/>.
    /// </summary>
    private BbBarcodeDecoder? _frameDecoder;
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
    private BbCameraDevice[] _devices = [];
    private List<BbCameraDevice> _cameras = [];
    private BbScannerTexts? _camerasLabelledWith;
    private BbCameraInfo? _camera;
    private BbScanRegion _region = BbScanRegion.Full;

    private BbScannerStatus _status = BbScannerStatus.Idle;
    private string? _message;
    private string? _notice;
    private int _noticeVersion;
    private int _imageRequest;
    private int _scanRequest;
    private string? _announcement;
    private string? _alert;
    private bool _announcementMark;
    private readonly List<Highlight> _highlights = [];
    private long _symbolSeenAt;
    private string? _activeDeviceId;
    private string? _lastRequestedDeviceId;
    private BbCameraFacing _lastRequestedFacing;
    private bool _restartRequested;

    /// <summary>Set while a camera the browser ended in the background waits for the page to be shown again.</summary>
    private bool _awaitingVisible;
    private bool _mirrored;
    private bool _torchOn;
    private double _zoom;
    private int _processingWidth;
    private int _processingHeight;

    /// <summary>Changes whenever the processing size does, so a frame grabbed at the old size is recognised and dropped.</summary>
    private int _processingVersion;

    /// <summary>Set when the processing size changed and the page has yet to be told: the next tick tells it, before it grabs.</summary>
    private bool _configurePending;

    /// <summary>The rendered size of the video element, packed as <c>(width &lt;&lt; 16) | height</c>, refreshed whenever it changes.</summary>
    private int _viewSize;

    /// <summary>Whether the page wired the root up, so that the Scan image button opens the file picker by itself.</summary>
    private bool _rootAttached;
    private bool _renderPending = true;
    private long _lastDiagnosticsAt;
    private int _startVersion;
    private int _changeToken;

    /// <summary>
    /// Set when a camera opened: the change token is read after the camera was described, so a resize in between would
    /// count as seen. The first tick describes the camera again, whatever the token says.
    /// </summary>
    private bool _describePending;
    private bool _disposed;

    /// <summary>
    /// The symbologies to look for: <see cref="BbBarcodeFormat.Common"/> unless set. The others,
    /// PDF417 and Aztec among them, are asked for by name, since each costs every frame time of its own.
    /// </summary>
    [Parameter]
    public BbBarcodeFormat Formats { get; set; } = BbBarcodeFormat.Common;

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
    /// <remarks>
    /// <see cref="Timeout.InfiniteTimeSpan"/> reports each value once for as long as the camera
    /// stays open - a stocktake, a check-in desk - remembering up to
    /// <see cref="BbDuplicateFilter.SessionCapacity"/> values. <see cref="StartAsync"/> on a
    /// scanner with no camera open, after <see cref="StopAsync"/> or a fault, and
    /// <see cref="ResetDuplicateFilter"/> forget them; switching cameras, a camera reopened once the
    /// page is shown again and, unlike a finite window, <see cref="Resume"/> do not.
    /// </remarks>
    [Parameter]
    public TimeSpan? DuplicateSuppressionWindow { get; set; }

    /// <summary>
    /// The most symbols to read from one frame. The default, one, reads the first symbol found
    /// and is the cheapest; more reads a sheet of labels or a shelf at once, each symbol reported
    /// through <see cref="OnScan"/> and outlined on its own. Applies to still images too.
    /// </summary>
    [Parameter]
    public int MaxSymbolsPerFrame { get; set; } = 1;

    /// <summary>What the scanner does once it has reported a scan: keep going, pause, or stop.</summary>
    [Parameter]
    public BbAfterScan AfterScan { get; set; } = BbAfterScan.Continue;

    /// <summary>
    /// Decides which results are reported. A result it turns down is not reported, not
    /// outlined, not announced and does not count as a scan for <see cref="AfterScan"/> - the
    /// scanner simply keeps looking - so an application expecting, say, a ticket number can
    /// ignore every other code in view.
    /// </summary>
    /// <remarks>
    /// Called on every frame that reads a symbol, before duplicate suppression, so it should be
    /// cheap. An exception it throws rejects the result and is routed to the renderer.
    /// </remarks>
    [Parameter]
    public Func<BbBarcodeResult, bool>? AcceptResult { get; set; }

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
    /// The largest image file, in bytes, the built-in file picker accepts; larger files are refused
    /// before they are decoded. Zero accepts any size. The default, 32 MB, holds any photograph a
    /// phone takes.
    /// </summary>
    /// <remarks>
    /// The size of a file says little about the size of the image it decodes to, so an image whose
    /// header declares more than about 268 megapixels is refused before it is decoded as well,
    /// whatever this is set to.
    /// </remarks>
    [Parameter]
    public long MaxImageFileSize { get; set; } = 32 * 1024 * 1024;

    /// <summary>
    /// Stretch to the size of the parent instead of keeping the camera's aspect ratio. The video
    /// is cropped to fill; overlays and highlights are mapped onto the visible part.
    /// </summary>
    [Parameter]
    public bool Fill { get; set; }

    /// <summary>Accessible name for the scanner region.</summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Barcode scanner";

    /// <summary>
    /// The texts the scanner shows and announces - its controls, status, notices, error messages
    /// and screen reader announcements - for a language other than English. Anything the set
    /// leaves out stays English.
    /// </summary>
    [Parameter]
    public BbScannerTexts? Texts { get; set; }

    /// <summary>
    /// A vibration, a beep, or both, each time a scan is reported. Off by default; see
    /// <see cref="BbScanFeedback"/> for what browsers allow.
    /// </summary>
    [Parameter]
    public BbScanFeedback Feedback { get; set; }

    /// <summary>
    /// Close the camera while the page is hidden - another tab, a minimised window, a locked
    /// phone - and open it again when the page is shown. It saves the battery and puts the
    /// camera indicator out, at the cost of a moment's restart, and of the permission prompt
    /// again on a browser that did not remember the user's answer.
    /// </summary>
    /// <remarks>
    /// Whatever this is set to, a camera the browser itself ends because the page went into the
    /// background, which phones do, is reopened when the page comes back rather than reported as
    /// lost.
    /// </remarks>
    [Parameter]
    public bool ReleaseCameraWhenHidden { get; set; }

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

    /// <summary>How the page is reached where .NET does not run in it: Blazor Server and Blazor Hybrid.</summary>
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>Where an <see cref="IBbCameraSource"/> is found, when the app registered one.</summary>
    [Inject]
    private IServiceProvider Services { get; set; } = default!;

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

        // The cameras the browser left unnamed are named in the language the scanner speaks now.
        if (!ReferenceEquals(Texts, _camerasLabelledWith))
        {
            LabelCameras();
        }

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
        // A camera waiting for the page to be shown again picks the new choice up when it reopens.
        if (_status is BbScannerStatus.Starting or BbScannerStatus.Scanning or BbScannerStatus.Paused && !_awaitingVisible &&
            (DeviceId != _lastRequestedDeviceId || (DeviceId is null && Facing != _lastRequestedFacing)))
        {
            _restartRequested = true;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Only an interactive renderer calls this - a prerender never does - so from here on the page can be reached,
        // whichever of the render modes this is.
        if (_disposed)
        {
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
                await ReleaseCameraAsync().ConfigureAwait(true);
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

        if (!await EnsureModuleAsync().ConfigureAwait(true) || _disposed || _bridge is not { } bridge)
        {
            return;
        }

        int capabilities;
        while (true)
        {
            try
            {
                // Before the camera check: a page with no camera, or one served without HTTPS, can still
                // scan an image dropped onto it.
                await bridge.AttachRootAsync(_sessionId, RootId(), _fileInputId).ConfigureAwait(true);
                _rootAttached = true;
                capabilities = await bridge.GetCapabilitiesAsync().ConfigureAwait(true);
                break;
            }
            catch (Exception ex) when (bridge.IsTransient(ex) && !_disposed)
            {
                // Not answered for now - a circuit reconnecting, or a slow one - is not gone: nothing else would ever
                // attach the scanner or start it, so it is asked again. A call that timed out waited long enough already.
            }
            catch (Exception ex) when (bridge.IsInteropFailure(ex))
            {
                // The page went away while the scanner was being attached: there is nobody left to tell.
                return;
            }
        }

        if (_disposed)
        {
            return;
        }

        if ((capabilities & BbScannerBridge.CapabilityCamera) == 0)
        {
            await FailAsync(new BbScannerException(
                BbScannerErrorKind.NotSupported,
                (capabilities & BbScannerBridge.CapabilitySecureContext) != 0
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

    /// <summary>
    /// Asks the browser what it would do with a request for the camera, without making one: so a page can explain
    /// why it needs the camera before the prompt appears, or say how to allow it once it has been refused.
    /// </summary>
    /// <returns>
    /// The permission, or <see cref="BbCameraPermission.Unknown"/> where the browser will not say - older Firefox
    /// releases do not know the camera permission by name, and a WebView may not implement the Permissions API - or
    /// while the component is not interactive yet. Browsers that grant the camera for one visit at a time may report
    /// <see cref="BbCameraPermission.Granted"/> for such a grant too.
    /// </returns>
    public async Task<BbCameraPermission> GetCameraPermissionAsync()
    {
        if (_disposed || !await EnsureModuleAsync().ConfigureAwait(true) || _bridge is not { } bridge)
        {
            return BbCameraPermission.Unknown;
        }

        string state;
        try
        {
            state = await bridge.QueryCameraPermissionAsync().ConfigureAwait(true);
        }
        catch (Exception ex) when (bridge.IsInteropFailure(ex))
        {
            return BbCameraPermission.Unknown;
        }

        return state switch
        {
            "granted" => BbCameraPermission.Granted,
            "denied" => BbCameraPermission.Denied,
            "prompt" => BbCameraPermission.Prompt,
            _ => BbCameraPermission.Unknown,
        };
    }

    /// <summary>Opens the camera and starts decoding.</summary>
    /// <remarks>
    /// Calling this while a previous start is still waiting for the permission prompt supersedes
    /// that start: only the most recent request ever ends up owning the camera.
    /// </remarks>
    /// <param name="deviceId">A specific camera, or <see langword="null"/> to use <see cref="DeviceId"/> and <see cref="Facing"/>.</param>
    public async Task StartAsync(string? deviceId = null)
    {
        if (_disposed || !await EnsureModuleAsync().ConfigureAwait(true) || _disposed || _bridge is not { } bridge)
        {
            return;
        }

        // A start that opens a camera for a scanner that has none - idle, stopped or faulted - begins a scanning session
        // and forgets what the last one reported. One that only changes the camera of a running scanner, or reopens it
        // once the page is shown again, carries the session on: under an infinite DuplicateSuppressionWindow what it
        // reported stays reported, which is the whole point of one - a stocktake must not count everything again after
        // switching lenses or a screen lock.
        if (_status is not (BbScannerStatus.Starting or BbScannerStatus.Scanning or BbScannerStatus.Paused))
        {
            _duplicateFilter.Reset();
        }

        var version = ++_startVersion;
        StopFrameLoop();

        _highlights.Clear();
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
                await ReleaseCameraAsync().ConfigureAwait(true);
                await FailAsync(InvalidOptions(ex)).ConfigureAwait(true);
                return;
            }
        }

        await SetStatusAsync(BbScannerStatus.Starting, T.RequestingCamera).ConfigureAwait(true);
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
            json = await bridge.StartAsync(
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
                // rather than trusting the rejection to have cleaned up after itself. A native camera's failure
                // arrives classified already.
                await ReleaseCameraAsync().ConfigureAwait(true);
                await FailAsync(ex as BbScannerException ?? BbScannerException.FromBrowserError(ex)).ConfigureAwait(true);
            }

            return;
        }

        if (IsStale(version))
        {
            // A newer start, a stop or disposal won the race; JavaScript has already released
            // this stream.
            return;
        }

        var camera = ParseCamera(json);
        if (camera is null || camera.Width <= 0 || camera.Height <= 0)
        {
            await ReleaseCameraAsync().ConfigureAwait(true);
            await FailAsync(new BbScannerException(BbScannerErrorKind.Unknown, "The camera opened but reported no video size.")).ConfigureAwait(true);
            return;
        }

        _camera = camera;
        _activeDeviceId = camera.DeviceId;
        _mirrored = camera.FacingMode == "user";
        _torchOn = camera.TorchOn;
        _zoom = camera.Zoom;

        ConfigureProcessingSize(camera.Width, camera.Height);
        _describePending = true;
        try
        {
            await ApplyProcessingSizeAsync().ConfigureAwait(true);
            _changeToken = await bridge.PollAsync(_sessionId).ConfigureAwait(true) & BbScannerBridge.PollChanges;
            _viewSize = await bridge.GetViewSizeAsync(_sessionId).ConfigureAwait(true);
        }
        catch (Exception ex) when (bridge.IsTransient(ex))
        {
            // A circuit reconnecting: the frame loop tells the page the size it still needs to, and asks what changed,
            // once the page answers again.
        }
        catch (Exception ex) when (bridge.IsInteropFailure(ex))
        {
            if (!IsStale(version))
            {
                await ReleaseCameraAsync().ConfigureAwait(true);
                await FailAsync(new BbScannerException(BbScannerErrorKind.Unknown, "The page stopped answering while the camera opened.", ex)).ConfigureAwait(true);
            }

            return;
        }

        if (IsStale(version))
        {
            return;
        }

        // Under a finite window the symbol in view of the camera just opened is reported again, as it would be after
        // Resume.
        if (_duplicateFilter.Window != Timeout.InfiniteTimeSpan)
        {
            _duplicateFilter.Reset();
        }

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
        await ReleaseCameraAsync().ConfigureAwait(true);

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
    /// <remarks>
    /// The symbol that was in view when scanning paused is reported again, unless
    /// <see cref="DuplicateSuppressionWindow"/> is <see cref="Timeout.InfiniteTimeSpan"/>, whose
    /// whole point is that a value is reported once for as long as the camera stays open.
    /// </remarks>
    public void Resume()
    {
        if (_status == BbScannerStatus.Paused)
        {
            if (_duplicateFilter.Window != Timeout.InfiniteTimeSpan)
            {
                _duplicateFilter.Reset();
            }

            _ = SetStatusAsync(BbScannerStatus.Scanning, null);
        }
    }

    /// <summary>
    /// Forgets the values already reported, so that each is reported again the next time it is
    /// read - after a stocktake is saved, say, under a <see cref="Timeout.InfiniteTimeSpan"/>
    /// <see cref="DuplicateSuppressionWindow"/>.
    /// </summary>
    public void ResetDuplicateFilter() => _duplicateFilter.Reset();

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
        if (_disposed || _camera?.SupportsTorch != true || _bridge is not { IsLoaded: true } bridge)
        {
            return false;
        }

        var camera = _camera;
        bool accepted;
        try
        {
            accepted = await bridge.SetTorchAsync(_sessionId, on).ConfigureAwait(true);
        }
        catch (Exception ex) when (bridge.IsInteropFailure(ex))
        {
            accepted = false;
        }

        if (_disposed || !ReferenceEquals(camera, _camera))
        {
            // The camera it was asked of closed, or another opened, while the browser answered: nothing about the
            // camera open now has changed.
            return false;
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
        if (_disposed || _camera?.SupportsZoom != true || _bridge is not { IsLoaded: true } bridge)
        {
            return false;
        }

        if (double.IsNaN(zoom))
        {
            return false;
        }

        zoom = Math.Clamp(zoom, _camera.ZoomMin, _camera.ZoomMax);
        var camera = _camera;
        bool accepted;
        try
        {
            accepted = await bridge.SetZoomAsync(_sessionId, zoom).ConfigureAwait(true);
        }
        catch (Exception ex) when (bridge.IsInteropFailure(ex))
        {
            accepted = false;
        }

        if (_disposed || !ReferenceEquals(camera, _camera))
        {
            return false;
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
    /// <remarks>
    /// A browser opens a file picker for a script only while a click of the user's is fresh. Called from a click
    /// handler, that is always so on WebAssembly and in a WebView; under Blazor Server the click reaches the page again
    /// a network round trip later, which a strict browser may no longer count as the user's. The built-in Scan image
    /// button opens the picker in the page itself, so it is not affected.
    /// </remarks>
    public async Task OpenFilePickerAsync()
    {
        if (_disposed || !EnableFileScanning)
        {
            return;
        }

        if (await EnsureModuleAsync().ConfigureAwait(true) && !_disposed && _bridge is { } bridge)
        {
            try
            {
                await bridge.ClickAsync(_fileInputId).ConfigureAwait(true);
            }
            catch (Exception ex) when (bridge.IsInteropFailure(ex))
            {
                // The page is gone, and the picker with it.
            }
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

    /// <summary>
    /// Decodes every symbol in an image already loaded into a pixel buffer, up to
    /// <paramref name="maxSymbols"/>, with the still image settings and the symbologies and
    /// options of this scanner. Raises no events.
    /// </summary>
    /// <param name="pixels">Packed pixels.</param>
    /// <param name="width">Image width.</param>
    /// <param name="height">Image height.</param>
    /// <param name="format">Pixel layout.</param>
    /// <param name="maxSymbols">The most symbols to return, at least one.</param>
    /// <returns>The symbols found; empty when there are none.</returns>
    public IReadOnlyList<BbBarcodeResult> DecodeAllInImage(ReadOnlySpan<byte> pixels, int width, int height, BbPixelFormat format, int maxSymbols = 16)
    {
        using var decoder = new BbBarcodeDecoder(BuildStillImageOptions());
        return decoder.DecodeAll(pixels, width, height, format, maxSymbols, suppressDuplicates: false);
    }

    /// <summary>
    /// Scans an encoded image - the bytes of a PNG, JPEG, WebP or any format the browser decodes -
    /// such as a file from Blazor's <c>InputFile</c>. It is decoded by the page exactly as a
    /// file chosen from the built-in picker is, with the same size limits and EXIF orientation, and
    /// needs no camera, so no permission is asked for. Raises no events; the results are returned.
    /// </summary>
    /// <remarks>
    /// Under Blazor Server the image is decoded in the browser too, so a stream the server holds is sent to the
    /// browser and its luminance comes back: an image the user uploads crosses the network twice. The built-in picker,
    /// which decodes the file where the user chose it, sends it once.
    /// </remarks>
    /// <param name="image">
    /// The encoded image. Read to its end, up to <see cref="MaxImageFileSize"/>; not read at all while the component
    /// is not interactive.
    /// </param>
    /// <param name="maxSymbols">The most symbols to return, at least one.</param>
    /// <param name="cancellationToken">Abandons the scan; an image already being decoded is discarded when it is ready.</param>
    /// <returns>The symbols found, matrix symbols first; empty when the image holds none.</returns>
    /// <exception cref="BbScannerException">
    /// <see cref="BbScannerErrorKind.InvalidImage"/> when the image is too large or cannot be decoded,
    /// <see cref="BbScannerErrorKind.NotSupported"/> while the component is not interactive - prerendering - or its
    /// script could not be loaded.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The scanner has been disposed.</exception>
    public async Task<IReadOnlyList<BbBarcodeResult>> ScanImageAsync(Stream image, int maxSymbols = 16, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSymbols);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Refused before a byte is read: a prerendered scanner can reach no decoder, and an upload over a circuit
        // would cross the network in full only to be refused.
        if (!await EnsureModuleAsync().ConfigureAwait(true))
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            throw ImageScanningNotSupported();
        }

        // Read with the limit applied as the bytes arrive, so an oversized upload is refused
        // without first being held in memory in full.
        var limit = MaxImageFileSize > 0 ? MaxImageFileSize : long.MaxValue;
        using var copy = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await image.ReadAsync(chunk, cancellationToken).ConfigureAwait(true)) > 0)
        {
            if (copy.Length + read > limit)
            {
                throw new BbScannerException(BbScannerErrorKind.InvalidImage, TooLargeMessage());
            }

            copy.Write(chunk, 0, read);
        }

        return await ScanImageAsync(copy.GetBuffer().AsMemory(0, (int)copy.Length), null, maxSymbols, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>
    /// Scans an encoded image held in memory; see <see cref="ScanImageAsync(Stream, int, CancellationToken)"/>.
    /// </summary>
    /// <param name="image">The encoded image.</param>
    /// <param name="contentType">Its media type, such as <c>image/png</c>, when known; a type that is not an image is refused.</param>
    /// <param name="maxSymbols">The most symbols to return, at least one.</param>
    /// <param name="cancellationToken">Abandons the scan.</param>
    /// <returns>The symbols found, matrix symbols first; empty when the image holds none.</returns>
    /// <exception cref="BbScannerException">
    /// <see cref="BbScannerErrorKind.InvalidImage"/> when the image is refused or cannot be decoded,
    /// <see cref="BbScannerErrorKind.NotSupported"/> while the component is not interactive - prerendering - or its
    /// script could not be loaded.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The scanner has been disposed.</exception>
    public async Task<IReadOnlyList<BbBarcodeResult>> ScanImageAsync(
        ReadOnlyMemory<byte> image, string? contentType = null, int maxSymbols = 16, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSymbols);
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!await EnsureModuleAsync().ConfigureAwait(true) || _bridge is not { } bridge)
        {
            // Disposed while the module loaded is a lifetime error, not a platform limitation.
            ObjectDisposedException.ThrowIf(_disposed, this);
            throw ImageScanningNotSupported();
        }

        if (MaxImageFileSize > 0 && image.Length > MaxImageFileSize)
        {
            throw new BbScannerException(BbScannerErrorKind.InvalidImage, TooLargeMessage());
        }

        var request = ++_scanRequest | ScanRequestBit;
        int packed;
        try
        {
            packed = await bridge.PrepareImageAsync(_sessionId, request, image, contentType, StillImageMaxDimension, Math.Max(0, MaxImageFileSize))
                .ConfigureAwait(true);
        }
        catch (Exception ex) when (bridge.IsInteropFailure(ex))
        {
            throw new BbScannerException(BbScannerErrorKind.InvalidImage, T.ImageUnreadable, ex);
        }

        if (_disposed || cancellationToken.IsCancellationRequested)
        {
            await ReleasePreparedAsync(request).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            return [];
        }

        if (packed < 0)
        {
            throw new BbScannerException(BbScannerErrorKind.InvalidImage, RefusalMessage(packed));
        }

        (byte[] Pixels, int Width, int Height, BbPixelFormat Format)? prepared;
        try
        {
            prepared = packed == 0 ? null : await ReadPreparedPixelsAsync(request, packed).ConfigureAwait(true);
        }
        catch (Exception ex) when (bridge.IsInteropFailure(ex) && ex is not ObjectDisposedException)
        {
            throw new BbScannerException(BbScannerErrorKind.InvalidImage, T.ImageUnreadable, ex);
        }

        if (prepared is not { } decoded)
        {
            throw new BbScannerException(BbScannerErrorKind.InvalidImage, T.ImageUnreadable);
        }

        return await DecodeStillAsync(decoded.Pixels, decoded.Width, decoded.Height, decoded.Format, maxSymbols).ConfigureAwait(true);
    }

    /// <summary>
    /// Scans the camera's current frame at the camera's own resolution, with the still image
    /// settings. The live scan reads frames scaled down to <see cref="MaxProcessingSize"/> to keep
    /// up with the camera; a code too dense or too far away for that is often still readable at
    /// full size, which is what this is for - a "capture" button beside the viewfinder, say.
    /// Raises no events.
    /// </summary>
    /// <param name="maxSymbols">The most symbols to return, at least one.</param>
    /// <returns>The symbols found; empty when the frame holds none.</returns>
    /// <exception cref="InvalidOperationException">The camera is not open.</exception>
    /// <exception cref="ObjectDisposedException">The scanner has been disposed.</exception>
    public Task<IReadOnlyList<BbBarcodeResult>> ScanCurrentFrameAsync(int maxSymbols = 16)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSymbols);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_bridge is not { IsLoaded: true } bridge || _status is not (BbScannerStatus.Scanning or BbScannerStatus.Paused))
        {
            throw new InvalidOperationException("The camera is not open; start the scanner before capturing a frame.");
        }

        return CaptureAsync(bridge, maxSymbols);
    }

    private async Task<IReadOnlyList<BbBarcodeResult>> CaptureAsync(BbScannerBridge bridge, int maxSymbols)
    {
        var request = ++_scanRequest | ScanRequestBit;
        (byte[] Pixels, int Width, int Height, BbPixelFormat Format)? frame;
        try
        {
            var packed = await bridge.PrepareVideoFrameAsync(_sessionId, request, CaptureMaxDimension).ConfigureAwait(true);
            frame = packed > 0 ? await ReadPreparedPixelsAsync(request, packed).ConfigureAwait(true) : null;
        }
        catch (Exception ex) when (bridge.IsInteropFailure(ex) && ex is not ObjectDisposedException)
        {
            // The page could not hand the frame over - a circuit reconnecting, say: there is no frame to read.
            return [];
        }

        return frame is { } captured
            ? await DecodeStillAsync(captured.Pixels, captured.Width, captured.Height, captured.Format, maxSymbols).ConfigureAwait(true)
            : [];
    }

    private async Task OnFileSelectedAsync(ChangeEventArgs args)
    {
        if (_disposed || _bridge is not { IsLoaded: true } bridge)
        {
            return;
        }

        // A newer selection supersedes this one, whichever of the two finishes preparing first;
        // each is prepared under its own request, so neither can read the other's pixels.
        var request = ++_imageRequest;
        var camera = _startVersion;

        // The still image path speaks through a notice of its own rather than the status message,
        // so it never hides why the scanner is starting or why it faulted, and never leaves its
        // own outcome pinned over a running camera.
        SetNotice(T.ReadingImage);

        int packed;
        try
        {
            // Large photographs are scaled down in the browser: a 12 megapixel image carries no
            // more barcode information than a 2 megapixel one and costs six times as much to scan.
            packed = await bridge.PrepareImageFromInputAsync(_sessionId, _fileInputId, request, StillImageMaxDimension, Math.Max(0, MaxImageFileSize))
                .ConfigureAwait(true);
        }
        catch (Exception)
        {
            // An unreadable file is a message for the user, not a fault of the running camera.
            if (!_disposed && request == _imageRequest)
            {
                SetNotice(T.ImageUnreadable, transient: true);
            }

            return;
        }

        if (_disposed)
        {
            return;
        }

        if (request != _imageRequest)
        {
            await ReleasePreparedAsync(request).ConfigureAwait(true);
            return;
        }

        if (packed < 0)
        {
            // Refused before the image was ever decoded: too big, not an image, or declaring more
            // pixels than a tab can afford to decode.
            SetNotice(RefusalMessage(packed), transient: true);
            return;
        }

        if (packed == 0)
        {
            SetNotice(null);
            return;
        }

        (byte[] Pixels, int Width, int Height, BbPixelFormat Format)? prepared;
        try
        {
            prepared = await ReadPreparedPixelsAsync(request, packed).ConfigureAwait(true);
        }
        catch (Exception ex) when (bridge.IsInteropFailure(ex))
        {
            prepared = null;
        }

        if (_disposed)
        {
            return;
        }

        if (prepared is not { } image)
        {
            if (request == _imageRequest)
            {
                SetNotice(T.ImageUnreadable, transient: true);
            }

            return;
        }

        IReadOnlyList<BbBarcodeResult> results;
        try
        {
            results = await DecodeStillAsync(image.Pixels, image.Width, image.Height, image.Format, Math.Max(1, MaxSymbolsPerFrame)).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            // The fault is the application's to see whatever was chosen since; the notice is told only while this is
            // still the selection the user is waiting on, as a result would be.
            if (!_disposed)
            {
                if (request == _imageRequest)
                {
                    SetNotice(T.ImageNotScanned, transient: true);
                }

                await DispatchExceptionAsync(ex).ConfigureAwait(true);
            }

            return;
        }

        // The decode gives the browser a turn, in which the scanner can be disposed or another image chosen; the newer
        // choice is the one the user is waiting for.
        if (_disposed || request != _imageRequest)
        {
            return;
        }

        // Read but turned down by AcceptResult is, to the user, the same as not there at all.
        var reported = await ReportStillAsync(results, camera).ConfigureAwait(true);
        if (!_disposed && request == _imageRequest)
        {
            SetNotice(reported == 0 ? T.NoBarcodeFound : null, transient: reported == 0);
        }
    }

    /// <summary>
    /// Copies an image JavaScript prepared under <paramref name="request"/> into managed memory - as RGBA on
    /// WebAssembly, as luminance where it crosses to another process - or returns <see langword="null"/> when it is
    /// gone or short.
    /// </summary>
    private async Task<(byte[] Pixels, int Width, int Height, BbPixelFormat Format)?> ReadPreparedPixelsAsync(int request, int packed)
    {
        if (_bridge is not { IsLoaded: true } bridge)
        {
            return null;
        }

        var width = packed >> 16;
        var height = packed & 0xFFFF;
        return await bridge.ReadPreparedAsync(_sessionId, request, width, height).ConfigureAwait(true) is { } prepared
            ? (prepared.Pixels, width, height, prepared.Format)
            : null;
    }

    /// <summary>Forgets an image prepared under <paramref name="request"/> that nobody will read.</summary>
    private async Task ReleasePreparedAsync(int request)
    {
        if (_bridge is not { IsLoaded: true } bridge)
        {
            return;
        }

        try
        {
            await bridge.ReleasePreparedAsync(_sessionId, request).ConfigureAwait(true);
        }
        catch (Exception ex) when (bridge.IsInteropFailure(ex))
        {
            // Gone with the page.
        }
    }

    /// <summary>
    /// The quick look, when <paramref name="quickOptions"/> asks for one, and then the exhaustive pass, with options
    /// built beforehand: it reads nothing of the component, so it runs on any thread.
    /// </summary>
    private static IReadOnlyList<BbBarcodeResult> DecodeStillImage(
        byte[] pixels, int width, int height, BbPixelFormat format, int maxSymbols, BbScannerOptions? quickOptions, BbScannerOptions stillOptions)
    {
        if (quickOptions is not null)
        {
            using var quick = new BbBarcodeDecoder(quickOptions);
            var found = quick.DecodeAll(pixels, width, height, format, 1, suppressDuplicates: false);
            if (found.Count > 0)
            {
                return found;
            }
        }

        using var decoder = new BbBarcodeDecoder(stillOptions);
        return decoder.DecodeAll(pixels, width, height, format, maxSymbols, suppressDuplicates: false);
    }

    /// <summary>
    /// Decodes a still image the way a person waiting for it is best served. When one symbol is all that is wanted, a
    /// quick look comes first - at half size, with the camera's settings and inverted symbols - which reads most
    /// photographs of a code in a small fraction of the time; only when it finds nothing does the exhaustive pass run.
    /// That pass is the expensive one: without ahead of time compilation a large photograph holds the browser's only
    /// thread for seconds, so the browser is given a turn first, and the notice that the image is being read is on
    /// screen while it runs.
    /// </summary>
    /// <remarks>
    /// The pixels are always a copy the component made from what JavaScript prepared, never an application's buffer, and
    /// they are zeroed once decoded: a photograph of a document is no business of anything after the scan.
    /// </remarks>
    private async Task<IReadOnlyList<BbBarcodeResult>> DecodeStillAsync(byte[] pixels, int width, int height, BbPixelFormat format, int maxSymbols)
    {
        try
        {
            var quickOptions = maxSymbols == 1 ? BuildQuickStillImageOptions() : null;
            var stillOptions = BuildStillImageOptions();

            // In a Blazor Hybrid app the dispatcher is the app's UI thread, which a large photograph would hold for a
            // noticeable while: the decoders here are this image's own, so they can run anywhere.
            if (_bridge?.DecodesOffThread == true)
            {
                return await Task.Run(() => DecodeStillImage(pixels, width, height, format, maxSymbols, quickOptions, stillOptions)).ConfigureAwait(true);
            }

            // A timer, not Task.Yield: a continuation queued straight back onto the single WebAssembly thread can run
            // before the browser has painted anything.
            await Task.Delay(1).ConfigureAwait(true);
            return DecodeStillImage(pixels, width, height, format, maxSymbols, quickOptions, stillOptions);
        }
        finally
        {
            Array.Clear(pixels);
        }
    }

    /// <summary>
    /// The quick first look at a still image: the application's symbology options at the camera's trade-offs, on a half
    /// size copy, light-on-dark symbols included. The pipeline reports what it finds in the image's own coordinates.
    /// </summary>
    private BbScannerOptions BuildQuickStillImageOptions()
    {
        var options = Options?.Clone() ?? new BbScannerOptions();
        options.Formats = Formats;
        options.Region = BbScanRegion.Full;
        options.DownsampleFactor = 2;
        options.AllowInverted = true;
        options.TryVerticalLines = true;
        options.DuplicateSuppressionWindow = TimeSpan.Zero;
        return options;
    }

    /// <summary>Why image preparation refused an image, for the user.</summary>
    private string RefusalMessage(int code) => code switch
    {
        ImageTooLarge => TooLargeMessage(),
        ImageNotAnImage => T.NotAnImage,
        _ => T.ImageTooManyPixels,
    };

    private string TooLargeMessage() => Say(T.ImageTooLarge, BbScannerTexts.Default.ImageTooLarge, MaxImageFileSize / (1024.0 * 1024.0));

    private static BbScannerException ImageScanningNotSupported() => new(
        BbScannerErrorKind.NotSupported,
        "Scanning an encoded image needs the browser's image decoders, which the component only reaches once it is " +
        "interactive. Wait for it to be, or decode the image to pixels with an image library and pass them to " +
        "DecodeAllInImage instead.");

    private async Task RunFrameLoopAsync(CancellationToken cancellationToken, int version)
    {
        if (_bridge is not { } bridge)
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

                bool running;
                try
                {
                    running = await TickAsync(bridge, version).ConfigureAwait(true);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested && bridge.IsTransient(ex))
                {
                    // A call into the page timed out: under Blazor Server, a circuit reconnecting. The next tick asks
                    // again, and the camera the page's watchdog closed meanwhile, if it did, is reported then.
                    continue;
                }
                catch (Exception ex) when (bridge.IsDisconnected(ex))
                {
                    // The circuit is gone for good; the page's watchdog closes the camera, and the component is about
                    // to be disposed with the rest of the circuit.
                    return;
                }

                if (!running)
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
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
                    await ReleaseCameraAsync().ConfigureAwait(true);
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
    /// One tick of the frame loop: picks up what the page changed, then grabs, decodes and reports a frame if the
    /// camera delivered one. Returns <see langword="false"/> once the loop is finished - the camera lost, released or
    /// faulted.
    /// </summary>
    private async Task<bool> TickAsync(BbScannerBridge bridge, int version)
    {
        // Everything the page could have changed, in one call: across a network that is one round trip a tick. When a
        // frame is wanted, the region of it the decoder searches is asked for along with it, should one be waiting.
        (int X, int Y, int Width, int Height)? grab = null;
        if (_status == BbScannerStatus.Scanning && !_configurePending && _frameDecoder is null && _decoder is { } wanted)
        {
            grab = wanted.Options.Region.ToPixels(_processingWidth, _processingHeight);
        }

        var state = await bridge.PollAsync(_sessionId, grab).ConfigureAwait(true);
        if (IsStale(version))
        {
            return false;
        }

        // A paused scanner decodes nothing but still owns the camera, so whatever the
        // browser changes underneath it - a rotation, a resize, a camera that went away -
        // is picked up now rather than discovered on resume.
        await ApplyBrowserChangesAsync(bridge, state & BbScannerBridge.PollChanges).ConfigureAwait(true);
        if (IsStale(version))
        {
            return false;
        }

        ExpireHighlight(Environment.TickCount64);

        if (ReleaseCameraWhenHidden && (state & BbScannerBridge.PollHidden) != 0)
        {
            await ReleaseCameraAsync().ConfigureAwait(true);
            await ReopenWhenVisibleAsync(version).ConfigureAwait(true);
            return false;
        }

        if ((state & BbScannerBridge.PollEnded) != 0)
        {
            // How it ended comes in the same poll. Asking again would leave a round trip in which a newer start could
            // begin opening a camera - that the release would then close under it.
            await LoseCameraAsync(version, interrupted: (state & BbScannerBridge.PollInterrupted) != 0).ConfigureAwait(true);
            return false;
        }

        // The size the page is told to read frames at changed, and it has not been told yet. It is told whether or not a
        // frame is waiting: a page that was never told has nothing to read frames into, and reports none until it is.
        await ApplyProcessingSizeAsync().ConfigureAwait(true);
        if (IsStale(version))
        {
            return false;
        }

        // One that changed again while the page was being told leaves the page a size behind, and the frame waits for
        // the next tick. Nothing new since the last grab is the usual answer between camera frames, and costs nothing
        // more. This is also the backpressure mechanism: a slow decode simply misses ticks and the next grab returns the
        // newest frame rather than a queued stale one.
        if (_configurePending || _status == BbScannerStatus.Paused || (state & BbScannerBridge.PollFrame) == 0)
        {
            return true;
        }

        // A frame of the loop before this one - stopped and started again meanwhile - still on its way or still decoding
        // on another thread has the frame buffer to itself: the next frame waits for it.
        if (_frameDecoder is not null)
        {
            return true;
        }

        // The frame is decoded at the size it was grabbed at: a parameter change while it was on its way, which only
        // a remote page leaves time for, makes it the wrong size, and it is dropped rather than decoded as another.
        var width = _processingWidth;
        var height = _processingHeight;
        var shape = _processingVersion;
        var decoder = _decoder;
        if (decoder is null)
        {
            return true;
        }

        // Where the page is remote it sends the searched region only, as luminance; on WebAssembly it hands over the
        // whole frame and the decoder cuts the region out itself.
        var region = decoder.Options.Region.ToPixels(width, height);
        var buffer = _frameBuffer;
        var remote = bridge.IsRemote;
        var maxSymbols = MaxSymbolsPerFrame;
        BbBarcodeResult? result;
        IReadOnlyList<BbBarcodeResult>? results;

        _frameDecoder = decoder;
        try
        {
            var written = await bridge.GrabFrameAsync(_sessionId, buffer, region).ConfigureAwait(true);

            // Stopped while the frame was on its way. Or nothing new - or a camera track that ended, which the next poll
            // reports along with how it ended.
            if (IsStale(version) || written is 0 or BbScannerBridge.FrameTrackEnded)
            {
                return !IsStale(version);
            }

            if (written == BbScannerBridge.FrameBufferTooSmall)
            {
                // The frame buffer and the canvas are sized together, so this cannot happen
                // without an invariant having been broken. Retrying would spin at the frame
                // rate reporting nothing, so it is treated as a fault.
                await ReleaseCameraAsync().ConfigureAwait(true);
                await FailAsync(new BbScannerException(
                    BbScannerErrorKind.Unknown,
                    "The frame buffer no longer matches the camera's frame size.")).ConfigureAwait(true);
                return false;
            }

            // The decoder may have been replaced, or the processing size changed, while the frame was on its way.
            if (shape != _processingVersion || !ReferenceEquals(buffer, _frameBuffer) || !ReferenceEquals(decoder, _decoder) ||
                _status != BbScannerStatus.Scanning)
            {
                return true;
            }

            // A remote page sends the searched region as luminance. Otherwise the frame is whole: the grayscale plane, a
            // byte a pixel, from the GPU path or a native camera, and RGBA from the 2D canvas. The sizes are multiplied in
            // 64 bits: a product past the largest int wraps round to one a page could send.
            var pixels = (long)width * height;
            if (remote ? written != (long)region.Width * region.Height : written != pixels && written < pixels * 4)
            {
                return true;
            }

            var format = remote || written == pixels ? BbPixelFormat.Gray8 : BbPixelFormat.Rgba32;

            // Every decoded frame is returned, duplicates included, so the highlight can
            // follow a symbol held in view; suppression is applied below, before reporting.
            // One symbol a frame, the default, takes the cheaper single result path.
            try
            {
                (result, results) = bridge.DecodesOffThread
                    ? await Task.Run(() => DecodeFrame(decoder, buffer, written, format, remote, region, width, height, maxSymbols)).ConfigureAwait(true)
                    : DecodeFrame(decoder, buffer, written, format, remote, region, width, height, maxSymbols);

                _consecutiveDecodeFailures = 0;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A scanner stopped, or given another decoder, while the frame decoded on another thread: the failure
                // says nothing about the decoder there is now.
                if (IsStale(version) || !ReferenceEquals(decoder, _decoder))
                {
                    return !IsStale(version);
                }

                // One frame the decoder trips over is skipped; the next frame is a different
                // image. Only a decoder that fails frame after frame ends the session.
                if (++_consecutiveDecodeFailures < MaxConsecutiveDecodeFailures)
                {
                    return true;
                }

                await ReleaseCameraAsync().ConfigureAwait(true);
                await FailAsync(new BbScannerException(
                    BbScannerErrorKind.Unknown,
                    $"The decoder failed on {MaxConsecutiveDecodeFailures} frames in a row.",
                    ex)).ConfigureAwait(true);
                return false;
            }

            // Stopped, or given another decoder, while the frame was decoded on another thread: what it found is the old
            // decoder's, for a session that may be gone.
            if (IsStale(version) || !ReferenceEquals(decoder, _decoder))
            {
                return !IsStale(version);
            }

            _lastDiagnostics = decoder.LastFrame;
        }
        finally
        {
            EndFrame(decoder, buffer, version);
        }

        await ReportDiagnosticsAsync().ConfigureAwait(true);

        // An OnDiagnostics handler, or the user, may have paused or stopped the scanner meanwhile: the frame
        // was grabbed before that, and is not reported after it.
        if ((result is null && (results is null || results.Count == 0)) || Interrupted(version))
        {
            return !IsStale(version);
        }

        await ReportLiveAsync(result, results, version).ConfigureAwait(true);
        return !IsStale(version);
    }

    /// <summary>Decodes one live frame: the searched region a remote page sent, or a whole frame.</summary>
    private static (BbBarcodeResult? Single, IReadOnlyList<BbBarcodeResult>? All) DecodeFrame(
        BbBarcodeDecoder decoder, byte[] buffer, int written, BbPixelFormat format, bool remote,
        (int X, int Y, int Width, int Height) region, int width, int height, int maxSymbols)
    {
        var frame = buffer.AsSpan(0, written);
        if (remote)
        {
            return maxSymbols > 1
                ? (null, decoder.DecodeAllRegion(frame, format, region, width, height, maxSymbols, suppressDuplicates: false))
                : (decoder.DecodeRegion(frame, format, region, width, height, suppressDuplicates: false), null);
        }

        return maxSymbols > 1
            ? (null, decoder.DecodeAll(frame, width, height, format, maxSymbols, suppressDuplicates: false))
            : (decoder.Decode(frame, width, height, format, suppressDuplicates: false), null);
    }

    /// <summary>
    /// Hands the frame buffer and <paramref name="decoder"/> back once the frame the loop had in hand is done with them,
    /// and does what was left to it meanwhile. A buffer let go of - the camera closed, the scanner stopped or started
    /// again, or a new size given - is wiped, along with what the decoder kept of the frame, and a decoder retired
    /// meanwhile - the formats changed, or the scanner disposed - is disposed now rather than under the frame.
    /// </summary>
    private void EndFrame(BbBarcodeDecoder decoder, byte[] buffer, int version)
    {
        _frameDecoder = null;
        var closed = IsStale(version) || _camera is null;
        if (closed || !ReferenceEquals(buffer, _frameBuffer))
        {
            Array.Clear(buffer);
        }

        if (!ReferenceEquals(decoder, _decoder))
        {
            decoder.Dispose();
        }
        else if (closed)
        {
            decoder.ForgetImage();
        }
    }

    /// <summary>
    /// Picks up the things the browser can change underneath a running scanner: the set of cameras, the size of the
    /// video element, and the resolution the camera track renegotiated - whichever of them the poll's change token says
    /// changed.
    /// </summary>
    /// <remarks>
    /// A track that changes resolution, which mobile browsers do when the device is rotated,
    /// would otherwise keep being drawn into a canvas of the old shape. The frames handed to the
    /// decoder would be anamorphically squashed, finder patterns would stop being square, and
    /// the scanner would go on reporting "Scanning" while reading nothing at all.
    /// </remarks>
    private async Task ApplyBrowserChangesAsync(BbScannerBridge bridge, int token)
    {
        if (token == _changeToken && !_describePending)
        {
            return;
        }

        // The token is taken up only once what it says changed has been applied: a call lost on the way - a circuit
        // reconnecting - leaves it as it was, so the next tick applies the change again rather than never.
        var devicesChanged = (token >> 16) != (_changeToken >> 16);
        var videoChanged = _describePending || ((token >> 8) & 0xFF) != ((_changeToken >> 8) & 0xFF);
        var viewChanged = (token & 0xFF) != (_changeToken & 0xFF);

        if (devicesChanged)
        {
            // A camera was plugged in or unplugged: the picker offers what is there now.
            await RefreshCamerasAsync().ConfigureAwait(true);
            StateHasChanged();
        }

        if (viewChanged || videoChanged)
        {
            // Kept here rather than asked for on every render: the overlays are drawn from it, and a render cannot
            // wait on a page across a network.
            _viewSize = await bridge.GetViewSizeAsync(_sessionId).ConfigureAwait(true);
        }

        if (!videoChanged)
        {
            _changeToken = token;

            // Only the element was resized. That moves the overlays, which are expressed
            // relative to the visible part of the frame, but nothing else.
            if (viewChanged && Fill)
            {
                RequestRender();
                StateHasChanged();
            }

            return;
        }

        var camera = ParseCamera(await bridge.DescribeAsync(_sessionId).ConfigureAwait(true));
        if (camera is null || camera.Width <= 0 || camera.Height <= 0)
        {
            // No camera to describe: the track is gone, which the poll reports on its own.
            _changeToken = token;
            _describePending = false;
            return;
        }

        var previous = _camera;
        var resized = previous is null || camera.Width != previous.Width || camera.Height != previous.Height;
        _camera = camera;
        _mirrored = camera.FacingMode == "user";

        if (resized)
        {
            ConfigureProcessingSize(camera.Width, camera.Height);
            try
            {
                await ApplyProcessingSizeAsync().ConfigureAwait(true);
            }
            catch when (ReferenceEquals(_camera, camera))
            {
                // Lost on the way - a circuit reconnecting. The next tick describes the camera again, and has to find it
                // resized again, for OnCameraChanged to be raised once the page has the new size.
                _camera = previous;
                throw;
            }
        }

        _changeToken = token;
        _describePending = false;
        RequestRender();
        StateHasChanged();

        if (resized)
        {
            await InvokeCallbackAsync(OnCameraChanged, camera).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// The camera went away. One the browser ended because the page went into the background,
    /// which phones do to a page that is not in front, is an interruption, and is opened again
    /// once the page is back - as is one the page's watchdog closed while a Blazor Server circuit was
    /// reconnecting. Anything else - unplugged, claimed by another application, the permission
    /// revoked - releases what is left and is reported.
    /// </summary>
    /// <param name="version">The start the frame loop that found the camera gone belongs to.</param>
    /// <param name="interrupted">
    /// Whether the poll that said the camera ended said it was interrupted: the page forgets how a camera ended once it
    /// is released.
    /// </param>
    private async Task LoseCameraAsync(int version, bool interrupted)
    {
        if (IsStale(version))
        {
            return;
        }

        await ReleaseCameraAsync().ConfigureAwait(true);
        if (IsStale(version))
        {
            return;
        }

        if (interrupted)
        {
            await ReopenWhenVisibleAsync(version).ConfigureAwait(true);
            return;
        }

        await FailAsync(new BbScannerException(
            BbScannerErrorKind.CameraUnavailable,
            "The camera stopped delivering frames; it may have been disconnected or claimed by another application.")).ConfigureAwait(true);
    }

    /// <summary>
    /// Waits, with the camera already released, for the page to be shown again, then opens the
    /// same camera - paused, if it was paused. The status reads <see cref="BbScannerStatus.Starting"/>
    /// meanwhile: the scanner is on its way back, not stopped, since nobody stopped it. A start,
    /// a stop or disposal in the meantime supersedes the wait.
    /// </summary>
    private async Task ReopenWhenVisibleAsync(int version)
    {
        var deviceId = _activeDeviceId;
        var paused = _status == BbScannerStatus.Paused;
        ClearCameraState();
        _awaitingVisible = true;
        await SetStatusAsync(BbScannerStatus.Starting, null).ConfigureAwait(true);
        if (!IsStale(version))
        {
            _ = WaitThenReopenAsync(version, deviceId, paused);
        }
        else
        {
            _awaitingVisible = false;
        }
    }

    private async Task WaitThenReopenAsync(int version, string? deviceId, bool paused)
    {
        if (_bridge is not { } bridge)
        {
            return;
        }

        try
        {
            // A hidden page's timers are slowed to once a second or less, which only delays
            // noticing that the page is back by as much. A page across a network that stops answering - a circuit
            // reconnecting - is asked again rather than given up on.
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
            while (!IsStale(version))
            {
                try
                {
                    if ((await bridge.PollAsync(_sessionId).ConfigureAwait(true) & BbScannerBridge.PollHidden) == 0)
                    {
                        break;
                    }
                }
                catch (Exception ex) when (bridge.IsTransient(ex))
                {
                    // Asked again on the next tick.
                }

                await timer.WaitForNextTickAsync().ConfigureAwait(true);
            }

            _awaitingVisible = false;
            if (IsStale(version))
            {
                return;
            }

            // A different camera asked for while the page was hidden is the one opened now, rather than straight away
            // on a page nobody can see.
            if (DeviceId != _lastRequestedDeviceId || (DeviceId is null && Facing != _lastRequestedFacing))
            {
                deviceId = null;
            }

            await StartAsync(deviceId).ConfigureAwait(true);

            if (paused)
            {
                Pause();
            }
        }
        catch (Exception ex) when (bridge.IsDisconnected(ex))
        {
            // The circuit is gone for good, and the component with it.
        }
        catch (Exception ex)
        {
            // A fire-and-forget task: nothing may escape it unseen.
            if (!_disposed)
            {
                await DispatchExceptionAsync(ex).ConfigureAwait(true);
            }
        }
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
        _highlights.Clear();
        _camera = null;
        _torchOn = false;
        _zoom = 0;

        // The buffers are kept for the next start, but not the picture in them: once the camera is closed, nothing
        // holds on to the last thing it saw - not the frame, and not the decoder's copies and measurements of it. A frame
        // still on its way, or still decoding, wipes them once it is done with them.
        if (_frameDecoder is null)
        {
            Array.Clear(_frameBuffer);
            _decoder?.ForgetImage();
        }
    }

    /// <summary>
    /// Works out the size frames are decoded at for a camera of the given size, and sizes the frame buffer for it. The
    /// page is told by <see cref="ApplyProcessingSizeAsync"/>, before the next frame is grabbed.
    /// </summary>
    private void ConfigureProcessingSize(int cameraWidth, int cameraHeight)
    {
        var longest = Math.Max(cameraWidth, cameraHeight);
        var limit = Math.Max(120, MaxProcessingSize);
        var scale = longest > limit ? (double)limit / longest : 1.0;

        // A multiple of four wide: the GPU readback packs four pixels' luminance into each texel it reads back, and
        // only a whole number of texels a row comes back as a plain grayscale plane.
        _processingWidth = Math.Max(4, (int)Math.Round(cameraWidth * scale) & ~3);
        _processingHeight = Math.Max(2, (int)Math.Round(cameraHeight * scale) & ~1);

        var required = _processingWidth * _processingHeight * 4;
        if (_frameBuffer.Length < required)
        {
            // The buffer lives for the whole session; frames are copied into it in place. The one it replaces is
            // zeroed first: let go of, it would keep its last frame until the memory happened to be reused. A frame
            // still on its way into it, or decoding from it, zeroes it once it is done.
            if (_frameDecoder is null)
            {
                Array.Clear(_frameBuffer);
            }

            _frameBuffer = new byte[required];
        }

        _processingVersion++;
        _configurePending = true;
    }

    /// <summary>Tells the page the size to read frames at, when it changed since the page was last told.</summary>
    private async ValueTask ApplyProcessingSizeAsync()
    {
        // Configure creates the session if it is missing, so a call after disposal would
        // resurrect one that nothing will ever release.
        if (!_configurePending || _disposed || _bridge is not { IsLoaded: true } bridge)
        {
            return;
        }

        // Settled only once the page has it, and only if no newer size came while it was on its way: a call lost to a
        // circuit reconnecting, or overtaken by another change, is made again on the next tick.
        var version = _processingVersion;
        await bridge.ConfigureAsync(_sessionId, _processingWidth, _processingHeight).ConfigureAwait(true);
        if (version == _processingVersion)
        {
            _configurePending = false;
        }
    }

    /// <summary>
    /// Closes the camera, if one is open, keeping the session for a restart. Nothing the page says on the way - a
    /// script error, a dropped circuit - stops the scanner from going on to release the rest.
    /// </summary>
    private async ValueTask ReleaseCameraAsync()
    {
        if (_bridge is not { IsLoaded: true } bridge)
        {
            return;
        }

        try
        {
            await bridge.StopAsync(_sessionId).ConfigureAwait(true);
        }
        catch (Exception ex) when (bridge.IsInteropFailure(ex))
        {
            // Under Blazor Server, a page that cannot be reached closes the camera itself once it stops hearing from
            // the scanner.
        }
    }

    /// <summary>Reads the camera description the page returns, or <see langword="null"/> when there is none.</summary>
    private static BbCameraInfo? ParseCamera(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(json, BbScannerJsonContext.Default.BbCameraInfo);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Loads the JavaScript module, reporting a load failure through the normal error path. Returns
    /// <see langword="false"/>, without a fault, while the component is not interactive: a prerendered scanner has no
    /// page to reach yet.
    /// </summary>
    /// <remarks>
    /// Every entry point that reaches JavaScript goes through here or through
    /// <see cref="BbScannerBridge.IsLoaded"/> first. Calling a binding on a module that has not
    /// been imported aborts the WebAssembly runtime outright - it is an assertion, not an
    /// exception - which leaves the whole application dead rather than just the scanner.
    /// </remarks>
    private async Task<bool> EnsureModuleAsync()
    {
        if (_disposed)
        {
            return false;
        }

        if (!RendererInfo.IsInteractive)
        {
            return false;
        }

        var bridge = _bridge ??= BbScannerBridge.Create(JSRuntime, Services.GetService(typeof(IBbCameraSource)) as IBbCameraSource, InWebView());
        if (bridge.IsLoaded)
        {
            return true;
        }

        try
        {
            await bridge.LoadAsync().ConfigureAwait(true);
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

    /// <summary>
    /// Whether the component runs in a BlazorWebView - a Blazor Hybrid app - where the page is in the same process,
    /// behind the WebView, rather than across a network.
    /// </summary>
    private bool InWebView() => RendererInfo.Name == "WebView";

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

        // The decoder of a frame in hand is disposed by EndFrame, once the frame is done with it.
        if (!ReferenceEquals(_decoder, _frameDecoder))
        {
            _decoder.Dispose();
        }

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
            $"{(int)Formats}|{options?.MaxScanLines}|{options?.DownsampleFactor}|{options?.TryHarder}|{options?.AllowInverted}|{options?.TryReversedRows}|{options?.TryVerticalLines}|{options?.Code39CheckDigit}|{options?.Code39ExtendedMode}|{options?.CodabarStartStop}|{options?.RequireEanExtension}|{options?.ItfLengths is null}|{itfLengths}");
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
    /// other than <see cref="Timeout.InfiniteTimeSpan"/> is treated as zero rather than left to
    /// throw inside the decoder.
    /// </summary>
    private TimeSpan EffectiveDuplicateWindow()
    {
        var window = DuplicateSuppressionWindow ?? Options?.DuplicateSuppressionWindow ?? TimeSpan.FromSeconds(2);
        return window == Timeout.InfiniteTimeSpan || window >= TimeSpan.Zero ? window : TimeSpan.Zero;
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
        if (_disposed || _bridge is not { IsLoaded: true } bridge)
        {
            return;
        }

        try
        {
            var json = await bridge.ListCamerasAsync().ConfigureAwait(true);
            _devices = JsonSerializer.Deserialize(json, BbScannerJsonContext.Default.BbCameraDeviceArray) ?? [];
            LabelCameras();
        }
        catch (Exception ex) when (bridge.IsTransient(ex))
        {
            // Not answered for now - a circuit reconnecting - is not no cameras: the list stays what it was.
        }
        catch
        {
            // Enumeration is best effort: a browser that refuses it still supports scanning with
            // the default camera.
            _devices = [];
            _cameras = [];
        }
    }

    /// <summary>
    /// Builds <see cref="Cameras"/> from what the browser listed. The browser names no camera until permission has been
    /// granted once; those are numbered here, in the application's language, rather than in the bridge, and numbered
    /// again whenever <see cref="Texts"/> changes language.
    /// </summary>
    private void LabelCameras()
    {
        _camerasLabelledWith = Texts;
        _cameras = [.. _devices.Select((device, index) => string.IsNullOrWhiteSpace(device.Label)
            ? device with { Label = Say(T.UnnamedCamera, BbScannerTexts.Default.UnnamedCamera, index + 1) }
            : device)];
        RequestRender();
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

    /// <summary>
    /// The Scan image button's own click. The page opens the picker in its own click handler once the root is wired
    /// up; until then - the module still loading, or failed to load and loaded now - it is opened from here.
    /// </summary>
    private Task OnPickClickedAsync() => _rootAttached ? Task.CompletedTask : OpenFilePickerAsync();

    /// <summary>Whether the camera is open, decoding or paused: what the Pause button acts on.</summary>
    private bool IsRunning => _status is BbScannerStatus.Scanning or BbScannerStatus.Paused;

    /// <summary>
    /// Whether the Start/Stop button reads Stop: also while the camera is still opening - during the permission prompt,
    /// say - so that it cancels the start rather than asking for the camera a second time.
    /// </summary>
    private bool CanStop => IsRunning || _status == BbScannerStatus.Starting;

    private Task ToggleRunningAsync() => CanStop ? StopAsync() : StartAsync();

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
    /// Reports what one camera frame decoded: the results <see cref="AcceptResult"/> takes are
    /// outlined, and those duplicate suppression lets through are announced and raised through
    /// <see cref="OnScan"/>, after which <see cref="AfterScan"/> decides what happens next.
    /// </summary>
    private async Task ReportLiveAsync(BbBarcodeResult? result, IReadOnlyList<BbBarcodeResult>? results, int version)
    {
        // A list of the frame's own, not one kept for every frame: the awaits below can let a frame loop superseded by a
        // restart resume and go on adding to it while the new loop is still reporting from it.
        var accepted = new List<BbBarcodeResult>(results?.Count ?? 1);
        if (result is not null)
        {
            if (await AcceptsAsync(result).ConfigureAwait(true))
            {
                accepted.Add(result);
            }
        }
        else
        {
            foreach (var candidate in results!)
            {
                if (await AcceptsAsync(candidate).ConfigureAwait(true))
                {
                    accepted.Add(candidate);
                }
            }
        }

        if (accepted.Count == 0 || Interrupted(version))
        {
            return;
        }

        var now = Environment.TickCount64;
        _symbolSeenAt = now;
        FollowHighlights(accepted);

        var reported = false;
        foreach (var scanned in accepted)
        {
            if (!_duplicateFilter.ShouldReport(scanned, now))
            {
                continue;
            }

            if (!reported)
            {
                // Once a frame, however many symbols it reported.
                SignalScan();
            }

            reported = true;
            Announce(scanned);
            await InvokeCallbackAsync(OnScan, scanned).ConfigureAwait(true);

            // An OnScan handler that paused or stopped the scanner wants no more of this frame.
            if (Interrupted(version))
            {
                return;
            }
        }

        if (reported)
        {
            await ApplyAfterScanAsync().ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Whether a frame being reported should be let go: the camera session it came from is gone, or the scanner is no
    /// longer scanning - paused, from a handler or by the user, while the report was waiting on one.
    /// </summary>
    private bool Interrupted(int version) => IsStale(version) || _status != BbScannerStatus.Scanning;

    /// <summary>
    /// Reports what a still image decoded. A still image is scanned once and on purpose, so every
    /// result <see cref="AcceptResult"/> takes is reported, whatever was reported before.
    /// </summary>
    /// <returns>How many results were reported.</returns>
    /// <param name="results">What the image held.</param>
    /// <param name="camera">
    /// The camera session, by its start version, that was open when the image was chosen: <see cref="AfterScan"/> pauses or
    /// stops that one, never one started while the image was being read.
    /// </param>
    private async Task<int> ReportStillAsync(IReadOnlyList<BbBarcodeResult> results, int camera)
    {
        var reported = 0;
        foreach (var result in results)
        {
            if (_disposed || !await AcceptsAsync(result).ConfigureAwait(true))
            {
                continue;
            }

            if (reported == 0)
            {
                SignalScan();
            }

            reported++;
            Announce(result);
            await InvokeCallbackAsync(OnScan, result).ConfigureAwait(true);
        }

        if (reported > 0 && !_disposed && camera == _startVersion)
        {
            await ApplyAfterScanAsync().ConfigureAwait(true);
        }

        return reported;
    }

    /// <summary>
    /// Asks <see cref="AcceptResult"/> about a result. One that throws has its exception routed to
    /// the renderer, like any other application callback, and the result is turned down.
    /// </summary>
    private async ValueTask<bool> AcceptsAsync(BbBarcodeResult result)
    {
        if (AcceptResult is not { } accept)
        {
            return true;
        }

        try
        {
            return accept(result);
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex).ConfigureAwait(true);
            return false;
        }
    }

    /// <summary>Puts a result in the live region, for assistive technology to read out.</summary>
    private void Announce(BbBarcodeResult result) => Announce(Say(T.ScanAnnouncement, BbScannerTexts.Default.ScanAnnouncement, result));

    /// <summary>
    /// Puts text in the polite live region. A live region only speaks when its content changes,
    /// so the same value scanned twice would be read out once; every other announcement carries
    /// a trailing no-break space, which a screen reader does not pronounce but the region sees.
    /// </summary>
    private void Announce(string text)
    {
        _announcementMark = !_announcementMark;
        _announcement = _announcementMark ? text : text + " ";
        RequestRender();
        StateHasChanged();
    }

    /// <summary>Vibrates, beeps or both for a reported scan, as <see cref="Feedback"/> asks.</summary>
    private void SignalScan()
    {
        if (Feedback != BbScanFeedback.None && _bridge is { IsLoaded: true } bridge)
        {
            // Not awaited: the result is reported while the page beeps, not a round trip after it.
            _ = SignalAsync(bridge, (int)Feedback);
        }

        static async Task SignalAsync(BbScannerBridge bridge, int kinds)
        {
            try
            {
                await bridge.FeedbackAsync(kinds).ConfigureAwait(false);
            }
            catch (Exception ex) when (bridge.IsInteropFailure(ex))
            {
                // Feedback is best effort.
            }
        }
    }

    /// <summary>
    /// Formats a text the application supplied. One that throws is routed to the renderer, like
    /// any other application callback, and the English default is used, so a translation mistake
    /// cannot take the scanner down with it.
    /// </summary>
    private string Say<TArgument>(Func<TArgument, string> text, Func<TArgument, string> fallback, TArgument argument)
    {
        try
        {
            return text(argument) ?? fallback(argument);
        }
        catch (Exception ex)
        {
            _ = DispatchExceptionAsync(ex);
            return fallback(argument);
        }
    }

    /// <summary>The texts in force: the application's, or the English defaults.</summary>
    private BbScannerTexts T => Texts ?? BbScannerTexts.Default;

    /// <summary>Pauses or stops a running camera after a scan, as <see cref="AfterScan"/> says.</summary>
    private async Task ApplyAfterScanAsync()
    {
        if (_status is not (BbScannerStatus.Scanning or BbScannerStatus.Paused))
        {
            // A still image scanned with no camera open has no camera to pause or stop.
            return;
        }

        switch (AfterScan)
        {
            case BbAfterScan.Pause:
                Pause();
                break;
            case BbAfterScan.Stop:
                await StopAsync().ConfigureAwait(true);
                break;
        }
    }

    /// <summary>
    /// Outlines the symbols a frame decoded, and keeps outlining them while they stay in view,
    /// moving with them. Called for every frame that decodes, whether or not its results are
    /// reported.
    /// </summary>
    private void FollowHighlights(List<BbBarcodeResult> results)
    {
        if (!HighlightResults)
        {
            return;
        }

        // Nothing is rendered for a symbol that jitters by a pixel or two: only a different
        // number of symbols, or one that moved, changes what the user sees.
        var changed = results.Count != _highlights.Count;
        for (var i = 0; i < results.Count && !changed; i++)
        {
            if (!ViewBoxOf(results[i], out var view))
            {
                return;
            }

            var previous = _highlights[i].Box;
            changed = Math.Abs(view.Left - previous.Left) >= HighlightTolerance ||
                      Math.Abs(view.Top - previous.Top) >= HighlightTolerance ||
                      Math.Abs(view.Width - previous.Width) >= HighlightTolerance ||
                      Math.Abs(view.Height - previous.Height) >= HighlightTolerance;
        }

        if (!changed)
        {
            return;
        }

        _highlights.Clear();
        foreach (var result in results)
        {
            if (ViewBoxOf(result, out var view))
            {
                _highlights.Add(new Highlight(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"--bit-bbscanner-hit-left:{view.Left * 100:0.##}%;--bit-bbscanner-hit-top:{view.Top * 100:0.##}%;--bit-bbscanner-hit-width:{view.Width * 100:0.##}%;--bit-bbscanner-hit-height:{view.Height * 100:0.##}%"),
                    view));
            }
        }

        RequestRender();
        StateHasChanged();
    }

    /// <summary>Where a result's bounding box falls on the visible part of the video, as fractions of it.</summary>
    private bool ViewBoxOf(BbBarcodeResult result, out (double Left, double Top, double Width, double Height) view)
    {
        view = default;
        if (result.FrameWidth <= 0 || result.FrameHeight <= 0)
        {
            return false;
        }

        var box = result.BoundingBox;
        view = FrameToView(
            box.X / result.FrameWidth,
            box.Y / result.FrameHeight,
            Math.Max(box.Width, 4) / result.FrameWidth,
            Math.Max(box.Height, 4) / result.FrameHeight);
        return true;
    }

    /// <summary>Takes the highlights down once their symbols have not been decoded for a while.</summary>
    private void ExpireHighlight(long now)
    {
        if (_highlights.Count == 0 || now - _symbolSeenAt <= HighlightHoldMilliseconds)
        {
            return;
        }

        _highlights.Clear();
        RequestRender();
        StateHasChanged();
    }

    /// <summary>One outlined symbol: the inline style that places it, and the box it was computed from.</summary>
    private readonly record struct Highlight(string Style, (double Left, double Top, double Width, double Height) Box);

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

        if (!Fill || _camera is null || _camera.Width <= 0 || _camera.Height <= 0)
        {
            return (left, top, width, height);
        }

        var packed = _viewSize;
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
    /// The root element's id: the application's, when it supplied one through an unmatched
    /// attribute, otherwise one of the component's own, which the drop target is found by.
    /// </summary>
    private string RootId() =>
        AdditionalAttributes?.TryGetValue("id", out var id) == true && id is string text && !string.IsNullOrWhiteSpace(text)
            ? text
            : _rootElementId;

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
        BbScannerStatus.Idle => T.StatusIdle,
        BbScannerStatus.Starting => T.StatusStarting,
        BbScannerStatus.Scanning => _camera is null
            ? T.StatusScanning
            : string.Create(CultureInfo.InvariantCulture, $"{T.StatusScanning} · {_camera.Width}×{_camera.Height}"),
        BbScannerStatus.Paused => T.StatusPaused,
        BbScannerStatus.Stopped => T.StatusStopped,
        _ => T.StatusFaulted,
    };

    private Task SetStatusAsync(BbScannerStatus status, string? message)
    {
        // A disposed scanner has nothing left to report a status for: Pause or Resume called on it change nothing.
        if (_disposed || (_status == status && _message == message))
        {
            return Task.CompletedTask;
        }

        var changed = _status != status;
        var opened = changed && status == BbScannerStatus.Scanning && _status == BbScannerStatus.Starting;
        _status = status;
        _message = message;
        if (changed)
        {
            // A new state speaks for itself; a still image notice from the old one is stale.
            ClearNotice();
            _alert = null;
        }

        if (opened)
        {
            // The one change a screen reader user cannot otherwise tell happened: the camera is
            // now live. Pausing, resuming and stopping are told by the button that was pressed,
            // and a result is not to be talked over by the pause that follows it.
            Announce(T.StatusScanning);
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
            // An outcome - nothing found, the file refused - is news; "reading image" is not.
            Announce(notice);
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
        _message = Say(T.ErrorMessage, BbScannerTexts.Default.ErrorMessage, exception);

        // Told at once, interrupting: whatever the user was waiting for is not going to happen.
        _alert = _message;
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

        // Everything managed is let go before the page is told, which over a circuit takes a round trip and may never
        // complete. A frame still in hand wipes the buffer, and disposes its decoder, once it is done with them.
        RetireDecoder();
        if (_frameDecoder is null)
        {
            Array.Clear(_frameBuffer);
        }

        _frameBuffer = [];
        _camera = null;

        if (_bridge is { } bridge)
        {
            if (bridge.IsLoaded)
            {
                try
                {
                    await bridge.DisposeSessionAsync(_sessionId).ConfigureAwait(false);
                }
                catch (Exception ex) when (bridge.IsInteropFailure(ex) || ex is InvalidOperationException)
                {
                    // The page may already be tearing down; there is nothing left to release.
                }
            }

            await bridge.DisposeAsync().ConfigureAwait(false);
        }
    }
}
