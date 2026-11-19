using Bit.Butil;
using Microsoft.JSInterop;

namespace Bit.Bcrdt;

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
