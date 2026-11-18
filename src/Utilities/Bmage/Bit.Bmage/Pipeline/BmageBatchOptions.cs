namespace Bit.Bmage;

/// <summary>Options for a batch run.</summary>
public sealed record BmageBatchOptions
{
    public static BmageBatchOptions Default { get; } = new();

    /// <summary>
    /// Maximum images processed at once, from 1 to 256. Keep low on memory constrained devices, or let
    /// <see cref="MaxMemoryBytes"/> decide. Default 2.
    /// </summary>
    public int MaxConcurrency { get; init; } = 2;

    /// <summary>
    /// The most memory the images in flight may be expected to hold together, or null for no limit beyond
    /// <see cref="MaxConcurrency"/>. Each image's need is estimated from its file's header before it is decoded: the
    /// file, its pixels and the pipeline's working buffers (see <see cref="BmageImagePipeline.EstimateMemory"/>). An image
    /// waits until its estimate fits beside the others, so small photos run several at a time and a large one runs
    /// with room to spare; an image larger than the whole budget runs alone.
    /// </summary>
    /// <remarks>
    /// This schedules by estimate; to enforce a hard ceiling on what is actually allocated, give the processor a
    /// <see cref="BmageBudgetedPixelAllocator"/>.
    /// </remarks>
    public long? MaxMemoryBytes
    {
        get;
        init => field = value is <= 0 ? throw new ArgumentOutOfRangeException(nameof(MaxMemoryBytes), "A memory budget must be positive.") : value;
    }

    /// <summary>Export settings applied to every image.</summary>
    public BmageImageExportOptions? ExportOptions { get; init; }

    /// <summary>Decode settings applied to every image.</summary>
    public BmageDecodeOptions? DecodeOptions { get; init; }

    /// <summary>Receives an update as each image finishes. An image a cancellation stops is not reported.</summary>
    public IProgress<BmageBatchProgress>? Progress { get; init; }

    /// <summary>Cancel the whole batch when any image fails. Default false.</summary>
    public bool StopOnFirstError { get; init; }
}
