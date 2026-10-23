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
    internal MaskRenderContext(Size size, ImageBuffer? source, OperationContext operation, MaskUnits units, Rectangle window)
    {
        Size = size;
        Source = source;
        Operation = operation;
        ToPixels = units == MaskUnits.Relative ? Matrix3x2.CreateScale(size.Width, size.Height) : Matrix3x2.Identity;
        Window = window;
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

    /// <summary>
    /// The part of the image the target holds, in pixels of the image: the whole image, except for a built-in mask
    /// drawing only the part an edit or a layer uses. The target's row 0 is the window's top row, its column 0 the
    /// window's left column.
    /// </summary>
    internal Rectangle Window { get; }

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
        => RenderWindow(size, new Rectangle(Point.Empty, size), source, context);

    /// <summary>
    /// Renders the part <paramref name="window"/> of the mask for an image of <paramref name="size"/>: exactly those
    /// values of <see cref="Render"/>'s result, in a buffer of the window's size. Built-in masks draw only the window,
    /// and around it what their feather reaches; others are drawn whole and the window copied out.
    /// </summary>
    internal MaskBuffer RenderWindow(Size size, Rectangle window, ImageBuffer? source, OperationContext? context)
    {
        if (size.Width <= 0 || size.Height <= 0) throw new ArgumentOutOfRangeException(nameof(size), "A mask needs a positive size.");
        if (source is not null && source.Size != size)
            throw new ArgumentException($"The image is {source.Width}x{source.Height}, but the mask is rendered at {size.Width}x{size.Height}.", nameof(source));
        window.Intersect(new Rectangle(Point.Empty, size));
        if (window.IsEmptyArea()) throw new ArgumentOutOfRangeException(nameof(window), "The window does not overlap the image.");
        context ??= OperationContext.Default;
        var mask = MaskBuffer.Create(window.Width, window.Height, 0, context.Allocator, context.Limits);
        try
        {
            RenderInto(mask, new MaskRenderContext(size, source, context, Units, window));
            return mask;
        }
        catch
        {
            mask.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Renders the mask, its feather, inversion and opacity included, into <paramref name="target"/>: cleared to 0 and
    /// holding <see cref="MaskRenderContext.Window"/> of the image. The context's units are ignored; the mask's own apply.
    /// </summary>
    internal void RenderInto(MaskBuffer target, MaskRenderContext context)
    {
        if (!IsEmpty) RenderShape(target, context.Size, context.Window, context.Source, context.Operation);
        if (Inverted) target.Invert();
        target.Scale(Opacity);
    }

    /// <summary>
    /// The plain shape, feathered. A built-in mask draws the window and, around it, what the feather carries into it, so
    /// the values match the whole image's exactly; any other mask is drawn on the whole image and the window copied out.
    /// </summary>
    private void RenderShape(MaskBuffer target, Size size, Rectangle window, ImageBuffer? source, OperationContext context)
    {
        var full = new Rectangle(Point.Empty, size);
        // The feather a whole mask gets, which clamps a blur wider than the image.
        var feather = Feather >= 0.05f ? Math.Min(Feather, Math.Max(size.Width, size.Height)) : 0f;
        var drawn = full;
        if (window != full && DrawsWindows)
        {
            drawn = window;
            if (feather > 0)
            {
                var reach = MaskBuffer.FeatherReach(feather);
                drawn.Inflate(reach, reach);
                drawn.Intersect(full);
                // A blur wider than the part drawn would be clamped there, and no longer match the whole image's.
                if (feather > Math.Max(drawn.Width, drawn.Height)) drawn = full;
            }
        }
        if (drawn == window)
        {
            RenderCoverage(target, new MaskRenderContext(size, source, context, Units, window));
            if (feather > 0) target.Feather(feather, context);
            return;
        }
        using var canvas = MaskBuffer.Create(drawn.Width, drawn.Height, 0, context.Allocator, context.Limits);
        RenderCoverage(canvas, new MaskRenderContext(size, source, context, Units, drawn));
        if (feather > 0) canvas.Feather(feather, context);
        for (var y = 0; y < window.Height; y++)
            canvas.GetRow(window.Y - drawn.Y + y).Slice(window.X - drawn.X, window.Width).CopyTo(target.GetRow(y));
    }

    /// <summary>
    /// True for the built-in masks, which draw into a target holding any <see cref="MaskRenderContext.Window"/> of the
    /// image. Every other mask is handed a target the size of the whole image, as its <see cref="RenderCoverage"/> expects.
    /// </summary>
    private protected virtual bool DrawsWindows => false;

    /// <summary>
    /// The most masks a document may nest so that drawing one holds this many beside its own (see
    /// <see cref="MasksHeld"/>). A mask is one byte per pixel, so on a 24 megapixel photo this is about 200 MB.
    /// </summary>
    internal const int MaxMasksHeld = 8;

    /// <summary>
    /// How many masks, at most, drawing this one holds at once beside its own target: one for each side of a
    /// combination drawn on its own, and the working copy of each feathered mask, bitmap or mask drawn whole, along the
    /// deepest path. A chain built a step at a time, <c>a.Union(b).Union(c)</c>, holds one however long it grows.
    /// </summary>
    internal int MasksHeld()
    {
        var held = 0;
        // What the masks above hold while the current one is drawn into the target they share.
        var above = 0;
        Mask mask = this;
        // The first sides of a chain are walked rather than recursed into: each is drawn into the same target.
        while (true)
        {
            above += (mask.Feather >= 0.05f ? 1 : 0) + (mask is BitmapMask ? 1 : 0) + (mask.DrawsWindows ? 0 : 1);
            // A morphed mask holds its source, drawn whole, the vertical distances, and for a border the inner edge too.
            if (mask is MorphedMask morphed)
                return Math.Max(held, above + (morphed.Morphology == MaskMorphology.Border ? 3 : 2) + morphed.Source.MasksHeld());
            if (mask is not CombinedMask combined) return Math.Max(held, above);
            held = Math.Max(held, above + 1 + combined.Second.MasksHeld());
            mask = combined.First;
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

    /// <summary>
    /// Returns the mask as it belongs after the image it applies to is transformed by <paramref name="matrix"/>, which
    /// maps the image's coordinates before the edit to those after it. A mask in pixels moves, scales and turns with the
    /// image, its feather and widths included, so it keeps covering the same content. A mask in relative units is a
    /// fraction of whatever image it applies to and stays as it is, as do masks that select by colour or tone, and bitmap
    /// masks, which are stretched over the whole image.
    /// </summary>
    public virtual Mask Transform(Matrix3x2 matrix)
    {
        if (matrix.IsIdentity || Units == MaskUnits.Relative) return this;
        return TransformGeometry(matrix) with { Feather = Feather * AffineParts.Of(matrix).Scale };
    }

    /// <summary>
    /// The mask with its pixel geometry transformed by <paramref name="matrix"/>; <see cref="Transform"/> scales the
    /// feather. Masks without geometry return themselves.
    /// </summary>
    protected virtual Mask TransformGeometry(Matrix3x2 matrix) => this;

    /// <summary>
    /// <paramref name="other"/>, a mask of another kind standing in for this one after a transform, with this one's
    /// inversion, opacity, units and feather.
    /// </summary>
    private protected Mask Inherit(Mask other) => other with { Inverted = Inverted, Opacity = Opacity, Units = Units, Feather = Feather };

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

    /// <summary>This mask grown by <paramref name="pixels"/> in every direction.</summary>
    public Mask Expand(float pixels) => new MorphedMask(this, MaskMorphology.Expand, pixels);

    /// <summary>This mask shrunk by <paramref name="pixels"/> from every edge.</summary>
    public Mask Contract(float pixels) => new MorphedMask(this, MaskMorphology.Contract, pixels);

    /// <summary>A band <paramref name="width"/> pixels wide along this mask's edge.</summary>
    public Mask Border(float width) => new MorphedMask(this, MaskMorphology.Border, width);

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

    private protected override bool DrawsWindows => true;

    /// <summary>True when the combination draws exactly what combining its sides gives: no feather, inversion or opacity.</summary>
    private bool IsPlain => Feather < 0.05f && !Inverted && Opacity >= 1;

    protected internal override void RenderCoverage(MaskBuffer target, MaskRenderContext context)
    {
        // Each side is rendered whole, its own feather, inversion and opacity included, then the two are combined. The
        // first side is drawn straight into the target and the second on its own. A chain built a step at a time,
        // a.Union(b).Union(c), nests to the left: its plain links are walked rather than recursed into, so however long
        // it grows it holds one mask beside the target and none of the stack.
        var links = new Stack<CombinedMask>();
        links.Push(this);
        var first = First;
        while (first is CombinedMask { IsPlain: true } link)
        {
            links.Push(link);
            first = link.First;
        }
        first.RenderInto(target, context);
        while (links.TryPop(out var link))
        {
            context.Operation.ThrowIfCancellationRequested();
            using var second = link.Second.RenderWindow(context.Size, context.Window, context.Source, context.Operation);
            target.Combine(second, link.Mode);
        }
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

    /// <summary>Transforms both sides, each according to its own units, then the combination's own feather.</summary>
    public override Mask Transform(Matrix3x2 matrix) => matrix.IsIdentity ? this : this with
    {
        First = First.Transform(matrix),
        Second = Second.Transform(matrix),
        Feather = Feather * AffineParts.Of(matrix).Scale,
    };
}
