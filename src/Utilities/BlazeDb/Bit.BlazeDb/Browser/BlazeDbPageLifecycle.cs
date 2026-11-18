using System.Runtime.Versioning;

namespace Bit.BlazeDb;

/// <summary>
/// Starts a flush of every open database in the tab when the page is hidden or unloaded, so a commit made
/// just before a reload, a navigation that loads a new page or a closed tab does not wait for the flusher's
/// next tick - which never comes. It is best effort: the browser does not wait for the writes it starts, so
/// <see cref="BlazeDbDatabase.FlushAsync"/> before leaving the page is still the only guarantee.
/// </summary>
[SupportedOSPlatform("browser")]
internal static class BlazeDbPageLifecycle
{
    // Locked, because a multithreaded WebAssembly runtime (WasmEnableThreads) can open two databases
    // at once from continuations on different threads. Weak, so a database the application drops
    // without disposing is not kept alive by this.
    private static readonly object Gate = new();
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

        bool register;
        lock (Gate)
        {
            // Claimed under the lock, so two opens racing here register the handler once.
            register = !_watching;
            _watching = true;
            Databases.RemoveAll(static weak => !weak.TryGetTarget(out var open) || open.IsDisposed);
            Databases.Add(new(database));
        }
        if (!register)
        {
            return;
        }

        // Outside the lock, so the handler - which takes it - never waits on a thread that is itself waiting
        // for JavaScript. Should the runtime refuse the call on this thread - a worker without a JavaScript
        // context, on a multithreaded runtime - the database is open already - with a flusher and a writer lock that an exception from
        // its OpenAsync would leave behind - and this hook is only a best effort, so the failure does not
        // fail the open. The claim is given back instead, and the next database opened tries again.
        try
        {
            BlazeDbOpfsInterop.WatchPageHide(FlushAll);
        }
        catch
        {
            lock (Gate)
            {
                _watching = false;
            }
        }
    }

    private static void FlushAll()
    {
        WeakReference<BlazeDbDatabase>[] databases;
        lock (Gate)
        {
            databases = [.. Databases];
        }
        foreach (var weak in databases)
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
