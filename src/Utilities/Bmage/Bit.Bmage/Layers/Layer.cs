using System.Buffers;
using System.Drawing;
using System.Numerics;
using Bit.Bmage.Drawing;
using Bit.Bmage.Geometry;
using Bit.Bmage.Masks;
using Bit.Bmage.Operations;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Layers;

/// <summary>
/// One layer of a composition, blended onto everything beneath it: placed pixels (<see cref="ImageLayer"/>), a colour
/// (<see cref="FillLayer"/>), or an edit of what is below (<see cref="AdjustmentLayer"/>). Every layer has a blend
/// mode, an opacity and an optional mask, and like operations and annotations it is an immutable value, so layer
/// edits get undo, redo and proxy previews for free.
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
        using var mask = Mask?.Render(target.Size, target, context.WithProgress(null));
        // The layer's rows start at the area's left as it was given, before the mask narrows it.
        var origin = area.Left;
        if (mask is not null)
        {
            area.Intersect(Mask!.GetAffectedBounds(target.Size));
            if (area.IsEmptyArea()) return;
        }
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
                    var coverage = mask is null ? default : (ReadOnlySpan<byte>)mask.GetRow(y).Slice(area.Left, area.Width);
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
    public RectangleF GetRotatedBounds()
    {
        if (Rotation == 0) return Bounds;
        var m = Matrix3x2.CreateRotation(Rotation * MathF.PI / 180f, new Vector2(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2));
        return MaskRasterizer.BoundsOf(
        [
            Vector2.Transform(new Vector2(Bounds.Left, Bounds.Top), m),
            Vector2.Transform(new Vector2(Bounds.Right, Bounds.Top), m),
            Vector2.Transform(new Vector2(Bounds.Right, Bounds.Bottom), m),
            Vector2.Transform(new Vector2(Bounds.Left, Bounds.Bottom), m),
        ]);
    }

    protected internal override void CompositeCore(ImageBuffer target, OperationContext context)
    {
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

        var pixels = PrepareImage(context);
        try
        {
            if (Mask is null && Rotation == 0)
            {
                // Without a mask the canvas blends the image straight onto the target, with the same arithmetic.
                using var direct = ImageCanvas.FromBuffer(target);
                direct.Opacity = Opacity;
                direct.DrawImage(pixels, device, BlendMode, Filter);
                return;
            }
            // The layer drawn on its own, over transparency, across the part of the target it covers. Shifting by whole
            // pixels keeps every sample where drawing it on the target would put it.
            using var raster = ImageBuffer.Create(area.Width, area.Height, context.Allocator, clear: true, ImageLimits.Unrestricted);
            using (var canvas = ImageCanvas.FromBuffer(raster))
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
            if (!ReferenceEquals(pixels, Image)) pixels.Dispose();
        }
    }

    /// <summary>The layer's rotated rectangle, in pixels.</summary>
    private PolygonMask Outline()
    {
        var m = Matrix3x2.CreateRotation(Rotation * MathF.PI / 180f, new Vector2(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2));
        return new PolygonMask(
        [
            Vector2.Transform(new Vector2(Bounds.Left, Bounds.Top), m),
            Vector2.Transform(new Vector2(Bounds.Right, Bounds.Top), m),
            Vector2.Transform(new Vector2(Bounds.Right, Bounds.Bottom), m),
            Vector2.Transform(new Vector2(Bounds.Left, Bounds.Bottom), m),
        ]);
    }

    /// <summary>
    /// The image with its pipeline applied: first shrunk to the size it is drawn at when that is smaller, so a
    /// full-resolution layer previewed on a proxy costs what the proxy does.
    /// </summary>
    private ImageBuffer PrepareImage(OperationContext context)
    {
        if (Pipeline.IsEmpty) return Image;
        var scale = Math.Min(1.0, Math.Min(Bounds.Width / Image.Width, Bounds.Height / Image.Height));
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
            MaxDegreeOfParallelism = context.MaxDegreeOfParallelism,
        });
        // A custom operation may produce another size than it said it would.
        if (adjusted.Size != target.Size)
            throw new InvalidOperationException($"An adjustment layer must keep the image's size, but its pipeline produced {adjusted.Width}x{adjusted.Height} from {target.Width}x{target.Height}.");
        BlendRows(target, target.Bounds, context, y => adjusted.GetRow(y), mixForSourceOver: true);
    }

    protected override Layer ScaleCore(double scale) => this with { Pipeline = Pipeline.ForScale(scale) };
}

/// <summary>Composites layers.</summary>
public static class LayerCompositor
{
    /// <summary>Blends <paramref name="layers"/>, bottom first, onto <paramref name="target"/> in place.</summary>
    public static void Composite(ImageBuffer target, IEnumerable<Layer> layers, OperationContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(layers);
        context ??= OperationContext.Default;
        var list = layers as IReadOnlyList<Layer> ?? [.. layers];
        for (var i = 0; i < list.Count; i++)
        {
            context.ThrowIfCancellationRequested();
            // Each layer reports its progress within its own share of the whole.
            var progress = context.Progress is null ? null : new ScaledProgress(context.Progress, i / (double)list.Count, 1.0 / list.Count);
            list[i].Composite(target, context.WithProgress(progress));
        }
        context.ReportProgress(1);
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
