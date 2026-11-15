using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Browser;

/// <summary>
/// Turns the browser's own refusal of a write for lack of space into <see cref="BlazeDbStorageQuotaExceededException"/>.
/// <para>
/// The engine checks an estimate before every flush, but the estimate covers the whole origin - the Cache API,
/// other databases and other tabs write there too - and some browsers do not give one, so a write can still
/// reach the real limit. The browser then rejects it with a <c>QuotaExceededError</c>, which the modules mark
/// (<c>BLAZEDB_QUOTA_EXCEEDED</c>) because only the message survives the trip into a <see cref="JSException"/>.
/// Reported as the engine's own quota exception, it gets the same handling as one the estimate predicted: a
/// compaction first, then <see cref="BlazeDbDatabaseOptions.OnQuotaPressure"/>, and an error an application can
/// recognize in <see cref="BlazeDbDatabase.LastBackgroundError"/>.
/// </para>
/// </summary>
[SupportedOSPlatform("browser")]
internal static class BlazeDbBrowserQuota
{
    private const string Marker = "BLAZEDB_QUOTA_EXCEEDED";

    public static bool IsQuotaError(JSException exception) =>
        exception.Message.Contains(Marker, StringComparison.Ordinal);

    public static async ValueTask<BlazeDbStorageQuotaExceededException> RefusalAsync(
        IBlazeDbQuotaAwareStorage storage, long requiredBytes, JSException cause)
    {
        BlazeDbStorageQuota quota = default;
        try
        {
            quota = await storage.GetQuotaAsync().ConfigureAwait(false) ?? default;
        }
        catch
        {
            // The refusal is what matters; the numbers only describe it.
        }
        return new BlazeDbStorageQuotaExceededException(quota, requiredBytes, cause);
    }
}
