using Bit.Bmage.Memory;

namespace Bit.Bmage.Operations;

/// <summary>
/// Ambient services and policies passed to an operation while it executes: memory allocation, limits,
/// cancellation, progress reporting, how many threads it may use and whether the source buffer may be modified in place.
/// </summary>
public sealed class OperationContext
{
    /// <summary>
    /// How many threads an operation uses when nothing else is configured: one, the thread that runs it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Spreading rows over more threads is opted into, by setting <see cref="BitBmageOptions.MaxDegreeOfParallelism"/>,
    /// <see cref="Pipeline.ImageProcessor.MaxDegreeOfParallelism"/>,
    /// <see cref="Pipeline.PipelineExecutionOptions.MaxDegreeOfParallelism"/> or the context's own. A server already
    /// runs requests, batch items and editor circuits side by side, and every one of them fanning out over every core
    /// would multiply the threads it competes for and hold a thread-pool thread waiting on each run; a desktop app, a
    /// command-line tool or a background job processing one image at a time is where more threads pay.
    /// </para>
    /// <para>
    /// Blazor WebAssembly runs .NET on the page's own thread unless the application is built with
    /// <c>WasmEnableThreads</c>, and waiting for work handed to another thread is not possible there; only an
    /// application built with threads should set more than one.
    /// </para>
    /// </remarks>
    public const int DefaultMaxDegreeOfParallelism = 1;

    /// <summary>A default context using the shared pooled allocator, default limits, one thread, no cancellation and no progress.</summary>
    public static OperationContext Default { get; } = new();

    public OperationContext(
        IPixelAllocator? allocator = null,
        ImageLimits? limits = null,
        CancellationToken cancellationToken = default,
        IProgress<double>? progress = null,
        bool canMutateSource = false,
        int? maxDegreeOfParallelism = null)
    {
        if (maxDegreeOfParallelism is < 1) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "At least one thread is needed.");
        Allocator = allocator ?? PooledPixelAllocator.Shared;
        Limits = limits ?? ImageLimits.Default;
        CancellationToken = cancellationToken;
        Progress = progress;
        CanMutateSource = canMutateSource;
        MaxDegreeOfParallelism = maxDegreeOfParallelism ?? DefaultMaxDegreeOfParallelism;
    }

    /// <summary>Allocator used for intermediate and output buffers.</summary>
    public IPixelAllocator Allocator { get; }

    /// <summary>Limits applied to any buffer allocated by an operation.</summary>
    public ImageLimits Limits { get; }

    /// <summary>Token that cancels the operation. Operations check it periodically (typically every few rows).</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Optional progress sink receiving values in [0,1] for the current operation.</summary>
    public IProgress<double>? Progress { get; }

    /// <summary>
    /// When true the operation owns the source buffer and may modify it in place and return it as the result.
    /// When false the source must be left untouched.
    /// </summary>
    public bool CanMutateSource { get; }

    /// <summary>
    /// The most threads the operation may spread its rows over. Built-in operations whose rows are independent split
    /// the image into bands and produce exactly the pixels they produce on one thread. Defaults to
    /// <see cref="DefaultMaxDegreeOfParallelism"/>.
    /// </summary>
    public int MaxDegreeOfParallelism { get; }

    /// <summary>Allocates a buffer using this context's allocator and limits.</summary>
    public ImageBuffer Allocate(int width, int height, bool clear = false) => ImageBuffer.Create(width, height, Allocator, clear, Limits);

    /// <summary>Throws <see cref="OperationCanceledException"/> when cancellation has been requested.</summary>
    public void ThrowIfCancellationRequested() => CancellationToken.ThrowIfCancellationRequested();

    /// <summary>Reports progress for the current operation.</summary>
    public void ReportProgress(double fraction) => Progress?.Report(fraction < 0 ? 0 : fraction > 1 ? 1 : fraction);

    /// <summary>Returns a copy of the context with a different mutation policy.</summary>
    public OperationContext WithMutation(bool canMutateSource)
        => canMutateSource == CanMutateSource ? this : new OperationContext(Allocator, Limits, CancellationToken, Progress, canMutateSource, MaxDegreeOfParallelism);

    /// <summary>Returns a copy of the context with a different progress sink.</summary>
    public OperationContext WithProgress(IProgress<double>? progress)
        => new(Allocator, Limits, CancellationToken, progress, CanMutateSource, MaxDegreeOfParallelism);

    /// <summary>Returns a copy of the context with a different cancellation token.</summary>
    public OperationContext WithCancellation(CancellationToken cancellationToken)
        => new(Allocator, Limits, cancellationToken, Progress, CanMutateSource, MaxDegreeOfParallelism);

    /// <summary>Returns a copy of the context that may use at most <paramref name="maxDegreeOfParallelism"/> threads.</summary>
    public OperationContext WithParallelism(int maxDegreeOfParallelism)
        => maxDegreeOfParallelism == MaxDegreeOfParallelism ? this : new(Allocator, Limits, CancellationToken, Progress, CanMutateSource, maxDegreeOfParallelism);
}
