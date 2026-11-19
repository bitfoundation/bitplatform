using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Changes the canvas around the image without scaling it: grows it, filling the new area with
/// <see cref="Background"/>, or shrinks it, cutting the edges the anchor leaves outside. A border, a square canvas for a
/// product photo, or room for a caption.
/// </summary>
/// <remarks>
/// The canvas is given either as an absolute size with an <see cref="Anchor"/> saying where the image sits in it, or
/// as margins added to each side (<see cref="Margins(int, int, int, int, BmageRgba32)"/>), negative margins cutting into the
/// image. Pixels are copied, never resampled, so the image itself is unchanged.
/// </remarks>
/// <example>
/// <code>
/// // A 40 pixel white border.
/// pipeline.Apply(BmageCanvasSizeOperation.Margins(40, BmageRgba32.White));
///
/// // A square canvas with the photo at the top.
/// pipeline.CanvasSize(1200, 1200, BmageAnchor.Top, BmageRgba32.White);
/// </code>
/// </example>
public sealed class BmageCanvasSizeOperation : BmageImageOperation, IBmageGeometricOperation
{
    /// <summary>A canvas of an absolute size, with the image placed in it by <paramref name="anchor"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The size is not positive.</exception>
    public BmageCanvasSizeOperation(int width, int height, BmageAnchor anchor = BmageAnchor.Center, BmageRgba32 background = default)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "The canvas must be at least one pixel wide.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "The canvas must be at least one pixel high.");
        Size = new Size(width, height);
        Anchor = anchor;
        Background = background;
    }

    private BmageCanvasSizeOperation(int left, int top, int right, int bottom, BmageRgba32 background)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Background = background;
    }

    /// <summary>
    /// A canvas grown by the given margins on each side, or shrunk where a margin is negative. The image keeps its
    /// position relative to the margins, so <c>Margins(10, 0, 0, 0)</c> adds a ten pixel strip on the left.
    /// </summary>
    public static BmageCanvasSizeOperation Margins(int left, int top, int right, int bottom, BmageRgba32 background = default)
        => new(left, top, right, bottom, background);

    /// <summary>A canvas grown by <paramref name="all"/> pixels on every side: a border.</summary>
    public static BmageCanvasSizeOperation Margins(int all, BmageRgba32 background = default) => new(all, all, all, all, background);

    /// <summary>The absolute size of the canvas, or null when it is given by margins.</summary>
    public Size? Size { get; }

    /// <summary>Where the image sits in a canvas of an absolute <see cref="Size"/>. Unused for margins.</summary>
    public BmageAnchor Anchor { get; }

    /// <summary>Pixels added to the left, or cut from it when negative. Zero for an absolute size.</summary>
    public int Left { get; }

    /// <summary>Pixels added to the top, or cut from it when negative. Zero for an absolute size.</summary>
    public int Top { get; }

    /// <summary>Pixels added to the right, or cut from it when negative. Zero for an absolute size.</summary>
    public int Right { get; }

    /// <summary>Pixels added to the bottom, or cut from it when negative. Zero for an absolute size.</summary>
    public int Bottom { get; }

    /// <summary>The colour of the area the canvas gains. Transparent by default.</summary>
    public BmageRgba32 Background { get; }

    public override string Name => "Canvas size";

    public override bool IsIdentity => Size is null && Left == 0 && Top == 0 && Right == 0 && Bottom == 0;

    /// <summary>
    /// Where the image lands on the canvas for an input of <paramref name="inputSize"/>, and the canvas's size. The
    /// placement may reach outside the canvas, where the canvas is smaller than the image.
    /// </summary>
    /// <exception cref="ArgumentException">The margins leave no canvas.</exception>
    public (Rectangle Placement, Size Canvas) GetPlacement(Size inputSize)
    {
        if (Size is { } size) return (Anchor.Place(inputSize, size), size);
        var width = (long)inputSize.Width + Left + Right;
        var height = (long)inputSize.Height + Top + Bottom;
        if (width <= 0 || height <= 0)
            throw new ArgumentException($"Margins of {Left}, {Top}, {Right} and {Bottom} leave no canvas of an image of {inputSize.Width}x{inputSize.Height}.");
        if (width > int.MaxValue || height > int.MaxValue)
            throw new ArgumentException($"Margins of {Left}, {Top}, {Right} and {Bottom} make a canvas too large to address.");
        return (new Rectangle(Left, Top, inputSize.Width, inputSize.Height), new Size((int)width, (int)height));
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">The margins leave no canvas.</exception>
    public override Size GetOutputSize(Size inputSize) => GetPlacement(inputSize).Canvas;

    public Matrix3x2? GetTransform(Size inputSize)
    {
        var placement = GetPlacement(inputSize).Placement;
        return Matrix3x2.CreateTranslation(placement.X, placement.Y);
    }

    public override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
    {
        var (placement, canvas) = GetPlacement(source.Size);
        if (canvas == source.Size && placement.Location == Point.Empty)
            return context.CanMutateSource ? source : source.Clone(context.Allocator);
        var output = context.Allocate(canvas.Width, canvas.Height, clear: Background.A == 0);
        output.Metadata = source.Metadata;
        try
        {
            if (Background.A != 0) output.Fill(Background);
            var visible = placement;
            visible.Intersect(new Rectangle(Point.Empty, canvas));
            for (var y = visible.Top; y < visible.Bottom; y++)
            {
                if (((y - visible.Top) & 63) == 0) context.ThrowIfCancellationRequested();
                source.GetRow(y - placement.Y).Slice(visible.Left - placement.X, visible.Width).CopyTo(output.GetRow(y).Slice(visible.Left, visible.Width));
            }
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    public override IBmageImageOperation ForScale(double scale)
    {
        if (scale == 1) return this;
        static int Scale(int value, double scale) => (int)Math.Round(value * scale);
        if (Size is { } size)
            return new BmageCanvasSizeOperation(Math.Max(1, Scale(size.Width, scale)), Math.Max(1, Scale(size.Height, scale)), Anchor, Background);
        return new BmageCanvasSizeOperation(Scale(Left, scale), Scale(Top, scale), Scale(Right, scale), Scale(Bottom, scale), Background);
    }

    public override string ToString() => Size is { } s
        ? $"CanvasSize({s.Width}x{s.Height}, {Anchor})"
        : $"CanvasSize(margins {Left},{Top},{Right},{Bottom})";
}
