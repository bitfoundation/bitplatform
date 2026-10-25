using System.Collections.Immutable;
using System.Drawing;
using System.Numerics;
using Bit.Bmage.Annotations;
using Bit.Bmage.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Layers;
using Bit.Bmage.Operations;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Editing;

/// <summary>
/// The complete, non-destructive description of an edit: the pipeline applied to the pixels plus the annotation overlay.
/// A document is an immutable value, which is what makes undo, redo and live previews cheap. The original pixels are
/// held separately and never modified.
/// </summary>
public sealed record ImageDocument
{
    private ImageDocument(Size sourceSize, ImagePipeline pipeline, ImmutableList<Layer> layers, ImmutableList<Annotation> annotations)
    {
        SourceSize = sourceSize;
        Pipeline = pipeline;
        LayerList = layers;
        AnnotationList = annotations;
    }

    /// <summary>Creates an empty document for an image of the given size.</summary>
    public static ImageDocument Create(Size sourceSize)
    {
        if (sourceSize.Width <= 0 || sourceSize.Height <= 0) throw new ArgumentOutOfRangeException(nameof(sourceSize));
        return new ImageDocument(sourceSize, ImagePipeline.Empty, [], []);
    }

    /// <summary>Creates an empty document sized to an image.</summary>
    public static ImageDocument Create(ImageBuffer source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Create(source.Size);
    }

    /// <summary>Size of the untouched source image.</summary>
    public Size SourceSize { get; }

    /// <summary>Pixel operations applied to the source, in order.</summary>
    public ImagePipeline Pipeline { get; private init; }

    private ImmutableList<Layer> LayerList { get; init; }

    /// <summary>
    /// Layers composited onto the pipeline's output, bottom first, before the annotations are drawn over them: placed
    /// images, colour fills, adjustment, paint and group layers, each with a blend mode, an opacity and an optional mask.
    /// The layers inside groups are in each <see cref="GroupLayer.Layers"/>; the layer edits below find them at any depth.
    /// </summary>
    public IReadOnlyList<Layer> Layers => LayerList;

    private ImmutableList<Annotation> AnnotationList { get; init; }

    /// <summary>The annotation overlay, in draw order.</summary>
    public IReadOnlyList<Annotation> Annotations => AnnotationList;

    /// <summary>Size of the image after the pipeline runs, before layers and annotations (which never change the size).</summary>
    /// <exception cref="ArgumentException">The pipeline cannot be applied to an image of <see cref="SourceSize"/>.</exception>
    public Size OutputSize => Pipeline.GetOutputSize(SourceSize);

    /// <summary>
    /// Size of the rendered image, or false when the current pipeline cannot be applied to the source. Use this rather
    /// than <see cref="OutputSize"/> in UI code, which must keep rendering while an edit is being composed.
    /// </summary>
    public bool TryGetOutputSize(out Size outputSize) => Pipeline.TryGetOutputSize(SourceSize, out outputSize);

    /// <summary>True when nothing has been changed.</summary>
    public bool IsEmpty => Pipeline.IsEmpty && LayerList.IsEmpty && AnnotationList.IsEmpty;

    // ---- Pipeline edits ----

    /// <summary>Returns a document with an operation appended.</summary>
    public ImageDocument AddOperation(IImageOperation operation) => this with { Pipeline = Pipeline.Add(operation) };

    /// <summary>Returns a document with a different pipeline.</summary>
    public ImageDocument WithPipeline(ImagePipeline pipeline)
    {
        pipeline ??= ImagePipeline.Empty;
        return ReferenceEquals(pipeline, Pipeline) ? this : this with { Pipeline = pipeline };
    }

    /// <summary>
    /// Returns a document with a different pipeline, its annotations and layers moved the way the change moves the
    /// image, so each stays on the content it marks: crop the image and an arrow keeps pointing at the same thing, turn
    /// it and a label turns with it, straighten it a little more and they follow. With
    /// <paramref name="keepOverlaysAttached"/> false, or when either pipeline's effect on coordinates is not affine (see
    /// <see cref="ImagePipeline.TryGetTransform"/>), the overlays stay where they are in output coordinates, as
    /// <see cref="WithPipeline(ImagePipeline)"/> leaves them.
    /// </summary>
    public ImageDocument WithPipeline(ImagePipeline pipeline, bool keepOverlaysAttached)
    {
        var next = WithPipeline(pipeline);
        if (!keepOverlaysAttached || ReferenceEquals(next, this) || (LayerList.IsEmpty && AnnotationList.IsEmpty)) return next;
        if (!Pipeline.TryGetTransform(SourceSize, out var before) || !next.Pipeline.TryGetTransform(SourceSize, out var after)) return next;
        if (!Matrix3x2.Invert(before, out var inverse)) return next;
        // From the old output back to the source, then forward to the new output.
        var change = inverse * after;
        return IsNearlyIdentity(change) ? next : next.TransformOverlays(change);
    }

    /// <summary>
    /// Returns a document with an operation appended and, with <paramref name="keepOverlaysAttached"/>, its annotations
    /// and layers moved with the image as <see cref="WithPipeline(ImagePipeline, bool)"/> moves them. What the editor's
    /// crop, rotate, flip, straighten and resize use.
    /// </summary>
    public ImageDocument AddOperation(IImageOperation operation, bool keepOverlaysAttached) => WithPipeline(Pipeline.Add(operation), keepOverlaysAttached);

    /// <summary>
    /// Returns a document with every annotation and layer transformed by <paramref name="matrix"/>, which maps output
    /// coordinates to new output coordinates. See <see cref="Annotation.Transform"/> and <see cref="Layer.Transform"/>.
    /// </summary>
    public ImageDocument TransformOverlays(Matrix3x2 matrix) => matrix.IsIdentity ? this : this with
    {
        LayerList = [.. LayerList.Select(l => l.Transform(matrix))],
        AnnotationList = [.. AnnotationList.Select(a => a.Transform(matrix))],
    };

    /// <summary>Float noise from inverting a transform and composing it again is not a move.</summary>
    private static bool IsNearlyIdentity(Matrix3x2 m)
        => MathF.Abs(m.M11 - 1) < 1e-6f && MathF.Abs(m.M22 - 1) < 1e-6f && MathF.Abs(m.M12) < 1e-6f && MathF.Abs(m.M21) < 1e-6f
           && MathF.Abs(m.M31) < 1e-3f && MathF.Abs(m.M32) < 1e-3f;

    /// <summary>Returns a document with the operation at <paramref name="index"/> replaced, for re-editing a past step.</summary>
    public ImageDocument ReplaceOperation(int index, IImageOperation operation) => this with { Pipeline = Pipeline.Replace(index, operation) };

    /// <summary>Returns a document with the operation at <paramref name="index"/> removed.</summary>
    public ImageDocument RemoveOperation(int index) => this with { Pipeline = Pipeline.RemoveAt(index) };

    /// <summary>Returns a document with all pixel operations cleared, keeping annotations.</summary>
    public ImageDocument ClearOperations() => this with { Pipeline = ImagePipeline.Empty };

    // ---- Layer edits ----

    /// <summary>Returns a document with a layer added on top of the others.</summary>
    public ImageDocument AddLayer(Layer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        return this with { LayerList = LayerList.Add(layer) };
    }

    /// <summary>Returns a document with the layer of the same id, at any depth, replaced, or unchanged when the id is unknown.</summary>
    public ImageDocument UpdateLayer(Layer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        return EditLayers(layer.Id, (list, index) => ReferenceEquals(list[index], layer) ? list : list.SetItem(index, layer));
    }

    /// <summary>Returns a document with a layer, at any depth, removed by id.</summary>
    public ImageDocument RemoveLayer(Guid id) => EditLayers(id, (list, index) => list.RemoveAt(index));

    /// <summary>
    /// Returns a document with a layer moved to <paramref name="index"/> in the list that holds it, the document's or a
    /// group's, 0 being the bottom. The index is clamped to that list.
    /// </summary>
    public ImageDocument MoveLayer(Guid id, int index) => EditLayers(id, (list, from) =>
    {
        var to = Math.Clamp(index, 0, list.Count - 1);
        return to == from ? list : list.RemoveAt(from).Insert(to, list[from]);
    });

    /// <summary>
    /// Returns a document with a copy of a layer, under a new id, placed directly above it; unchanged when the id is
    /// unknown. The copy shares the original's pixels, which no layer owns; a group's copy holds copies of its layers,
    /// under new ids too.
    /// </summary>
    public ImageDocument DuplicateLayer(Guid id) => DuplicateLayer(id, out _);

    /// <summary>Duplicates a layer as <see cref="DuplicateLayer(Guid)"/> does, returning the copy's id, or null when the id is unknown.</summary>
    public ImageDocument DuplicateLayer(Guid id, out Guid? copyId)
    {
        Guid? made = null;
        var next = EditLayers(id, (list, index) =>
        {
            var copy = list[index].WithNewIds();
            made = copy.Id;
            return list.Insert(index + 1, copy);
        });
        copyId = made;
        return next;
    }

    /// <summary>Returns a document with every layer removed.</summary>
    public ImageDocument ClearLayers() => LayerList.IsEmpty ? this : this with { LayerList = [] };

    /// <summary>Finds a layer by id, at any depth.</summary>
    public Layer? FindLayer(Guid id) => TryLocateLayer(id, out var parent, out var index) ? (parent?.Layers ?? LayerList)[index] : null;

    /// <summary>
    /// Finds where a layer is: the group that holds it, or null for the document's own list, and its index in that list.
    /// </summary>
    public bool TryLocateLayer(Guid id, out GroupLayer? parent, out int index)
    {
        static bool Search(IReadOnlyList<Layer> list, GroupLayer? owner, Guid id, out GroupLayer? parent, out int index)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Id != id) continue;
                parent = owner;
                index = i;
                return true;
            }
            foreach (var layer in list)
                if (layer is GroupLayer group && Search(group.Layers, group, id, out parent, out index)) return true;
            parent = null;
            index = -1;
            return false;
        }

        return Search(LayerList, null, id, out parent, out index);
    }

    /// <summary>
    /// Returns a document with the given layers, which must share one list, gathered into a new group where the topmost
    /// of them was, in the order they were; unchanged, with a null id, when none of the ids is known.
    /// </summary>
    /// <exception cref="ArgumentException">The layers are in different lists, or the group would nest too deep.</exception>
    public ImageDocument GroupLayers(IEnumerable<Guid> ids, out Guid? groupId, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var wanted = ids.ToHashSet();
        groupId = null;
        if (wanted.Count == 0) return this;
        GroupLayer? owner = null;
        var found = 0;
        foreach (var id in wanted)
        {
            if (!TryLocateLayer(id, out var parent, out _)) continue;
            if (found++ > 0 && !ReferenceEquals(parent, owner))
                throw new ArgumentException("Only layers in the same list, the document's or one group's, can be grouped together.", nameof(ids));
            owner = parent;
        }
        if (found == 0) return this;

        var list = owner?.LayerList ?? LayerList;
        var members = list.Where(l => wanted.Contains(l.Id)).ToArray();
        var group = new GroupLayer(members) { Name = name };
        // The group takes the topmost member's place, once the others, all below it, are out of the list.
        var top = list.FindLastIndex(l => wanted.Contains(l.Id));
        var regrouped = list.RemoveAll(l => wanted.Contains(l.Id)).Insert(top - (members.Length - 1), group);
        groupId = group.Id;
        return owner is null ? this with { LayerList = regrouped } : UpdateLayer(owner with { Layers = regrouped });
    }

    /// <summary>
    /// Returns a document with a group replaced by its layers, in their order, where it was. The group's own opacity,
    /// blend mode and mask go with it, as in photo editors. Unchanged when the id is not a group's.
    /// </summary>
    public ImageDocument Ungroup(Guid groupId) => EditLayers(groupId, (list, index) =>
        list[index] is GroupLayer group ? list.RemoveAt(index).InsertRange(index, group.LayerList) : list);

    /// <summary>
    /// Returns a document with a layer moved, from wherever it is, to the top of a group's layers; unchanged when either
    /// id is unknown, the target is not a group, or it is the layer or inside it.
    /// </summary>
    /// <exception cref="ArgumentException">The group would nest too deep.</exception>
    public ImageDocument MoveLayerIntoGroup(Guid id, Guid groupId)
    {
        if (id == groupId || FindLayer(id) is not { } layer || FindLayer(groupId) is not GroupLayer) return this;
        if (layer is GroupLayer moving && Contains(moving, groupId)) return this;
        var removed = RemoveLayer(id);
        return removed.FindLayer(groupId) is GroupLayer target ? removed.UpdateLayer(target with { Layers = target.LayerList.Add(layer) }) : this;
    }

    /// <summary>
    /// Returns a document with a layer that is in a group moved out of it, directly above the group in the list that
    /// holds the group; unchanged when the layer is unknown or not in a group.
    /// </summary>
    public ImageDocument MoveLayerOutOfGroup(Guid id)
    {
        if (!TryLocateLayer(id, out var parent, out var index) || parent is null) return this;
        var layer = parent.LayerList[index];
        var emptied = UpdateLayer(parent with { Layers = parent.LayerList.RemoveAt(index) });
        return emptied.EditLayers(parent.Id, (list, at) => list.Insert(at + 1, layer));
    }

    private static bool Contains(GroupLayer group, Guid id)
        => group.LayerList.Any(l => l.Id == id || (l is GroupLayer inner && Contains(inner, id)));

    /// <summary>
    /// The document with the list holding the layer of <paramref name="id"/>, at any depth, replaced by what
    /// <paramref name="change"/> makes of it and the layer's index; unchanged when no layer has the id.
    /// </summary>
    private ImageDocument EditLayers(Guid id, Func<ImmutableList<Layer>, int, ImmutableList<Layer>> change)
    {
        static ImmutableList<Layer>? Edit(ImmutableList<Layer> list, Guid id, Func<ImmutableList<Layer>, int, ImmutableList<Layer>> change)
        {
            var index = list.FindIndex(l => l.Id == id);
            if (index >= 0) return change(list, index);
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] is not GroupLayer group || Edit(group.LayerList, id, change) is not { } edited) continue;
                return ReferenceEquals(edited, group.LayerList) ? list : list.SetItem(i, group with { Layers = edited });
            }
            return null;
        }

        var next = Edit(LayerList, id, change);
        return next is null || ReferenceEquals(next, LayerList) ? this : this with { LayerList = next };
    }

    // ---- Annotation edits ----

    /// <summary>Returns a document with an annotation added on top.</summary>
    public ImageDocument AddAnnotation(Annotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        return this with { AnnotationList = AnnotationList.Add(annotation) };
    }

    /// <summary>Returns a document with several annotations added.</summary>
    public ImageDocument AddAnnotations(IEnumerable<Annotation> annotations)
    {
        ArgumentNullException.ThrowIfNull(annotations);
        return this with { AnnotationList = AnnotationList.AddRange(annotations) };
    }

    /// <summary>Returns a document with the annotation of the same id replaced, or unchanged when the id is unknown.</summary>
    public ImageDocument UpdateAnnotation(Annotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        var index = AnnotationList.FindIndex(a => a.Id == annotation.Id);
        return index < 0 ? this : this with { AnnotationList = AnnotationList.SetItem(index, annotation) };
    }

    /// <summary>Returns a document with an annotation removed by id.</summary>
    public ImageDocument RemoveAnnotation(Guid id)
    {
        var index = AnnotationList.FindIndex(a => a.Id == id);
        return index < 0 ? this : this with { AnnotationList = AnnotationList.RemoveAt(index) };
    }

    /// <summary>Returns a document with all annotations removed.</summary>
    public ImageDocument ClearAnnotations() => AnnotationList.IsEmpty ? this : this with { AnnotationList = [] };

    /// <summary>Finds an annotation by id.</summary>
    public Annotation? FindAnnotation(Guid id) => AnnotationList.FirstOrDefault(a => a.Id == id);

    /// <summary>Moves an annotation to the front (drawn last).</summary>
    public ImageDocument BringToFront(Guid id)
    {
        var index = AnnotationList.FindIndex(a => a.Id == id);
        if (index < 0 || index == AnnotationList.Count - 1) return this;
        var item = AnnotationList[index];
        return this with { AnnotationList = AnnotationList.RemoveAt(index).Add(item) };
    }

    /// <summary>Moves an annotation to the back (drawn first).</summary>
    public ImageDocument SendToBack(Guid id)
    {
        var index = AnnotationList.FindIndex(a => a.Id == id);
        if (index <= 0) return this;
        var item = AnnotationList[index];
        return this with { AnnotationList = AnnotationList.RemoveAt(index).Insert(0, item) };
    }

    /// <summary>
    /// The annotations in the order they should be drawn: sorted by <see cref="Annotation.ZIndex"/>, ties broken by
    /// their position in the list. Redactions always come first so later annotations are not blurred away.
    /// </summary>
    public IReadOnlyList<Annotation> GetRenderOrder()
    {
        var indexed = AnnotationList.Select((a, i) => (Annotation: a, Index: i));
        return
        [
            .. indexed.Where(x => x.Annotation is RedactionAnnotation).OrderBy(x => x.Annotation.ZIndex).ThenBy(x => x.Index).Select(x => x.Annotation),
            .. indexed.Where(x => x.Annotation is not RedactionAnnotation).OrderBy(x => x.Annotation.ZIndex).ThenBy(x => x.Index).Select(x => x.Annotation),
        ];
    }

    /// <summary>Returns the topmost annotation at a point in output coordinates, or null.</summary>
    public Annotation? HitTest(Vector2 point, float tolerance = 4f)
    {
        var order = GetRenderOrder();
        for (var i = order.Count - 1; i >= 0; i--)
        {
            var a = order[i];
            if (a.IsVisible && !a.IsLocked && a.HitTest(point, tolerance)) return a;
        }
        return null;
    }

    /// <summary>
    /// Returns a document expressed for an image scaled by <paramref name="scale"/>, so a full-resolution edit can be
    /// previewed on a smaller proxy.
    /// </summary>
    public ImageDocument ForScale(double scale)
    {
        if (scale == 1) return this;
        return new ImageDocument(
            new Size(Math.Max(1, (int)Math.Round(SourceSize.Width * scale)), Math.Max(1, (int)Math.Round(SourceSize.Height * scale))),
            Pipeline.ForScale(scale),
            [.. LayerList.Select(l => l.ForScale(scale))],
            [.. AnnotationList.Select(a => a.ForScale(scale))]);
    }
}

/// <summary>Renders an <see cref="ImageDocument"/>: pixel pipeline first, then the layers, then the annotation overlay.</summary>
public sealed class DocumentRenderer
{
    private readonly ITextRasterizer? _textRasterizer;

    public DocumentRenderer(ITextRasterizer? textRasterizer = null) => _textRasterizer = textRasterizer;

    /// <summary>Renders a document against its source image. The caller owns the returned buffer.</summary>
    public ImageBuffer Render(ImageDocument document, ImageBuffer source, PipelineExecutionOptions? options = null, double annotationScale = 1.0)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(source);
        options ??= PipelineExecutionOptions.Default;
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
    public async ValueTask<ImageBuffer> RenderAsync(ImageDocument document, ImageBuffer source, PipelineExecutionOptions? options = null, double annotationScale = 1.0)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(source);
        options ??= PipelineExecutionOptions.Default;
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
    private static (PipelineExecutionOptions Pipeline, IProgress<double>? Layers) SplitProgress(ImageDocument document, PipelineExecutionOptions options)
    {
        if (options.Progress is not { } progress || document.Layers.Count == 0) return (options, null);
        var steps = document.Pipeline.Count;
        var share = steps / (double)(steps + document.Layers.Count);
        return (options with { Progress = share > 0 ? new ScaledProgress(progress, 0, share) : null }, new ScaledProgress(progress, share, 1 - share));
    }

    /// <summary>The context layers and annotations are drawn with: the run's allocator, limits, threads and cancellation.</summary>
    internal static OperationContext ContextFor(PipelineExecutionOptions options, IProgress<double>? progress = null)
        => new(options.Allocator, options.Limits, options.CancellationToken, progress, maxDegreeOfParallelism: options.MaxDegreeOfParallelism);

    /// <summary>
    /// Composites layers, already at the target's scale, onto the pipeline's output in place, reporting through
    /// <paramref name="progress"/> and keeping prepared layer images in <paramref name="cache"/> when there is one.
    /// </summary>
    internal static void CompositeLayers(IReadOnlyList<Layer> layers, ImageBuffer target, PipelineExecutionOptions? options, IProgress<double>? progress = null, LayerImageCache? cache = null)
    {
        if (layers.Count == 0) return;
        LayerCompositor.Composite(target, layers, ContextFor(options ?? PipelineExecutionOptions.Default, progress), cache);
    }

    /// <summary>Draws only the annotation overlay onto an existing buffer.</summary>
    public void RenderAnnotations(ImageDocument document, ImageBuffer target, double annotationScale = 1.0, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(target);
        RenderAnnotations(document.GetRenderOrder(), target, annotationScale, OperationContext.Default.WithCancellation(cancellationToken));
    }

    /// <summary>
    /// Draws annotations, already in render order and at the target's scale, onto an existing buffer, with the
    /// allocator, limits, threads and cancellation of <paramref name="context"/>.
    /// </summary>
    internal void RenderAnnotations(IReadOnlyList<Annotation> order, ImageBuffer target, double annotationScale, OperationContext context)
    {
        if (order.Count == 0) return;
        var renderContext = new AnnotationRenderContext(_textRasterizer, annotationScale) { Operation = context };
        using var canvas = ImageCanvas.FromBuffer(target, context);
        foreach (var annotation in order)
        {
            context.ThrowIfCancellationRequested();
            annotation.RenderWithTransform(canvas, renderContext);
        }
    }
}
