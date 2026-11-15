using Bit.Butil;
using Microsoft.JSInterop;

namespace Bit.Bcrdt.Butil;

/// <summary>Something the browser reported about the page or the network.</summary>
internal enum BcrdtBrowserSignal
{
    /// <summary><c>online</c>: the browser believes it regained connectivity.</summary>
    Online,

    /// <summary><c>offline</c>: the browser believes it lost connectivity.</summary>
    Offline,

    /// <summary><c>visibilitychange</c> to hidden: another tab, a minimized window, a backgrounded app.</summary>
    Hidden,

    /// <summary><c>visibilitychange</c> to visible.</summary>
    Visible,

    /// <summary><c>pagehide</c>: the page is being unloaded, or put in the back/forward cache.</summary>
    PageHide,

    /// <summary><c>pageshow</c>: the page was loaded, or restored from the back/forward cache.</summary>
    PageShow,

    /// <summary><c>freeze</c>: the browser is about to suspend the page's tasks, and may discard it.</summary>
    Freeze,

    /// <summary><c>resume</c>: a frozen page runs again.</summary>
    Resume,
}

/// <summary>The few things the browser lifecycle needs from the browser.</summary>
/// <remarks>
/// An interface so the decisions in <see cref="BcrdtBrowserLifecycle"/> can be exercised without a browser; the
/// one implementation that ships, <see cref="BcrdtButilBrowser"/>, is a thin translation of Bit.Butil's services.
/// </remarks>
internal interface IBcrdtBrowser
{
    /// <summary>Whether this process runs in a browser, so the events below exist.</summary>
    bool IsAvailable { get; }

    /// <summary>Starts reporting the signals; disposing the result stops it.</summary>
    ValueTask<IAsyncDisposable> SubscribeAsync(Func<BcrdtBrowserSignal, Task> handler, CancellationToken cancellationToken);

    /// <summary><c>navigator.onLine</c>.</summary>
    ValueTask<bool> IsOnlineAsync(CancellationToken cancellationToken);

    /// <summary>Asks for persistent storage unless it is already granted; whether it is.</summary>
    ValueTask<bool> RequestPersistentStorageAsync(CancellationToken cancellationToken);
}

/// <summary>The browser, as Bit.Butil's services reach it.</summary>
internal sealed class BcrdtButilBrowser(Window window, Document document, Navigator navigator, StorageManager storageManager) : IBcrdtBrowser
{
    /// <remarks>
    /// The browser itself, not merely a JavaScript runtime: in Blazor Server the runtime is the circuit's, but the
    /// documents, their connections and their stores live in the server process, which a tab going offline or
    /// out of sight does not touch - <c>AddBitBcrdtCircuitPresence</c> covers presence there.
    /// </remarks>
    public bool IsAvailable => OperatingSystem.IsBrowser();

    public async ValueTask<IAsyncDisposable> SubscribeAsync(Func<BcrdtBrowserSignal, Task> handler, CancellationToken cancellationToken)
    {
        List<ButilSubscription> subscriptions = [];
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            subscriptions.Add(await window.SubscribeEvent<object>(ButilEvents.Online, _ => _ = handler(BcrdtBrowserSignal.Online)).ConfigureAwait(false));
            subscriptions.Add(await window.SubscribeEvent<object>(ButilEvents.Offline, _ => _ = handler(BcrdtBrowserSignal.Offline)).ConfigureAwait(false));
            subscriptions.Add(await window.SubscribeEvent<object>("pagehide", _ => _ = handler(BcrdtBrowserSignal.PageHide)).ConfigureAwait(false));
            subscriptions.Add(await window.SubscribeEvent<object>("pageshow", _ => _ = handler(BcrdtBrowserSignal.PageShow)).ConfigureAwait(false));
            subscriptions.Add(await window.SubscribeFreeze(() => _ = handler(BcrdtBrowserSignal.Freeze)).ConfigureAwait(false));
            subscriptions.Add(await window.SubscribeResume(() => _ = handler(BcrdtBrowserSignal.Resume)).ConfigureAwait(false));
            subscriptions.Add(await document.SubscribeVisibilityChange(state =>
                _ = handler(state == VisibilityState.Hidden ? BcrdtBrowserSignal.Hidden : BcrdtBrowserSignal.Visible)).ConfigureAwait(false));
        }
        catch
        {
            await DisposeAllAsync(subscriptions).ConfigureAwait(false);
            throw;
        }

        return new Subscriptions(subscriptions);
    }

    public async ValueTask<bool> IsOnlineAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await navigator.IsOnLine().ConfigureAwait(false);
    }

    public async ValueTask<bool> RequestPersistentStorageAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await storageManager.IsSupported().ConfigureAwait(false)) return false;

        // persist() may show a prompt in some browsers; once granted it stays granted, so ask only when it is not.
        return await storageManager.Persisted().ConfigureAwait(false) || await storageManager.Persist().ConfigureAwait(false);
    }

    private static async ValueTask DisposeAllAsync(List<ButilSubscription> subscriptions)
    {
        foreach (ButilSubscription subscription in subscriptions)
        {
            try
            {
                await subscription.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
                // The page is gone, and its listeners with it.
            }
        }
    }

    private sealed class Subscriptions(List<ButilSubscription> subscriptions) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => DisposeAllAsync(subscriptions);
    }
}
