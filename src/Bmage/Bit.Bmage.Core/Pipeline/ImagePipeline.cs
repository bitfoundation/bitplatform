using System.Collections;
using System.Collections.Immutable;
using System.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Adjustments;
using Bit.Bmage.Operations.Filters;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Analysis;

namespace Bit.Bmage.Pipeline;

/// <summary>
/// An immutable, ordered list of operations describing a complete image transformation. Pipelines are values: every
/// builder method returns a new pipeline, so one can be created once and reused across images and threads.
/// Nothing executes until <see cref="ExecuteAsync"/> (or a synchronous <see cref="Execute"/>) is called.
/// </summary>
public sealed class ImagePipeline : IReadOnlyList<IImageOperation>
{
    private readonly ImmutableArray<IImageOperation> _operations;

    private ImagePipeline(ImmutableArray<IImageOperation> operations) => _operations = operations;

    /// <summary>An empty pipeline.</summary>
    public static ImagePipeline Empty { get; } = new([]);

    /// <summary>Creates an empty pipeline to build on.</summary>
    public static ImagePipeline Create() => Empty;

    /// <summary>Creates a pipeline from an existing sequence of operations.</summary>
    public static ImagePipeline From(IEnumerable<IImageOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        var array = operations.ToImmutableArray();
        for (var i = 0; i < array.Length; i++)
            if (array[i] is null) throw new ArgumentException($"Operation at index {i} is null.", nameof(operations));
        return new ImagePipeline(array);
    }

    public int Count => _operations.Length;
    public IImageOperation this[int index] => _operations[index];
    public bool IsEmpty => _operations.IsEmpty;
    public IEnumerator<IImageOperation> GetEnumerator() => ((IEnumerable<IImageOperation>)_operations).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Appends an operation.</summary>
    public ImagePipeline Add(IImageOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new ImagePipeline(_operations.Add(operation));
    }

    /// <summary>Appends several operations.</summary>
    public ImagePipeline AddRange(IEnumerable<IImageOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        return Concat(From(operations));
    }

    /// <summary>Inserts an operation at a position.</summary>
    public ImagePipeline Insert(int index, IImageOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new ImagePipeline(_operations.Insert(index, operation));
    }

    /// <summary>Removes the operation at a position.</summary>
    public ImagePipeline RemoveAt(int index) => new(_operations.RemoveAt(index));

    /// <summary>Replaces the operation at a position; used for re-editing a previous step non-destructively.</summary>
    public ImagePipeline Replace(int index, IImageOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new ImagePipeline(_operations.SetItem(index, operation));
    }

    /// <summary>Appends every operation from another pipeline.</summary>
    public ImagePipeline Concat(ImagePipeline other) => other is null ? this : new ImagePipeline(_operations.AddRange(other._operations));

    // ---- Fluent operation builders ----

    /// <summary>Applies the EXIF orientation so the image appears upright.</summary>
    public ImagePipeline AutoOrient() => Add(AutoOrientOperation.Instance);

    /// <summary>Crops to a rectangle in image coordinates.</summary>
    public ImagePipeline Crop(Rectangle rectangle) => Add(new CropOperation(rectangle));

    /// <summary>Crops to a rectangle in image coordinates.</summary>
    public ImagePipeline Crop(int x, int y, int width, int height) => Add(new CropOperation(x, y, width, height));

    /// <summary>Crops to the largest centred region with the given aspect ratio.</summary>
    public ImagePipeline CropToAspect(AspectRatio ratio, Anchor anchor = Anchor.Center) => Add(new AspectCropOperation(ratio, anchor));

    /// <summary>
    /// Crops to the given aspect ratio at the position that looks most interesting, rather than a fixed anchor. Avoids
    /// the classic failure of centre-cropping a portrait and cutting off the top of a head.
    /// </summary>
    public ImagePipeline SmartCrop(AspectRatio ratio, SmartCropOptions? options = null) => Add(new SmartCropOperation(ratio, options));

    /// <summary>Resizes the image.</summary>
    public ImagePipeline Resize(int? width = null, int? height = null, ResizeMode mode = ResizeMode.Fit, ResamplingFilter filter = ResamplingFilter.Auto, Anchor anchor = Anchor.Center, Rgba32 background = default)
        => Add(new ResizeOperation(width, height, mode, filter, anchor, background));

    /// <summary>Shrinks the image so neither dimension exceeds the given bounds. Never enlarges.</summary>
    public ImagePipeline MaxSize(int maxWidth, int maxHeight, ResamplingFilter filter = ResamplingFilter.Auto)
        => Add(new ResizeOperation(maxWidth, maxHeight, ResizeMode.Max, filter));

    /// <summary>Scales the image so it covers the target size, cropping the overflow.</summary>
    public ImagePipeline Cover(int width, int height, Anchor anchor = Anchor.Center, ResamplingFilter filter = ResamplingFilter.Auto)
        => Add(new ResizeOperation(width, height, ResizeMode.Cover, filter, anchor));

    /// <summary>Rotates clockwise by an arbitrary angle.</summary>
    public ImagePipeline Rotate(double degrees, Rgba32 background = default, bool expandCanvas = true) => Add(new RotateOperation(degrees, background, expandCanvas));

    /// <summary>Applies one of the eight lossless orientations.</summary>
    public ImagePipeline Orient(Orientation orientation) => Add(new OrientationOperation(orientation));

    /// <summary>Mirrors horizontally.</summary>
    public ImagePipeline FlipHorizontal() => Add(OrientationOperation.FlipHorizontal);

    /// <summary>Mirrors vertically.</summary>
    public ImagePipeline FlipVertical() => Add(OrientationOperation.FlipVertical);

    /// <summary>Shears the image by the given angles in degrees.</summary>
    public ImagePipeline Skew(double degreesX, double degreesY, Rgba32 background = default) => Add(new SkewOperation(degreesX, degreesY, background));

    /// <summary>Adds a constant to the RGB channels. Amount in [-1,1].</summary>
    public ImagePipeline Brightness(float amount) => Add(new BrightnessAdjustment(amount));

    /// <summary>Scales contrast around mid grey. Amount in [-1,1].</summary>
    public ImagePipeline Contrast(float amount) => Add(new ContrastAdjustment(amount));

    /// <summary>Adjusts saturation. Amount in [-1,1].</summary>
    public ImagePipeline Saturation(float amount) => Add(new SaturationAdjustment(amount));

    /// <summary>Rotates hue by degrees.</summary>
    public ImagePipeline Hue(float degrees) => Add(new HueAdjustment(degrees));

    /// <summary>Applies exposure compensation in stops.</summary>
    public ImagePipeline Exposure(float ev) => Add(new ExposureAdjustment(ev));

    /// <summary>Applies a gamma curve.</summary>
    public ImagePipeline Gamma(float gamma) => Add(new GammaAdjustment(gamma));

    /// <summary>Warms (positive) or cools (negative) the image. Amount in [-1,1].</summary>
    public ImagePipeline Temperature(float amount) => Add(new TemperatureAdjustment(amount));

    /// <summary>Shifts towards magenta (positive) or green (negative). Amount in [-1,1].</summary>
    public ImagePipeline Tint(float amount) => Add(new TintAdjustment(amount));

    /// <summary>Multiplies the alpha channel. Opacity in [0,1].</summary>
    public ImagePipeline Opacity(float opacity) => Add(new OpacityAdjustment(opacity));

    /// <summary>Boosts muted colours without over-saturating already vivid ones.</summary>
    public ImagePipeline Vibrance(float amount) => Add(new VibranceAdjustment(amount));

    /// <summary>Lifts shadows and recovers highlights. Both amounts in [-1,1].</summary>
    public ImagePipeline ShadowsHighlights(float shadows, float highlights) => Add(new ShadowsHighlightsAdjustment(shadows, highlights));

    /// <summary>Converts towards grayscale.</summary>
    public ImagePipeline Grayscale(float amount = 1f) => Add(new GrayscaleFilter(amount));

    /// <summary>Applies a sepia tone.</summary>
    public ImagePipeline Sepia(float amount = 1f) => Add(new SepiaFilter(amount));

    /// <summary>Inverts colours.</summary>
    public ImagePipeline Invert(float amount = 1f) => Add(new InvertFilter(amount));

    /// <summary>Reduces each channel to a fixed number of levels.</summary>
    public ImagePipeline Posterize(int levels) => Add(new PosterizeFilter(levels));

    /// <summary>Converts to pure black and white at the given luminance level.</summary>
    public ImagePipeline Threshold(float level = 0.5f) => Add(new ThresholdFilter(level));

    /// <summary>Applies a gaussian blur, optionally limited to a region.</summary>
    public ImagePipeline Blur(float radius, Rectangle? region = null) => Add(new BlurFilter(radius, region));

    /// <summary>Applies unsharp-mask sharpening.</summary>
    public ImagePipeline Sharpen(float amount = 0.5f, float radius = 1f, float threshold = 0f, Rectangle? region = null) => Add(new SharpenFilter(amount, radius, threshold, region));

    /// <summary>Replaces blocks of pixels with their average colour.</summary>
    public ImagePipeline Pixelate(int blockSize, Rectangle? region = null) => Add(new PixelateFilter(blockSize, region));

    /// <summary>Adds deterministic noise.</summary>
    public ImagePipeline Noise(float amount, bool monochrome = false, int seed = 0, Rectangle? region = null) => Add(new NoiseFilter(amount, monochrome, seed, region));

    /// <summary>Applies a median filter for noise reduction.</summary>
    public ImagePipeline DenoiseMedian(int radius = 1, Rectangle? region = null) => Add(new MedianFilter(radius, region));

    /// <summary>Darkens the corners.</summary>
    public ImagePipeline Vignette(float amount = 0.5f, float radius = 0.75f, float softness = 0.5f) => Add(new VignetteFilter(amount, radius, softness));

    /// <summary>
    /// Stretches the tonal range so the darkest pixels become black and the brightest become white. The most useful
    /// single automatic improvement for an under-exposed or flat photo.
    /// </summary>
    public ImagePipeline AutoLevels(double clipFraction = 0.005, bool perChannel = false, float strength = 1f)
        => Add(new AutoLevelsOperation(clipFraction, perChannel, strength));

    /// <summary>Moves the mean luminance towards a target. Gentler than <see cref="AutoLevels"/>.</summary>
    public ImagePipeline AutoContrast(float targetMean = 0.5f, float strength = 0.7f)
        => Add(new AutoContrastOperation(targetMean, strength));

    /// <summary>Applies an arbitrary colour matrix.</summary>
    public ImagePipeline ColorMatrix(ColorMatrix matrix, string? name = null) => Add(new ColorMatrixOperation(matrix, name));

    /// <summary>Applies a custom per-pixel operation.</summary>
    public ImagePipeline Apply(IImageOperation operation) => Add(operation);

    // ---- Analysis ----

    /// <summary>Computes the output size for a given input size without touching pixels.</summary>
    /// <exception cref="ArgumentException">
    /// An operation cannot be applied to the size it would receive, for example a crop that falls entirely outside the
    /// image it follows. Use <see cref="TryGetOutputSize"/> when the pipeline may be in a state the user is still
    /// editing and an exception would be inconvenient.
    /// </exception>
    public Size GetOutputSize(Size inputSize)
    {
        var size = inputSize;
        foreach (var op in _operations) size = op.GetOutputSize(size);
        return size;
    }

    /// <summary>
    /// Computes the output size, returning false instead of throwing when some operation cannot be applied to the size
    /// it would receive. Intended for UI code that displays the output size while an edit is being composed.
    /// </summary>
    public bool TryGetOutputSize(Size inputSize, out Size outputSize)
    {
        try
        {
            outputSize = GetOutputSize(inputSize);
            return true;
        }
        catch (ArgumentException)
        {
            outputSize = Size.Empty;
            return false;
        }
    }

    /// <summary>
    /// Returns an equivalent pipeline for an image scaled by <paramref name="scale"/>. Used to preview full-resolution
    /// edits on a downscaled proxy.
    /// </summary>
    public ImagePipeline ForScale(double scale)
    {
        if (scale == 1 || _operations.IsEmpty) return this;
        var builder = ImmutableArray.CreateBuilder<IImageOperation>(_operations.Length);
        foreach (var op in _operations) builder.Add(op.ForScale(scale));
        return new ImagePipeline(builder.MoveToImmutable());
    }

    /// <summary>
    /// Returns an equivalent but cheaper pipeline: identity operations are dropped, consecutive crops, orientations and
    /// resizes are merged, and neighbouring per-pixel operations are fused into a single pass.
    /// </summary>
    public ImagePipeline Optimize(Size? inputSize = null) => PipelineOptimizer.Optimize(this, inputSize);

    /// <summary>A short human readable description, e.g. "AutoOrient → Resize(1920x auto) → Sharpen".</summary>
    public override string ToString() => _operations.IsEmpty ? "(empty pipeline)" : string.Join(" → ", _operations.Select(o => o.ToString()));

    // ---- Execution ----

    /// <summary>
    /// Runs the pipeline on a copy of the source and returns the result. The source is never modified; the caller owns
    /// the returned buffer.
    /// </summary>
    public ImageBuffer Execute(ImageBuffer source, PipelineExecutionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        options ??= PipelineExecutionOptions.Default;
        var pipeline = options.Optimize ? Optimize(source.Size) : this;
        if (pipeline.IsEmpty) return source.Clone(options.Allocator);

        var current = source;
        var owned = false;
        var count = pipeline.Count;
        try
        {
            for (var i = 0; i < count; i++)
            {
                var op = pipeline[i];
                options.CancellationToken.ThrowIfCancellationRequested();
                var stageProgress = options.Progress is null ? null : new StageProgress(options.Progress, i, count);
                var context = new OperationContext(options.Allocator, options.Limits, options.CancellationToken, stageProgress, canMutateSource: owned);
                ImageBuffer next;
                try
                {
                    next = op.Apply(current, context);
                }
                catch (Exception ex) when (ex is not (OperationCanceledException or ImageException))
                {
                    throw new ImageException($"Operation '{op.Name}' (step {i + 1} of {count}) failed: {ex.Message}", ex);
                }
                if (!ReferenceEquals(next, current))
                {
                    if (owned) current.Dispose();
                    current = next;
                    owned = true;
                }
                options.Progress?.Report((i + 1) / (double)count);
            }
            if (!owned) current = source.Clone(options.Allocator);
            return current;
        }
        catch
        {
            if (owned) current.Dispose();
            throw;
        }
    }

    /// <summary>Runs the pipeline, yielding to the scheduler between operations so the UI stays responsive.</summary>
    public async ValueTask<ImageBuffer> ExecuteAsync(ImageBuffer source, PipelineExecutionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        options ??= PipelineExecutionOptions.Default;
        var pipeline = options.Optimize ? Optimize(source.Size) : this;
        if (pipeline.IsEmpty) return source.Clone(options.Allocator);

        var current = source;
        var owned = false;
        var count = pipeline.Count;
        try
        {
            for (var i = 0; i < count; i++)
            {
                var op = pipeline[i];
                options.CancellationToken.ThrowIfCancellationRequested();
                var stageProgress = options.Progress is null ? null : new StageProgress(options.Progress, i, count);
                var context = new OperationContext(options.Allocator, options.Limits, options.CancellationToken, stageProgress, canMutateSource: owned);
                ImageBuffer next;
                try
                {
                    next = op.Apply(current, context);
                }
                catch (Exception ex) when (ex is not (OperationCanceledException or ImageException))
                {
                    throw new ImageException($"Operation '{op.Name}' (step {i + 1} of {count}) failed: {ex.Message}", ex);
                }
                if (!ReferenceEquals(next, current))
                {
                    if (owned) current.Dispose();
                    current = next;
                    owned = true;
                }
                options.Progress?.Report((i + 1) / (double)count);
                if (options.YieldBetweenOperations && i + 1 < count) await options.Yield().ConfigureAwait(false);
            }
            if (!owned) current = source.Clone(options.Allocator);
            return current;
        }
        catch
        {
            if (owned) current.Dispose();
            throw;
        }
    }

    /// <summary>Maps an operation's own progress into the pipeline's overall progress range.</summary>
    private sealed class StageProgress : IProgress<double>
    {
        private readonly IProgress<double> _inner;
        private readonly int _index, _count;
        public StageProgress(IProgress<double> inner, int index, int count) { _inner = inner; _index = index; _count = count; }
        public void Report(double value) => _inner.Report((_index + Math.Clamp(value, 0, 1)) / _count);
    }
}

/// <summary>Options controlling a pipeline run.</summary>
public sealed record PipelineExecutionOptions
{
    public static PipelineExecutionOptions Default { get; } = new();

    /// <summary>Allocator for intermediate buffers.</summary>
    public Memory.IPixelAllocator Allocator { get; init; } = Memory.PooledPixelAllocator.Shared;

    /// <summary>Limits applied to intermediate and output buffers.</summary>
    public ImageLimits Limits { get; init; } = ImageLimits.Default;

    /// <summary>Cancellation token checked between and inside operations.</summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>Receives values in [0,1] as the run progresses.</summary>
    public IProgress<double>? Progress { get; init; }

    /// <summary>Run the optimiser before executing. Default true.</summary>
    public bool Optimize { get; init; } = true;

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

/// <summary>Crops to the largest region with a given aspect ratio, positioned by an anchor.</summary>
public sealed class AspectCropOperation : ImageOperation, IGeometricOperation
{
    public AspectCropOperation(AspectRatio ratio, Anchor anchor = Anchor.Center)
    {
        if (ratio.Width <= 0 || ratio.Height <= 0) throw new ArgumentOutOfRangeException(nameof(ratio));
        Ratio = ratio;
        Anchor = anchor;
    }

    public AspectRatio Ratio { get; }
    public Anchor Anchor { get; }
    public override string Name => "Crop to aspect";

    private CropOperation Resolve(Size input) => new(Ratio.FitInside(new Rectangle(Point.Empty, input), Anchor));

    public override Size GetOutputSize(Size inputSize) => Resolve(inputSize).GetOutputSize(inputSize);
    public System.Numerics.Matrix3x2? GetTransform(Size inputSize) => Resolve(inputSize).GetTransform(inputSize);
    public override ImageBuffer Apply(ImageBuffer source, OperationContext context) => Resolve(source.Size).Apply(source, context);
    public override string ToString() => $"CropToAspect({Ratio})";
}
