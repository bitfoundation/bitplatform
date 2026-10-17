namespace Bit.BlazeDb.Storage;

/// <summary>How much of the origin's storage allowance is used, as reported by the backend.</summary>
/// <param name="UsageBytes">Bytes currently attributed to this origin.</param>
/// <param name="QuotaBytes">Total bytes the origin may use, or 0 when the backend cannot say.</param>
public readonly record struct BlazeDbStorageQuota(long UsageBytes, long QuotaBytes)
{
    /// <summary>Bytes still available before the origin hits its limit.</summary>
    public long AvailableBytes => QuotaBytes <= 0 ? long.MaxValue : Math.Max(0, QuotaBytes - UsageBytes);

    /// <summary>Fraction of the allowance used, 0 when the quota is unknown.</summary>
    public double UsedFraction => QuotaBytes <= 0 ? 0 : (double)UsageBytes / QuotaBytes;
}
