namespace Bit.Bmage;

/// <summary>The outcome for one image in a batch.</summary>
public sealed class BmageBatchItemResult
{
    private BmageBatchItemResult(BmageBatchItem item, BmageEncodedImage? output, Exception? error, bool cancelled, TimeSpan duration, long inputBytes)
    {
        Item = item; Output = output; Error = error; WasCancelled = cancelled; Duration = duration; InputBytes = inputBytes;
    }

    public static BmageBatchItemResult Success(BmageBatchItem item, BmageEncodedImage output, TimeSpan duration, long? inputBytes = null) => new(item, output, null, false, duration, inputBytes ?? item.Data.Length);
    public static BmageBatchItemResult Failure(BmageBatchItem item, Exception error, TimeSpan duration, long? inputBytes = null) => new(item, null, error, false, duration, inputBytes ?? item.Data.Length);
    public static BmageBatchItemResult Cancelled(BmageBatchItem item, long? inputBytes = null) => new(item, null, null, true, TimeSpan.Zero, inputBytes ?? item.Data.Length);

    public BmageBatchItem Item { get; }
    public BmageEncodedImage? Output { get; }
    public Exception? Error { get; }
    public bool WasCancelled { get; }
    public TimeSpan Duration { get; }
    public bool IsSuccess => Output is not null;
    public string? FileName => Item.FileName;

    /// <summary>The size of the item's file, once read: 0 for a deferred item the batch never reached.</summary>
    public long InputBytes { get; }
}
