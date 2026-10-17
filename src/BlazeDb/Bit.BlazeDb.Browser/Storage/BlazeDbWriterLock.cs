using System.Runtime.Versioning;

namespace Bit.BlazeDb.Browser;

/// <summary>
/// The cross-tab writer election both browser backends run through the Web Locks API. Held for the
/// life of the storage instance, and checked again before every write, because a document brought
/// back from the back/forward cache cannot assume it still holds what it held when it was frozen.
/// </summary>
[SupportedOSPlatform("browser")]
internal static class BlazeDbWriterLock
{
    public static string NameFor(string databaseName) => "blazedb:" + databaseName;

    /// <summary>Takes the database's writer lock or throws with the reason it could not.</summary>
    public static async ValueTask<string> AcquireAsync(string databaseName, bool allowWithoutWebLocks)
    {
        var lockName = NameFor(databaseName);
        if (await BlazeDbOpfsInterop.AcquireLock(lockName, allowWithoutWebLocks).ConfigureAwait(false))
        {
            return lockName;
        }
        if (!BlazeDbOpfsInterop.HasWebLocks())
        {
            throw new NotSupportedException(
                "This browser has no Web Locks API, so BlazeDb cannot make sure only one tab writes to " +
                $"'{databaseName}'. Pass allowWithoutWebLocks: true to open it anyway if the application " +
                "guarantees a single tab.");
        }
        throw new BlazeDbDatabaseLockedException(
            $"BlazeDbDatabase '{databaseName}' is already open in another tab. " +
            "BlazeDb allows a single writer tab per database.");
    }

    /// <summary>
    /// Refuses a write once the lock can no longer be vouched for. The data is still safe in memory
    /// and the engine reports the failure through <see cref="BlazeDbDatabase.LastBackgroundError"/>;
    /// what must not happen is this tab's bytes interleaving with the tab that now holds the lock.
    /// </summary>
    public static void EnsureHeld(string lockName)
    {
        if (!BlazeDbOpfsInterop.IsLockHeld(lockName))
        {
            throw new BlazeDbDatabaseLockedException(
                "This tab no longer holds the database's writer lock - it was restored from the " +
                "back/forward cache and another tab has taken over. Reload the page to continue.");
        }
    }
}
