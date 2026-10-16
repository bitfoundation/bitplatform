using System.Text.Json.Serialization;
using Bit.Bmage.Interop;
using Microsoft.JSInterop;

namespace Bit.Bmage.Capabilities;

/// <summary>
/// Detects what the current browser can do by actually exercising it: encoding a one pixel image in each candidate
/// format, decoding a tiny probe image, and finding the real canvas size limit. Feature strings and user agents lie;
/// these probes do not.
/// </summary>
/// <remarks>
/// The result is cached for the lifetime of the provider because it cannot change without a page reload. Detection
/// costs a handful of milliseconds and runs once.
/// </remarks>
public sealed class BrowserCapabilityProvider : IImageCapabilityProvider
{
    private readonly BrowserImageInterop _interop;
    private readonly IJSRuntime _jsRuntime;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private ImageCapabilities? _cached;

    public BrowserCapabilityProvider(BrowserImageInterop interop, IJSRuntime jsRuntime)
    {
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    /// <summary>The detected capabilities, or null before the first successful detection.</summary>
    public ImageCapabilities? Cached => _cached;

    /// <summary>
    /// Detects capabilities, caching the result. When the browser is not reachable (prerendering, or a server render
    /// pass) this returns the managed capability set rather than throwing, so calling code can render something sane.
    /// </summary>
    public async ValueTask<ImageCapabilities> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is { } cached) return cached;
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cached is { } existing) return existing;
            ImageCapabilities result;
            try
            {
                var raw = await _interop.InvokeAsync<BrowserCapabilityReport>("getCapabilities", cancellationToken).ConfigureAwait(false);
                result = Translate(raw);
            }
            catch (Exception ex) when (ex is ImageCapabilityException or JSException or InvalidOperationException or JSDisconnectedException)
            {
                // No browser available yet. Report the managed baseline and try again next time.
                return ImageCapabilities.Managed with { Environment = ImageEnvironment.Prerendering };
            }
            _cached = result;
            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    private ImageCapabilities Translate(BrowserCapabilityReport report)
    {
        var decodable = new HashSet<ImageFormat>();
        var encodable = new HashSet<ImageFormat>();
        // Every browser that runs Blazor decodes these; the probes confirm the rest.
        decodable.Add(ImageFormat.Png);
        decodable.Add(ImageFormat.Jpeg);
        decodable.Add(ImageFormat.Bmp);
        foreach (var mime in report.DecodableMimeTypes ?? [])
        {
            var format = ImageFormats.FromMimeType(mime);
            if (format != ImageFormat.Unknown) decodable.Add(format);
        }
        foreach (var mime in report.EncodableMimeTypes ?? [])
        {
            var format = ImageFormats.FromMimeType(mime);
            if (format != ImageFormat.Unknown) encodable.Add(format);
        }
        // BMP encoding is always available through the managed encoder.
        encodable.Add(ImageFormat.Bmp);

        var environment = OperatingSystem.IsBrowser() ? ImageEnvironment.BrowserWebAssembly : ImageEnvironment.BrowserServer;
        var maxDimension = report.MaxCanvasDimension > 0 ? report.MaxCanvasDimension : 4096;
        return new ImageCapabilities
        {
            Environment = environment,
            DecodableFormats = decodable,
            EncodableFormats = encodable,
            SupportsOffscreenCanvas = report.SupportsOffscreenCanvas,
            SupportsWebWorkers = report.SupportsWebWorkers,
            SupportsImageBitmap = report.SupportsImageBitmap,
            SupportsWebCodecs = report.SupportsWebCodecs,
            SupportsClipboardRead = report.SupportsClipboardRead,
            SupportsClipboardWrite = report.SupportsClipboardWrite,
            SupportsFileSystemAccess = report.SupportsFileSystemAccess,
            SupportsCamera = report.SupportsCamera,
            SupportsWasmSimd = environment == ImageEnvironment.BrowserWebAssembly && System.Numerics.Vector.IsHardwareAccelerated,
            SupportsWasmThreads = environment == ImageEnvironment.BrowserWebAssembly && System.Environment.ProcessorCount > 1,
            DevicePixelRatio = report.DevicePixelRatio > 0 ? report.DevicePixelRatio : 1,
            HardwareConcurrency = report.HardwareConcurrency,
            DeviceMemoryGb = report.DeviceMemoryGb,
            MaxCanvasDimension = maxDimension,
            // Square of the edge unless the probe found a tighter area cap (iOS Safari).
            MaxCanvasArea = report.MaxCanvasArea > 0 ? Math.Min(report.MaxCanvasArea, (long)maxDimension * maxDimension) : (long)maxDimension * maxDimension,
        };
    }

    /// <summary>Clears the cache so the next call re-probes. Useful after a display or permission change.</summary>
    public void Invalidate() => _cached = null;

    private sealed class BrowserCapabilityReport
    {
        [JsonPropertyName("encodableMimeTypes")] public string[]? EncodableMimeTypes { get; set; }
        [JsonPropertyName("decodableMimeTypes")] public string[]? DecodableMimeTypes { get; set; }
        [JsonPropertyName("supportsOffscreenCanvas")] public bool SupportsOffscreenCanvas { get; set; }
        [JsonPropertyName("supportsWebWorkers")] public bool SupportsWebWorkers { get; set; }
        [JsonPropertyName("supportsImageBitmap")] public bool SupportsImageBitmap { get; set; }
        [JsonPropertyName("supportsWebCodecs")] public bool SupportsWebCodecs { get; set; }
        [JsonPropertyName("supportsClipboardRead")] public bool SupportsClipboardRead { get; set; }
        [JsonPropertyName("supportsClipboardWrite")] public bool SupportsClipboardWrite { get; set; }
        [JsonPropertyName("supportsFileSystemAccess")] public bool SupportsFileSystemAccess { get; set; }
        [JsonPropertyName("supportsCamera")] public bool SupportsCamera { get; set; }
        [JsonPropertyName("devicePixelRatio")] public double DevicePixelRatio { get; set; }
        [JsonPropertyName("hardwareConcurrency")] public int? HardwareConcurrency { get; set; }
        [JsonPropertyName("deviceMemoryGb")] public double? DeviceMemoryGb { get; set; }
        [JsonPropertyName("maxCanvasDimension")] public int MaxCanvasDimension { get; set; }
        [JsonPropertyName("maxCanvasArea")] public long MaxCanvasArea { get; set; }
    }
}
