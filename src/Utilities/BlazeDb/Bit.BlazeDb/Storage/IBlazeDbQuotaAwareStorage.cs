namespace Bit.BlazeDb.Storage;

/// <summary>
/// Implemented by storage backends that can report the origin's remaining allowance - in the
/// browser, <c>navigator.storage.estimate()</c>. The engine consults this before flushing so it
/// can compact or stop deliberately, rather than discovering the limit as a failed append with
/// committed data already gone from the log buffer.
/// </summary>
public interface IBlazeDbQuotaAwareStorage : IBlazeDbStorage
{
    /// <summary>Current usage and allowance, or null when unavailable.</summary>
    ValueTask<BlazeDbStorageQuota?> GetQuotaAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether <see cref="IBlazeDbStorage.AppendAsync"/> rewrites the whole file instead of adding to
    /// its end. Both browser backends do - OPFS stages the new contents in a swap copy, IndexedDB
    /// puts the record back whole - so extending the log briefly needs room for the log twice over.
    /// The engine charges that against the allowance, which is the difference between compacting
    /// deliberately and discovering the limit as a failed write with the log already drained.
    /// </summary>
    bool AppendRewritesWholeFile => false;
}
