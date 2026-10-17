using System.Drawing;
using System.Numerics;
using Bit.Bmage.Geometry;

namespace Bit.Bmage.Operations.Transforms;

/// <summary>
/// An operation whose effect on coordinates can be described by an affine matrix. Used to keep annotations attached to
/// image content when geometric edits are inserted before them, and by the pipeline optimiser.
/// </summary>
public interface IGeometricOperation : IImageOperation
{
    /// <summary>
    /// Returns the matrix mapping input pixel coordinates to output pixel coordinates for an input of the given size,
    /// or null when the mapping is not affine (e.g. perspective warps).
    /// </summary>
    Matrix3x2? GetTransform(Size inputSize);
}

/// <summary>Extracts a rectangular region. The rectangle is clipped to the image bounds.</summary>
public sealed class CropOperation : ImageOperation, IGeometricOperation
{
    public CropOperation(Rectangle rectangle)
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(rectangle), "Crop rectangle must have positive size.");
        Rectangle = rectangle;
    }

    public CropOperation(int x, int y, int width, int height) : this(new Rectangle(x, y, width, height)) { }

    /// <summary>The requested region in input coordinates.</summary>
    public Rectangle Rectangle { get; }

    public override string Name => "Crop";

    /// <summary>The effective region for an input of the given size (clipped to bounds).</summary>
    public Rectangle GetEffectiveRectangle(Size inputSize)
    {
        var r = Rectangle;
        r.Intersect(new Rectangle(Point.Empty, inputSize));
        return r;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">The rectangle does not intersect an image of that size.</exception>
    public override Size GetOutputSize(Size inputSize)
    {
        var r = GetEffectiveRectangle(inputSize);
        if (r.IsEmptyArea()) throw NotIntersecting(inputSize);
        return r.Size;
    }

    private ArgumentException NotIntersecting(Size inputSize)
        => new($"Crop rectangle {Rectangle} does not intersect the image ({inputSize.Width}x{inputSize.Height}).");

    public Matrix3x2? GetTransform(Size inputSize)
    {
        var r = GetEffectiveRectangle(inputSize);
        return Matrix3x2.CreateTranslation(-r.X, -r.Y);
    }

    public override ImageBuffer Apply(ImageBuffer source, OperationContext context)
    {
        var r = GetEffectiveRectangle(source.Size);
        if (r.IsEmptyArea()) throw NotIntersecting(source.Size);
        if (r == source.Bounds)
            return context.CanMutateSource ? source : source.Clone(context.Allocator);
        var output = context.Allocate(r.Width, r.Height);
        output.Metadata = source.Metadata;
        for (var y = 0; y < r.Height; y++)
        {
            if ((y & 63) == 0) context.ThrowIfCancellationRequested();
            source.GetRow(r.Y + y).Slice(r.X, r.Width).CopyTo(output.GetRow(y));
        }
        return output;
    }

    public override IImageOperation ForScale(double scale)
    {
        if (scale == 1) return this;
        var x = (int)Math.Round(Rectangle.X * scale);
        var y = (int)Math.Round(Rectangle.Y * scale);
        var w = Math.Max(1, (int)Math.Round(Rectangle.Right * scale) - x);
        var h = Math.Max(1, (int)Math.Round(Rectangle.Bottom * scale) - y);
        return new CropOperation(x, y, w, h);
    }

    /// <summary>
    /// Combines two consecutive crops into one, or returns null when the second crop selects nothing from the first.
    /// A null result means the pair cannot be replaced by a single crop; running them in sequence would fail, and the
    /// caller must let it fail rather than substituting a region the user never asked for.
    /// </summary>
    public CropOperation? TryThen(CropOperation next)
    {
        ArgumentNullException.ThrowIfNull(next);
        // At run time the first crop is clipped to the image, so the second is relative to the clipped origin. A
        // negative origin clips to zero whatever the image size; the far edges need no such care, because clipping
        // the merged rectangle to the image at run time gives the same result.
        var r = next.Rectangle;
        r.Offset(Math.Max(0, Rectangle.X), Math.Max(0, Rectangle.Y));
        r.Intersect(Rectangle);
        return r.IsEmptyArea() ? null : new CropOperation(r);
    }

    /// <summary>Combines two consecutive crops into one.</summary>
    /// <exception cref="ArgumentException">The second crop selects no part of the first.</exception>
    public CropOperation Then(CropOperation next)
        => TryThen(next) ?? throw new ArgumentException($"Crop {next.Rectangle} selects nothing from crop {Rectangle}; the two do not overlap.", nameof(next));

    public override string ToString() => $"Crop({Rectangle.X},{Rectangle.Y},{Rectangle.Width},{Rectangle.Height})";
}
