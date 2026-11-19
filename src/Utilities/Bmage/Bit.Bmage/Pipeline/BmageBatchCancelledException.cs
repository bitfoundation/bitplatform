namespace Bit.Bmage;

/// <summary>The aggregate outcome of a batch run.</summary>
/// <summary>
/// Thrown by <see cref="BmageImageProcessor.ProcessBatchAsync"/> when its token is cancelled before every item finished.
/// <see cref="Result"/> is what the batch had done by then: every image that completed keeps its output or its error,
/// and the rest are its <see cref="BmageBatchResult.Cancelled"/>.
/// </summary>
public sealed class BmageBatchCancelledException : OperationCanceledException
{
    public BmageBatchCancelledException(BmageBatchResult result, CancellationToken cancellationToken)
        : base("The batch was cancelled before every item finished.", cancellationToken)
    {
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }

    /// <summary>The batch as it stood when it was stopped, with <see cref="BmageBatchResult.WasCancelled"/> set.</summary>
    public BmageBatchResult Result { get; }
}
