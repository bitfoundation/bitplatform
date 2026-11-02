using System.Text.Json;
using Microsoft.JSInterop;

namespace Bit.BbScanner;

/// <summary>
/// The bridge where an <see cref="IBbCameraSource"/> supplies the camera: a Blazor Hybrid app with the device's own
/// camera. The camera's frames come from the source, in the app's process; everything the page still does - the drop
/// target, the file picker, still images, feedback, and showing the source's preview through itself - goes through
/// <see cref="BbScannerJsRuntimeBridge"/>.
/// </summary>
/// <remarks>
/// The frames are whole, upright and already luminance, so the scanner decodes them as it decodes a frame on
/// WebAssembly - and on a thread of its own rather than the app's UI thread, which the dispatcher of a BlazorWebView is.
/// </remarks>
internal sealed class BbScannerNativeBridge(BbScannerJsRuntimeBridge page, IBbCameraSource source) : BbScannerBridge
{
    private readonly Dictionary<int, (byte[] Pixels, int Width, int Height)> _captured = [];

    // One camera at a time: a start waits for the one before it to finish opening, and closes what that opened.
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IBbCameraSession? _session;
    private DotNetObjectReference<BbNativePreviewReceiver>? _receiver;
    private BbCameraInfo? _described;
    private int _generation;
    private int _videoVersion;
    private int _viewVersion;
    private long _lastFrame;
    private int _width;
    private int _height;
    private (double Width, double Height) _view;

    public override bool IsRemote => false;

    public override bool DecodesOffThread => true;

    public override bool IsLoaded => page.IsLoaded;

    public override ValueTask LoadAsync() => page.LoadAsync();

    // The camera is the source's, which needs no secure context: the platform asks the user itself.
    public override ValueTask<int> GetCapabilitiesAsync() => new(CapabilityCamera | CapabilitySecureContext);

    public override async ValueTask<string> QueryCameraPermissionAsync()
    {
        BbCameraPermission permission;
        try
        {
            permission = await source.GetPermissionAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // A platform that throws rather than answer - an app that does not declare the camera, say - is one that will
            // not say; opening the camera reports what is wrong.
            return "";
        }

        return permission switch
        {
            BbCameraPermission.Granted => "granted",
            BbCameraPermission.Denied => "denied",
            BbCameraPermission.Prompt => "prompt",
            _ => "",
        };
    }

    public override async ValueTask<string> ListCamerasAsync()
    {
        var cameras = await source.GetCamerasAsync().ConfigureAwait(false);
        return JsonSerializer.Serialize([.. cameras], BbScannerJsonContext.Default.BbCameraDeviceArray);
    }

    public override async ValueTask<string> StartAsync(string sessionId, string videoElementId, string? deviceId, int requestedWidth, int requestedHeight, string? facingMode)
    {
        // A newer start, a stop or disposal supersedes this one: a camera that opens for it after all is closed again.
        // The count goes up before the wait, so the start this one waits for already knows it is superseded.
        var generation = Interlocked.Increment(ref _generation);
        IBbCameraSession session;
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            ThrowIfSuperseded(generation);

            // Another camera for the same scanner: the page goes on showing a preview through itself meanwhile.
            await CloseAsync(detach: false).ConfigureAwait(false);

            session = await source.OpenAsync(new BbCameraRequest
            {
                DeviceId = deviceId,
                Facing = facingMode switch
                {
                    "environment" => BbCameraFacing.Rear,
                    "user" => BbCameraFacing.Front,
                    _ => BbCameraFacing.Any,
                },
                Width = requestedWidth > 0 ? requestedWidth : 1280,
                Height = requestedHeight > 0 ? requestedHeight : 720,
            }).ConfigureAwait(false);

            if (generation != Volatile.Read(ref _generation))
            {
                await DisposeQuietlyAsync(session, detach: true).ConfigureAwait(false);
                ThrowIfSuperseded(generation);
            }

            _session = session;
            _described = session.Camera;
            _lastFrame = 0;

            try
            {
                // The page shows the preview through itself, and says where, for the source to draw it there.
                _receiver ??= DotNetObjectReference.Create(new BbNativePreviewReceiver(Place));
                await page.AttachNativePreviewAsync(sessionId, videoElementId, _receiver).ConfigureAwait(false);

                // A stop that came meanwhile found the gate taken, and left the camera to this.
                ThrowIfSuperseded(generation);
            }
            catch
            {
                await CloseAsync(detach: true).ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }

        // A stop or disposal after that check found the gate still taken, and left the camera to this as well.
        if (generation != Volatile.Read(ref _generation))
        {
            await CloseUnlessOpeningAsync(session).ConfigureAwait(false);
            ThrowIfSuperseded(generation);
        }

        return Serialize(session.Camera);
    }

    public override ValueTask ConfigureAsync(string sessionId, int width, int height)
    {
        _width = width;
        _height = height;
        return default;
    }

    public override ValueTask<int> PollAsync(string sessionId, (int X, int Y, int Width, int Height)? grab = null)
    {
        // Hidden is the app in the background only. A camera the platform interrupts while the app is in front - iPad
        // multitasking, system pressure - is waited for with the camera kept: released, it would be opened again at once,
        // into the same interruption, over and over.
        var hidden = source.IsInBackground;
        var flags = hidden ? PollHidden : 0;
        if (_session is { } session)
        {
            if (session.State == BbCameraSessionState.Ended)
            {
                // As in the page: a camera that ends while the app is in the background is an interruption, opened
                // again once the app is back.
                flags |= hidden ? PollEnded | PollInterrupted : PollEnded;
            }

            // A turned device delivers frames of another shape, which the scanner has to be sized for again.
            var camera = session.Camera;
            if (!ReferenceEquals(camera, _described))
            {
                _described = camera;
                _videoVersion = (_videoVersion + 1) & 0xFF;
            }

            if (session.FrameCount > _lastFrame)
            {
                flags |= PollFrame;
            }
        }

        return new(flags | (_videoVersion << 8) | _viewVersion);
    }

    public override async ValueTask<int> GrabFrameAsync(string sessionId, byte[] buffer, (int X, int Y, int Width, int Height) region)
    {
        if (_session is not { } session || _width <= 0 || _height <= 0)
        {
            return 0;
        }

        var (width, height) = (_width, _height);
        var length = width * height;
        if (buffer.Length < length)
        {
            return FrameBufferTooSmall;
        }

        // Scaling a camera frame down to the processing size touches every pixel of it: off the UI thread.
        var frame = await Task.Run(() => session.ReadFrame(buffer.AsSpan(0, length), width, height)).ConfigureAwait(false);
        if (frame == 0 || frame <= _lastFrame)
        {
            return 0;
        }

        _lastFrame = frame;
        return length;
    }

    public override ValueTask<string> DescribeAsync(string sessionId) => new(_session is { } session ? Serialize(session.Camera) : "");

    public override ValueTask<int> GetViewSizeAsync(string sessionId)
    {
        var width = Math.Clamp((int)Math.Round(_view.Width), 0, 0x7FFF);
        var height = Math.Clamp((int)Math.Round(_view.Height), 0, 0xFFFF);
        return new((width << 16) | height);
    }

    public override async ValueTask<bool> SetTorchAsync(string sessionId, bool on)
        => _session is { } session && await session.SetTorchAsync(on).ConfigureAwait(false);

    public override async ValueTask<bool> SetZoomAsync(string sessionId, double value)
        => _session is { } session && await session.SetZoomAsync(value).ConfigureAwait(false);

    public override async ValueTask StopAsync(string sessionId)
    {
        Interlocked.Increment(ref _generation);

        // The page first, then the camera: the WebView turns opaque again only once the page paints its own
        // backgrounds again, so nothing flashes through between the two.
        try
        {
            await page.DetachNativePreviewAsync(sessionId).ConfigureAwait(false);
        }
        finally
        {
            await CloseUnlessOpeningAsync().ConfigureAwait(false);
        }
    }

    public override async ValueTask DisposeSessionAsync(string sessionId)
    {
        Interlocked.Increment(ref _generation);
        try
        {
            await CloseUnlessOpeningAsync().ConfigureAwait(false);
        }
        finally
        {
            await page.DisposeSessionAsync(sessionId).ConfigureAwait(false);
        }
    }

    public override ValueTask FeedbackAsync(int kinds) => page.FeedbackAsync(kinds);

    public override ValueTask AttachRootAsync(string sessionId, string rootElementId, string inputElementId)
        => page.AttachRootAsync(sessionId, rootElementId, inputElementId);

    public override ValueTask ClickAsync(string elementId) => page.ClickAsync(elementId);

    public override ValueTask<int> PrepareImageFromInputAsync(string sessionId, string inputElementId, int requestId, int maxDimension, double maxBytes)
        => page.PrepareImageFromInputAsync(sessionId, inputElementId, requestId, maxDimension, maxBytes);

    public override ValueTask<int> PrepareImageAsync(string sessionId, int requestId, ReadOnlyMemory<byte> image, string? contentType, int maxDimension, double maxBytes)
        => page.PrepareImageAsync(sessionId, requestId, image, contentType, maxDimension, maxBytes);

    public override async ValueTask<int> PrepareVideoFrameAsync(string sessionId, int requestId, int maxDimension)
    {
        if (_session is not { } session || session.FrameCount == 0)
        {
            return 0;
        }

        // The camera's own frame, scaled only as far as maxDimension: what a dense or distant symbol needs.
        var camera = session.Camera;
        var longest = Math.Max(camera.Width, camera.Height);
        var scale = maxDimension > 0 && longest > maxDimension ? (double)maxDimension / longest : 1;
        var width = Math.Clamp((int)Math.Round(camera.Width * scale), 1, 0x7FFF);
        var height = Math.Clamp((int)Math.Round(camera.Height * scale), 1, 0xFFFF);
        var pixels = new byte[width * height];
        if (await Task.Run(() => session.ReadFrame(pixels, width, height)).ConfigureAwait(false) == 0)
        {
            return 0;
        }

        lock (_captured)
        {
            _captured[requestId] = (pixels, width, height);
        }

        return (width << 16) | height;
    }

    public override async ValueTask<(byte[] Pixels, BbPixelFormat Format)?> ReadPreparedAsync(string sessionId, int requestId, int width, int height)
    {
        if (TakeCaptured(requestId) is { } captured)
        {
            if (captured.Width == width && captured.Height == height)
            {
                return (captured.Pixels, BbPixelFormat.Gray8);
            }

            Array.Clear(captured.Pixels);
            return null;
        }

        return await page.ReadPreparedAsync(sessionId, requestId, width, height).ConfigureAwait(false);
    }

    public override ValueTask ReleasePreparedAsync(string sessionId, int requestId)
    {
        if (TakeCaptured(requestId) is { } captured)
        {
            Array.Clear(captured.Pixels);
            return default;
        }

        return page.ReleasePreparedAsync(sessionId, requestId);
    }

    public override bool IsInteropFailure(Exception exception) => page.IsInteropFailure(exception);

    public override bool IsTransient(Exception exception) => page.IsTransient(exception);

    public override bool IsDisconnected(Exception exception) => page.IsDisconnected(exception);

    public override async ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _generation);
        try
        {
            await CloseUnlessOpeningAsync().ConfigureAwait(false);
        }
        finally
        {
            _receiver?.Dispose();
            _receiver = null;
            await page.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>Where the page says the scanner's video element is: the preview goes there, and the overlays follow its size.</summary>
    private void Place(BbPreviewPlacement placement)
    {
        if (Math.Abs(placement.Width - _view.Width) >= 0.5 || Math.Abs(placement.Height - _view.Height) >= 0.5)
        {
            _view = (placement.Width, placement.Height);
            _viewVersion = (_viewVersion + 1) & 0xFF;
        }

        _session?.PlacePreview(placement);
    }

    /// <summary>
    /// Closes the camera - <paramref name="only"/>, when given, and only while it is still the one open - unless a start
    /// is still opening one: that start finds itself superseded once it has, and closes it itself.
    /// </summary>
    private async ValueTask CloseUnlessOpeningAsync(IBbCameraSession? only = null)
    {
        if (!await _gate.WaitAsync(0).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            if (only is null || ReferenceEquals(_session, only))
            {
                await CloseAsync(detach: true).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <param name="detach">
    /// Whether the preview is done with, rather than about to be replaced by another camera's: the source puts back what
    /// it changed to show it.
    /// </param>
    private async ValueTask CloseAsync(bool detach)
    {
        var session = _session;
        _session = null;
        _described = null;
        lock (_captured)
        {
            foreach (var captured in _captured.Values)
            {
                Array.Clear(captured.Pixels);
            }

            _captured.Clear();
        }

        if (session is not null)
        {
            await DisposeQuietlyAsync(session, detach).ConfigureAwait(false);
        }
    }

    // A camera that fails to close is closed as far as the scanner is concerned: the platform has it back either way.
    private static async ValueTask DisposeQuietlyAsync(IBbCameraSession session, bool detach)
    {
        try
        {
            if (detach)
            {
                session.PlacePreview(default);
            }

            await session.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
        }
    }

    private void ThrowIfSuperseded(int generation)
    {
        if (generation != Volatile.Read(ref _generation))
        {
            throw new OperationCanceledException("The camera request was superseded.");
        }
    }

    private (byte[] Pixels, int Width, int Height)? TakeCaptured(int requestId)
    {
        lock (_captured)
        {
            return _captured.Remove(requestId, out var captured) ? captured : null;
        }
    }

    private static string Serialize(BbCameraInfo camera) => JsonSerializer.Serialize(camera, BbScannerJsonContext.Default.BbCameraInfo);
}

/// <summary>Receives, from the page, where the scanner's video element is, for a native camera's preview.</summary>
internal sealed class BbNativePreviewReceiver(Action<BbPreviewPlacement> place)
{
    /// <summary>Called by the page whenever the element moves, resizes, shows or hides.</summary>
    [JSInvokable]
    public void Place(double x, double y, double width, double height, double cornerRadius, bool visible, string? backdrop,
        double clipX, double clipY, double clipWidth, double clipHeight)
        => place(new BbPreviewPlacement(x, y, width, height, cornerRadius, visible, backdrop, clipX, clipY, clipWidth, clipHeight));
}
