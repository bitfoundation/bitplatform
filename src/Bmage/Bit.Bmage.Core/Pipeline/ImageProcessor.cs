using System.Diagnostics;
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

    /// <summary>Reads dimensions and format from encoded bytes without decoding pixels. Returns null when unrecognised.</summary>
    public ImageInfo? Identify(ReadOnlySpan<byte> data) => Codecs.Identify(data);

    /// <summary>Decodes an encoded image. The caller owns the returned buffer.</summary>
    public async ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, DecodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new DecodeOptions { Limits = Limits };
        var format = ImageFormats.Detect(data.Span);
        if (format == ImageFormat.Unknown)
            throw new ImageDecodeException("The data does not look like a supported image format.");
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
                .Apply(image, new Operations.OperationContext(Allocator, Limits, ct));
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
        });
    }

    /// <summary>
    /// Processes many images with bounded concurrency. Per-item failures are captured in the result rather than
    /// aborting the batch, unless <see cref="BatchOptions.StopOnFirstError"/> is set.
    /// </summary>
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
        var completed = 0;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var throttle = new SemaphoreSlim(Math.Max(1, options.MaxConcurrency));
        var stopwatch = Stopwatch.StartNew();

        var tasks = new List<Task>(list.Count);
        for (var i = 0; i < list.Count; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                await throttle.WaitAsync(linked.Token).ConfigureAwait(false);
                try
                {
                    var item = list[index];
                    var itemWatch = Stopwatch.StartNew();
                    try
                    {
                        var encoded = await ProcessAsync(item.Data, pipeline, options.ExportOptions, options.DecodeOptions, null, linked.Token).ConfigureAwait(false);
                        results[index] = BatchItemResult.Success(item, encoded, itemWatch.Elapsed);
                    }
                    catch (OperationCanceledException) when (linked.Token.IsCancellationRequested)
                    {
                        results[index] = BatchItemResult.Cancelled(item);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        results[index] = BatchItemResult.Failure(item, ex, itemWatch.Elapsed);
                        if (options.StopOnFirstError) await linked.CancelAsync().ConfigureAwait(false);
                    }
                    var done = Interlocked.Increment(ref completed);
                    options.Progress?.Report(new BatchProgress(done, list.Count, results[index]));
                }
                finally
                {
                    throttle.Release();
                }
            }, linked.Token));
        }

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        for (var i = 0; i < results.Length; i++)
            results[i] ??= BatchItemResult.Cancelled(list[i]);
        return new BatchResult(results, stopwatch.Elapsed);
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
public sealed record BatchItem(ReadOnlyMemory<byte> Data, string? FileName = null, object? Tag = null);

/// <summary>Options for a batch run.</summary>
public sealed record BatchOptions
{
    public static BatchOptions Default { get; } = new();

    /// <summary>Maximum images processed at once. Keep low on memory constrained devices. Default 2.</summary>
    public int MaxConcurrency { get; init; } = 2;

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
public readonly record struct BatchProgress(int Completed, int Total, BatchItemResult Last)
{
    public double Fraction => Total == 0 ? 1 : Completed / (double)Total;
}

/// <summary>The outcome for one image in a batch.</summary>
public sealed class BatchItemResult
{
    private BatchItemResult(BatchItem item, EncodedImage? output, Exception? error, bool cancelled, TimeSpan duration)
    {
        Item = item; Output = output; Error = error; WasCancelled = cancelled; Duration = duration;
    }

    public static BatchItemResult Success(BatchItem item, EncodedImage output, TimeSpan duration) => new(item, output, null, false, duration);
    public static BatchItemResult Failure(BatchItem item, Exception error, TimeSpan duration) => new(item, null, error, false, duration);
    public static BatchItemResult Cancelled(BatchItem item) => new(item, null, null, true, TimeSpan.Zero);

    public BatchItem Item { get; }
    public EncodedImage? Output { get; }
    public Exception? Error { get; }
    public bool WasCancelled { get; }
    public TimeSpan Duration { get; }
    public bool IsSuccess => Output is not null;
    public string? FileName => Item.FileName;
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
    /// <summary>Total bytes of all inputs.</summary>
    public long TotalInputBytes => Results.Sum(r => (long)r.Item.Data.Length);
}
