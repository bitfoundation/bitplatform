using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Bit.Bmage.Codecs;
using Bit.Bmage.Geometry;
using Bit.Bmage.Memory;
using Bit.Bmage.Operations.Transforms;

namespace Bit.Bmage.Pipeline;

/// <summary>
/// The headless entry point: decode, run a pipeline, encode. Fully usable without any Blazor component. On WebAssembly
/// the browser-backed codec registry is injected by the Blazor package; elsewhere the managed codecs are used.
/// </summary>
public class ImageProcessor
{
    /// <summary>Creates a processor using the built-in managed codecs.</summary>
    public ImageProcessor() : this(ImageCodecRegistry.CreateDefault()) { }

    public ImageProcessor(ImageCodecRegistry codecs, IPixelAllocator? allocator = null, ImageLimits? limits = null)
    {
        Codecs = codecs ?? throw new ArgumentNullException(nameof(codecs));
        Allocator = allocator ?? PooledPixelAllocator.Shared;
        Limits = limits ?? ImageLimits.Default;
    }

    /// <summary>Registry used to find decoders and encoders.</summary>
    public ImageCodecRegistry Codecs { get; }

    /// <summary>Allocator used for decoded and intermediate buffers.</summary>
    public IPixelAllocator Allocator { get; }

    /// <summary>Safety limits applied to decoding and processing.</summary>
    public ImageLimits Limits { get; }

    /// <summary>
    /// The most threads one operation may spread its rows over, decoding included, or null for
    /// <see cref="Operations.OperationContext.DefaultMaxDegreeOfParallelism"/>. See
    /// <see cref="PipelineExecutionOptions.MaxDegreeOfParallelism"/>. In a batch every image in flight may use this
    /// many, so <see cref="BatchOptions.MaxConcurrency"/> times this is what the batch can occupy.
    /// </summary>
    public int? MaxDegreeOfParallelism
    {
        get;
        init => field = value is < 1 ? throw new ArgumentOutOfRangeException(nameof(MaxDegreeOfParallelism), "At least one thread is needed.") : value;
    }

    /// <summary>Reads dimensions and format from encoded bytes without decoding pixels. Returns null when unrecognised.</summary>
    public ImageInfo? Identify(ReadOnlySpan<byte> data) => Codecs.Identify(data);

    /// <summary>Decodes an encoded image. The caller owns the returned buffer.</summary>
    public async ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, DecodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new DecodeOptions { Limits = Limits };
        // Turning a photo upright as it is decoded is an operation like any other, on the processor's threads.
        if (options.MaxDegreeOfParallelism is null && MaxDegreeOfParallelism is { } threads) options = options with { MaxDegreeOfParallelism = threads };
        var format = ImageFormats.Detect(data.Span);
        if (format == ImageFormat.Unknown)
            throw new ImageDecodeException("The data does not look like a supported image format.");
        // No registry has an SVG decoder, so without this the refusal would read as a missing registration.
        if (format == ImageFormat.Svg)
            throw new ImageDecodeException(ImageFormats.SvgRefused);
        var decoder = Codecs.GetDecoder(format)
            ?? throw new ImageCapabilityException($"No decoder is registered for {format}. On WebAssembly, register the browser codecs; otherwise convert the image first.");
        return await decoder.DecodeAsync(data, options, Allocator, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Encodes an image, honouring quality, metadata policy and any target file size.</summary>
    public async ValueTask<EncodedImage> EncodeAsync(ImageBuffer image, ImageExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= ImageExportOptions.Png;
        var encoder = Codecs.GetEncoder(options.Format)
            ?? throw new ImageCapabilityException($"No encoder is registered for {options.Format} in this environment. Check ImageCapabilities before exporting, or choose another format.");
        if (options.MaxFileSize is not { } maxBytes) return await encoder.EncodeAsync(image, options, cancellationToken).ConfigureAwait(false);
        return await EncodeWithinSizeAsync(encoder, image, options, maxBytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for encoder settings that keep the file at or below <paramref name="maxBytes"/>: a bisection on quality
    /// first, then progressive downscaling when quality alone is not enough.
    /// </summary>
    private async ValueTask<EncodedImage> EncodeWithinSizeAsync(IImageEncoder encoder, ImageBuffer image, ImageExportOptions options, long maxBytes, CancellationToken ct)
    {
        if (maxBytes <= 0) throw new ArgumentOutOfRangeException(nameof(options), "MaxFileSize must be positive.");
        var lossy = options.Format.IsLossy();
        var minQuality = Math.Clamp(options.MinQuality, 0.05, options.Quality);
        EncodedImage? best = null;

        async ValueTask<EncodedImage> Try(ImageBuffer img, double quality)
            => await encoder.EncodeAsync(img, options with { Quality = quality, MaxFileSize = null }, ct).ConfigureAwait(false);

        // Stage 1: quality search at full size.
        if (lossy && options.MaxFileSizeStrategy != MaxFileSizeStrategy.DimensionsOnly)
        {
            var high = options.Quality;
            var result = await Try(image, high).ConfigureAwait(false);
            if (result.Length <= maxBytes) return result;
            best = result;
            var low = minQuality;
            var lowResult = await Try(image, low).ConfigureAwait(false);
            if (lowResult.Length <= maxBytes)
            {
                best = lowResult;
                // Bisect for the highest quality that still fits.
                for (var i = 0; i < 6; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var mid = (low + high) / 2;
                    var candidate = await Try(image, mid).ConfigureAwait(false);
                    if (candidate.Length <= maxBytes) { best = candidate; low = mid; }
                    else high = mid;
                }
                return best;
            }
            best = lowResult;
        }
        else if (options.MaxFileSizeStrategy == MaxFileSizeStrategy.QualityOnly)
        {
            var only = await Try(image, options.Quality).ConfigureAwait(false);
            if (only.Length <= maxBytes) return only;
            throw new ImageEncodeException($"Cannot reach {maxBytes} bytes with {options.Format} without changing dimensions (smallest was {only.Length} bytes).");
        }

        if (options.MaxFileSizeStrategy == MaxFileSizeStrategy.QualityOnly)
            throw new ImageEncodeException($"Cannot reach {maxBytes} bytes at quality {minQuality:0.##} (smallest was {best!.Length} bytes). Allow dimension changes or raise the limit.");

        // Stage 2: shrink dimensions. File size scales roughly with pixel count, so estimate then refine.
        // DimensionsOnly means exactly that: the requested quality is held fixed and only the size moves.
        var quality = lossy && options.MaxFileSizeStrategy != MaxFileSizeStrategy.DimensionsOnly
            ? minQuality
            : options.Quality;
        var scale = 1.0;
        var current = best ?? await Try(image, quality).ConfigureAwait(false);
        if (current.Length <= maxBytes) return current;

        for (var attempt = 0; attempt < 8; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            var ratio = maxBytes / (double)current.Length;
            scale *= Math.Clamp(Math.Sqrt(ratio) * 0.95, 0.3, 0.95);
            var w = Math.Max(1, (int)Math.Round(image.Width * scale));
            var h = Math.Max(1, (int)Math.Round(image.Height * scale));
            using var resized = new ResizeOperation(w, h, ResizeMode.Stretch, ResamplingFilter.Lanczos3)
                .Apply(image, new Operations.OperationContext(Allocator, Limits, ct, maxDegreeOfParallelism: MaxDegreeOfParallelism));
            current = await Try(resized, quality).ConfigureAwait(false);
            if (current.Length <= maxBytes) return current;
            if (w <= 16 || h <= 16) break;
        }
        throw new ImageEncodeException($"Cannot reach {maxBytes} bytes for this image (smallest attempt was {current.Length} bytes).");
    }

    /// <summary>Decodes, processes and encodes in one call. Nothing but the result is retained.</summary>
    public async ValueTask<EncodedImage> ProcessAsync(
        ReadOnlyMemory<byte> data,
        ImagePipeline pipeline,
        ImageExportOptions? exportOptions = null,
        DecodeOptions? decodeOptions = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        progress?.Report(0);
        using var decoded = await DecodeAsync(data, decodeOptions, cancellationToken).ConfigureAwait(false);
        progress?.Report(0.15);
        var pipelineProgress = progress is null ? null : new ScaledProgress(progress, 0.15, 0.85);
        using var processed = await pipeline.ExecuteAsync(decoded, new PipelineExecutionOptions
        {
            Allocator = Allocator,
            Limits = Limits,
            CancellationToken = cancellationToken,
            Progress = pipelineProgress,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
        }).ConfigureAwait(false);
        var result = await EncodeAsync(processed, exportOptions, cancellationToken).ConfigureAwait(false);
        progress?.Report(1);
        return result;
    }

    /// <summary>Runs a pipeline on an already decoded image.</summary>
    public ValueTask<ImageBuffer> ProcessAsync(ImageBuffer image, ImagePipeline pipeline, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(pipeline);
        return pipeline.ExecuteAsync(image, new PipelineExecutionOptions
        {
            Allocator = Allocator,
            Limits = Limits,
            CancellationToken = cancellationToken,
            Progress = progress,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
        });
    }


    /// <summary>
    /// Processes many images with bounded concurrency. Per-item failures are captured in the result rather than
    /// aborting the batch, unless <see cref="BatchOptions.StopOnFirstError"/> is set.
    /// </summary>
    /// <remarks>
    /// Every output is kept until the batch ends. For a batch too large for that, use
    /// <see cref="ProcessBatchStreamAsync(IEnumerable{BatchItem}, ImagePipeline, BatchOptions?, CancellationToken)"/>,
    /// which hands each result over as it completes, with <see cref="BatchItem.Deferred"/> items that are only read when
    /// their turn comes.
    /// </remarks>
    public async Task<BatchResult> ProcessBatchAsync(
        IEnumerable<BatchItem> items,
        ImagePipeline pipeline,
        BatchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(pipeline);
        options ??= BatchOptions.Default;
        var list = items.ToList();
        var results = new BatchItemResult[list.Count];
        var stopwatch = Stopwatch.StartNew();

        await foreach (var (index, result) in RunBatchAsync(ToAsync(list), list.Count, pipeline, options, cancellationToken).ConfigureAwait(false))
            results[index] = result;
        for (var i = 0; i < results.Length; i++)
            results[i] ??= BatchItemResult.Cancelled(list[i] ?? MissingItem);
        return new BatchResult(results, stopwatch.Elapsed);
    }

    /// <summary>What a result reports as its item when the caller passed null in its place.</summary>
    private static readonly BatchItem MissingItem = new(ReadOnlyMemory<byte>.Empty);

    /// <summary>
    /// Processes many images and hands over each result as soon as it is ready, in the order they finish, keeping none
    /// of them. With <see cref="BatchItem.Deferred"/> items and <see cref="BatchOptions.MaxMemoryBytes"/>, a batch of
    /// any length holds only the files and pixels of the images in flight.
    /// </summary>
    /// <remarks>
    /// A failure is reported as a result and the batch goes on, unless <see cref="BatchOptions.StopOnFirstError"/> is set:
    /// the stream then ends after the failed item and the images still in flight, and the items not started are not
    /// reported. Cancelling <paramref name="cancellationToken"/> ends the stream with an
    /// <see cref="OperationCanceledException"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// var items = files.Select(file => BatchItem.Deferred(ct => ReadAsync(file, ct), file.Name));
    /// await foreach (var result in processor.ProcessBatchStreamAsync(items, pipeline, new BatchOptions
    /// {
    ///     MaxConcurrency = 4,
    ///     MaxMemoryBytes = 512L * 1024 * 1024,
    ///     ExportOptions = new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.8 },
    /// }))
    /// {
    ///     if (result.Output is { } output) await SaveAsync(result.FileName, output.Data);
    /// }
    /// </code>
    /// </example>
    public IAsyncEnumerable<BatchItemResult> ProcessBatchStreamAsync(
        IEnumerable<BatchItem> items,
        ImagePipeline pipeline,
        BatchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        var total = items switch
        {
            ICollection<BatchItem> collection => collection.Count,
            IReadOnlyCollection<BatchItem> collection => collection.Count,
            _ => -1,
        };
        return Stream(ToAsync(items), total, pipeline, options, cancellationToken);
    }

    /// <summary>
    /// Processes images as an asynchronous source produces them, handing over each result as soon as it is ready. See
    /// <see cref="ProcessBatchStreamAsync(IEnumerable{BatchItem}, ImagePipeline, BatchOptions?, CancellationToken)"/>.
    /// The number of items is not known in advance, so <see cref="BatchProgress.Total"/> is -1.
    /// </summary>
    public IAsyncEnumerable<BatchItemResult> ProcessBatchStreamAsync(
        IAsyncEnumerable<BatchItem> items,
        ImagePipeline pipeline,
        BatchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        return Stream(items, -1, pipeline, options, cancellationToken);
    }

    private async IAsyncEnumerable<BatchItemResult> Stream(IAsyncEnumerable<BatchItem> items, int total, ImagePipeline pipeline, BatchOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        await foreach (var (_, result) in RunBatchAsync(items, total, pipeline, options ?? BatchOptions.Default, cancellationToken).ConfigureAwait(false))
            yield return result;
    }

    /// <summary>
    /// Runs the batch: <see cref="BatchOptions.MaxConcurrency"/> workers take items from the source one at a time, wait
    /// for the memory gate when there is one, and post each result, with the item's position in the source, as it
    /// completes.
    /// </summary>
    private async IAsyncEnumerable<(int Index, BatchItemResult Result)> RunBatchAsync(IAsyncEnumerable<BatchItem> items, int total, ImagePipeline pipeline, BatchOptions options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        // Cancelled once nobody reads the results any more, so a worker waiting to hand one over stops waiting.
        using var abandoned = new CancellationTokenSource();
        var token = linked.Token;
        var workerCount = WorkerCount(options.MaxConcurrency, total);
        // Bounded, so a worker hands its result over before taking the next item: a slow consumer slows the batch down
        // rather than finished images piling up.
        var results = Channel.CreateBounded<(int, BatchItemResult)>(new BoundedChannelOptions(workerCount)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait,
        });
        var gate = options.MaxMemoryBytes is { } budget ? new MemoryGate(budget) : null;
        var source = items.GetAsyncEnumerator(token);
        using var pull = new SemaphoreSlim(1, 1);
        var next = 0;
        var completed = 0;
        Exception? sourceFailure = null;

        async Task WorkAsync()
        {
            while (!token.IsCancellationRequested)
            {
                int index;
                BatchItem? item;
                await pull.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    try
                    {
                        if (!await source.MoveNextAsync().ConfigureAwait(false)) return;
                        index = next++;
                        // A null item fails on its own, like an unreadable file; the source itself is fine.
                        item = source.Current;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException || !token.IsCancellationRequested)
                    {
                        // The source failed, a timeout of its own included: that is the batch's error, the items in flight
                        // finish as cancelled, and nothing more is read from it.
                        sourceFailure ??= ex;
                        await linked.CancelAsync().ConfigureAwait(false);
                        return;
                    }
                }
                finally
                {
                    pull.Release();
                }
                var result = await ProcessItemAsync(item, index, pipeline, options, gate, linked).ConfigureAwait(false);
                if (!result.WasCancelled) options.Progress?.Report(new BatchProgress(Interlocked.Increment(ref completed), total, result));
                await results.Writer.WriteAsync((index, result), abandoned.Token).ConfigureAwait(false);
            }
        }

        var workers = new Task[workerCount];
        for (var i = 0; i < workers.Length; i++) workers[i] = Task.Run(WorkAsync, CancellationToken.None);
        _ = Task.WhenAll(workers).ContinueWith(
            // A cancelled batch simply ends; the source failing is the batch's error.
            done => results.Writer.TryComplete(sourceFailure ?? done.Exception?.Flatten().InnerExceptions.FirstOrDefault(e => e is not OperationCanceledException)),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        try
        {
            await foreach (var result in results.Reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
                yield return result;
        }
        finally
        {
            // The consumer stopped early, or the batch is over: either way no worker may still be reading the source or
            // waiting to hand over a result.
            await abandoned.CancelAsync().ConfigureAwait(false);
            await linked.CancelAsync().ConfigureAwait(false);
            try { await Task.WhenAll(workers).ConfigureAwait(false); }
            catch { /* reported through the channel */ }
            await source.DisposeAsync().ConfigureAwait(false);
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// The workers a batch runs: <see cref="BatchOptions.MaxConcurrency"/>, but never more than there are items, nor
    /// more than 256, which already outnumbers the cores and the memory of any machine running a batch.
    /// </summary>
    internal static int WorkerCount(int maxConcurrency, int total)
    {
        var count = Math.Clamp(maxConcurrency, 1, 256);
        return total >= 0 ? Math.Min(count, Math.Max(1, total)) : count;
    }

    private async Task<BatchItemResult> ProcessItemAsync(BatchItem? item, int index, ImagePipeline pipeline, BatchOptions options, MemoryGate? gate, CancellationTokenSource linked)
    {
        var token = linked.Token;
        var watch = Stopwatch.StartNew();
        var reported = item ?? MissingItem;
        var inputBytes = (long)reported.Data.Length;
        var reserved = 0L;
        try
        {
            if (item is null) throw new ArgumentNullException(nameof(item), $"Batch item {index} is null.");
            var data = item.Loader is { } load ? await load(token).ConfigureAwait(false) : item.Data;
            inputBytes = data.Length;
            if (gate is not null) reserved = await gate.AcquireAsync(EstimateBatchMemory(data.Span, pipeline, options), token).ConfigureAwait(false);
            var encoded = await ProcessAsync(data, pipeline, options.ExportOptions, options.DecodeOptions, null, token).ConfigureAwait(false);
            return BatchItemResult.Success(item, encoded, watch.Elapsed, inputBytes);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return BatchItemResult.Cancelled(reported, inputBytes);
        }
        catch (Exception ex)
        {
            if (options.StopOnFirstError) await linked.CancelAsync().ConfigureAwait(false);
            return BatchItemResult.Failure(reported, ex, watch.Elapsed, inputBytes);
        }
        finally
        {
            if (reserved > 0) gate!.Release(reserved);
        }
    }

    /// <summary>
    /// What one image of a batch is expected to hold at its peak: its file, its decoded pixels, the pipeline's
    /// working buffers and an allowance for encoding the result. Read from the file's header, without decoding.
    /// </summary>
    internal long EstimateBatchMemory(ReadOnlySpan<byte> data, ImagePipeline pipeline, BatchOptions options)
    {
        // A file whose header cannot be read is assumed to decode to ten times its size, and at least 16 MB.
        if (Identify(data) is not { } info) return data.Length + Math.Max(16L << 20, data.Length * 10L);
        var size = info.DisplaySize;
        var decoded = 4L * size.Width * size.Height;
        try
        {
            var estimate = pipeline.EstimateMemory(size);
            return data.Length + decoded + estimate.PeakBytes + estimate.OutputBytes / 2;
        }
        catch (ArgumentException)
        {
            // The pipeline cannot run on this image; it will fail fast, holding little more than the decoded pixels.
            return data.Length + decoded * 2;
        }
    }

    private static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> items)
    {
        foreach (var item in items) yield return item;
        await Task.CompletedTask.ConfigureAwait(false);
    }
}

/// <summary>
/// Admits work by its memory: a request waits until what it needs fits beside what is already admitted, in the order
/// requests arrive. A request larger than the whole capacity is admitted when nothing else is, so it runs alone instead
/// of never.
/// </summary>
internal sealed class MemoryGate(long capacity)
{
    private readonly object _lock = new();
    private readonly LinkedList<Waiter> _queue = new();
    private long _used;
    private int _admitted;

    private sealed class Waiter(long bytes)
    {
        public long Bytes { get; } = bytes;
        public TaskCompletionSource<long> Admission { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationTokenRegistration Registration { get; set; }
    }

    /// <summary>Bytes admitted and not yet released.</summary>
    public long Used { get { lock (_lock) return _used; } }

    /// <summary>Waits until <paramref name="bytes"/> can be admitted, and returns them to pass to <see cref="Release"/>.</summary>
    public Task<long> AcquireAsync(long bytes, CancellationToken cancellationToken)
    {
        bytes = Math.Max(1, bytes);
        lock (_lock)
        {
            if (_queue.Count == 0 && Fits(bytes))
            {
                Admit(bytes);
                return Task.FromResult(bytes);
            }
            var waiter = new Waiter(bytes);
            var node = _queue.AddLast(waiter);
            if (cancellationToken.CanBeCanceled)
            {
                waiter.Registration = cancellationToken.Register(() =>
                {
                    lock (_lock)
                    {
                        if (node.List is null) return;
                        _queue.Remove(node);
                        waiter.Admission.TrySetCanceled(cancellationToken);
                        // The head may have been what held back those behind it.
                        Drain();
                    }
                });
            }
            return waiter.Admission.Task;
        }
    }

    /// <summary>Returns bytes an earlier <see cref="AcquireAsync"/> admitted.</summary>
    public void Release(long bytes)
    {
        lock (_lock)
        {
            _used -= bytes;
            _admitted--;
            Drain();
        }
    }

    private bool Fits(long bytes) => _admitted == 0 || _used + bytes <= capacity;

    private void Admit(long bytes)
    {
        _used += bytes;
        _admitted++;
    }

    private void Drain()
    {
        while (_queue.First is { } head && Fits(head.Value.Bytes))
        {
            _queue.RemoveFirst();
            var waiter = head.Value;
            Admit(waiter.Bytes);
            // Unregister rather than dispose: disposing waits for a callback that may be blocked on this lock.
            waiter.Registration.Unregister();
            waiter.Admission.TrySetResult(waiter.Bytes);
        }
    }
}

/// <summary>Scales a child progress range into a parent's [0,1].</summary>
internal sealed class ScaledProgress : IProgress<double>
{
    private readonly IProgress<double> _inner;
    private readonly double _offset, _scale;
    public ScaledProgress(IProgress<double> inner, double offset, double scale) { _inner = inner; _offset = offset; _scale = scale; }
    public void Report(double value) => _inner.Report(_offset + Math.Clamp(value, 0, 1) * _scale);
}

/// <summary>One input for a batch run.</summary>
/// <param name="Data">The encoded file. Empty for an item made by <see cref="Deferred"/>.</param>
/// <param name="FileName">A name to report the item by.</param>
/// <param name="Tag">Anything the caller wants back with the result.</param>
public sealed record BatchItem(ReadOnlyMemory<byte> Data, string? FileName = null, object? Tag = null)
{
    /// <summary>
    /// An item whose file is read only when the batch reaches it, so a batch of a thousand photos never holds a
    /// thousand files: only those of the images in flight.
    /// </summary>
    public static BatchItem Deferred(Func<CancellationToken, ValueTask<ReadOnlyMemory<byte>>> load, string? fileName = null, object? tag = null)
    {
        ArgumentNullException.ThrowIfNull(load);
        return new BatchItem(ReadOnlyMemory<byte>.Empty, fileName, tag) { Loader = load };
    }

    /// <summary>Reads the file of a deferred item.</summary>
    internal Func<CancellationToken, ValueTask<ReadOnlyMemory<byte>>>? Loader { get; init; }

    /// <summary>True when the file is read only when the batch reaches the item.</summary>
    public bool IsDeferred => Loader is not null;
}

/// <summary>Options for a batch run.</summary>
public sealed record BatchOptions
{
    public static BatchOptions Default { get; } = new();

    /// <summary>
    /// Maximum images processed at once, from 1 to 256. Keep low on memory constrained devices, or let
    /// <see cref="MaxMemoryBytes"/> decide. Default 2.
    /// </summary>
    public int MaxConcurrency { get; init; } = 2;

    /// <summary>
    /// The most memory the images in flight may be expected to hold together, or null for no limit beyond
    /// <see cref="MaxConcurrency"/>. Each image's need is estimated from its file's header before it is decoded: the
    /// file, its pixels and the pipeline's working buffers (see <see cref="ImagePipeline.EstimateMemory"/>). An image
    /// waits until its estimate fits beside the others, so small photos run several at a time and a large one runs
    /// with room to spare; an image larger than the whole budget runs alone.
    /// </summary>
    /// <remarks>
    /// This schedules by estimate; to enforce a hard ceiling on what is actually allocated, give the processor a
    /// <see cref="Memory.BudgetedPixelAllocator"/>.
    /// </remarks>
    public long? MaxMemoryBytes
    {
        get;
        init => field = value is <= 0 ? throw new ArgumentOutOfRangeException(nameof(MaxMemoryBytes), "A memory budget must be positive.") : value;
    }

    /// <summary>Export settings applied to every image.</summary>
    public ImageExportOptions? ExportOptions { get; init; }

    /// <summary>Decode settings applied to every image.</summary>
    public DecodeOptions? DecodeOptions { get; init; }

    /// <summary>Receives an update as each image finishes.</summary>
    public IProgress<BatchProgress>? Progress { get; init; }

    /// <summary>Cancel the whole batch when any image fails. Default false.</summary>
    public bool StopOnFirstError { get; init; }
}

/// <summary>Progress update from a batch run.</summary>
/// <param name="Completed">Images finished so far.</param>
/// <param name="Total">Images in the batch, or -1 when they come from a stream whose length is not known.</param>
/// <param name="Last">The image that just finished.</param>
public readonly record struct BatchProgress(int Completed, int Total, BatchItemResult Last)
{
    /// <summary>The share of the batch finished, or 0 when the total is not known.</summary>
    public double Fraction => Total < 0 ? 0 : Total == 0 ? 1 : Completed / (double)Total;
}

/// <summary>The outcome for one image in a batch.</summary>
public sealed class BatchItemResult
{
    private BatchItemResult(BatchItem item, EncodedImage? output, Exception? error, bool cancelled, TimeSpan duration, long inputBytes)
    {
        Item = item; Output = output; Error = error; WasCancelled = cancelled; Duration = duration; InputBytes = inputBytes;
    }

    public static BatchItemResult Success(BatchItem item, EncodedImage output, TimeSpan duration, long? inputBytes = null) => new(item, output, null, false, duration, inputBytes ?? item.Data.Length);
    public static BatchItemResult Failure(BatchItem item, Exception error, TimeSpan duration, long? inputBytes = null) => new(item, null, error, false, duration, inputBytes ?? item.Data.Length);
    public static BatchItemResult Cancelled(BatchItem item, long? inputBytes = null) => new(item, null, null, true, TimeSpan.Zero, inputBytes ?? item.Data.Length);

    public BatchItem Item { get; }
    public EncodedImage? Output { get; }
    public Exception? Error { get; }
    public bool WasCancelled { get; }
    public TimeSpan Duration { get; }
    public bool IsSuccess => Output is not null;
    public string? FileName => Item.FileName;

    /// <summary>The size of the item's file, once read: 0 for a deferred item the batch never reached.</summary>
    public long InputBytes { get; }
}

/// <summary>The aggregate outcome of a batch run.</summary>
public sealed class BatchResult
{
    public BatchResult(IReadOnlyList<BatchItemResult> results, TimeSpan duration)
    {
        Results = results;
        Duration = duration;
    }

    public IReadOnlyList<BatchItemResult> Results { get; }
    public TimeSpan Duration { get; }
    public IEnumerable<BatchItemResult> Succeeded => Results.Where(r => r.IsSuccess);
    public IEnumerable<BatchItemResult> Failed => Results.Where(r => r.Error is not null);
    public int SuccessCount => Results.Count(r => r.IsSuccess);
    public int FailureCount => Results.Count(r => r.Error is not null);
    public bool AllSucceeded => Results.All(r => r.IsSuccess);
    /// <summary>Total bytes of all successful outputs.</summary>
    public long TotalOutputBytes => Results.Where(r => r.Output is not null).Sum(r => r.Output!.Length);
    /// <summary>Total bytes of all inputs that were read.</summary>
    public long TotalInputBytes => Results.Sum(r => r.InputBytes);
}
