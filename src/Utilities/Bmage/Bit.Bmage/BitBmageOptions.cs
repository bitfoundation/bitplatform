using Bit.Bmage.Memory;

namespace Bit.Bmage;

/// <summary>Options for the Bmage services.</summary>
public sealed class BitBmageOptions
{
    /// <summary>Safety limits applied to decoding and processing. Defaults to <see cref="ImageLimits.Default"/>.</summary>
    public ImageLimits Limits { get; set; } = ImageLimits.Default;

    /// <summary>Allocator used for pixel buffers. Defaults to the shared pooled allocator.</summary>
    public IPixelAllocator? Allocator { get; set; }

    /// <summary>Maximum encoded bytes accepted for a single uploaded image. Defaults to 64 MB.</summary>
    public long MaxSourceBytes { get; set; } = 64L * 1024 * 1024;

    /// <summary>
    /// Largest working resolution used for interactive editing. Larger images are edited through a downscaled proxy and
    /// the edit is replayed at full resolution on export. Null uses the device-appropriate default.
    /// </summary>
    public int? MaxInteractiveDimension { get; set; }

    /// <summary>
    /// The most threads one operation may spread its rows over, for the processors, loaders and editors these services
    /// create. Null uses <see cref="Operations.OperationContext.DefaultMaxDegreeOfParallelism"/>, one: a server already
    /// spends its cores on concurrent requests and circuits. Set it to <see cref="Environment.ProcessorCount"/> where one
    /// image at a time should use every core, such as a desktop app, a background job or a WebAssembly app built with
    /// <c>WasmEnableThreads</c>. Pixels are identical whatever the value.
    /// </summary>
    public int? MaxDegreeOfParallelism
    {
        get;
        set => field = value is < 1 ? throw new ArgumentOutOfRangeException(nameof(MaxDegreeOfParallelism), "At least one thread is needed.") : value;
    }

    /// <summary>
    /// The most Web Workers the browser codecs may use for decoding, encoding and building the editor's working copy, so
    /// that work leaves the page's thread and runs on several cores at once. Null uses one per spare core, at most four;
    /// 0 keeps it all on the page's thread. Wherever workers are unavailable, or a Content Security Policy blocks them
    /// (allow <c>worker-src 'self'</c>), the work runs on the page's thread without any setting.
    /// </summary>
    public int? MaxCodecWorkers
    {
        get;
        set => field = value is < 0 ? throw new ArgumentOutOfRangeException(nameof(MaxCodecWorkers), "The number of workers cannot be negative.") : value;
    }
}
