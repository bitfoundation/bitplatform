using System.Drawing;
using Bit.Bmage.Annotations;
using Bit.Bmage.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Layers;
using Bit.Bmage.Memory;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Editing;

/// <summary>
/// An interactive editing session over one image: the original pixels, the current non-destructive document, the undo
/// history and a live preview.
/// </summary>
/// <remarks>
/// <para>
/// Interactive editing on a 24 megapixel photo cannot re-run the pipeline at full resolution on every slider tick. This
/// session keeps a downscaled <b>proxy</b> sized for the display, previews edits on that, and replays the edit at full
/// resolution only when exporting. Because operations know how to rescale themselves, the preview and the export are
/// the same edit at two resolutions rather than two different code paths.
/// </para>
/// <para>
/// Preview rendering is serialised and cancellable: a new request cancels the one in flight, so dragging a slider never
/// queues work faster than it completes.
/// </para>
/// <para>
/// Previews reuse pixels earlier renders computed for the same operation and annotation instances, which relies on
/// those being immutable, as every built-in one is. A custom operation must not change in place: replace it with a new
/// instance instead. When something outside the document changes what it renders to, such as a font registered after
/// text was drawn, call <see cref="Invalidate"/>.
/// </para>
/// </remarks>
public sealed class ImageEditSession : IDisposable
{
    private readonly IPixelAllocator _allocator;
    private readonly ImageLimits _limits;
    private readonly int? _maxDegreeOfParallelism;
    private readonly DocumentRenderer _renderer;
    private readonly Func<ImageBuffer, Size, CancellationToken, ValueTask<ImageBuffer?>>? _proxyScaler;
    private ImageBuffer _original;
    private ImageBuffer? _proxy;
    private ImageBuffer? _preview;
    private CancellationTokenSource? _renderCts;
    // Never disposed: a render that is still running releases it on the way out, and SemaphoreSlim only needs
    // disposing when its wait handle has been used.
    private readonly SemaphoreSlim _renderLock = new(1, 1);
    private bool _disposed;
    // Incremented whenever the source changes, so a render that started on the old source cannot publish its result.
    private int _sourceVersion;
    // The unscaled document the current preview shows, overlay included.
    private ImageDocument? _previewDocument;
    // Pixels earlier renders computed, one set per resolution, only touched under the render lock.
    private readonly RenderLayer _full = new();
    private readonly RenderLayer _draft = new();
    private OverlayBase? _overlayBase;
    // Half the proxy's resolution, built on the first draft render.
    private ImageBuffer? _draftSource;
    // Counted by Invalidate; a render that finds more than it last saw starts without anything earlier renders kept.
    private int _invalidations;
    private int _invalidationsSeen;

    /// <summary>Creates a session that takes ownership of <paramref name="original"/>.</summary>
    public ImageEditSession(ImageBuffer original, ImageEditSessionOptions? options = null)
    {
        _original = original ?? throw new ArgumentNullException(nameof(original));
        var o = options ?? new ImageEditSessionOptions();
        _allocator = o.Allocator ?? PooledPixelAllocator.Shared;
        _limits = o.Limits ?? ImageLimits.Default;
        _maxDegreeOfParallelism = o.MaxDegreeOfParallelism;
        _renderer = new DocumentRenderer(o.TextRasterizer);
        _proxyScaler = o.ProxyScaler;
        MaxProxyDimension = o.MaxProxyDimension;
        UpdateProxyGeometry();
        History = new EditHistory(ImageDocument.Create(original), o.MaxHistoryDepth);
        History.Changed += _ => MarkPreviewStale();
    }

    /// <summary>The untouched source image. Never modified.</summary>
    public ImageBuffer Original => _original;

    /// <summary>Size of the source image.</summary>
    public Size SourceSize => _original.Size;

    /// <summary>Undo/redo over the document.</summary>
    public EditHistory History { get; }

    /// <summary>The current non-destructive edit.</summary>
    public ImageDocument Document => History.Current;

    /// <summary>Size the exported image will have.</summary>
    public Size OutputSize => Document.OutputSize;

    /// <summary>
    /// Longest edge of the working proxy. Editing happens at this resolution; export always uses the full image.
    /// </summary>
    public int MaxProxyDimension { get; }

    /// <summary>
    /// The scale of the proxy relative to the original, 1 when the image is small enough to edit directly. Valid from
    /// construction, before the first preview render.
    /// </summary>
    public double ProxyScale { get; private set; } = 1.0;

    /// <summary>
    /// Size of the working proxy. Equals <see cref="SourceSize"/> when the image is edited at full resolution.
    /// Valid from construction, before the first preview render.
    /// </summary>
    public Size ProxySize { get; private set; }

    /// <summary>True when the image is large enough that editing happens on a downscaled proxy.</summary>
    public bool UsesProxy => ProxyScale < 1.0;

    /// <summary>
    /// The scale <see cref="RenderDraftAsync"/> renders at, relative to the original: half of <see cref="ProxyScale"/>,
    /// or <see cref="ProxyScale"/> itself when the proxy is already small enough to render quickly.
    /// </summary>
    public double DraftScale { get; private set; } = 1.0;

    /// <summary>
    /// The scale of <see cref="Preview"/> relative to the original: <see cref="ProxyScale"/>, or
    /// <see cref="DraftScale"/> when the last render was a draft. Previews are meant to be shown at the proxy's size
    /// either way, so coordinates keep mapping through <see cref="PreviewToOutput"/>.
    /// </summary>
    public double PreviewScale { get; private set; } = 1.0;

    /// <summary>How long the last full resolution preview took to render, for deciding when a draft is worth it.</summary>
    public TimeSpan LastPreviewDuration { get; private set; }

    /// <summary>The most recently rendered preview, or null before the first render. Owned by the session.</summary>
    public ImageBuffer? Preview => _preview;

    /// <summary>True when the preview no longer matches the document.</summary>
    public bool IsPreviewStale { get; private set; } = true;

    /// <summary>Raised after a preview render completes.</summary>
    public event Action<ImageBuffer>? PreviewUpdated;

    /// <summary>Raised when the document changes, before the new preview is available.</summary>
    public event Action<ImageDocument>? DocumentChanged;

    /// <summary>Applies a change to the document and records it in the history.</summary>
    public void Apply(Func<ImageDocument, ImageDocument> change, string label)
    {
        ArgumentNullException.ThrowIfNull(change);
        var next = change(Document);
        if (History.IsTransientOpen)
        {
            // The end of a drag: its final state replaces the entry the drag opened, rather than adding a second one.
            History.CommitTransient(next, label);
            DocumentChanged?.Invoke(next);
            return;
        }
        if (ReferenceEquals(next, Document)) return;
        History.Push(next, label);
        DocumentChanged?.Invoke(next);
    }

    /// <summary>
    /// Applies a change while a control is being dragged. The first change of an interaction adds one history entry and
    /// every later one replaces it, so a drag costs a single undo step and undo returns to the state before the drag.
    /// End the interaction with <see cref="Commit"/>, or with <see cref="Apply"/> carrying the final value.
    /// </summary>
    public void ApplyTransient(Func<ImageDocument, ImageDocument> change, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(change);
        var next = change(Document);
        if (ReferenceEquals(next, Document)) return;
        History.PushTransient(next, label ?? "Edit");
        DocumentChanged?.Invoke(next);
    }

    /// <summary>
    /// Ends the interaction started by <see cref="ApplyTransient"/>, labelling its history entry. Does nothing when no
    /// interaction is in progress, so a click that never moved anything adds no entry.
    /// </summary>
    public void Commit(string label)
    {
        if (History.CommitTransient(Document, label)) DocumentChanged?.Invoke(Document);
    }

    /// <summary>Adds an operation and records it.</summary>
    public void AddOperation(IImageOperation operation, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(operation);
        Apply(d => d.AddOperation(operation), label ?? operation.Name);
    }

    /// <summary>Adds an annotation and records it.</summary>
    public void AddAnnotation(Annotations.Annotation annotation, string? label = null)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        Apply(d => d.AddAnnotation(annotation), label ?? $"Add {annotation.Kind.ToLowerInvariant()}");
    }

    /// <summary>Undoes one step.</summary>
    public void Undo()
    {
        if (!History.CanUndo) return;
        History.Undo();
        DocumentChanged?.Invoke(Document);
    }

    /// <summary>Redoes one step.</summary>
    public void Redo()
    {
        if (!History.CanRedo) return;
        History.Redo();
        DocumentChanged?.Invoke(Document);
    }

    /// <summary>Discards every edit.</summary>
    public void Reset()
    {
        History.Push(ImageDocument.Create(_original), "Reset");
        DocumentChanged?.Invoke(Document);
    }

    /// <summary>Replaces the source image, discarding all edits and history.</summary>
    public void ReplaceSource(ImageBuffer image, bool disposeExisting = true)
    {
        ArgumentNullException.ThrowIfNull(image);
        ObjectDisposedException.ThrowIf(_disposed, this);
        var old = _original;
        var oldProxy = _proxy;
        var oldDraft = _draftSource;
        _original = image;
        _proxy = null;
        _draftSource = null;
        _sourceVersion++;
        // A render of the old source is now pointless, and must not outlive the buffers it reads.
        CancelQuietly(Interlocked.Exchange(ref _renderCts, null));
        DisposePreview();
        UpdateProxyGeometry();
        History.Clear(ImageDocument.Create(image));
        var releaseProxy = oldProxy is not null && !ReferenceEquals(oldProxy, old) ? oldProxy : null;
        var releaseOriginal = disposeExisting && !ReferenceEquals(old, image) ? old : null;
        WhenIdle(() =>
        {
            releaseProxy?.Dispose();
            releaseOriginal?.Dispose();
            oldDraft?.Dispose();
            DisposeStaleCaches();
        });
        MarkPreviewStale();
        DocumentChanged?.Invoke(Document);
    }

    private void MarkPreviewStale() => IsPreviewStale = true;

    /// <summary>
    /// Makes the next render run the whole document again rather than reuse pixels earlier renders computed, for when
    /// something outside the document changed what it renders to, such as a font registered after text was drawn.
    /// </summary>
    public void Invalidate()
    {
        Interlocked.Increment(ref _invalidations);
        MarkPreviewStale();
    }

    /// <summary>
    /// Renders the preview at proxy resolution. Cancels any render already running, so rapid edits never pile up.
    /// Returns the preview buffer, which the session continues to own.
    /// </summary>
    /// <remarks>
    /// A superseded call throws <see cref="OperationCanceledException"/>. Callers that fire a render on every slider
    /// tick should catch and ignore it: it means a newer render has taken over, not that anything failed.
    /// </remarks>
    /// <exception cref="OperationCanceledException">The render was superseded by a newer one, or the caller cancelled.</exception>
    public ValueTask<ImageBuffer> RenderPreviewAsync(CancellationToken cancellationToken = default)
        => RenderPreviewAsync(null, cancellationToken);

    /// <summary>
    /// Renders a quick preview at <see cref="DraftScale"/>, for showing while a control is dragged: a quarter of the
    /// pixels when the proxy is large, so even filters that cost a lot per pixel keep up with the pointer. Render the
    /// full preview again when the interaction ends. The draft counts as stale.
    /// </summary>
    /// <exception cref="OperationCanceledException">The render was superseded by a newer one, or the caller cancelled.</exception>
    public ValueTask<ImageBuffer> RenderDraftAsync(Func<ImageDocument, ImageDocument>? overlay = null, CancellationToken cancellationToken = default)
        => RenderAsync(overlay, draft: DraftScale < ProxyScale, cancellationToken);

    /// <summary>
    /// Renders the preview of the document as changed by <paramref name="overlay"/>, without recording that change.
    /// Use it to show something that is not part of the edit yet, such as a shape still being drawn. The resulting
    /// preview counts as stale, since it does not match the document.
    /// </summary>
    /// <exception cref="OperationCanceledException">The render was superseded by a newer one, or the caller cancelled.</exception>
    public ValueTask<ImageBuffer> RenderPreviewAsync(Func<ImageDocument, ImageDocument>? overlay, CancellationToken cancellationToken = default)
        => RenderAsync(overlay, draft: false, cancellationToken);

    private async ValueTask<ImageBuffer> RenderAsync(Func<ImageDocument, ImageDocument>? overlay, bool draft, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Each call owns exactly one token source and is the only code that disposes it. Publishing it before
        // cancelling the previous one means a render that is still queued behind the lock is cancelled too.
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancelQuietly(Interlocked.Exchange(ref _renderCts, cts));

        try
        {
            // Wait on the render's own token so a newer render, or Dispose, also releases a call still queued here.
            await _renderLock.WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch
        {
            Interlocked.CompareExchange(ref _renderCts, null, cts);
            cts.Dispose();
            throw;
        }

        try
        {
            cts.Token.ThrowIfCancellationRequested();
            if (_disposed) throw new OperationCanceledException(cts.Token);
            var sourceVersion = _sourceVersion;
            DisposeStaleCaches();
            var invalidations = Volatile.Read(ref _invalidations);
            if (invalidations != _invalidationsSeen)
            {
                _invalidationsSeen = invalidations;
                DisposeCaches();
                _previewDocument = null;
            }
            var source = Document;
            // Rendering the document the preview already shows would only produce the same pixels again.
            if (overlay is null && ReferenceEquals(source, _previewDocument) && _preview is { IsDisposed: false } current)
            {
                if (!draft) IsPreviewStale = false;
                PreviewUpdated?.Invoke(current);
                return current;
            }
            var proxy = await EnsureProxyAsync(cts.Token).ConfigureAwait(false);
            var target = draft ? await EnsureDraftSourceAsync(proxy, cts.Token).ConfigureAwait(false) : proxy;
            var scale = draft ? DraftScale : ProxyScale;
            var document = overlay is null ? source : overlay(source);
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            var rendered = await RenderCachedAsync(source, document, target, scale, draft ? _draft : _full, sourceVersion, new PipelineExecutionOptions
            {
                Allocator = _allocator,
                Limits = _limits,
                MaxDegreeOfParallelism = _maxDegreeOfParallelism,
                CancellationToken = cts.Token,
            }).ConfigureAwait(false);
            if (!draft) LastPreviewDuration = System.Diagnostics.Stopwatch.GetElapsedTime(started);

            if (_disposed || sourceVersion != _sourceVersion)
            {
                // The session was disposed or given a new source while this ran; the result belongs to neither.
                rendered.Dispose();
                if (_disposed) DisposeCaches();
                throw new OperationCanceledException(cts.Token);
            }

            var old = _preview;
            _preview = rendered;
            PreviewScale = scale;
            // A draft is never the preview a later render could stand in for.
            _previewDocument = draft ? null : document;
            // An edit or invalidation made while this ran is not in the result, so the preview is only fresh if nothing changed.
            IsPreviewStale = draft || overlay is not null || !ReferenceEquals(source, Document) || Volatile.Read(ref _invalidations) != _invalidationsSeen;
            old?.Dispose();
            PreviewUpdated?.Invoke(rendered);
            return rendered;
        }
        finally
        {
            Interlocked.CompareExchange(ref _renderCts, null, cts);
            _renderLock.Release();
            cts.Dispose();
        }
    }

    /// <summary>
    /// Renders <paramref name="document"/> at proxy resolution, reusing pixels earlier renders computed when that gives
    /// exactly the same result.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pipeline is split after its last step the optimiser never rewrites together with a neighbour, such as a blur
    /// (see <see cref="PipelineOptimizer.IsBarrier"/>). Optimising and running the two halves separately therefore
    /// produces the same pixels as running it whole, and the first half is kept for as long as the document keeps those
    /// same operations. Dragging a slider for an adjustment after a blur re-runs only the adjustment.
    /// </para>
    /// <para>
    /// When the document has annotations, the pipeline's output is kept too, so editing annotations never re-runs the
    /// pipeline. While a shape is drawn as an overlay, the preview of the document without it is kept, and each frame
    /// only draws the overlay's own annotations on a copy of it.
    /// </para>
    /// <para>
    /// Everything cached is tagged with <c>version</c>, the source version the render started on, so pixels of a source
    /// replaced while the render ran are never taken for the new source's.
    /// </para>
    /// </remarks>
    private async ValueTask<ImageBuffer> RenderCachedAsync(ImageDocument baseDocument, ImageDocument document, ImageBuffer source, double scale, RenderLayer layer, int version, PipelineExecutionOptions options)
    {
        var order = document.GetRenderOrder();
        if (layer == _full)
        {
            if (!ReferenceEquals(document, baseDocument))
            {
                if (TryRenderOverBase(baseDocument, document, order, version, options.CancellationToken) is { } overlaid) return overlaid;
            }
            else
            {
                DisposeOverlayBase();
            }
        }

        var (pixels, shared) = await RenderLayersAsync(document, source, scale, layer, version, options, keepOutput: order.Count > 0).ConfigureAwait(false);
        var rendered = shared ? pixels.Clone(_allocator) : pixels;
        try
        {
            _renderer.RenderAnnotations(ForScale(order, scale), rendered, scale, options.CancellationToken);
            return rendered;
        }
        catch
        {
            rendered.Dispose();
            throw;
        }
    }

    /// <summary>
    /// The pipeline's output with the document's layers composited onto it, and whether a cache still owns it. With
    /// annotations to draw over it the composite is kept, so moving an annotation never re-composites the layers.
    /// </summary>
    private async ValueTask<(ImageBuffer Pixels, bool Shared)> RenderLayersAsync(ImageDocument document, ImageBuffer source, double scale, RenderLayer layer, int version, PipelineExecutionOptions options, bool keepOutput)
    {
        var layers = document.Layers;
        if (layers.Count == 0)
        {
            Replace(ref layer.Composite, null);
            return await RenderPipelineAsync(document.Pipeline, source, scale, layer, version, options, keepOutput).ConfigureAwait(false);
        }
        if (layer.Composite?.Matches(document.Pipeline, layers, version) == true) return (layer.Composite.Pixels, true);

        // The pipeline's output is kept too: editing a layer re-composites the layers without re-running the pipeline.
        var (pixels, shared) = await RenderPipelineAsync(document.Pipeline, source, scale, layer, version, options, keepOutput: true).ConfigureAwait(false);
        var composite = shared ? pixels.Clone(_allocator) : pixels;
        try
        {
            DocumentRenderer.CompositeLayers(scale == 1 ? layers : [.. layers.Select(l => l.ForScale(scale))], composite, options);
        }
        catch
        {
            composite.Dispose();
            throw;
        }
        if (!keepOutput)
        {
            Replace(ref layer.Composite, null);
            return (composite, false);
        }
        Replace(ref layer.Composite, new CompositeCache(document.Pipeline, layers, version, composite));
        return (composite, true);
    }

    /// <summary>
    /// The pipeline's output at proxy resolution, and whether a cache (or the proxy itself) still owns it, in which
    /// case the caller must copy it before drawing on it.
    /// </summary>
    private async ValueTask<(ImageBuffer Pixels, bool Shared)> RenderPipelineAsync(ImagePipeline pipeline, ImageBuffer source, double scale, RenderLayer layer, int version, PipelineExecutionOptions options, bool keepOutput)
    {
        if (layer.Pipeline?.Matches(pipeline, pipeline.Count, version) == true) return (layer.Pipeline.Pixels, true);

        var scaled = scale < 1 ? pipeline.ForScale(scale) : pipeline;
        var unoptimised = options with { Optimize = false };
        var boundary = CacheBoundary(scaled);
        var start = source;
        if (boundary == 0)
        {
            Replace(ref layer.Stage, null);
        }
        else
        {
            if (layer.Stage?.Matches(pipeline, boundary, version) != true)
            {
                var stage = await Slice(scaled, 0, boundary).Optimize(source.Size).ExecuteAsync(source, unoptimised).ConfigureAwait(false);
                Replace(ref layer.Stage, new RenderCache(pipeline, boundary, version, stage));
            }
            start = layer.Stage!.Pixels;
        }
        if (boundary == scaled.Count)
        {
            // The stage is the output, so a kept output of an earlier pipeline can never match again.
            Replace(ref layer.Pipeline, null);
            return (start, true);
        }

        var rest = Slice(scaled, boundary, scaled.Count).Optimize(PipelineOptimizer.SizeBefore(scaled, boundary, source.Size));
        var output = await rest.ExecuteAsync(start, unoptimised).ConfigureAwait(false);
        if (!keepOutput)
        {
            Replace(ref layer.Pipeline, null);
            return (output, false);
        }
        Replace(ref layer.Pipeline, new RenderCache(pipeline, pipeline.Count, version, output));
        return (output, true);
    }

    /// <summary>
    /// Draws the overlay's extra annotations over a kept preview of <paramref name="baseDocument"/>, or returns null
    /// when the overlay does more than add annotations on top.
    /// </summary>
    private ImageBuffer? TryRenderOverBase(ImageDocument baseDocument, ImageDocument document, IReadOnlyList<Annotation> order, int version, CancellationToken cancellationToken)
    {
        if (!ReferenceEquals(document.Pipeline, baseDocument.Pipeline) || !ReferenceEquals(document.Layers, baseDocument.Layers)) return null;
        if (_overlayBase is not { } overlayBase || !ReferenceEquals(overlayBase.Document, baseDocument) || overlayBase.SourceVersion != version)
        {
            // The first overlay frame: the current preview shows exactly the document without the overlay, or nothing can be reused.
            if (version != _sourceVersion || _preview is not { IsDisposed: false } preview || !ReferenceEquals(_previewDocument, baseDocument)) return null;
            DisposeOverlayBase();
            overlayBase = _overlayBase = new OverlayBase(baseDocument, baseDocument.GetRenderOrder(), version, preview.Clone(_allocator));
        }

        var below = overlayBase.Order;
        if (order.Count < below.Count) return null;
        for (var i = 0; i < below.Count; i++)
            if (!ReferenceEquals(order[i], below[i])) return null;

        var rendered = overlayBase.Pixels.Clone(_allocator);
        try
        {
            var added = new Annotation[order.Count - below.Count];
            for (var i = 0; i < added.Length; i++) added[i] = order[below.Count + i];
            _renderer.RenderAnnotations(ForScale(added, ProxyScale), rendered, ProxyScale, cancellationToken);
            return rendered;
        }
        catch
        {
            rendered.Dispose();
            throw;
        }
    }

    /// <summary>Annotations expressed for a scaled image, the way <see cref="ImageDocument.ForScale"/> expresses them.</summary>
    private static IReadOnlyList<Annotation> ForScale(IReadOnlyList<Annotation> order, double scale)
    {
        if (scale == 1 || order.Count == 0) return order;
        var scaled = new Annotation[order.Count];
        for (var i = 0; i < scaled.Length; i++) scaled[i] = order[i].ForScale(scale);
        return scaled;
    }

    /// <summary>The index after the pipeline's last optimiser barrier, or 0 when it has none.</summary>
    private static int CacheBoundary(ImagePipeline scaled)
    {
        for (var i = scaled.Count - 1; i >= 0; i--)
            if (PipelineOptimizer.IsBarrier(scaled[i])) return i + 1;
        return 0;
    }

    private static ImagePipeline Slice(ImagePipeline pipeline, int start, int end)
    {
        var operations = new IImageOperation[end - start];
        for (var i = start; i < end; i++) operations[i - start] = pipeline[i];
        return ImagePipeline.From(operations);
    }

    private static void Replace(ref RenderCache? cache, RenderCache? next)
    {
        var old = cache;
        cache = next;
        if (!ReferenceEquals(old, next)) old?.Pixels.Dispose();
    }

    private static void Replace(ref CompositeCache? cache, CompositeCache? next)
    {
        var old = cache;
        cache = next;
        if (!ReferenceEquals(old, next)) old?.Pixels.Dispose();
    }

    private void DisposeOverlayBase()
    {
        _overlayBase?.Pixels.Dispose();
        _overlayBase = null;
    }

    /// <summary>Releases cached pixels of an earlier source. Only called under the render lock.</summary>
    private void DisposeStaleCaches()
    {
        foreach (var layer in new[] { _full, _draft })
        {
            if (layer.Stage is { } stage && stage.SourceVersion != _sourceVersion) Replace(ref layer.Stage, null);
            if (layer.Pipeline is { } pipeline && pipeline.SourceVersion != _sourceVersion) Replace(ref layer.Pipeline, null);
            if (layer.Composite is { } composite && composite.SourceVersion != _sourceVersion) Replace(ref layer.Composite, null);
        }
        if (_overlayBase is { } overlay && overlay.SourceVersion != _sourceVersion) DisposeOverlayBase();
    }

    private void DisposeCaches()
    {
        foreach (var layer in new[] { _full, _draft })
        {
            Replace(ref layer.Stage, null);
            Replace(ref layer.Pipeline, null);
            Replace(ref layer.Composite, null);
        }
        DisposeOverlayBase();
    }

    /// <summary>The caches of one resolution: the full preview's, or the draft's.</summary>
    private sealed class RenderLayer
    {
        public RenderCache? Stage;
        public RenderCache? Pipeline;
        public CompositeCache? Composite;
    }

    /// <summary>The pipeline's output with the document's layers composited onto it.</summary>
    private sealed class CompositeCache(ImagePipeline pipeline, IReadOnlyList<Layer> layers, int sourceVersion, ImageBuffer pixels)
    {
        // Operations and layers are immutable, so the same instances in the same order mean the same pixels.
        private readonly IImageOperation[] _operations = [.. pipeline];
        private readonly Layer[] _layers = [.. layers];

        public int SourceVersion { get; } = sourceVersion;

        public ImageBuffer Pixels { get; } = pixels;

        public bool Matches(ImagePipeline otherPipeline, IReadOnlyList<Layer> otherLayers, int sourceVersion)
        {
            if (sourceVersion != SourceVersion || otherPipeline.Count != _operations.Length || otherLayers.Count != _layers.Length) return false;
            for (var i = 0; i < _operations.Length; i++)
                if (!ReferenceEquals(otherPipeline[i], _operations[i])) return false;
            for (var i = 0; i < _layers.Length; i++)
                if (!ReferenceEquals(otherLayers[i], _layers[i])) return false;
            return true;
        }
    }

    /// <summary>Pixels a pipeline's first operations produced, valid for the same operations on the same source.</summary>
    private sealed class RenderCache(ImagePipeline pipeline, int count, int sourceVersion, ImageBuffer pixels)
    {
        // The unscaled operations themselves: they are immutable, so the same instances mean the same edit.
        private readonly IImageOperation[] _operations = [.. pipeline.Take(count)];

        public int SourceVersion { get; } = sourceVersion;

        public ImageBuffer Pixels { get; } = pixels;

        public bool Matches(ImagePipeline other, int count, int sourceVersion)
        {
            if (sourceVersion != SourceVersion || count != _operations.Length || other.Count < count) return false;
            for (var i = 0; i < count; i++)
                if (!ReferenceEquals(other[i], _operations[i])) return false;
            return true;
        }
    }

    /// <summary>The preview of a document, kept while overlays are drawn on top of it.</summary>
    private sealed record OverlayBase(ImageDocument Document, IReadOnlyList<Annotation> Order, int SourceVersion, ImageBuffer Pixels);

    /// <summary>
    /// Cancels a token source that another call owns. The owner may have disposed it already, which is not an error:
    /// a disposed source belongs to a render that has finished and therefore needs no cancelling.
    /// </summary>
    private static void CancelQuietly(CancellationTokenSource? cts)
    {
        if (cts is null) return;
        try { cts.Cancel(); }
        catch (ObjectDisposedException) { /* the owning render already completed */ }
    }

    /// <summary>
    /// Renders the final image at full resolution. Slower than the preview and allocates a full-size buffer, so call it
    /// on export rather than on every change. The caller owns the returned buffer.
    /// </summary>
    public ValueTask<ImageBuffer> RenderFullAsync(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _renderer.RenderAsync(Document, _original, new PipelineExecutionOptions
        {
            Allocator = _allocator,
            Limits = _limits,
            MaxDegreeOfParallelism = _maxDegreeOfParallelism,
            CancellationToken = cancellationToken,
            Progress = progress,
        });
    }

    /// <summary>Maps a point in preview coordinates to the full-resolution output coordinates.</summary>
    public System.Numerics.Vector2 PreviewToOutput(System.Numerics.Vector2 point)
        => UsesProxy ? point / (float)ProxyScale : point;

    /// <summary>Maps a point in full-resolution output coordinates to preview coordinates.</summary>
    public System.Numerics.Vector2 OutputToPreview(System.Numerics.Vector2 point)
        => UsesProxy ? point * (float)ProxyScale : point;

    /// <summary>
    /// Resolves the proxy size and scale from the source dimensions alone. This is arithmetic, not pixels, so it runs
    /// as soon as a source image is set: pointer coordinates must map correctly from the first frame, long before the
    /// first preview render has finished.
    /// </summary>
    private void UpdateProxyGeometry()
    {
        var longest = Math.Max(_original.Width, _original.Height);
        if (MaxProxyDimension <= 0 || longest <= MaxProxyDimension)
        {
            ProxySize = _original.Size;
            ProxyScale = 1.0;
            UpdateDraftGeometry();
            return;
        }
        var nominal = MaxProxyDimension / (double)longest;
        var width = Math.Max(1, (int)Math.Round(_original.Width * nominal));
        var height = Math.Max(1, (int)Math.Round(_original.Height * nominal));
        ProxySize = new Size(width, height);
        // Derive the effective scale from the longest edge, the one actually pinned to MaxProxyDimension, so the
        // rounding error in the mapping is as small as it can be.
        ProxyScale = _original.Width >= _original.Height
            ? width / (double)_original.Width
            : height / (double)_original.Height;
        UpdateDraftGeometry();
    }

    /// <summary>Below this many pixels a preview renders quickly anyway, and a draft would only blur it.</summary>
    private const long DraftThreshold = 1024 * 1024;

    /// <summary>A draft has half the proxy's longest edge, and the other in proportion.</summary>
    private void UpdateDraftGeometry()
    {
        PreviewScale = ProxyScale;
        DraftSize = ProxySize;
        DraftScale = ProxyScale;
        if ((long)ProxySize.Width * ProxySize.Height <= DraftThreshold) return;
        // Sized from the original the way ImageDocument.ForScale sizes it, so the draft and the document scaled for it agree.
        var longest = Math.Max(_original.Width, _original.Height);
        var draftLongest = Math.Max(1, (int)Math.Round(Math.Max(ProxySize.Width, ProxySize.Height) / 2.0));
        DraftScale = draftLongest / (double)longest;
        DraftSize = new Size(Math.Max(1, (int)Math.Round(_original.Width * DraftScale)), Math.Max(1, (int)Math.Round(_original.Height * DraftScale)));
    }

    private Size DraftSize { get; set; }

    private async ValueTask<ImageBuffer> EnsureDraftSourceAsync(ImageBuffer proxy, CancellationToken cancellationToken)
    {
        if (_draftSource is { } existing) return existing;
        var sourceVersion = _sourceVersion;
        var draft = await ScaleProxyAsync(proxy, DraftSize, cancellationToken).ConfigureAwait(false);
        if (_disposed || sourceVersion != _sourceVersion)
        {
            draft.Dispose();
            throw new OperationCanceledException(cancellationToken);
        }
        _draftSource = draft;
        return draft;
    }

    private async ValueTask<ImageBuffer> EnsureProxyAsync(CancellationToken cancellationToken)
    {
        if (_proxy is { } existing) return existing;
        if (!UsesProxy) return _original;
        var sourceVersion = _sourceVersion;
        var proxy = await ScaleProxyAsync(_original, ProxySize, cancellationToken).ConfigureAwait(false);
        if (_disposed || sourceVersion != _sourceVersion)
        {
            // Built for a source the session no longer has.
            proxy.Dispose();
            throw new OperationCanceledException(cancellationToken);
        }
        _proxy = proxy;
        return proxy;
    }

    /// <summary>
    /// The working copy, from the scaler the editor supplies in a browser (whose native resampler is many times faster
    /// than the managed one on WebAssembly) and from the managed resampler otherwise, or when the scaler cannot help.
    /// </summary>
    private async ValueTask<ImageBuffer> ScaleProxyAsync(ImageBuffer original, Size size, CancellationToken cancellationToken)
    {
        if (_proxyScaler is not null && await _proxyScaler(original, size, cancellationToken).ConfigureAwait(false) is { } scaled)
        {
            if (scaled.Size == size) return scaled;
            scaled.Dispose();
        }
        return new ResizeOperation(size.Width, size.Height, ResizeMode.Stretch, ResamplingFilter.Auto)
            .Apply(original, new OperationContext(_allocator, _limits, cancellationToken, maxDegreeOfParallelism: _maxDegreeOfParallelism));
    }

    private void DisposePreview()
    {
        var preview = _preview;
        _preview = null;
        preview?.Dispose();
    }

    /// <summary>Releases the source image, proxy and preview.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Cancel but do not dispose: the token source belongs to the render that created it, and that render is
        // still reading its token. It disposes it on the way out.
        CancelQuietly(Interlocked.Exchange(ref _renderCts, null));
        DisposePreview();
        var proxy = _proxy;
        _proxy = null;
        var releaseProxy = proxy is not null && !ReferenceEquals(proxy, _original) ? proxy : null;
        var original = _original;
        var draft = _draftSource;
        _draftSource = null;
        WhenIdle(() =>
        {
            releaseProxy?.Dispose();
            original.Dispose();
            draft?.Dispose();
            DisposeCaches();
        });
    }

    /// <summary>
    /// Releases buffers once no render is reading them. A cancelled render only notices at its next check, and
    /// returning its input to the pool before then would let another image overwrite the pixels mid-read.
    /// </summary>
    /// <remarks>
    /// Never waits synchronously: <see cref="SemaphoreSlim.Wait(int)"/> is unsupported on the browser, and .NET 11's
    /// single-threaded WebAssembly runtimes throw even for a zero timeout. Waiting on a free lock completes at once, so
    /// when no render runs the buffers are still released before this returns.
    /// </remarks>
    private void WhenIdle(Action release) => _ = ReleaseAfterRenderAsync(release);

    private async Task ReleaseAfterRenderAsync(Action release)
    {
        await _renderLock.WaitAsync().ConfigureAwait(false);
        try { release(); }
        finally { _renderLock.Release(); }
    }
}

/// <summary>Options for an <see cref="ImageEditSession"/>.</summary>
public sealed class ImageEditSessionOptions
{
    /// <summary>
    /// Longest edge of the interactive proxy. 2048 keeps editing smooth on phones while staying sharp on a desktop
    /// display. Set to 0 to always edit at full resolution.
    /// </summary>
    public int MaxProxyDimension { get; set; } = 2048;

    /// <summary>Number of undo steps kept.</summary>
    public int MaxHistoryDepth { get; set; } = 100;

    /// <summary>Allocator used for proxies and previews.</summary>
    public IPixelAllocator? Allocator { get; set; }

    /// <summary>Limits applied to rendering.</summary>
    public ImageLimits? Limits { get; set; }

    /// <summary>Rasteriser used for text annotations.</summary>
    public ITextRasterizer? TextRasterizer { get; set; }

    /// <summary>
    /// The most threads one operation may spread its rows over while rendering, or null for
    /// <see cref="OperationContext.DefaultMaxDegreeOfParallelism"/>.
    /// </summary>
    public int? MaxDegreeOfParallelism
    {
        get;
        set => field = value is < 1 ? throw new ArgumentOutOfRangeException(nameof(MaxDegreeOfParallelism), "At least one thread is needed.") : value;
    }

    /// <summary>
    /// Builds the working proxy, or returns null to leave it to the managed resampler. The editor supplies the
    /// browser's resampler here on WebAssembly. The preview is all it affects: export always replays the edit on the
    /// original pixels.
    /// </summary>
    internal Func<ImageBuffer, Size, CancellationToken, ValueTask<ImageBuffer?>>? ProxyScaler { get; set; }
}
