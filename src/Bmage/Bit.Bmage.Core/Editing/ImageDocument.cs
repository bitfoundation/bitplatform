using System.Collections.Immutable;
using System.Drawing;
using System.Numerics;
using Bit.Bmage.Annotations;
using Bit.Bmage.Drawing;
using Bit.Bmage.Geometry;
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
    private ImageDocument(Size sourceSize, ImagePipeline pipeline, ImmutableList<Annotation> annotations)
    {
        SourceSize = sourceSize;
        Pipeline = pipeline;
        AnnotationList = annotations;
    }

    /// <summary>Creates an empty document for an image of the given size.</summary>
    public static ImageDocument Create(Size sourceSize)
    {
        if (sourceSize.Width <= 0 || sourceSize.Height <= 0) throw new ArgumentOutOfRangeException(nameof(sourceSize));
        return new ImageDocument(sourceSize, ImagePipeline.Empty, []);
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

    private ImmutableList<Annotation> AnnotationList { get; init; }

    /// <summary>The annotation overlay, in draw order.</summary>
    public IReadOnlyList<Annotation> Annotations => AnnotationList;

    /// <summary>Size of the image after the pipeline runs, before annotations (which never change the size).</summary>
    /// <exception cref="ArgumentException">The pipeline cannot be applied to an image of <see cref="SourceSize"/>.</exception>
    public Size OutputSize => Pipeline.GetOutputSize(SourceSize);

    /// <summary>
    /// Size of the rendered image, or false when the current pipeline cannot be applied to the source. Use this rather
    /// than <see cref="OutputSize"/> in UI code, which must keep rendering while an edit is being composed.
    /// </summary>
    public bool TryGetOutputSize(out Size outputSize) => Pipeline.TryGetOutputSize(SourceSize, out outputSize);

    /// <summary>True when nothing has been changed.</summary>
    public bool IsEmpty => Pipeline.IsEmpty && AnnotationList.IsEmpty;

    // ---- Pipeline edits ----

    /// <summary>Returns a document with an operation appended.</summary>
    public ImageDocument AddOperation(IImageOperation operation) => this with { Pipeline = Pipeline.Add(operation) };

    /// <summary>Returns a document with a different pipeline.</summary>
    public ImageDocument WithPipeline(ImagePipeline pipeline) => this with { Pipeline = pipeline ?? ImagePipeline.Empty };

    /// <summary>Returns a document with the operation at <paramref name="index"/> replaced, for re-editing a past step.</summary>
    public ImageDocument ReplaceOperation(int index, IImageOperation operation) => this with { Pipeline = Pipeline.Replace(index, operation) };

    /// <summary>Returns a document with the operation at <paramref name="index"/> removed.</summary>
    public ImageDocument RemoveOperation(int index) => this with { Pipeline = Pipeline.RemoveAt(index) };

    /// <summary>Returns a document with all pixel operations cleared, keeping annotations.</summary>
    public ImageDocument ClearOperations() => this with { Pipeline = ImagePipeline.Empty };

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
            [.. AnnotationList.Select(a => a.ForScale(scale))]);
    }
}

/// <summary>Renders an <see cref="ImageDocument"/>: pixel pipeline first, then the annotation overlay.</summary>
public sealed class DocumentRenderer
{
    private readonly ITextRasterizer? _textRasterizer;

    public DocumentRenderer(ITextRasterizer? textRasterizer = null) => _textRasterizer = textRasterizer;

    /// <summary>Renders a document against its source image. The caller owns the returned buffer.</summary>
    public ImageBuffer Render(ImageDocument document, ImageBuffer source, PipelineExecutionOptions? options = null, double annotationScale = 1.0)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(source);
        var result = document.Pipeline.Execute(source, options);
        try
        {
            RenderAnnotations(document, result, annotationScale, options?.CancellationToken ?? default);
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
        var result = await document.Pipeline.ExecuteAsync(source, options).ConfigureAwait(false);
        try
        {
            RenderAnnotations(document, result, annotationScale, options?.CancellationToken ?? default);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    /// <summary>Draws only the annotation overlay onto an existing buffer.</summary>
    public void RenderAnnotations(ImageDocument document, ImageBuffer target, double annotationScale = 1.0, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(target);
        var order = document.GetRenderOrder();
        if (order.Count == 0) return;
        var context = new AnnotationRenderContext(_textRasterizer, annotationScale);
        using var canvas = ImageCanvas.FromBuffer(target);
        foreach (var annotation in order)
        {
            cancellationToken.ThrowIfCancellationRequested();
            annotation.RenderWithTransform(canvas, context);
        }
    }
}
