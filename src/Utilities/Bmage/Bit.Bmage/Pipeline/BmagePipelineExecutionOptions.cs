namespace Bit.Bmage;

/// <summary>Options controlling a pipeline run.</summary>
public sealed record BmagePipelineExecutionOptions
{
    public static BmagePipelineExecutionOptions Default { get; } = new();

    /// <summary>Allocator for intermediate buffers.</summary>
    public IBmagePixelAllocator Allocator { get; init; } = BmagePooledPixelAllocator.Shared;

    /// <summary>Limits applied to intermediate and output buffers.</summary>
    public BmageImageLimits Limits { get; init; } = BmageImageLimits.Default;

    /// <summary>Cancellation token checked between and inside operations.</summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>Receives values in [0,1] as the run progresses.</summary>
    public IProgress<double>? Progress { get; init; }

    /// <summary>Run the optimiser before executing. Default true.</summary>
    public bool Optimize { get; init; } = true;

    /// <summary>
    /// The most threads an operation may spread its rows over. Null uses
    /// <see cref="BmageOperationContext.DefaultMaxDegreeOfParallelism"/>, one, which keeps a run on the calling thread. The
    /// pixels are identical whatever the value; set <see cref="Environment.ProcessorCount"/> to finish one large image
    /// sooner where nothing else needs the cores, but not on a server already busy with many requests at once.
    /// </summary>
    public int? MaxDegreeOfParallelism
    {
        get;
        init => field = value is < 1 ? throw new ArgumentOutOfRangeException(nameof(MaxDegreeOfParallelism), "At least one thread is needed.") : value;
    }

    /// <summary>Yield to the scheduler between operations so a single-threaded UI can repaint. Default true.</summary>
    public bool YieldBetweenOperations { get; init; } = true;

    /// <summary>
    /// How to yield between operations. On WebAssembly this should be a real task yield so the browser can paint;
    /// the default is <see cref="Task.Yield"/>.
    /// </summary>
    public Func<ValueTask>? YieldCallback { get; init; }

    internal ValueTask Yield() => YieldCallback is { } cb ? cb() : DefaultYield();

    private static async ValueTask DefaultYield() => await Task.Yield();
}
