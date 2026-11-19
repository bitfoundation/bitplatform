using System.Buffers;
using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// One layer of a composition, blended onto everything beneath it: placed pixels (<see cref="BmageImageLayer"/>), a colour
/// (<see cref="BmageFillLayer"/>), an edit of what is below (<see cref="BmageAdjustmentLayer"/>), painted strokes
/// (<see cref="BmagePaintLayer"/>) or other layers kept together (<see cref="BmageGroupLayer"/>). Every layer has a blend mode,
/// an opacity and an optional mask, may be clipped to the layer below it, and like operations and annotations it is an
/// immutable value, so layer edits get undo, redo and proxy previews for free.
/// </summary>
/// <remarks>
/// Coordinates, including the mask's, are those of the image the layer is composited onto: in an
/// <see cref="BmageImageDocument"/>, the output of its pipeline.
/// </remarks>
public abstract record BmageLayer
{
    /// <summary>Stable identifier, generated when the layer is created.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>A name to show in a layer list, or null to show <see cref="Kind"/>.</summary>
    public string? Name { get; init; }

    /// <summary>How the layer combines with what is beneath it.</summary>
    public BmageBlendMode BlendMode { get; init; } = BmageBlendMode.SourceOver;

    /// <summary>Overall opacity in [0,1].</summary>
    public float Opacity
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 1f;

    /// <summary>Where the layer shows, or null for everywhere it has pixels.</summary>
    public BmageMask? Mask { get; init; }

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
    public void Composite(BmageImageBuffer target, BmageOperationContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (IsNoOp) return;
        CompositeCore(target, context ?? BmageOperationContext.Default);
    }

    /// <summary>Blends the layer onto the target. Called only for a visible layer with some opacity.</summary>
    protected internal abstract void CompositeCore(BmageImageBuffer target, BmageOperationContext context);

    /// <summary>Returns the layer expressed for an image scaled by <paramref name="scale"/>, its mask included.</summary>
    public BmageLayer ForScale(double scale) => scale == 1 ? this : ScaleCore(scale) with { Mask = Mask?.ForScale(scale) };

    /// <summary>The layer with its own geometry scaled; the mask is scaled by <see cref="ForScale"/>.</summary>
    protected abstract BmageLayer ScaleCore(double scale);

    /// <summary>
    /// Returns the layer as it belongs after the image beneath it is transformed by <paramref name="matrix"/>, which maps
    /// the image's coordinates before the edit to those after it, its mask included: the layer stays on the content it
    /// covers. A layer over the whole image, a fill or an adjustment, keeps covering it, and only its mask moves.
    /// </summary>
    public BmageLayer Transform(Matrix3x2 matrix) => matrix.IsIdentity ? this : TransformCore(matrix) with { Mask = Mask?.Transform(matrix) };

    /// <summary>The layer with its own geometry transformed; the mask is transformed by <see cref="Transform"/>.</summary>
    protected virtual BmageLayer TransformCore(Matrix3x2 matrix) => this;

    /// <summary>A copy under a new id, and new ids for every layer it holds, as a duplicate needs.</summary>
    internal virtual BmageLayer WithNewIds() => this with { Id = Guid.NewGuid() };

    /// <summary>The layer's pixels for row <paramref name="y"/> of the target, starting at the composited area's left.</summary>
    protected delegate ReadOnlySpan<BmageRgba32> LayerRow(int y);

    /// <summary>
    /// Blends <paramref name="rows"/> of layer pixels onto the target through the layer's <see cref="BmageBlendMode"/>,
    /// <see cref="Opacity"/> and <see cref="Mask"/>, over <paramref name="area"/> and on as many threads as the context
    /// allows. <paramref name="rows"/> returns the layer's pixels for a target row, starting at <paramref name="area"/>'s
    /// left, and may be called from several threads at once. With <paramref name="mixForSourceOver"/>, the normal mode
    /// replaces what is below by the coverage instead of compositing over it, as an edit of the layers below does.
    /// </summary>
    /// <remarks>A custom layer produces its pixels and calls this; it never needs to apply the mask itself.</remarks>
    protected void BlendRows(BmageImageBuffer target, Rectangle area, BmageOperationContext context, LayerRow rows, bool mixForSourceOver = false)
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
        var mix = mixForSourceOver && mode == BmageBlendMode.SourceOver;
        var coverageScale = (int)MathF.Round(opacity * 255);
        BmageParallelRows.For(context, area.Top, area.Bottom, area.Width * (mode is BmageBlendMode.SourceOver or BmageBlendMode.Copy ? 1L : 4L), (start, end, band) =>
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
                        BmageCompositor.BlendRow(source, destination, mode, opacity, coverage);
                        continue;
                    }
                    if (coverage.IsEmpty)
                    {
                        if (coverageScale >= 255) source.CopyTo(destination);
                        else for (var x = 0; x < destination.Length; x++) destination[x] = BmageMaskedOperation.Mix(destination[x], source[x], coverageScale);
                        continue;
                    }
                    if (scaled is not null)
                    {
                        var weights = scaled.AsSpan(0, area.Width);
                        for (var x = 0; x < weights.Length; x++) weights[x] = (byte)((coverage[x] * coverageScale + 127) / 255);
                        coverage = weights;
                    }
                    BmageMaskedOperation.Mix(destination, source, coverage, destination);
                }
            }
            finally
            {
                if (scaled is not null) ArrayPool<byte>.Shared.Return(scaled);
            }
        });
    }
}
