using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Bit.BbScanner;

/// <summary>
/// The bridge on Blazor WebAssembly, where .NET runs in the page: the <see cref="BbScannerJsInterop"/> bindings, with a
/// frame copied by the browser straight into the managed buffer through a memory view.
/// </summary>
/// <remarks>
/// Every binding but the ones that wait on the browser completes synchronously, so the <see cref="ValueTask"/>s here
/// wrap a result and allocate nothing: the frame loop costs on WebAssembly what it cost before there was a bridge.
/// </remarks>
[SupportedOSPlatform("browser")]
internal sealed class BbScannerWasmBridge : BbScannerBridge
{
    public override bool IsRemote => false;

    public override bool IsLoaded => BbScannerJsInterop.IsImported;

    public override async ValueTask LoadAsync() => await BbScannerJsInterop.EnsureImportedAsync().ConfigureAwait(false);

    public override ValueTask<int> GetCapabilitiesAsync() => new(BbScannerJsInterop.Capabilities());

    public override async ValueTask<string> QueryCameraPermissionAsync()
        => await BbScannerJsInterop.QueryCameraPermissionAsync().ConfigureAwait(false);

    public override async ValueTask<string> ListCamerasAsync() => await BbScannerJsInterop.ListCamerasAsync().ConfigureAwait(false);

    // No watchdog: a .NET that runs in the page goes away with it.
    public override async ValueTask<string> StartAsync(string sessionId, string videoElementId, string? deviceId, int requestedWidth, int requestedHeight, string? facingMode)
        => await BbScannerJsInterop.StartAsync(sessionId, videoElementId, deviceId, requestedWidth, requestedHeight, facingMode, watchdogMs: 0).ConfigureAwait(false);

    public override ValueTask ConfigureAsync(string sessionId, int width, int height)
    {
        BbScannerJsInterop.Configure(sessionId, width, height);
        return default;
    }

    // A poll costs nothing here, and a grab hands the whole frame over through a memory view: nothing to fold together.
    public override ValueTask<int> PollAsync(string sessionId, (int X, int Y, int Width, int Height)? grab = null)
        => new(BbScannerJsInterop.Poll(sessionId));

    // The whole frame: converting and cropping it in managed code costs less here than doing it in JavaScript.
    public override ValueTask<int> GrabFrameAsync(string sessionId, byte[] buffer, (int X, int Y, int Width, int Height) region)
        => new(BbScannerJsInterop.GrabFrame(sessionId, buffer));

    public override ValueTask<string> DescribeAsync(string sessionId) => new(BbScannerJsInterop.Describe(sessionId));

    public override ValueTask<int> GetViewSizeAsync(string sessionId) => new(BbScannerJsInterop.GetViewSize(sessionId));

    public override async ValueTask<bool> SetTorchAsync(string sessionId, bool on)
        => await BbScannerJsInterop.SetTorchAsync(sessionId, on).ConfigureAwait(false);

    public override async ValueTask<bool> SetZoomAsync(string sessionId, double value)
        => await BbScannerJsInterop.SetZoomAsync(sessionId, value).ConfigureAwait(false);

    public override ValueTask StopAsync(string sessionId)
    {
        BbScannerJsInterop.Stop(sessionId);
        return default;
    }

    public override ValueTask DisposeSessionAsync(string sessionId)
    {
        BbScannerJsInterop.DisposeSession(sessionId);
        return default;
    }

    public override ValueTask FeedbackAsync(int kinds)
    {
        BbScannerJsInterop.Feedback(kinds);
        return default;
    }

    public override ValueTask AttachRootAsync(string sessionId, string rootElementId, string inputElementId)
    {
        BbScannerJsInterop.AttachRoot(sessionId, rootElementId, inputElementId);
        return default;
    }

    public override ValueTask ClickAsync(string elementId)
    {
        BbScannerJsInterop.Click(elementId);
        return default;
    }

    public override async ValueTask<int> PrepareImageFromInputAsync(string sessionId, string inputElementId, int requestId, int maxDimension, double maxBytes)
        => await BbScannerJsInterop.PrepareImageFromInputAsync(sessionId, inputElementId, requestId, maxDimension, maxBytes).ConfigureAwait(false);

    public override async ValueTask<int> PrepareImageAsync(string sessionId, int requestId, ReadOnlyMemory<byte> image, string? contentType, int maxDimension, double maxBytes)
    {
        // The view onto the managed bytes lives for the one synchronous call that stages them; the browser decodes its
        // own copy afterwards.
        BbScannerJsInterop.StageImage(sessionId, requestId, MemoryMarshal.AsMemory(image).Span, contentType);
        return await BbScannerJsInterop.PrepareStagedImageAsync(sessionId, requestId, maxDimension, maxBytes).ConfigureAwait(false);
    }

    public override ValueTask<int> PrepareVideoFrameAsync(string sessionId, int requestId, int maxDimension)
        => new(BbScannerJsInterop.PrepareVideoFrame(sessionId, requestId, maxDimension));

    public override ValueTask<(byte[] Pixels, BbPixelFormat Format)?> ReadPreparedAsync(string sessionId, int requestId, int width, int height)
    {
        var required = width * height * 4;
        var buffer = new byte[required];
        var written = BbScannerJsInterop.ReadPrepared(sessionId, requestId, buffer);
        return new(written < required ? null : (buffer, BbPixelFormat.Rgba32));
    }

    public override ValueTask ReleasePreparedAsync(string sessionId, int requestId)
    {
        BbScannerJsInterop.ReleasePrepared(sessionId, requestId);
        return default;
    }

    public override bool IsInteropFailure(Exception exception) => exception is JSException;
}
