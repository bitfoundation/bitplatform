using System.Drawing;
using System.Numerics;
using Bit.Bmage.Geometry;
using Bit.Bmage.Operations;

namespace Bit.Bmage.Masks;

/// <summary>What the coordinates of a geometric mask are measured in.</summary>
public enum MaskUnits
{
    /// <summary>Pixels of the image the mask is applied to. Rescaled with the edit when it is previewed on a proxy.</summary>
    Pixels = 0,
    /// <summary>
    /// Fractions of the image's width and height, so (0,0) is the top left corner and (1,1) the bottom right. The same
    /// mask then fits every image, which suits a pipeline reused across uploads of different sizes.
    /// </summary>
    Relative,
}

/// <summary>Where a mask is being rendered, and for what.</summary>
public sealed class MaskRenderContext
{
    internal MaskRenderContext(Size size, ImageBuffer? source, OperationContext operation, MaskUnits units)
    {
        Size = size;
        Source = source;
        Operation = operation;
        ToPixels = units == MaskUnits.Relative ? Matrix3x2.CreateScale(size.Width, size.Height) : Matrix3x2.Identity;
    }

    /// <summary>The size of the mask, which is the size of the image it applies to.</summary>
    public Size Size { get; }

    /// <summary>
    /// The pixels the mask is applied to, for masks that select by content (luminance, colour), or null when the mask is
    /// rendered on its own.
    /// </summary>
    public ImageBuffer? Source { get; }

    /// <summary>Allocation, limits, cancellation and threads for the render.</summary>
    public OperationContext Operation { get; }

    /// <summary>Maps the mask's own coordinates to pixels: identity for <see cref="MaskUnits.Pixels"/>.</summary>
    public Matrix3x2 ToPixels { get; }

    /// <summary>The image the mask selects from, or an exception saying the mask needs one.</summary>
    internal ImageBuffer RequireSource(string kind) => Source
        ?? throw new InvalidOperationException($"A {kind} mask selects pixels by their content, so it can only be rendered for an image. Pass the image to Mask.Render, or apply the mask through a pipeline.");
}

/// <summary>
/// A description of which pixels an edit affects: a shape, a gradient, a range of colours or tones, brush strokes, or
/// a combination of those. Like an operation, a mask is an immutable value that carries no pixels of its own; it is
/// rendered into a <see cref="MaskBuffer"/> for the image it applies to, which is what lets one mask preview on a
/// downscaled proxy and export at full resolution.
/// </summary>
/// <example>
/// <code>
/// // Darken the sky: an exposure change that fades out from the top of the image to its middle.
/// pipeline.Masked(new LinearGradientMask(new Vector2(0, 0), new Vector2(0, 0.5f)) { Units = MaskUnits.Relative },
///                 p => p.Exposure(-0.7f).Saturation(0.2f));
///
/// // A round avatar with transparent corners.
/// pipeline.Cover(512, 512).ApplyMask(Mask.Ellipse() with { Feather = 1 });
/// </code>
/// </example>
public abstract record Mask
{
    /// <summary>Swaps covered and uncovered, so the edit applies everywhere except the mask's area.</summary>
    public bool Inverted { get; init; }

    /// <summary>
    /// Softens the mask's edge with a gaussian of this standard deviation, in pixels of the image it applies to. 0 keeps
    /// the edge as drawn, already anti-aliased.
    /// </summary>
    public float Feather
    {
        get;
        init => field = value >= 0 && float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(Feather), "Feather must be a finite, non-negative number of pixels.");
    }

    /// <summary>How strongly the masked area is affected, in [0,1]: the mask's coverage is multiplied by it.</summary>
    public float Opacity
    {
        get;
        init => field = Math.Clamp(float.IsNaN(value) ? 0 : value, 0f, 1f);
    } = 1f;

    /// <summary>What geometric coordinates are measured in. Content-selecting masks ignore it.</summary>
    public MaskUnits Units { get; init; }

    /// <summary>A short type name for UI and serialisation, e.g. "Ellipse".</summary>
    public abstract string Kind { get; }

    /// <summary>True when the mask covers nothing whatever the image, so an edit through it changes nothing.</summary>
    public bool IsEmpty => Opacity <= 0;

    /// <summary>
    /// Renders the mask for an image of <paramref name="size"/>. Masks that select by content need
    /// <paramref name="source"/>, the image they apply to. The caller owns the returned buffer.
    /// </summary>
    public MaskBuffer Render(Size size, ImageBuffer? source = null, OperationContext? context = null)
    {
        if (size.Width <= 0 || size.Height <= 0) throw new ArgumentOutOfRangeException(nameof(size), "A mask needs a positive size.");
        if (source is not null && source.Size != size)
            throw new ArgumentException($"The image is {source.Width}x{source.Height}, but the mask is rendered at {size.Width}x{size.Height}.", nameof(source));
        context ??= OperationContext.Default;
        var mask = MaskBuffer.Create(size.Width, size.Height, 0, context.Allocator, context.Limits);
        try
        {
            if (!IsEmpty)
            {
                RenderCoverage(mask, new MaskRenderContext(size, source, context, Units));
                if (Feather >= 0.05f) mask.Feather(Feather, context);
            }
            if (Inverted) mask.Invert();
            mask.Scale(Opacity);
            return mask;
        }
        catch
        {
            mask.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Writes the mask's coverage into <paramref name="target"/>, which is cleared to 0 and has the size of the image.
    /// <see cref="Render"/> applies <see cref="Feather"/>, <see cref="Inverted"/> and <see cref="Opacity"/> afterwards,
    /// so an implementation draws the plain shape.
    /// </summary>
    protected internal abstract void RenderCoverage(MaskBuffer target, MaskRenderContext context);

    /// <summary>
    /// The area outside which <see cref="RenderCoverage"/> writes nothing, in pixels of an image of
    /// <paramref name="size"/>, or null when it can cover anything. Lets an edit through the mask skip untouched rows.
    /// </summary>
    public virtual RectangleF? GetCoverageBounds(Size size) => null;

    /// <summary>
    /// The pixels the rendered mask can affect, after feathering and inversion: an empty rectangle when it affects none,
    /// and the whole image when it cannot be narrowed down.
    /// </summary>
    public Rectangle GetAffectedBounds(Size size)
    {
        var full = new Rectangle(Point.Empty, size);
        if (IsEmpty) return Rectangle.Empty;
        if (Inverted || GetCoverageBounds(size) is not { } bounds) return full;
        // A gaussian reaches about three standard deviations before its weight rounds to nothing in 8 bits.
        var spread = Feather * 3 + 1;
        bounds.Inflate(spread, spread);
        if (!float.IsFinite(bounds.Left) || !float.IsFinite(bounds.Top) || !float.IsFinite(bounds.Right) || !float.IsFinite(bounds.Bottom)) return full;
        var device = Rectangle.FromLTRB(
            (int)Math.Floor(Math.Max(bounds.Left, -1f)), (int)Math.Floor(Math.Max(bounds.Top, -1f)),
            (int)Math.Ceiling(Math.Min(bounds.Right, size.Width + 1f)), (int)Math.Ceiling(Math.Min(bounds.Bottom, size.Height + 1f)));
        device.Intersect(full);
        return device.IsEmptyArea() ? Rectangle.Empty : device;
    }

    /// <summary>
    /// Returns the same mask expressed for an image scaled by <paramref name="scale"/>: pixel coordinates and the
    /// feather scale with it, relative coordinates stay as they are.
    /// </summary>
    public virtual Mask ForScale(double scale)
    {
        if (scale == 1) return this;
        var scaled = Units == MaskUnits.Pixels ? ScaleGeometry(scale) : this;
        return scaled with { Feather = (float)(Feather * scale) };
    }

    /// <summary>The mask with its pixel coordinates and sizes multiplied by <paramref name="scale"/>.</summary>
    protected virtual Mask ScaleGeometry(double scale) => this;

    /// <summary>The same mask, inverted.</summary>
    public Mask Invert() => this with { Inverted = !Inverted };

    /// <summary>Covered where this mask or <paramref name="other"/> is.</summary>
    public Mask Union(Mask other) => new CombinedMask(this, other, MaskCombineMode.Union);

    /// <summary>Covered where both this mask and <paramref name="other"/> are.</summary>
    public Mask Intersect(Mask other) => new CombinedMask(this, other, MaskCombineMode.Intersect);

    /// <summary>Covered where this mask is and <paramref name="other"/> is not.</summary>
    public Mask Subtract(Mask other) => new CombinedMask(this, other, MaskCombineMode.Subtract);

    /// <summary>Covered where exactly one of this mask and <paramref name="other"/> is.</summary>
    public Mask Exclude(Mask other) => new CombinedMask(this, other, MaskCombineMode.Exclude);

    /// <summary>The ellipse inscribed in the whole image, whatever its size: a round avatar or a spotlight.</summary>
    public static EllipseMask Ellipse() => new(new RectangleF(0, 0, 1, 1)) { Units = MaskUnits.Relative };

    /// <summary>The whole image.</summary>
    public static RectangleMask All() => new(new RectangleF(0, 0, 1, 1)) { Units = MaskUnits.Relative };

    /// <summary>
    /// Fades from fully covered at <paramref name="from"/> to uncovered at <paramref name="to"/>, both in fractions of
    /// the image: a graduated filter, such as <c>Mask.Gradient(new(0, 0), new(0, 0.6f))</c> for the sky.
    /// </summary>
    public static LinearGradientMask Gradient(Vector2 from, Vector2 to) => new(from, to) { Units = MaskUnits.Relative };
}

/// <summary>Two masks combined value by value.</summary>
public sealed record CombinedMask(Mask First, Mask Second, MaskCombineMode Mode) : Mask
{
    public Mask First { get; init; } = First ?? throw new ArgumentNullException(nameof(First));
    public Mask Second { get; init; } = Second ?? throw new ArgumentNullException(nameof(Second));

    public override string Kind => "Combined";

    protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
    {
        // Each side is rendered whole, its own feather, inversion and opacity included, then the two are combined.
        using var first = First.Render(context.Size, context.Source, context.Operation);
        using var second = Second.Render(context.Size, context.Source, context.Operation);
        first.Values.CopyTo(target.Values);
        target.Combine(second, Mode);
    }

    public override RectangleF? GetCoverageBounds(Size size)
    {
        var a = First.GetAffectedBounds(size);
        var b = Second.GetAffectedBounds(size);
        switch (Mode)
        {
            case MaskCombineMode.Intersect:
                a.Intersect(b);
                return a;
            case MaskCombineMode.Subtract:
                return a;
            default:
                if (a.IsEmptyArea()) return b;
                if (b.IsEmptyArea()) return a;
                return RectangleF.Union(a, b);
        }
    }

    /// <summary>Scales both sides, each according to its own units, then the combination's own feather.</summary>
    public override Mask ForScale(double scale) => scale == 1 ? this : this with
    {
        First = First.ForScale(scale),
        Second = Second.ForScale(scale),
        Feather = (float)(Feather * scale),
    };
}
