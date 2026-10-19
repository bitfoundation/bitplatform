namespace Bit.Bmage.Capabilities;

/// <summary>
/// What the current environment can actually do. Never assume a format or API is available: check here and degrade
/// gracefully, because browser support varies by vendor, version and platform.
/// </summary>
/// <example>
/// <code>
/// var caps = await capabilityProvider.GetAsync();
/// var format = caps.CanEncode(ImageFormat.Avif) ? ImageFormat.Avif
///            : caps.CanEncode(ImageFormat.WebP) ? ImageFormat.WebP
///            : ImageFormat.Jpeg;
/// </code>
/// </example>
public sealed record ImageCapabilities
{
    /// <summary>Capabilities of plain .NET with only the managed codecs: PNG, JPEG, BMP and GIF decoding.</summary>
    public static ImageCapabilities Managed { get; } = new()
    {
        Environment = ImageEnvironment.Server,
        DecodableFormats = new HashSet<ImageFormat> { ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.Bmp, ImageFormat.Gif },
        EncodableFormats = new HashSet<ImageFormat> { ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.Bmp },
    };

    /// <summary>Where the code is running.</summary>
    public ImageEnvironment Environment { get; init; } = ImageEnvironment.Unknown;

    /// <summary>Formats that can be decoded here.</summary>
    public IReadOnlySet<ImageFormat> DecodableFormats { get; init; } = new HashSet<ImageFormat>();

    /// <summary>Formats that can be encoded here.</summary>
    public IReadOnlySet<ImageFormat> EncodableFormats { get; init; } = new HashSet<ImageFormat>();

    /// <summary><c>OffscreenCanvas</c> is available, so rendering can move off the main thread.</summary>
    public bool SupportsOffscreenCanvas { get; init; }

    /// <summary>Web Workers are available.</summary>
    public bool SupportsWebWorkers { get; init; }

    /// <summary><c>createImageBitmap</c> is available, giving fast, off-main-thread decodes.</summary>
    public bool SupportsImageBitmap { get; init; }

    /// <summary>The WebCodecs <c>ImageDecoder</c> is available.</summary>
    public bool SupportsWebCodecs { get; init; }

    /// <summary>The async clipboard API can read images.</summary>
    public bool SupportsClipboardRead { get; init; }

    /// <summary>The async clipboard API can write images.</summary>
    public bool SupportsClipboardWrite { get; init; }

    /// <summary>The File System Access API is available, so exports can be saved with a real save dialog.</summary>
    public bool SupportsFileSystemAccess { get; init; }

    /// <summary>Camera capture through <c>getUserMedia</c> is available.</summary>
    public bool SupportsCamera { get; init; }

    /// <summary>The WebAssembly runtime has SIMD enabled.</summary>
    public bool SupportsWasmSimd { get; init; }

    /// <summary>The WebAssembly runtime has threads enabled.</summary>
    public bool SupportsWasmThreads { get; init; }

    /// <summary>Device pixel ratio of the display, used to render crisp previews on high-DPI screens.</summary>
    public double DevicePixelRatio { get; init; } = 1.0;

    /// <summary>Number of logical processors reported by the device, when known.</summary>
    public int? HardwareConcurrency { get; init; }

    /// <summary>Approximate device memory in gigabytes, when the browser reports it.</summary>
    public double? DeviceMemoryGb { get; init; }

    /// <summary>
    /// The largest canvas dimension the platform reliably supports. Browsers silently produce a blank canvas above this,
    /// so operations are capped against it. Safari on iOS is the usual constraint.
    /// </summary>
    public int MaxCanvasDimension { get; init; } = 8192;

    /// <summary>The largest total canvas area (in pixels) the platform reliably supports.</summary>
    public long MaxCanvasArea { get; init; } = 16384L * 16384L;

    /// <summary>True when the format can be decoded here.</summary>
    public bool CanDecode(ImageFormat format) => DecodableFormats.Contains(format);

    /// <summary>True when the format can be encoded here.</summary>
    public bool CanEncode(ImageFormat format) => EncodableFormats.Contains(format);

    /// <summary>WebP encoding is available.</summary>
    public bool SupportsWebP => CanEncode(ImageFormat.WebP);

    /// <summary>AVIF encoding is available.</summary>
    public bool SupportsAvif => CanEncode(ImageFormat.Avif);

    /// <summary>
    /// Picks the first format from <paramref name="preferred"/> that can be encoded here, falling back to
    /// <paramref name="fallback"/>. Returns the fallback even when it is also unsupported, so callers can report clearly.
    /// </summary>
    public ImageFormat ChooseEncodableFormat(IEnumerable<ImageFormat> preferred, ImageFormat fallback = ImageFormat.Jpeg)
    {
        ArgumentNullException.ThrowIfNull(preferred);
        foreach (var format in preferred)
            if (CanEncode(format)) return format;
        return fallback;
    }

    /// <summary>True when the given dimensions are within the platform's canvas limits.</summary>
    public bool CanRenderCanvas(int width, int height)
        => width > 0 && height > 0 && width <= MaxCanvasDimension && height <= MaxCanvasDimension && (long)width * height <= MaxCanvasArea;

    /// <summary>
    /// A sensible cap on working resolution for this device, used to keep interactive editing responsive on phones and
    /// low-memory machines.
    /// </summary>
    public int SuggestedMaxWorkingDimension => DeviceMemoryGb switch
    {
        <= 1 => 2048,
        <= 2 => 3000,
        <= 4 => 4096,
        _ => Math.Min(8192, MaxCanvasDimension),
    };
}

/// <summary>Where Bmage is running.</summary>
public enum ImageEnvironment
{
    Unknown = 0,
    /// <summary>Blazor WebAssembly, or another browser host: full browser API access.</summary>
    BrowserWebAssembly,
    /// <summary>Blazor Server: the browser is reachable only through JavaScript interop over a circuit.</summary>
    BrowserServer,
    /// <summary>Plain .NET with no browser: only the managed codecs and headless APIs work.</summary>
    Server,
    /// <summary>Server-side prerendering, before interactivity is established. No browser APIs are usable yet.</summary>
    Prerendering,
}

/// <summary>Supplies <see cref="ImageCapabilities"/>. The Blazor package provides a browser-backed implementation.</summary>
public interface IImageCapabilityProvider
{
    /// <summary>Detects capabilities, caching the result. Safe to call repeatedly.</summary>
    ValueTask<ImageCapabilities> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>Reports the fixed capabilities of managed .NET with no browser present.</summary>
public sealed class ManagedCapabilityProvider : IImageCapabilityProvider
{
    public static ManagedCapabilityProvider Instance { get; } = new();

    public ValueTask<ImageCapabilities> GetAsync(CancellationToken cancellationToken = default) => new(ImageCapabilities.Managed);
}
