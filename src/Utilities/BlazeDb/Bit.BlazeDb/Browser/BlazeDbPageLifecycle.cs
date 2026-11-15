using System.Runtime.Versioning;

namespace Bit.BlazeDb.Browser;

/// <summary>
/// Starts a flush of every open database in the tab when the page is hidden or unloaded, so a commit made
/// just before a reload, a navigation that loads a new page or a closed tab does not wait for the flusher's
/// next tick - which never comes. It is best effort: the browser does not wait for the writes it starts, so
/// <see cref="BlazeDbDatabase.FlushAsync"/> before leaving the page is still the only guarantee.
/// </summary>
[SupportedOSPlatform("browser")]
internal static class BlazeDbPageLifecycle
{
    // WebAssembly has one thread, so neither the list nor the flag needs a lock. Weak, so a database the
    // application drops without disposing is not kept alive by this.
    private static readonly List<WeakReference<BlazeDbDatabase>> Databases = [];
    private static bool _watching;

    public static void Watch(BlazeDbDatabase database)
    {
        // The module that elects the writer is imported by every browser backend before a database opens
        // on it. Where it is not, the database keeps its rows in a storage of the application's own, which
        // has nothing in the page to flush to.
        if (!BlazeDbBrowserModules.IsLoaded(BlazeDbBrowserModules.Opfs))
        {
            return;
        }
        if (!_watching)
        {
            BlazeDbOpfsInterop.WatchPageHide(FlushAll);
            _watching = true;
        }
        Databases.RemoveAll(static weak => !weak.TryGetTarget(out var open) || open.IsDisposed);
        Databases.Add(new(database));
    }

    private static void FlushAll()
    {
        foreach (var weak in Databases.ToArray())
        {
            if (weak.TryGetTarget(out var database) && !database.IsDisposed)
            {
                _ = FlushQuietlyAsync(database);
            }
        }
    }

    private static async Task FlushQuietlyAsync(BlazeDbDatabase database)
    {
        try
        {
            await database.FlushAsync().ConfigureAwait(false);
        }
        catch
        {
            // A flush that fails here is retried by the flusher if the page lives on, and reported through
            // LastBackgroundError like any other; a page going away has nobody to tell.
        }
    }
}
