using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// An operation that maps each pixel independently of its neighbours (colour adjustments, curves, matrices).
/// Point operations work on rows of <see cref="Vector4"/> pixels with channels in [0,1] (R,G,B,A, straight alpha),
/// which lets several of them run in a single pass over the image without intermediate quantisation.
/// </summary>
public abstract class BmagePointOperation : BmageImageOperation
{
    /// <summary>Transforms a row of pixels in place. Values may temporarily leave [0,1]; they are clamped when written back.</summary>
    public abstract void ProcessRow(Span<Vector4> pixels);

    /// <summary>
    /// Attempts to combine this operation with the one that follows it into a single equivalent operation.
    /// Returns null when the pair cannot be merged algebraically (they may still be executed in one pass).
    /// </summary>
    public virtual BmagePointOperation? TryFuse(BmagePointOperation next) => null;

    /// <summary>
    /// True when each output channel depends on the same input channel alone, so the operation is exactly a 256-entry
    /// table per channel. Only the library's own operations claim it, because their <see cref="ProcessRow"/> is known.
    /// </summary>
    internal virtual bool IsChannelSeparable => false;

    /// <summary>True for the library's own types, whose <see cref="ProcessRow"/> no other assembly can have replaced.</summary>
    private protected bool IsBuiltIn => GetType().Assembly == typeof(BmagePointOperation).Assembly;

    /// <summary>
    /// True when <see cref="ProcessRow"/> may run on several rows at once from different threads, which lets the
    /// operation spread over <see cref="BmageOperationContext.MaxDegreeOfParallelism"/> threads. The library's own operations
    /// keep no state between calls and say so; a custom operation keeps running on one thread unless it overrides this,
    /// which it should whenever its <see cref="ProcessRow"/> only reads its own fields.
    /// </summary>
    public virtual bool CanProcessRowsConcurrently => IsBuiltIn;

    /// <inheritdoc />
    public sealed override BmageImageBuffer Apply(BmageImageBuffer source, BmageOperationContext context)
        => BmagePointOperationExecutor.Run(source, context, this);
}
