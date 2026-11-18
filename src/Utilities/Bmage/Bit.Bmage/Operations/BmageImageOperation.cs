using System.Drawing;

namespace Bit.Bmage;

/// <summary>Convenience base class for operations with sensible defaults.</summary>
public abstract class BmageImageOperation : IBmageImageOperation
{
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public virtual Size GetOutputSize(Size inputSize) => inputSize;

    /// <inheritdoc />
    public abstract BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context);

    /// <inheritdoc />
    public virtual IBmageImageOperation ForScale(double scale) => this;

    /// <inheritdoc />
    public virtual bool IsIdentity => false;

    /// <summary>
    /// Returns a buffer the operation may write its output to: the source itself when in-place mutation is allowed and
    /// the output size matches, otherwise a fresh allocation.
    /// </summary>
    protected static BmageImageBuffer GetOutputBuffer(BmageImageBuffer source, BmageOperationContext context, int width, int height, bool clear = false)
    {
        if (context.CanMutateSource && source.Width == width && source.Height == height)
            return source;
        var output = context.Allocate(width, height, clear);
        output.Metadata = source.Metadata;
        return output;
    }

    /// <summary>Copies the source when in-place mutation is not allowed, otherwise returns the source.</summary>
    protected static BmageImageBuffer GetMutableCopy(BmageImageBuffer source, BmageOperationContext context)
    {
        if (context.CanMutateSource) return source;
        return source.Clone(context.Allocator);
    }

    public override string ToString() => Name;
}
