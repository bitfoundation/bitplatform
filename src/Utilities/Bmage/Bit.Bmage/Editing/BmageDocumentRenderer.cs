namespace Bit.Bmage;

/// <summary>Renders an <see cref="BmageImageDocument"/>: pixel pipeline first, then the layers, then the annotation overlay.</summary>
public sealed class BmageDocumentRenderer
{
    private readonly IBmageTextRasterizer? _textRasterizer;

    public BmageDocumentRenderer(IBmageTextRasterizer? textRasterizer = null) => _textRasterizer = textRasterizer;

    /// <summary>Renders a document against its source image. The caller owns the returned buffer.</summary>
    public BmageImageBuffer Render(BmageImageDocument document, BmageImageBuffer source, BmagePipelineExecutionOptions? options = null, double annotationScale = 1.0)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(source);
        options ??= BmagePipelineExecutionOptions.Default;
        var (pipelineOptions, layerProgress) = SplitProgress(document, options);
        var result = document.Pipeline.Execute(source, pipelineOptions);
        try
        {
            CompositeLayers(document.Layers, result, options, layerProgress);
            RenderAnnotations(document.GetRenderOrder(), result, annotationScale, ContextFor(options));
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    /// <summary>Renders a document asynchronously, yielding between pipeline steps.</summary>
    public async ValueTask<BmageImageBuffer> RenderAsync(BmageImageDocument document, BmageImageBuffer source, BmagePipelineExecutionOptions? options = null, double annotationScale = 1.0)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(source);
        options ??= BmagePipelineExecutionOptions.Default;
        var (pipelineOptions, layerProgress) = SplitProgress(document, options);
        var result = await document.Pipeline.ExecuteAsync(source, pipelineOptions).ConfigureAwait(false);
        try
        {
            CompositeLayers(document.Layers, result, options, layerProgress);
            RenderAnnotations(document.GetRenderOrder(), result, annotationScale, ContextFor(options));
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    /// <summary>
    /// The options the pipeline runs with, reporting its share of the progress, and the progress the layers report the
    /// rest through. A pipeline step and a layer, which blends over the whole image, each count as one share.
    /// </summary>
    private static (BmagePipelineExecutionOptions Pipeline, IProgress<double>? Layers) SplitProgress(BmageImageDocument document, BmagePipelineExecutionOptions options)
    {
        if (options.Progress is not { } progress || document.Layers.Count == 0) return (options, null);
        var steps = document.Pipeline.Count;
        var share = steps / (double)(steps + document.Layers.Count);
        return (options with { Progress = share > 0 ? new BmageScaledProgress(progress, 0, share) : null }, new BmageScaledProgress(progress, share, 1 - share));
    }

    /// <summary>The context layers and annotations are drawn with: the run's allocator, limits, threads and cancellation.</summary>
    internal static BmageOperationContext ContextFor(BmagePipelineExecutionOptions options, IProgress<double>? progress = null)
        => new(options.Allocator, options.Limits, options.CancellationToken, progress, maxDegreeOfParallelism: options.MaxDegreeOfParallelism);

    /// <summary>
    /// Composites layers, already at the target's scale, onto the pipeline's output in place, reporting through
    /// <paramref name="progress"/> and keeping prepared layer images in <paramref name="cache"/> when there is one.
    /// </summary>
    internal static void CompositeLayers(IReadOnlyList<BmageLayer> layers, BmageImageBuffer target, BmagePipelineExecutionOptions? options, IProgress<double>? progress = null, BmageLayerImageCache? cache = null)
    {
        if (layers.Count == 0) return;
        BmageLayerCompositor.Composite(target, layers, ContextFor(options ?? BmagePipelineExecutionOptions.Default, progress), cache);
    }

    /// <summary>Draws only the annotation overlay onto an existing buffer.</summary>
    public void RenderAnnotations(BmageImageDocument document, BmageImageBuffer target, double annotationScale = 1.0, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(target);
        RenderAnnotations(document.GetRenderOrder(), target, annotationScale, BmageOperationContext.Default.WithCancellation(cancellationToken));
    }

    /// <summary>
    /// Draws annotations, already in render order and at the target's scale, onto an existing buffer, with the
    /// allocator, limits, threads and cancellation of <paramref name="context"/>.
    /// </summary>
    internal void RenderAnnotations(IReadOnlyList<BmageAnnotation> order, BmageImageBuffer target, double annotationScale, BmageOperationContext context)
    {
        if (order.Count == 0) return;
        var renderContext = new BmageAnnotationRenderContext(_textRasterizer, annotationScale) { Operation = context };
        using var canvas = BmageImageCanvas.FromBuffer(target, context);
        foreach (var annotation in order)
        {
            context.ThrowIfCancellationRequested();
            annotation.RenderWithTransform(canvas, renderContext);
        }
    }
}
