using System.Runtime.InteropServices;
using Microsoft.JSInterop;

namespace Bit.BbScanner;

/// <summary>
/// The bridge under Blazor Server and Blazor Hybrid, where .NET runs in another process than the page: the module is
/// imported through <see cref="IJSRuntime"/>, and pixels travel in slices, as byte arrays.
/// </summary>
/// <remarks>
/// <para>
/// A frame is the expensive part, so it is cut down in the page before it travels: to the region the decoder searches,
/// and to its luminance, a byte a pixel. A 640 by 360 frame searched through a centred square of 0.7 is about 63 KB
/// rather than the 900 KB of the RGBA frame.
/// </para>
/// <para>
/// It is read in slices of <see cref="SliceBytes"/>, several at once, rather than as one array or a stream. A Blazor
/// Server circuit refuses a single message over 32 KB from the page unless the application raises the limit, and a
/// slice is under it. A JavaScript stream would be too, but a stream whose start is lost to a dropped connection is
/// never disposed, and the runtime ends the whole circuit when its own timeout runs out; a slice is an ordinary call,
/// which times out on its own and is asked for again. Images the application hands over go the other way in parts,
/// for the same reason.
/// </para>
/// <para>
/// Every call the frame loop makes has a timeout: a call made while a circuit is reconnecting is lost rather than
/// failed, and would otherwise hold the loop for the runtime's default of a minute. Opening the camera waits on the user
/// answering the permission prompt, so it is given longer, and a camera that opened while its answer was lost is found
/// by asking the page what it has open.
/// </para>
/// <para>
/// Under Blazor Server the camera this opens is guarded by a watchdog in the page, which closes it once .NET stops
/// asking for frames - a circuit that dropped for good leaves no one behind to close it otherwise.
/// </para>
/// <para>
/// In a Blazor Hybrid app the page is <paramref name="local"/>: behind a WebView, in the app's own process, with no
/// circuit to lose and no limit on a message. There the page's camera needs no watchdog, since the page and .NET go away
/// together, and slices are large, since each call costs a trip through the WebView however small it is.
/// </para>
/// </remarks>
internal sealed class BbScannerJsRuntimeBridge(IJSRuntime jsRuntime, bool local = false) : BbScannerBridge
{
    /// <summary>
    /// The module path, resolved by the browser against the document's base URI, as every Blazor host serves static web
    /// assets from it.
    /// </summary>
    internal const string ModulePath = "./_content/Bit.BbScanner/bit-bbscanner.js";

    /// <summary>
    /// The most pixel bytes read from the page in one call: under a Blazor Server circuit's default limit of 32 KB for a
    /// message from the page, with room for the message's own framing.
    /// </summary>
    internal const int SliceBytes = 30 * 1024;

    /// <summary>The most bytes of an encoded image sent to the page in one call.</summary>
    internal const int PartBytes = 256 * 1024;

    /// <summary>The most bytes read from, or sent to, a page behind a WebView in one call: a frame or two at a time.</summary>
    internal const int LocalSliceBytes = 1024 * 1024;

    /// <summary>readData's key for the latest frame; any other key is a prepared image's request.</summary>
    private const int FrameData = -1;

    /// <summary>
    /// How many slices or parts are asked for at once: enough to keep a connection busy across its round trips, few
    /// enough that a slow one delivers each inside <see cref="CallTimeout"/>.
    /// </summary>
    private const int InFlight = 8;

    /// <summary>
    /// How long the page keeps the camera open without hearing from .NET. Long enough to ride out a circuit
    /// reconnecting; an application callback that holds the frame loop for longer gets the camera closed and opened
    /// again afterwards.
    /// </summary>
    private const int WatchdogMilliseconds = 20_000;

    /// <summary>How long a call the frame loop makes may take before it is given up and asked again on the next tick.</summary>
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(10);

    /// <summary>How long opening the camera may take, the permission prompt included.</summary>
    private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(2);

    private Task<IJSObjectReference>? _loading;
    private IJSObjectReference? _module;
    private bool _disposed;

    /// <summary>The region the last poll grabbed a frame's luminance of, ready to read, or <see langword="null"/>.</summary>
    private (int X, int Y, int Width, int Height)? _grabbed;

    public override bool IsRemote => true;

    // Behind a WebView the component's dispatcher is the app's UI thread, which a decode would hold up.
    public override bool DecodesOffThread => local;

    /// <summary>The slice size in force: under a circuit's message limit, or a WebView's frame at a time.</summary>
    private int Slice => local ? LocalSliceBytes : SliceBytes;

    /// <summary>The part size in force.</summary>
    private int Part => local ? LocalSliceBytes : PartBytes;

    public override bool IsLoaded => _module is not null && !_disposed;

    private IJSObjectReference Module
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _module ?? throw new InvalidOperationException("The Bit.BbScanner module is not loaded.");
        }
    }

    public override async ValueTask LoadAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_module is not null)
        {
            return;
        }

        IJSObjectReference? module;
        try
        {
            // Components mounted together share the one in-flight import.
            module = await (_loading ??= jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask()).ConfigureAwait(false);
        }
        catch
        {
            // A failed import is not cached, so a later call can try again.
            _loading = null;
            throw;
        }

        if (module is null)
        {
            _loading = null;
            throw new InvalidOperationException("The Bit.BbScanner module did not load.");
        }

        if (_disposed)
        {
            await DisposeModuleAsync(module).ConfigureAwait(false);
            throw new ObjectDisposedException(GetType().Name);
        }

        _module = module;
    }

    public override ValueTask<int> GetCapabilitiesAsync() => Module.InvokeAsync<int>("capabilities", CallTimeout);

    public override async ValueTask<string> QueryCameraPermissionAsync()
        => await Module.InvokeAsync<string?>("queryCameraPermission", CallTimeout).ConfigureAwait(false) ?? "";

    public override async ValueTask<string> ListCamerasAsync()
        => await Module.InvokeAsync<string?>("listCameras", CallTimeout).ConfigureAwait(false) ?? "[]";

    public override async ValueTask<string> StartAsync(string sessionId, string videoElementId, string? deviceId, int requestedWidth, int requestedHeight, string? facingMode)
    {
        try
        {
            return await Module.InvokeAsync<string?>(
                "start", StartTimeout, sessionId, videoElementId, deviceId, requestedWidth, requestedHeight, facingMode, local ? 0 : WatchdogMilliseconds)
                .ConfigureAwait(false) ?? "";
        }
        catch (OperationCanceledException)
        {
            // The answer may have been lost to a circuit reconnecting rather than never given: a camera the page opened
            // is taken up as if the answer had arrived. One it has not - the prompt still unanswered - is superseded by
            // the stop the failure leads to, and closed as soon as it opens.
            var camera = await DescribeAsync(sessionId).ConfigureAwait(false);
            if (camera.Length > 0)
            {
                return camera;
            }

            throw;
        }
    }

    public override async ValueTask ConfigureAsync(string sessionId, int width, int height)
    {
        // A region grabbed at the size before is not the same region at this one, whatever its numbers.
        _grabbed = null;
        await Module.InvokeVoidAsync("configure", CallTimeout, sessionId, width, height).ConfigureAwait(false);
    }

    public override async ValueTask<int> PollAsync(string sessionId, (int X, int Y, int Width, int Height)? grab = null)
    {
        // A waiting frame is grabbed along with the poll: one round trip fewer a frame, of the three it would take to
        // poll, grab and read.
        _grabbed = null;
        var state = grab is { } region
            ? await Module.InvokeAsync<int>("poll", CallTimeout, sessionId, region.X, region.Y, region.Width, region.Height).ConfigureAwait(false)
            : await Module.InvokeAsync<int>("poll", CallTimeout, sessionId).ConfigureAwait(false);
        if ((state & PollGrabbed) != 0)
        {
            _grabbed = grab;
        }

        return state;
    }

    public override async ValueTask<int> GrabFrameAsync(string sessionId, byte[] buffer, (int X, int Y, int Width, int Height) region)
    {
        // Grabbed by the poll before this, when that was for this region; asked for now otherwise.
        var grabbed = _grabbed == region;
        _grabbed = null;
        var length = grabbed
            ? region.Width * region.Height
            : await Module.InvokeAsync<int>("grabFrameData", CallTimeout, sessionId, region.X, region.Y, region.Width, region.Height).ConfigureAwait(false);

        // Nothing new, or a frame of another size than asked for - read at a size the page has not been told yet.
        if (length <= 0 || length != region.Width * region.Height)
        {
            return 0;
        }

        if (length > buffer.Length)
        {
            return FrameBufferTooSmall;
        }

        return await ReadAsync(sessionId, FrameData, buffer, length).ConfigureAwait(false) ? length : 0;
    }

    public override async ValueTask<string> DescribeAsync(string sessionId)
        => await Module.InvokeAsync<string?>("describe", CallTimeout, sessionId).ConfigureAwait(false) ?? "";

    public override ValueTask<int> GetViewSizeAsync(string sessionId) => Module.InvokeAsync<int>("getViewSize", CallTimeout, sessionId);

    public override ValueTask<bool> SetTorchAsync(string sessionId, bool on) => Module.InvokeAsync<bool>("setTorch", CallTimeout, sessionId, on);

    public override ValueTask<bool> SetZoomAsync(string sessionId, double value) => Module.InvokeAsync<bool>("setZoom", CallTimeout, sessionId, value);

    public override async ValueTask StopAsync(string sessionId)
        => await Module.InvokeVoidAsync("stop", CallTimeout, sessionId).ConfigureAwait(false);

    public override async ValueTask DisposeSessionAsync(string sessionId)
        => await Module.InvokeVoidAsync("dispose", CallTimeout, sessionId).ConfigureAwait(false);

    public override async ValueTask FeedbackAsync(int kinds) => await Module.InvokeVoidAsync("feedback", CallTimeout, kinds).ConfigureAwait(false);

    public override async ValueTask AttachRootAsync(string sessionId, string rootElementId, string inputElementId)
        => await Module.InvokeVoidAsync("attachRoot", CallTimeout, sessionId, rootElementId, inputElementId).ConfigureAwait(false);

    public override async ValueTask ClickAsync(string elementId) => await Module.InvokeVoidAsync("click", CallTimeout, elementId).ConfigureAwait(false);

    // The browser decodes the image, which for a large photograph takes a while: the runtime's default timeout applies.
    public override ValueTask<int> PrepareImageFromInputAsync(string sessionId, string inputElementId, int requestId, int maxDimension, double maxBytes)
        => Module.InvokeAsync<int>("prepareImageFromInput", sessionId, inputElementId, requestId, maxDimension, maxBytes);

    public override async ValueTask<int> PrepareImageAsync(string sessionId, int requestId, ReadOnlyMemory<byte> image, string? contentType, int maxDimension, double maxBytes)
    {
        var module = Module;
        var size = Part;
        var count = (image.Length + size - 1) / size;
        try
        {
            await ForEachInFlightAsync(count, async index =>
            {
                var part = image.Slice(index * size, Math.Min(size, image.Length - (index * size)));

                // A byte[] is what crosses as binary rather than as JSON, and it has to be exactly the part.
                var bytes = MemoryMarshal.TryGetArray(part, out var segment) && segment.Offset == 0 && segment.Count == segment.Array!.Length
                    ? segment.Array
                    : part.ToArray();
                await module.InvokeVoidAsync("stageImagePart", CallTimeout, sessionId, requestId, index, bytes).ConfigureAwait(false);
                return true;
            }).ConfigureAwait(false);
        }
        catch
        {
            // The parts that did arrive would wait in the page until the session closes.
            try
            {
                await ReleasePreparedAsync(sessionId, requestId).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsInteropFailure(ex))
            {
                // Gone with the page; whatever made the staging fail is the error worth reporting.
            }

            throw;
        }

        return await module.InvokeAsync<int>("prepareStagedParts", sessionId, requestId, count, contentType, maxDimension, maxBytes).ConfigureAwait(false);
    }

    public override ValueTask<int> PrepareVideoFrameAsync(string sessionId, int requestId, int maxDimension)
        => Module.InvokeAsync<int>("prepareVideoFrame", CallTimeout, sessionId, requestId, maxDimension);

    public override async ValueTask<(byte[] Pixels, BbPixelFormat Format)?> ReadPreparedAsync(string sessionId, int requestId, int width, int height)
    {
        try
        {
            var length = await Module.InvokeAsync<int>("readPreparedData", CallTimeout, sessionId, requestId).ConfigureAwait(false);
            if (length != width * height)
            {
                return null;
            }

            var buffer = new byte[length];
            if (await ReadAsync(sessionId, requestId, buffer, length).ConfigureAwait(false))
            {
                return (buffer, BbPixelFormat.Gray8);
            }

            Array.Clear(buffer);
            return null;
        }
        finally
        {
            // The page holds the luminance until it is told it was read, and wipes it then.
            try
            {
                await ReleasePreparedAsync(sessionId, requestId).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsInteropFailure(ex))
            {
                // Gone with the page; whatever made the read fail is the error worth reporting.
            }
        }
    }

    public override async ValueTask ReleasePreparedAsync(string sessionId, int requestId)
        => await Module.InvokeVoidAsync("releasePrepared", CallTimeout, sessionId, requestId).ConfigureAwait(false);

    /// <summary>
    /// Has the page show a native camera's preview through itself where the video element is, and report where that
    /// is to <paramref name="receiver"/> whenever it moves.
    /// </summary>
    public async ValueTask AttachNativePreviewAsync(string sessionId, string videoElementId, DotNetObjectReference<BbNativePreviewReceiver> receiver)
        => await Module.InvokeVoidAsync("attachNativePreview", CallTimeout, sessionId, videoElementId, receiver).ConfigureAwait(false);

    /// <summary>Puts the page back as it was before <see cref="AttachNativePreviewAsync"/>.</summary>
    public async ValueTask DetachNativePreviewAsync(string sessionId)
        => await Module.InvokeVoidAsync("detachNativePreview", CallTimeout, sessionId).ConfigureAwait(false);

    // OperationCanceledException rather than TaskCanceledException alone: a call the runtime gives up on throws the
    // latter, but not everything that times out on the way does.
    public override bool IsInteropFailure(Exception exception)
        => exception is JSException or JSDisconnectedException or OperationCanceledException or ObjectDisposedException;

    public override bool IsTransient(Exception exception) => exception is OperationCanceledException;

    public override bool IsDisconnected(Exception exception) => exception is JSDisconnectedException;

    public override async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        var module = _module;
        _module = null;
        if (module is not null)
        {
            await DisposeModuleAsync(module).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads <paramref name="length"/> bytes the page readied under <paramref name="key"/> into
    /// <paramref name="buffer"/>, a slice at a time, several at once. Returns <see langword="false"/> when a slice came
    /// back short: the page let go of what it had readied.
    /// </summary>
    private async Task<bool> ReadAsync(string sessionId, int key, byte[] buffer, int length)
    {
        var module = Module;
        var size = Slice;
        return await ForEachInFlightAsync((length + size - 1) / size, async index =>
        {
            var offset = index * size;
            var count = Math.Min(size, length - offset);
            var slice = await module.InvokeAsync<byte[]>("readData", CallTimeout, sessionId, key, offset, count).ConfigureAwait(false);
            if (slice is null || slice.Length != count)
            {
                return false;
            }

            slice.CopyTo(buffer, offset);
            return true;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs <paramref name="step"/> for every index below <paramref name="count"/>, <see cref="InFlight"/> at a time;
    /// returns whether every one succeeded. The first failure is thrown once the steps in flight have finished, so none
    /// is left running unobserved.
    /// </summary>
    private static async Task<bool> ForEachInFlightAsync(int count, Func<int, Task<bool>> step)
    {
        var running = new List<Task<bool>>(Math.Min(count, InFlight));
        var succeeded = true;
        for (var index = 0; index < count; index++)
        {
            if (running.Count == InFlight)
            {
                var finished = await Task.WhenAny(running).ConfigureAwait(false);
                running.Remove(finished);
                if (finished.IsFaulted || finished.IsCanceled || !finished.Result)
                {
                    succeeded = false;
                    running.Add(finished);
                    break;
                }
            }

            running.Add(step(index));
        }

        var results = await Task.WhenAll(running).ConfigureAwait(false);
        return succeeded && results.All(result => result);
    }

    private static async ValueTask DisposeModuleAsync(IJSObjectReference module)
    {
        try
        {
            await module.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException or ObjectDisposedException)
        {
            // The page or the circuit is already gone, and the reference with it.
        }
    }
}
