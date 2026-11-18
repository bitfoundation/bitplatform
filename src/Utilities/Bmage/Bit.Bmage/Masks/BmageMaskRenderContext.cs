using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>Where a mask is being rendered, and for what.</summary>
public sealed class BmageMaskRenderContext
{
    internal BmageMaskRenderContext(Size size, BmageImageBuffer? source, BmageOperationContext operation, BmageMaskUnits units, Rectangle window)
    {
        Size = size;
        Source = source;
        Operation = operation;
        ToPixels = units == BmageMaskUnits.Relative ? Matrix3x2.CreateScale(size.Width, size.Height) : Matrix3x2.Identity;
        Window = window;
    }

    /// <summary>The size of the mask, which is the size of the image it applies to.</summary>
    public Size Size { get; }

    /// <summary>
    /// The pixels the mask is applied to, for masks that select by content (luminance, colour), or null when the mask is
    /// rendered on its own.
    /// </summary>
    public BmageImageBuffer? Source { get; }

    /// <summary>Allocation, limits, cancellation and threads for the render.</summary>
    public BmageOperationContext Operation { get; }

    /// <summary>Maps the mask's own coordinates to pixels: identity for <see cref="BmageMaskUnits.Pixels"/>.</summary>
    public Matrix3x2 ToPixels { get; }

    /// <summary>
    /// The part of the image the target holds, in pixels of the image: the whole image, except for a built-in mask
    /// drawing only the part an edit or a layer uses. The target's row 0 is the window's top row, its column 0 the
    /// window's left column.
    /// </summary>
    internal Rectangle Window { get; }

    /// <summary>The image the mask selects from, or an exception saying the mask needs one.</summary>
    internal BmageImageBuffer RequireSource(string kind) => Source
        ?? throw new InvalidOperationException($"A {kind} mask selects pixels by their content, so it can only be rendered for an image. Pass the image to BmageMask.Render, or apply the mask through a pipeline.");
}
