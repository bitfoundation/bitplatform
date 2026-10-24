using System.Buffers;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Bit.Bmage.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Masks;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Layers;

/// <summary>
/// One layer of a composition, blended onto everything beneath it: placed pixels (<see cref="ImageLayer"/>), a colour
/// (<see cref="FillLayer"/>), an edit of what is below (<see cref="AdjustmentLayer"/>), painted strokes
/// (<see cref="PaintLayer"/>) or other layers kept together (<see cref="GroupLayer"/>). Every layer has a blend mode,
/// an opacity and an optional mask, may be clipped to the layer below it, and like operations and annotations it is an
/// immutable value, so layer edits get undo, redo and proxy previews for free.
/// </summary>
/// <remarks>
/// Coordinates, including the mask's, are those of the image the layer is composited onto: in an
/// <see cref="Editing.ImageDocument"/>, the output of its pipeline.
/// </remarks>
public abstract record Layer
{
    /// <summary>Stable identifier, generated when the layer is created.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>A name to show in a layer list, or null to show <see cref="Kind"/>.</summary>
    public string? Name { get; init; }

    /// <summary>How the layer combines with what is beneath it.</summary>
    public BlendMode BlendMode { get; init; } = BlendMode.SourceOver;

    /// <summary>Overall opacity in [0,1].</summary>
    public float Opacity
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 1f;

    /// <summary>Where the layer shows, or null for everywhere it has pixels.</summary>
    public Mask? Mask { get; init; }

    /// <summary>Whether the layer is composited. Hidden layers stay in the document.</summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>Whether an editor should prevent the user from changing this layer.</summary>
    public bool IsLocked { get; init; }

    /// <summary>
    /// Whether the layer shows only where the nearest layer below it that is not clipped shows: the clipping mask of
    /// photo editors, for a texture inside lettering or an adjustment of one layer alone. That base layer and the layers
    /// clipped to it are drawn together, as if the base were opaque beneath them, then limited to its transparency and
    /// blended with its blend mode and opacity; hiding the base hides them too. The lowest layer of a list has nothing to
    /// clip to and is drawn as usual.
    /// </summary>
    public bool ClipToLayerBelow { get; init; }

    /// <summary>A short type name for UI and serialisation, e.g. "Image".</summary>
    public abstract string Kind { get; }

    /// <summary>The name to show for the layer.</summary>
    public string DisplayName => Name ?? Kind;

    /// <summary>True when compositing the layer cannot change anything.</summary>
    public virtual bool IsNoOp => !IsVisible || Opacity <= 0 || Mask is { IsEmpty: true };

    /// <summary>
    /// Blends the layer onto <paramref name="target"/>, which holds the composite of every layer beneath it and is
    /// modified in place.
    /// </summary>
    public void Composite(ImageBuffer target, OperationContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (IsNoOp) return;
        CompositeCore(target, context ?? OperationContext.Default);
    }

    /// <summary>Blends the layer onto the target. Called only for a visible layer with some opacity.</summary>
    protected internal abstract void CompositeCore(ImageBuffer target, OperationContext context);

    /// <summary>Returns the layer expressed for an image scaled by <paramref name="scale"/>, its mask included.</summary>
    public Layer ForScale(double scale) => scale == 1 ? this : ScaleCore(scale) with { Mask = Mask?.ForScale(scale) };

    /// <summary>The layer with its own geometry scaled; the mask is scaled by <see cref="ForScale"/>.</summary>
    protected abstract Layer ScaleCore(double scale);

    /// <summary>
    /// Returns the layer as it belongs after the image beneath it is transformed by <paramref name="matrix"/>, which maps
    /// the image's coordinates before the edit to those after it, its mask included: the layer stays on the content it
    /// covers. A layer over the whole image, a fill or an adjustment, keeps covering it, and only its mask moves.
    /// </summary>
    public Layer Transform(Matrix3x2 matrix) => matrix.IsIdentity ? this : TransformCore(matrix) with { Mask = Mask?.Transform(matrix) };

    /// <summary>The layer with its own geometry transformed; the mask is transformed by <see cref="Transform"/>.</summary>
    protected virtual Layer TransformCore(Matrix3x2 matrix) => this;

    /// <summary>A copy under a new id, and new ids for every layer it holds, as a duplicate needs.</summary>
    internal virtual Layer WithNewIds() => this with { Id = Guid.NewGuid() };

    /// <summary>The layer's pixels for row <paramref name="y"/> of the target, starting at the composited area's left.</summary>
    protected delegate ReadOnlySpan<Rgba32> LayerRow(int y);

    /// <summary>
    /// Blends <paramref name="rows"/> of layer pixels onto the target through the layer's <see cref="BlendMode"/>,
    /// <see cref="Opacity"/> and <see cref="Mask"/>, over <paramref name="area"/> and on as many threads as the context
    /// allows. <paramref name="rows"/> returns the layer's pixels for a target row, starting at <paramref name="area"/>'s
    /// left, and may be called from several threads at once. With <paramref name="mixForSourceOver"/>, the normal mode
    /// replaces what is below by the coverage instead of compositing over it, as an edit of the layers below does.
    /// </summary>
    /// <remarks>A custom layer produces its pixels and calls this; it never needs to apply the mask itself.</remarks>
    protected void BlendRows(ImageBuffer target, Rectangle area, OperationContext context, LayerRow rows, bool mixForSourceOver = false)
    {
        // The layer's rows start at the area's left as it was given, before the image or the mask narrows it.
        var origin = area.Left;
        area.Intersect(target.Bounds);
        if (Mask is not null) area.Intersect(Mask.GetAffectedBounds(target.Size));
        if (area.IsEmptyArea()) return;
        // Only the part of the mask over the area is drawn, so a small watermark's mask costs its own pixels, not the image's.
        using var mask = Mask?.RenderWindow(target.Size, area, target, context.WithProgress(null));
        var mode = BlendMode;
        var opacity = Opacity;
        // An edit at full strength through a normal layer replaces what is below, exactly; partly, it mixes towards it.
        var mix = mixForSourceOver && mode == BlendMode.SourceOver;
        var coverageScale = (int)MathF.Round(opacity * 255);
        ParallelRows.For(context, area.Top, area.Bottom, area.Width * (mode is BlendMode.SourceOver or BlendMode.Copy ? 1L : 4L), (start, end, band) =>
        {
            var scaled = mix && mask is not null && coverageScale < 255 ? ArrayPool<byte>.Shared.Rent(area.Width) : null;
            try
            {
                for (var y = start; y < end; y++)
                {
                    if (((y - start) & 31) == 0) band.ThrowIfCancellationRequested();
                    var source = rows(y).Slice(area.Left - origin, area.Width);
                    var destination = target.GetRow(y).Slice(area.Left, area.Width);
                    var coverage = mask is null ? default : (ReadOnlySpan<byte>)mask.GetRow(y - area.Top);
                    if (!mix)
                    {
                        Compositor.BlendRow(source, destination, mode, opacity, coverage);
                        continue;
                    }
                    if (coverage.IsEmpty)
                    {
                        if (coverageScale >= 255) source.CopyTo(destination);
                        else for (var x = 0; x < destination.Length; x++) destination[x] = MaskedOperation.Mix(destination[x], source[x], coverageScale);
                        continue;
                    }
                    if (scaled is not null)
                    {
                        var weights = scaled.AsSpan(0, area.Width);
                        for (var x = 0; x < weights.Length; x++) weights[x] = (byte)((coverage[x] * coverageScale + 127) / 255);
                        coverage = weights;
                    }
                    MaskedOperation.Mix(destination, source, coverage, destination);
                }
            }
            finally
            {
                if (scaled is not null) ArrayPool<byte>.Shared.Return(scaled);
            }
        });
    }
}

/// <summary>
/// Pixels placed on the composition: a photo, a texture, a logo, a sticker. The image is drawn into
/// <see cref="Bounds"/>, rotated about its centre, after its own <see cref="Pipeline"/> has been applied to it.
/// </summary>
public sealed record ImageLayer : Layer
{
    /// <summary>The layer's pixels. Not owned by the layer: the caller keeps it alive while the layer is used.</summary>
    public required ImageBuffer Image
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(Image));
    }

    /// <summary>Where the image is drawn, in pixels of the image the layer is composited onto.</summary>
    public required RectangleF Bounds { get; init; }

    /// <summary>Clockwise rotation in degrees about the centre of <see cref="Bounds"/>.</summary>
    public float Rotation { get; init; }

    /// <summary>
    /// Edits applied to the layer's own pixels before it is placed: desaturating a texture, blurring a backdrop. Run at
    /// the resolution the image is drawn at when that is smaller than the image, the way a proxy preview runs an edit.
    /// </summary>
    public ImagePipeline Pipeline
    {
        get;
        init => field = value ?? ImagePipeline.Empty;
    } = ImagePipeline.Empty;

    /// <summary>How the image is resampled into <see cref="Bounds"/>.</summary>
    public ResamplingFilter Filter { get; init; } = ResamplingFilter.Auto;

    public override string Kind => "Image";

    public override bool IsNoOp => base.IsNoOp || Bounds.Width <= 0 || Bounds.Height <= 0;

    /// <summary>The area the layer covers on an image, after rotation.</summary>
    public RectangleF GetRotatedBounds() => Rotation == 0 ? Bounds : MaskRasterizer.BoundsOf(RotatedCorners());

    /// <summary>The corners of <see cref="Bounds"/> turned by <see cref="Rotation"/> about its centre, from the top left clockwise.</summary>
    private Vector2[] RotatedCorners()
    {
        var m = Matrix3x2.CreateRotation(Rotation * MathF.PI / 180f, new Vector2(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2));
        return
        [
            Vector2.Transform(new Vector2(Bounds.Left, Bounds.Top), m),
            Vector2.Transform(new Vector2(Bounds.Right, Bounds.Top), m),
            Vector2.Transform(new Vector2(Bounds.Right, Bounds.Bottom), m),
            Vector2.Transform(new Vector2(Bounds.Left, Bounds.Bottom), m),
        ];
    }

    protected internal override void CompositeCore(ImageBuffer target, OperationContext context) => Composite(target, context, cache: null);

    /// <summary>Blends the layer onto the target, taking its prepared image from <paramref name="cache"/> when there is one.</summary>
    internal void Composite(ImageBuffer target, OperationContext context, LayerImageCache? cache)
    {
        if (IsNoOp) return;
        if (Image.IsDisposed) throw new ObjectDisposedException(nameof(Image), "The image this layer draws has been disposed.");
        var bounds = GetRotatedBounds();
        if (!float.IsFinite(bounds.Left) || !float.IsFinite(bounds.Top) || !float.IsFinite(bounds.Right) || !float.IsFinite(bounds.Bottom)) return;
        // A layer this far out cannot be placed in whole pixels at all.
        const float far = 1 << 24;
        if (!(bounds.Left > -far && bounds.Top > -far && bounds.Right < far && bounds.Bottom < far)) return;
        // An upright layer lands on whole pixels, rounded exactly as the canvas rounds a destination; a rotated one is
        // sampled at every pixel its outline reaches.
        var device = Rotation == 0
            ? Rectangle.FromLTRB((int)MathF.Round(Bounds.Left), (int)MathF.Round(Bounds.Top), (int)MathF.Round(Bounds.Right), (int)MathF.Round(Bounds.Bottom))
            : Rectangle.FromLTRB((int)MathF.Floor(bounds.Left), (int)MathF.Floor(bounds.Top), (int)MathF.Ceiling(bounds.Right), (int)MathF.Ceiling(bounds.Bottom));
        if (device.Width <= 0 || device.Height <= 0) return;
        var area = device;
        area.Intersect(target.Bounds);
        if (area.IsEmptyArea()) return;

        // The canvas draws with the layer's allocator, limits, threads and cancellation, and reports nothing itself.
        var drawing = context.WithProgress(null);
        var pixels = Pipeline.IsEmpty ? Image : cache is null ? PrepareImage(drawing) : cache.GetOrAdd(this, drawing);
        try
        {
            if (Mask is null && Rotation == 0)
            {
                // Without a mask the canvas blends the image straight onto the target, with the same arithmetic.
                using var direct = ImageCanvas.FromBuffer(target, drawing);
                direct.Opacity = Opacity;
                direct.DrawImage(pixels, device, BlendMode, Filter);
                return;
            }
            // The layer drawn on its own, over transparency, across the part of the target it covers. Shifting by whole
            // pixels keeps every sample where drawing it on the target would put it.
            using var raster = ImageBuffer.Create(area.Width, area.Height, context.Allocator, clear: true, ImageLimits.Unrestricted);
            using (var canvas = ImageCanvas.FromBuffer(raster, drawing))
            {
                if (Rotation == 0)
                {
                    canvas.DrawImage(pixels, new RectangleF(device.X - area.X, device.Y - area.Y, device.Width, device.Height), BlendMode.SourceOver, Filter);
                }
                else
                {
                    canvas.Translate(-area.X, -area.Y);
                    canvas.Rotate(Rotation, new Vector2(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2));
                    canvas.DrawImage(pixels, Bounds, BlendMode.SourceOver, Filter);
                }
            }
            // Copy replaces what is below, so a rotated layer must stop at its own outline: the raster's corners around
            // it are empty, not part of the layer.
            var layer = BlendMode == BlendMode.Copy && Rotation != 0
                ? this with { Mask = Mask is null ? Outline() : Mask.Intersect(Outline()) }
                : this;
            layer.BlendRows(target, area, context, y => raster.GetRow(y - area.Y));
        }
        finally
        {
            // A cached image stays with the cache for the next composite.
            if (!ReferenceEquals(pixels, Image) && cache is null) pixels.Dispose();
        }
    }

    /// <summary>The layer's rotated rectangle, in pixels.</summary>
    private PolygonMask Outline() => new(RotatedCorners());

    /// <summary>
    /// How far the image is shrunk before its pipeline runs: to the size the pipeline's result is drawn at, when that is
    /// smaller. A crop or a resize in the pipeline changes what lands in the bounds, so the scale compares the bounds with
    /// the pipeline's result, not with the image.
    /// </summary>
    internal double PrepareScale()
    {
        var output = Pipeline.TryGetOutputSize(Image.Size, out var size) ? size : Image.Size;
        return Math.Min(1.0, Math.Min(Bounds.Width / Math.Max(1, output.Width), Bounds.Height / Math.Max(1, output.Height)));
    }

    /// <summary>
    /// The image with its pipeline applied: first shrunk to the size it is drawn at when that is smaller, so a
    /// full-resolution layer previewed on a proxy costs what the proxy does. The caller owns the result.
    /// </summary>
    internal ImageBuffer PrepareImage(OperationContext context)
    {
        var scale = PrepareScale();
        var options = new PipelineExecutionOptions
        {
            Allocator = context.Allocator,
            Limits = context.Limits,
            CancellationToken = context.CancellationToken,
            MaxDegreeOfParallelism = context.MaxDegreeOfParallelism,
        };
        if (scale >= 0.999) return Pipeline.Execute(Image, options);
        var width = Math.Max(1, (int)Math.Round(Image.Width * scale));
        var height = Math.Max(1, (int)Math.Round(Image.Height * scale));
        // Never in place: the image is the caller's, and a resize to its own size would hand it straight back.
        using var reduced = new ResizeOperation(width, height, ResizeMode.Stretch, Filter).Apply(Image, context.WithProgress(null).WithMutation(false));
        return Pipeline.ForScale(scale).Execute(reduced, options);
    }

    protected override Layer ScaleCore(double scale) => this with
    {
        Bounds = new RectangleF((float)(Bounds.X * scale), (float)(Bounds.Y * scale), (float)(Bounds.Width * scale), (float)(Bounds.Height * scale)),
    };

    /// <remarks>
    /// A move or a scale maps the bounds exactly, the two axes scaling apart if the image's do. Otherwise the layer's
    /// centre follows the image, its size scales with it and it turns with it; a mirror mirrors its pixels too, by a flip
    /// added to its pipeline, so a photo placed on the image stays a faithful part of it.
    /// </remarks>
    protected override Layer TransformCore(Matrix3x2 matrix)
    {
        if (Rotation == 0 && AffineParts.IsScaleAndTranslation(matrix)) return this with { Bounds = AffineParts.MapRectangle(Bounds, matrix) };
        var parts = AffineParts.Of(matrix);
        var centre = Vector2.Transform(new Vector2(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2), matrix);
        var size = new SizeF(Bounds.Width * parts.Scale, Bounds.Height * parts.Scale);
        return this with
        {
            Bounds = new RectangleF(centre.X - size.Width / 2, centre.Y - size.Height / 2, size.Width, size.Height),
            // A layer turned by r and then mirrored is the mirrored layer turned by -r.
            Rotation = AffineParts.Normalize(parts.Mirrored ? parts.Angle - Rotation : Rotation + parts.Angle),
            Pipeline = parts.Mirrored ? Pipeline.FlipHorizontal() : Pipeline,
        };
    }
}

/// <summary>
/// A colour over the whole image, usually shaped by a mask and combined through a blend mode: a colour wash with
/// <see cref="BlendMode.Color"/>, a duotone with <see cref="BlendMode.Multiply"/> and <see cref="BlendMode.Screen"/>,
/// a gradient fade through a <see cref="LinearGradientMask"/>.
/// </summary>
/// <param name="Color">The fill colour, alpha included.</param>
public sealed record FillLayer(Rgba32 Color) : Layer
{
    public override string Kind => "Fill";

    public override bool IsNoOp => base.IsNoOp || (Color.A == 0 && BlendMode != BlendMode.Copy);

    protected internal override void CompositeCore(ImageBuffer target, OperationContext context)
    {
        var row = new Rgba32[target.Width];
        row.AsSpan().Fill(Color);
        BlendRows(target, target.Bounds, context, _ => row);
    }

    protected override Layer ScaleCore(double scale) => this;
}

/// <summary>
/// An edit of everything beneath it, through the layer's mask, opacity and blend mode: the adjustment layer of photo
/// editors. Unlike a step in the document's pipeline it applies to the layers below it too, and it can be reordered,
/// hidden or faded without touching anything else.
/// </summary>
/// <remarks>
/// With <see cref="BlendMode.SourceOver"/> the edited pixels replace the ones below by the layer's coverage, so at full
/// opacity and without a mask the result is exactly the pipeline's. Other modes blend the edited pixels onto the
/// unedited ones: an empty pipeline in <see cref="BlendMode.Multiply"/> darkens the image by itself.
/// </remarks>
public sealed record AdjustmentLayer : Layer
{
    /// <param name="pipeline">The edit. Every operation in it must keep the image's size.</param>
    public AdjustmentLayer(ImagePipeline pipeline) => Pipeline = pipeline;

    /// <summary>The edit applied to what is below.</summary>
    public ImagePipeline Pipeline
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(Pipeline));
    }

    public override string Kind => "Adjustment";

    public override bool IsNoOp => base.IsNoOp || (BlendMode == BlendMode.SourceOver && Pipeline.All(operation => operation.IsIdentity));

    protected internal override void CompositeCore(ImageBuffer target, OperationContext context)
    {
        var size = Pipeline.GetOutputSize(target.Size);
        if (size != target.Size)
            throw new InvalidOperationException($"An adjustment layer must keep the image's size, but its pipeline turns {target.Width}x{target.Height} into {size.Width}x{size.Height}.");
        using var adjusted = Pipeline.Execute(target, new PipelineExecutionOptions
        {
            Allocator = context.Allocator,
            Limits = context.Limits,
            CancellationToken = context.CancellationToken,
            // The edit is most of what the layer costs, so it reports the layer's progress; blending it back ends it.
            Progress = context.Progress,
            MaxDegreeOfParallelism = context.MaxDegreeOfParallelism,
        });
        // A custom operation may produce another size than it said it would.
        if (adjusted.Size != target.Size)
            throw new InvalidOperationException($"An adjustment layer must keep the image's size, but its pipeline produced {adjusted.Width}x{adjusted.Height} from {target.Width}x{target.Height}.");
        BlendRows(target, target.Bounds, context.WithProgress(null), y => adjusted.GetRow(y), mixForSourceOver: true);
    }

    protected override Layer ScaleCore(double scale) => this with { Pipeline = Pipeline.ForScale(scale) };
}

/// <summary>Composites layers.</summary>
public static class LayerCompositor
{
    /// <summary>Blends <paramref name="layers"/>, bottom first, onto <paramref name="target"/> in place.</summary>
    public static void Composite(ImageBuffer target, IEnumerable<Layer> layers, OperationContext? context = null)
        => Composite(target, layers, context, cache: null);

    /// <summary>
    /// Blends the layers onto the target, keeping the images image layers are drawn with in <paramref name="cache"/>
    /// for the next composite of the same layers, and releasing those this one did not use.
    /// </summary>
    internal static void Composite(ImageBuffer target, IEnumerable<Layer> layers, OperationContext? context, LayerImageCache? cache)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(layers);
        context ??= OperationContext.Default;
        cache?.Begin();
        CompositeList(target, layers as IReadOnlyList<Layer> ?? [.. layers], context, cache);
        cache?.End();
    }

    /// <summary>
    /// Blends a list of layers, bottom first, the layers clipped to one drawn with it; groups composite their own lists
    /// the same way. Reports progress by the share of the list done.
    /// </summary>
    internal static void CompositeList(ImageBuffer target, IReadOnlyList<Layer> list, OperationContext context, LayerImageCache? cache)
    {
        for (var i = 0; i < list.Count;)
        {
            context.ThrowIfCancellationRequested();
            var end = i + 1;
            while (end < list.Count && list[end].ClipToLayerBelow) end++;
            // Each layer reports its progress within its own share of the whole.
            var progress = context.Progress is null ? null : new ScaledProgress(context.Progress, i / (double)list.Count, (end - i) / (double)list.Count);
            var part = context.WithProgress(progress);
            if (end == i + 1) CompositeOne(list[i], target, part, cache);
            else CompositeClipped(list[i], list, i + 1, end, target, part, cache);
            // A layer blended on one thread reports nothing along the way, so its end is reported here.
            context.ReportProgress(end / (double)list.Count);
            i = end;
        }
        if (list.Count == 0) context.ReportProgress(1);
    }

    private static void CompositeOne(Layer layer, ImageBuffer target, OperationContext context, LayerImageCache? cache)
    {
        switch (layer)
        {
            case ImageLayer image when cache is not null:
                image.Composite(target, context, cache);
                break;
            case GroupLayer group:
                group.Composite(target, context, cache);
                break;
            default:
                layer.Composite(target, context);
                break;
        }
    }

    /// <summary>
    /// A base layer and the layers clipped to it, <paramref name="list"/>[<paramref name="from"/>..<paramref name="to"/>):
    /// drawn together over transparency, the base at full strength, then blended with the base's mode and opacity.
    /// </summary>
    private static void CompositeClipped(Layer bottom, IReadOnlyList<Layer> list, int from, int to, ImageBuffer target, OperationContext context, LayerImageCache? cache)
    {
        // A hidden base hides what is clipped to it.
        if (bottom.IsNoOp) return;
        var inner = context.WithProgress(null);
        using var stack = ImageBuffer.Create(target.Width, target.Height, context.Allocator, clear: true, ImageLimits.Unrestricted);
        CompositeOne(bottom with { BlendMode = BlendMode.SourceOver, Opacity = 1 }, stack, inner, cache);

        // The base's transparency, which the layers clipped to it are limited to, and the area it covers: outside it
        // the base is transparent, so nothing clipped to it can show there and nothing outside it is blended.
        int width = stack.Width, pixels = width * stack.Height;
        var bytes = MemoryMarshal.AsBytes(stack.Pixels);
        var alpha = ArrayPool<byte>.Shared.Rent(pixels);
        try
        {
            int minX = width, maxX = -1, minY = -1, maxY = -1;
            for (var y = 0; y < stack.Height; y++)
            {
                int start = y * width, first = -1, last = -1;
                for (var x = 0; x < width; x++)
                {
                    var a = alpha[start + x] = bytes[(start + x) * 4 + 3];
                    if (a == 0) continue;
                    if (first < 0) first = x;
                    last = x;
                }
                if (first < 0) continue;
                if (minY < 0) minY = y;
                maxY = y;
                minX = Math.Min(minX, first);
                maxX = Math.Max(maxX, last);
            }
            if (minY < 0) return;
            var area = Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);

            var opaque = false;
            var drawnOutside = false;
            for (var i = from; i < to; i++)
            {
                var layer = list[i];
                if (layer.IsNoOp) continue;
                // A layer of pixels blends onto the base as if it were opaque, which is what limiting the result to the
                // base's transparency afterwards needs; an edit sees the base's real transparency, so a blur in it does
                // not pull in the colour of what is transparent, nor what a layer drew outside the base.
                var wantOpaque = layer is not AdjustmentLayer;
                if (wantOpaque && !opaque) SetAlpha(bytes, alpha, width, area, opaque: true);
                else if (opaque || drawnOutside) SetAlpha(bytes, alpha, width, drawnOutside ? stack.Bounds : area, opaque: false);
                opaque = wantOpaque;
                drawnOutside = wantOpaque;
                CompositeOne(layer, stack, inner, cache);
            }
            SetAlpha(bytes, alpha, width, area, opaque: false);
            new StackLayer(stack, area) { BlendMode = bottom.BlendMode, Opacity = bottom.Opacity }.Composite(target, context);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(alpha);
        }
    }

    /// <summary>Sets the alpha of every pixel in <paramref name="area"/> to opaque, or back to the base's.</summary>
    private static void SetAlpha(Span<byte> bytes, byte[] alpha, int width, Rectangle area, bool opaque)
    {
        for (var y = area.Top; y < area.Bottom; y++)
        {
            var start = y * width + area.Left;
            var end = start + area.Width;
            if (opaque) for (var p = start; p < end; p++) bytes[p * 4 + 3] = 255;
            else for (var p = start; p < end; p++) bytes[p * 4 + 3] = alpha[p];
        }
    }

    /// <summary>Layers already composited together, blended onto the target as one over <paramref name="Area"/>.</summary>
    private sealed record StackLayer(ImageBuffer Pixels, Rectangle Area) : Layer
    {
        public override string Kind => "Stack";

        protected internal override void CompositeCore(ImageBuffer target, OperationContext context)
            => BlendRows(target, Area, context, y => Pixels.GetRow(y)[Area.Left..]);

        protected override Layer ScaleCore(double scale) => this;
    }

    /// <summary>
    /// A new image of <paramref name="size"/> filled with <paramref name="background"/> (transparent by default) with
    /// <paramref name="layers"/> composited onto it, bottom first. The caller owns the result.
    /// </summary>
    /// <example>
    /// <code>
    /// using var card = LayerCompositor.Flatten(new Size(1200, 630), Rgba32.White,
    /// [
    ///     new ImageLayer { Image = photo, Bounds = new RectangleF(0, 0, 1200, 630) },
    ///     new FillLayer(new Rgba32(10, 20, 60)) { BlendMode = BlendMode.Multiply, Mask = Mask.Gradient(new(0, 1), new(0, 0.3f)) },
    ///     new ImageLayer { Image = logo, Bounds = new RectangleF(40, 40, 160, 160), Opacity = 0.9f },
    /// ]);
    /// </code>
    /// </example>
    public static ImageBuffer Flatten(Size size, Rgba32 background, IEnumerable<Layer> layers, OperationContext? context = null)
    {
        context ??= OperationContext.Default;
        var image = ImageBuffer.Create(size.Width, size.Height, context.Allocator, clear: true, context.Limits);
        try
        {
            if (background.A != 0) image.Fill(background);
            Composite(image, layers, context);
            return image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
    }
}

/// <summary>
/// The images image layers are drawn with, shrunk and run through their pipeline once and kept from one composite to
/// the next: dragging a layer's opacity or moving it blends it again without preparing its image again. Used by one
/// composite at a time; it relies on a layer's image not changing in place, as an edit session does.
/// </summary>
internal sealed class LayerImageCache
{
    // The image, its pipeline and filter, and the scale it is shrunk by: the same instances and scale mean the same pixels.
    private readonly record struct Key(ImageBuffer Image, ImagePipeline Pipeline, ResamplingFilter Filter, double Scale);

    private sealed class Entry(ImageBuffer pixels)
    {
        public ImageBuffer Pixels { get; } = pixels;
        public int Composite { get; set; }
    }

    private readonly Dictionary<Key, Entry> _entries = [];
    private int _composite;

    /// <summary>Starts a composite: what it does not use is released at its <see cref="End"/>.</summary>
    public void Begin() => _composite++;

    /// <summary>The prepared image of <paramref name="layer"/>, prepared now unless an earlier composite kept it. The cache owns it.</summary>
    public ImageBuffer GetOrAdd(ImageLayer layer, OperationContext context)
    {
        var key = new Key(layer.Image, layer.Pipeline, layer.Filter, layer.PrepareScale());
        if (!_entries.TryGetValue(key, out var entry))
        {
            entry = new Entry(layer.PrepareImage(context));
            _entries[key] = entry;
        }
        entry.Composite = _composite;
        return entry.Pixels;
    }

    /// <summary>Ends a composite, releasing the images it did not draw. A composite that failed leaves everything for the next one.</summary>
    public void End()
    {
        foreach (var (key, entry) in _entries.Where(e => e.Value.Composite != _composite).ToArray())
        {
            entry.Pixels.Dispose();
            _entries.Remove(key);
        }
    }

    /// <summary>Releases every image.</summary>
    public void Clear()
    {
        foreach (var entry in _entries.Values) entry.Pixels.Dispose();
        _entries.Clear();
    }
}
