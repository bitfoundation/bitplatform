using System.Drawing;
using Bit.Bmage.Drawing;
using Bit.Bmage.Geometry;
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
/// </remarks>
public sealed class ImageEditSession : IDisposable
{
    private readonly IPixelAllocator _allocator;
    private readonly ImageLimits _limits;
    private readonly DocumentRenderer _renderer;
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

    /// <summary>Creates a session that takes ownership of <paramref name="original"/>.</summary>
    public ImageEditSession(ImageBuffer original, ImageEditSessionOptions? options = null)
    {
        _original = original ?? throw new ArgumentNullException(nameof(original));
        var o = options ?? new ImageEditSessionOptions();
        _allocator = o.Allocator ?? PooledPixelAllocator.Shared;
        _limits = o.Limits ?? ImageLimits.Default;
        _renderer = new DocumentRenderer(o.TextRasterizer);
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
        _original = image;
        _proxy = null;
        _sourceVersion++;
        // A render of the old source is now pointless, and must not outlive the buffers it reads.
        CancelQuietly(Interlocked.Exchange(ref _renderCts, null));
        DisposePreview();
        UpdateProxyGeometry();
        History.Clear(ImageDocument.Create(image));
        DisposeWhenIdle(
            oldProxy is not null && !ReferenceEquals(oldProxy, old) ? oldProxy : null,
            disposeExisting && !ReferenceEquals(old, image) ? old : null);
        MarkPreviewStale();
        DocumentChanged?.Invoke(Document);
    }

    private void MarkPreviewStale() => IsPreviewStale = true;

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
    /// Renders the preview of the document as changed by <paramref name="overlay"/>, without recording that change.
    /// Use it to show something that is not part of the edit yet, such as a shape still being drawn. The resulting
    /// preview counts as stale, since it does not match the document.
    /// </summary>
    /// <exception cref="OperationCanceledException">The render was superseded by a newer one, or the caller cancelled.</exception>
    public async ValueTask<ImageBuffer> RenderPreviewAsync(Func<ImageDocument, ImageDocument>? overlay, CancellationToken cancellationToken = default)
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
            var source = Document;
            var proxy = EnsureProxy();
            var document = overlay is null ? source : overlay(source);
            if (UsesProxy) document = document.ForScale(ProxyScale);
            var rendered = await _renderer.RenderAsync(document, proxy, new PipelineExecutionOptions
            {
                Allocator = _allocator,
                Limits = _limits,
                CancellationToken = cts.Token,
            }, ProxyScale).ConfigureAwait(false);

            if (_disposed || sourceVersion != _sourceVersion)
            {
                // The session was disposed or given a new source while this ran; the result belongs to neither.
                rendered.Dispose();
                throw new OperationCanceledException(cts.Token);
            }

            var old = _preview;
            _preview = rendered;
            // An edit made while this ran is not in the result, so the preview is only fresh if nothing changed.
            IsPreviewStale = overlay is not null || !ReferenceEquals(source, Document);
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
    }

    private ImageBuffer EnsureProxy()
    {
        if (_proxy is { } existing) return existing;
        if (!UsesProxy) return _original;
        _proxy = new ResizeOperation(ProxySize.Width, ProxySize.Height, ResizeMode.Stretch, ResamplingFilter.Auto)
            .Apply(_original, new OperationContext(_allocator, _limits));
        return _proxy;
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
        DisposeWhenIdle(proxy is not null && !ReferenceEquals(proxy, _original) ? proxy : null, _original);
    }

    /// <summary>
    /// Disposes source buffers once no render is reading them. A cancelled render only notices at its next check, and
    /// returning its input to the pool before then would let another image overwrite the pixels mid-read.
    /// </summary>
    private void DisposeWhenIdle(ImageBuffer? first, ImageBuffer? second)
    {
        if (first is null && second is null) return;
        if (_renderLock.Wait(0))
        {
            try { first?.Dispose(); second?.Dispose(); }
            finally { _renderLock.Release(); }
            return;
        }
        _ = DisposeAfterRenderAsync(first, second);
    }

    private async Task DisposeAfterRenderAsync(ImageBuffer? first, ImageBuffer? second)
    {
        await _renderLock.WaitAsync().ConfigureAwait(false);
        try { first?.Dispose(); second?.Dispose(); }
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
}
