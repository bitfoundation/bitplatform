using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Bit.Bmage;

/// <summary>
/// The headless entry point: decode, run a pipeline, encode. Fully usable without any Blazor component. On WebAssembly
/// the browser-backed codec registry is injected by the Blazor package; elsewhere the managed codecs are used.
/// </summary>
public class BmageImageProcessor
{
    /// <summary>Creates a processor using the built-in managed codecs.</summary>
    public BmageImageProcessor() : this(BmageImageCodecRegistry.CreateDefault()) { }

    public BmageImageProcessor(BmageImageCodecRegistry codecs, IBmagePixelAllocator? allocator = null, BmageImageLimits? limits = null)
    {
        Codecs = codecs ?? throw new ArgumentNullException(nameof(codecs));
        Allocator = allocator ?? BmagePooledPixelAllocator.Shared;
        Limits = limits ?? BmageImageLimits.Default;
    }

    /// <summary>Registry used to find decoders and encoders.</summary>
    public BmageImageCodecRegistry Codecs { get; }

    /// <summary>Allocator used for decoded and intermediate buffers.</summary>
    public IBmagePixelAllocator Allocator { get; }

    /// <summary>Safety limits applied to decoding and processing.</summary>
    public BmageImageLimits Limits { get; }

    /// <summary>
    /// The most threads one operation may spread its rows over, decoding included, or null for
    /// <see cref="BmageOperationContext.DefaultMaxDegreeOfParallelism"/>. See
    /// <see cref="BmagePipelineExecutionOptions.MaxDegreeOfParallelism"/>. In a batch every image in flight may use this
    /// many, so <see cref="BmageBatchOptions.MaxConcurrency"/> times this is what the batch can occupy.
    /// </summary>
    public int? MaxDegreeOfParallelism
    {
        get;
        init => field = value is < 1 ? throw new ArgumentOutOfRangeException(nameof(MaxDegreeOfParallelism), "At least one thread is needed.") : value;
    }

    /// <summary>Reads dimensions and format from encoded bytes without decoding pixels. Returns null when unrecognised.</summary>
    public BmageImageInfo? Identify(ReadOnlySpan<byte> data) => Codecs.Identify(data);

    /// <summary>Decodes an encoded image. The caller owns the returned buffer.</summary>
    public async ValueTask<BmageImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, BmageDecodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new BmageDecodeOptions { Limits = Limits };
        // Turning a photo upright as it is decoded is an operation like any other, on the processor's threads.
        if (options.MaxDegreeOfParallelism is null && MaxDegreeOfParallelism is { } threads) options = options with { MaxDegreeOfParallelism = threads };
        var format = BmageImageFormats.Detect(data.Span);
        if (format == BmageImageFormat.Unknown)
            throw new BmageImageDecodeException("The data does not look like a supported image format.");
        // No registry has an SVG decoder, so without this the refusal would read as a missing registration.
        if (format == BmageImageFormat.Svg)
            throw new BmageImageDecodeException(BmageImageFormats.SvgRefused);
        var decoder = Codecs.GetDecoder(format)
            ?? throw new BmageImageCapabilityException($"No decoder is registered for {format}. On WebAssembly, register the browser codecs; otherwise convert the image first.");
        return await decoder.DecodeAsync(data, options, Allocator, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Encodes an image, honouring quality, metadata policy and any target file size.</summary>
    public async ValueTask<BmageEncodedImage> EncodeAsync(BmageImageBuffer image, BmageImageExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= BmageImageExportOptions.Png;
        var encoder = Codecs.GetEncoder(options.Format)
            ?? throw new BmageImageCapabilityException($"No encoder is registered for {options.Format} in this environment. Check BmageImageCapabilities before exporting, or choose another format.");
        if (options.MaxFileSize is not { } maxBytes) return await encoder.EncodeAsync(image, options, cancellationToken).ConfigureAwait(false);
        return await EncodeWithinSizeAsync(encoder, image, options, maxBytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for encoder settings that keep the file at or below <paramref name="maxBytes"/>: a bisection on quality
    /// first, then progressive downscaling when quality alone is not enough.
    /// </summary>
    private async ValueTask<BmageEncodedImage> EncodeWithinSizeAsync(IBmageImageEncoder encoder, BmageImageBuffer image, BmageImageExportOptions options, long maxBytes, CancellationToken ct)
    {
        if (maxBytes <= 0) throw new ArgumentOutOfRangeException(nameof(options), "MaxFileSize must be positive.");
        var lossy = options.Format.IsLossy() && !options.EncodesLosslessly;
        var minQuality = Math.Clamp(options.MinQuality, 0.05, options.Quality);
        BmageEncodedImage? best = null;

        async ValueTask<BmageEncodedImage> Try(BmageImageBuffer img, double quality)
            => await encoder.EncodeAsync(img, options with { Quality = quality, MaxFileSize = null }, ct).ConfigureAwait(false);

        // Stage 1: quality search at full size.
        if (lossy && options.MaxFileSizeStrategy != BmageMaxFileSizeStrategy.DimensionsOnly)
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
        else if (options.MaxFileSizeStrategy == BmageMaxFileSizeStrategy.QualityOnly)
        {
            var only = await Try(image, options.Quality).ConfigureAwait(false);
            if (only.Length <= maxBytes) return only;
            throw new BmageImageEncodeException($"Cannot reach {maxBytes} bytes with {options.Format} without changing dimensions (smallest was {only.Length} bytes).");
        }

        if (options.MaxFileSizeStrategy == BmageMaxFileSizeStrategy.QualityOnly)
            throw new BmageImageEncodeException($"Cannot reach {maxBytes} bytes at quality {minQuality:0.##} (smallest was {best!.Length} bytes). Allow dimension changes or raise the limit.");

        // Stage 2: shrink dimensions. File size scales roughly with pixel count, so estimate then refine.
        // DimensionsOnly means exactly that: the requested quality is held fixed and only the size moves.
        var quality = lossy && options.MaxFileSizeStrategy != BmageMaxFileSizeStrategy.DimensionsOnly
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
            using var resized = new BmageResizeOperation(w, h, BmageResizeMode.Stretch, BmageResamplingFilter.Lanczos3)
                .Apply(image, new BmageOperationContext(Allocator, Limits, ct, maxDegreeOfParallelism: MaxDegreeOfParallelism));
            current = await Try(resized, quality).ConfigureAwait(false);
            if (current.Length <= maxBytes) return current;
            if (w <= 16 || h <= 16) break;
        }
        throw new BmageImageEncodeException($"Cannot reach {maxBytes} bytes for this image (smallest attempt was {current.Length} bytes).");
    }

    /// <summary>Decodes, processes and encodes in one call. Nothing but the result is retained.</summary>
    public async ValueTask<BmageEncodedImage> ProcessAsync(
        ReadOnlyMemory<byte> data,
        BmageImagePipeline pipeline,
        BmageImageExportOptions? exportOptions = null,
        BmageDecodeOptions? decodeOptions = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        progress?.Report(0);
        using var decoded = await DecodeAsync(data, decodeOptions, cancellationToken).ConfigureAwait(false);
        progress?.Report(0.15);
        var pipelineProgress = progress is null ? null : new BmageScaledProgress(progress, 0.15, 0.85);
        using var processed = await pipeline.ExecuteAsync(decoded, new BmagePipelineExecutionOptions
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
    public ValueTask<BmageImageBuffer> ProcessAsync(BmageImageBuffer image, BmageImagePipeline pipeline, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(pipeline);
        return pipeline.ExecuteAsync(image, new BmagePipelineExecutionOptions
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
    /// aborting the batch, unless <see cref="BmageBatchOptions.StopOnFirstError"/> is set.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cancelling <paramref name="cancellationToken"/> stops the batch without losing what it finished: it throws a
    /// <see cref="BmageBatchCancelledException"/>, an <see cref="OperationCanceledException"/> like any other cancelled
    /// method's, whose <see cref="BmageBatchCancelledException.Result"/> keeps every image that completed and reports the
    /// ones in flight and the ones never started as <see cref="BmageBatchItemResult.WasCancelled"/>.
    /// </para>
    /// <para>
    /// Every output is kept until the batch ends. For a batch too large for that, use
    /// <see cref="ProcessBatchStreamAsync(IEnumerable{BmageBatchItem}, BmageImagePipeline, BmageBatchOptions?, CancellationToken)"/>,
    /// which hands each result over as it completes, with <see cref="BmageBatchItem.Deferred"/> items that are only read when
    /// their turn comes.
    /// </para>
    /// </remarks>
    public async Task<BmageBatchResult> ProcessBatchAsync(
        IEnumerable<BmageBatchItem> items,
        BmageImagePipeline pipeline,
        BmageBatchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(pipeline);
        options ??= BmageBatchOptions.Default;
        var list = items.ToList();
        var results = new BmageBatchItemResult[list.Count];
        var stopwatch = Stopwatch.StartNew();

        var stopped = false;
        try
        {
            await foreach (var (index, item) in RunBatchAsync(ToAsync(list), list.Count, pipeline, options, cancellationToken).ConfigureAwait(false))
                results[index] = item;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller stopped the batch; every result that came in before is still in the array.
            stopped = true;
        }
        for (var i = 0; i < results.Length; i++)
            results[i] ??= BmageBatchItemResult.Cancelled(list[i] ?? MissingItem);
        var result = new BmageBatchResult(results, stopwatch.Elapsed)
        {
            // A token cancelled as the last image finished stopped nothing.
            WasCancelled = stopped && results.Any(r => r.WasCancelled),
        };
        if (result.WasCancelled) throw new BmageBatchCancelledException(result, cancellationToken);
        return result;
    }

    /// <summary>What a result reports as its item when the caller passed null in its place.</summary>
    private static readonly BmageBatchItem MissingItem = new(ReadOnlyMemory<byte>.Empty);

    /// <summary>
    /// Processes many images and hands over each result as soon as it is ready, in the order they finish, keeping none
    /// of them. With <see cref="BmageBatchItem.Deferred"/> items and <see cref="BmageBatchOptions.MaxMemoryBytes"/>, a batch of
    /// any length holds only the files and pixels of the images in flight.
    /// </summary>
    /// <remarks>
    /// A failure is reported as a result and the batch goes on, unless <see cref="BmageBatchOptions.StopOnFirstError"/> is set:
    /// the stream then ends after the failed item and the images still in flight, and the items not started are not
    /// reported. Cancelling <paramref name="cancellationToken"/> ends the stream with an
    /// <see cref="OperationCanceledException"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// var items = files.Select(file => BmageBatchItem.Deferred(ct => ReadAsync(file, ct), file.Name));
    /// await foreach (var result in processor.ProcessBatchStreamAsync(items, pipeline, new BmageBatchOptions
    /// {
    ///     MaxConcurrency = 4,
    ///     MaxMemoryBytes = 512L * 1024 * 1024,
    ///     ExportOptions = new BmageImageExportOptions { Format = BmageImageFormat.Jpeg, Quality = 0.8 },
    /// }))
    /// {
    ///     if (result.Output is { } output) await SaveAsync(result.FileName, output.Data);
    /// }
    /// </code>
    /// </example>
    public IAsyncEnumerable<BmageBatchItemResult> ProcessBatchStreamAsync(
        IEnumerable<BmageBatchItem> items,
        BmageImagePipeline pipeline,
        BmageBatchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        var total = items switch
        {
            ICollection<BmageBatchItem> collection => collection.Count,
            IReadOnlyCollection<BmageBatchItem> collection => collection.Count,
            _ => -1,
        };
        return Stream(ToAsync(items), total, pipeline, options, cancellationToken);
    }

    /// <summary>
    /// Processes images as an asynchronous source produces them, handing over each result as soon as it is ready. See
    /// <see cref="ProcessBatchStreamAsync(IEnumerable{BmageBatchItem}, BmageImagePipeline, BmageBatchOptions?, CancellationToken)"/>.
    /// The number of items is not known in advance, so <see cref="BmageBatchProgress.Total"/> is -1.
    /// </summary>
    public IAsyncEnumerable<BmageBatchItemResult> ProcessBatchStreamAsync(
        IAsyncEnumerable<BmageBatchItem> items,
        BmageImagePipeline pipeline,
        BmageBatchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        return Stream(items, -1, pipeline, options, cancellationToken);
    }

    private async IAsyncEnumerable<BmageBatchItemResult> Stream(IAsyncEnumerable<BmageBatchItem> items, int total, BmageImagePipeline pipeline, BmageBatchOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        await foreach (var (_, result) in RunBatchAsync(items, total, pipeline, options ?? BmageBatchOptions.Default, cancellationToken).ConfigureAwait(false))
            yield return result;
    }

    /// <summary>
    /// Runs the batch: <see cref="BmageBatchOptions.MaxConcurrency"/> workers take items from the source one at a time, wait
    /// for the memory gate when there is one, and post each result, with the item's position in the source, as it
    /// completes.
    /// </summary>
    private async IAsyncEnumerable<(int Index, BmageBatchItemResult Result)> RunBatchAsync(IAsyncEnumerable<BmageBatchItem> items, int total, BmageImagePipeline pipeline, BmageBatchOptions options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        // Cancelled once nobody reads the results any more, so a worker waiting to hand one over stops waiting.
        using var abandoned = new CancellationTokenSource();
        var token = linked.Token;
        var workerCount = WorkerCount(options.MaxConcurrency, total);
        // Bounded, so a worker hands its result over before taking the next item: a slow consumer slows the batch down
        // rather than finished images piling up.
        var results = Channel.CreateBounded<(int, BmageBatchItemResult)>(new BoundedChannelOptions(workerCount)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait,
        });
        var gate = options.MaxMemoryBytes is { } budget ? new BmageMemoryGate(budget) : null;
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
                BmageBatchItem? item;
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
                if (!result.WasCancelled) options.Progress?.Report(new BmageBatchProgress(Interlocked.Increment(ref completed), total, result));
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
    /// The workers a batch runs: <see cref="BmageBatchOptions.MaxConcurrency"/>, but never more than there are items, nor
    /// more than 256, which already outnumbers the cores and the memory of any machine running a batch.
    /// </summary>
    internal static int WorkerCount(int maxConcurrency, int total)
    {
        var count = Math.Clamp(maxConcurrency, 1, 256);
        return total >= 0 ? Math.Min(count, Math.Max(1, total)) : count;
    }

    private async Task<BmageBatchItemResult> ProcessItemAsync(BmageBatchItem? item, int index, BmageImagePipeline pipeline, BmageBatchOptions options, BmageMemoryGate? gate, CancellationTokenSource linked)
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
            return BmageBatchItemResult.Success(item, encoded, watch.Elapsed, inputBytes);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return BmageBatchItemResult.Cancelled(reported, inputBytes);
        }
        catch (Exception ex)
        {
            if (options.StopOnFirstError) await linked.CancelAsync().ConfigureAwait(false);
            return BmageBatchItemResult.Failure(reported, ex, watch.Elapsed, inputBytes);
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
    internal long EstimateBatchMemory(ReadOnlySpan<byte> data, BmageImagePipeline pipeline, BmageBatchOptions options)
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
