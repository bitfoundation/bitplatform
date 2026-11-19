using System.Drawing;

namespace Bit.Bmage;

/// <summary>
/// A unit of image processing that transforms one <see cref="BmageImageBuffer"/> into another. Operations are
/// immutable, serialisable descriptions of work; they carry no pixel data of their own and can be composed
/// into a pipeline, stored in edit history and replayed on proxy images.
/// </summary>
public interface IBmageImageOperation
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
    /// Executes the operation. When <see cref="BmageOperationContext.CanMutateSource"/> is true the implementation may modify
    /// <paramref name="source"/> in place and return the same instance; otherwise it must return a new buffer and leave the
    /// source untouched. The caller owns the returned buffer.
    /// </summary>
    BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context);

    /// <summary>
    /// Returns an equivalent operation expressed for an image that has been uniformly scaled by <paramref name="scale"/>
    /// relative to the coordinate space this operation was defined in. Resolution independent operations return themselves.
    /// Used to replay full-resolution edits on downscaled preview proxies.
    /// </summary>
    IBmageImageOperation ForScale(double scale);

    /// <summary>True when the operation cannot change any pixel (e.g. brightness 0) and may be dropped by the optimiser.</summary>
    bool IsIdentity { get; }
}
