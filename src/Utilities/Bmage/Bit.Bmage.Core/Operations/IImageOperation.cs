using System.Drawing;

namespace Bit.Bmage.Operations;

/// <summary>
/// A unit of image processing that transforms one <see cref="ImageBuffer"/> into another. Operations are
/// immutable, serialisable descriptions of work; they carry no pixel data of their own and can be composed
/// into a pipeline, stored in edit history and replayed on proxy images.
/// </summary>
public interface IImageOperation
{
    /// <summary>A short, stable, human readable name (e.g. "Crop", "Brightness").</summary>
    string Name { get; }

    /// <summary>
    /// Computes the output size for a given input size without executing the operation. Must agree with what
    /// <see cref="Apply"/> would produce, including by throwing the same exception when the operation cannot be
    /// applied to an input of that size (for example a crop rectangle that lies entirely outside the image).
    /// </summary>
    /// <exception cref="ArgumentException">The operation cannot be applied to an input of this size.</exception>
    Size GetOutputSize(Size inputSize);

    /// <summary>
    /// Executes the operation. When <see cref="OperationContext.CanMutateSource"/> is true the implementation may modify
    /// <paramref name="source"/> in place and return the same instance; otherwise it must return a new buffer and leave the
    /// source untouched. The caller owns the returned buffer.
    /// </summary>
    ImageBuffer Apply(ImageBuffer source, OperationContext context);

    /// <summary>
    /// Returns an equivalent operation expressed for an image that has been uniformly scaled by <paramref name="scale"/>
    /// relative to the coordinate space this operation was defined in. Resolution independent operations return themselves.
    /// Used to replay full-resolution edits on downscaled preview proxies.
    /// </summary>
    IImageOperation ForScale(double scale);

    /// <summary>True when the operation cannot change any pixel (e.g. brightness 0) and may be dropped by the optimiser.</summary>
    bool IsIdentity { get; }
}

/// <summary>Convenience base class for operations with sensible defaults.</summary>
public abstract class ImageOperation : IImageOperation
{
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public virtual Size GetOutputSize(Size inputSize) => inputSize;

    /// <inheritdoc />
    public abstract ImageBuffer Apply(ImageBuffer source, OperationContext context);

    /// <inheritdoc />
    public virtual IImageOperation ForScale(double scale) => this;

    /// <inheritdoc />
    public virtual bool IsIdentity => false;

    /// <summary>
    /// Returns a buffer the operation may write its output to: the source itself when in-place mutation is allowed and
    /// the output size matches, otherwise a fresh allocation.
    /// </summary>
    protected static ImageBuffer GetOutputBuffer(ImageBuffer source, OperationContext context, int width, int height, bool clear = false)
    {
        if (context.CanMutateSource && source.Width == width && source.Height == height)
            return source;
        var output = context.Allocate(width, height, clear);
        output.Metadata = source.Metadata;
        return output;
    }

    /// <summary>Copies the source when in-place mutation is not allowed, otherwise returns the source.</summary>
    protected static ImageBuffer GetMutableCopy(ImageBuffer source, OperationContext context)
    {
        if (context.CanMutateSource) return source;
        return source.Clone(context.Allocator);
    }

    public override string ToString() => Name;
}
