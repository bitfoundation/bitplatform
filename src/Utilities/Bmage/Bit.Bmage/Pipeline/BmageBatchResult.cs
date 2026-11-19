namespace Bit.Bmage;

public sealed class BmageBatchResult
{
    public BmageBatchResult(IReadOnlyList<BmageBatchItemResult> results, TimeSpan duration)
    {
        Results = results;
        Duration = duration;
    }

    public IReadOnlyList<BmageBatchItemResult> Results { get; }
    public TimeSpan Duration { get; }
    public IEnumerable<BmageBatchItemResult> Succeeded => Results.Where(r => r.IsSuccess);
    public IEnumerable<BmageBatchItemResult> Failed => Results.Where(r => r.Error is not null);
    /// <summary>The items that never finished: the batch was cancelled, or stopped by <see cref="BmageBatchOptions.StopOnFirstError"/>.</summary>
    public IEnumerable<BmageBatchItemResult> Cancelled => Results.Where(r => r.WasCancelled);
    public int SuccessCount => Results.Count(r => r.IsSuccess);
    public int FailureCount => Results.Count(r => r.Error is not null);
    public int CancelledCount => Results.Count(r => r.WasCancelled);

    /// <summary>
    /// True when the batch's cancellation token stopped it before every item finished. The items it finished keep
    /// their outcome; the rest are in <see cref="Cancelled"/>.
    /// </summary>
    public bool WasCancelled { get; init; }
    public bool AllSucceeded => Results.All(r => r.IsSuccess);
    /// <summary>Total bytes of all successful outputs.</summary>
    public long TotalOutputBytes => Results.Where(r => r.Output is not null).Sum(r => r.Output!.Length);
    /// <summary>Total bytes of all inputs that were read.</summary>
    public long TotalInputBytes => Results.Sum(r => r.InputBytes);
}
