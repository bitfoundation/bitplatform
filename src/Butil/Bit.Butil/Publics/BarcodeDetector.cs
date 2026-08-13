using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Barcode_Detection_API">Barcode Detection API</see>:
/// finds QR codes and barcodes in a video frame or an image, using the platform's own decoder
/// rather than a JavaScript one.
/// </summary>
/// <remarks>
/// The usual shape is a camera plus a scan loop: open a stream with
/// <see cref="MediaDevices.GetUserMedia"/>, attach it to a <c>&lt;video&gt;</c>, and point
/// <see cref="StartScan"/> at that element.
/// <br/>
/// Chromium-only at the time of writing, and even there the available formats vary by platform -
/// which is why <see cref="GetSupportedFormats"/> exists and why asking for a format the device
/// can't decode makes detection return nothing rather than throw.
/// </remarks>
public class BarcodeDetector(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeBarcodesDetected);

    private readonly ConcurrentDictionary<Guid, Action<DetectedBarcode[]>> _handlers = new();

    // Per-instance callback reference (see Keyboard): scans are isolated per circuit / WASM app and
    // released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<BarcodeDetector>? _dotNetRef;
    private DotNetObjectReference<BarcodeDetector> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>BarcodeDetector</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.barcodeDetector.isSupported");

    /// <summary>
    /// The symbologies this device can actually decode, e.g. <c>"qr_code"</c>, <c>"ean_13"</c>,
    /// <c>"code_128"</c>, <c>"pdf417"</c>.
    /// </summary>
    /// <returns>An empty array when the API is unavailable.</returns>
    /// <remarks>
    /// Worth calling before offering a scanner: the list is the platform's, not the browser's, so
    /// it differs between Android, ChromeOS and desktop Chrome on the same version.
    /// </remarks>
    public ValueTask<string[]> GetSupportedFormats() => js.Invoke<string[]>("BitButil.barcodeDetector.supportedFormats");

    /// <summary>
    /// Scans a single frame of an element - a <c>&lt;video&gt;</c>, <c>&lt;img&gt;</c> or
    /// <c>&lt;canvas&gt;</c> - once.
    /// </summary>
    /// <param name="element">The element to read.</param>
    /// <param name="formats">
    /// Which symbologies to look for. Narrowing the list makes detection faster; leave it null to
    /// let the platform try everything it supports.
    /// </param>
    /// <returns>Everything found in that frame, or an empty array - including when the video has no frame yet.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DetectedBarcode))]
    public ValueTask<DetectedBarcode[]> Detect(ElementReference element, string[]? formats = null)
        => js.Invoke<DetectedBarcode[]>("BitButil.barcodeDetector.detect", element, formats ?? []);

    /// <summary>
    /// Scans an encoded image - a PNG, JPEG or anything else the browser can decode.
    /// </summary>
    /// <param name="imageBytes">The image file's bytes, not raw pixels.</param>
    /// <param name="mimeType">The image's type, e.g. <c>"image/png"</c>.</param>
    /// <param name="formats">Which symbologies to look for. Null tries everything supported.</param>
    /// <returns>Everything found, or an empty array when the image couldn't be decoded.</returns>
    /// <remarks>
    /// The decoded bitmap is released as soon as the scan finishes - it holds uncompressed pixels,
    /// which would be a real leak in a loop.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DetectedBarcode))]
    public ValueTask<DetectedBarcode[]> DetectImage(byte[] imageBytes, string mimeType = "image/png", string[]? formats = null)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        return js.Invoke<DetectedBarcode[]>("BitButil.barcodeDetector.detectBytes", imageBytes, mimeType, formats ?? []);
    }

    /// <summary>
    /// Invoked from JS when a scan finds something. Public + <see cref="JSInvokableAttribute"/> so
    /// it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeBarcodesDetected(Guid id, DetectedBarcode[] barcodes)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(barcodes);
    }

    /// <summary>
    /// Repeatedly scans a live element and calls <paramref name="onDetected"/> whenever it finds
    /// something. Nothing is reported for a frame with no codes in it.
    /// </summary>
    /// <param name="element">Usually a <c>&lt;video&gt;</c> showing a camera stream.</param>
    /// <param name="onDetected">Called with everything found in a frame. Fires repeatedly while a code stays in view - debounce on the value if you only want it once.</param>
    /// <param name="formats">Which symbologies to look for. Null tries everything supported.</param>
    /// <param name="intervalMs">
    /// How often to sample, in milliseconds; values below 50 are raised to it. Detection is much
    /// slower than a frame, so scanning per-frame would only queue work - a few times a second is
    /// plenty for a user holding up a code.
    /// </param>
    /// <returns>A subscription - dispose it to stop scanning - or null when the API is unavailable.</returns>
    /// <remarks>
    /// A slow device degrades to a lower scan rate rather than building a backlog: a tick that
    /// arrives while the previous detection is still running is skipped.
    /// </remarks>
    [DynamicDependency(nameof(InvokeBarcodesDetected), typeof(BarcodeDetector))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DetectedBarcode))]
    public async ValueTask<ButilSubscription?> StartScan(ElementReference element,
                                                          Action<DetectedBarcode[]> onDetected,
                                                          string[]? formats = null,
                                                          int intervalMs = 250)
    {
        ArgumentNullException.ThrowIfNull(onDetected);

        var id = Guid.NewGuid();
        _handlers[id] = onDetected;

        var started = await js.Invoke<bool>("BitButil.barcodeDetector.startScan",
            DotNetRef, id, element, formats ?? [], intervalMs);

        if (started is false)
        {
            _handlers.TryRemove(id, out _);
            return null;
        }

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.barcodeDetector.stopScan", id);
        });
    }

    /// <summary>
    /// On scope/circuit teardown, stops any scan whose <see cref="ButilSubscription"/> was never
    /// disposed, so an abandoned poll loop can't keep decoding frames.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.barcodeDetector.disposeAll");
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}
