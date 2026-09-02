using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Background_Fetch_API">Background Fetch API</see>
/// (<c>ServiceWorkerRegistration.backgroundFetch</c>): downloads and uploads the browser owns, which
/// keep running after the tab is closed and show the user their own progress UI.
/// </summary>
/// <remarks>
/// <see cref="Fetch"/> is not <see cref="Fetch"/>-the-service: an ordinary fetch dies with the page
/// and is invisible to the user, while this one survives the tab closing, survives a reload, is
/// resumed by the browser after a connection drops, and appears in the same place as a browser
/// download so the user can see and cancel it. That is the trade: the browser is doing it, on its
/// own schedule, and it tells the user so.
/// <br/>
/// <see cref="BackgroundSync"/> is the other API for work that outlives the page, and it is a
/// different one: sync gets a service-worker event when connectivity returns and has to do the work
/// itself in a few seconds; this transfers whole files for as long as it takes.
/// <br/>
/// The responses land in the service worker, which receives <c>backgroundfetchsuccess</c> /
/// <c>backgroundfetchfail</c> / <c>backgroundfetchabort</c> and is where they should be stored (a
/// Cache, or <see cref="OriginPrivateFileSystem"/>) - by the time a page asks,
/// <see cref="BackgroundFetchRegistrationInfo.RecordsAvailable"/> has usually gone false.
/// <br/>
/// Requires an active service worker registration, and is Chromium-only at the time of writing.
/// </remarks>
[ButilService(typeof(BackgroundFetch))]
public class BackgroundFetch(IJSRuntime js) : IAsyncDisposable
{
    internal const string ProgressMethodName = nameof(InvokeBackgroundFetchProgress);

    private readonly ConcurrentDictionary<Guid, Action<BackgroundFetchRegistrationInfo>> _progressHandlers = new();

    // Per-instance callback reference (see Keyboard): subscriptions are isolated per circuit / WASM
    // app and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<BackgroundFetch>? _dotNetRef;
    private DotNetObjectReference<BackgroundFetch> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the active service worker registration exposes <c>backgroundFetch</c>.</summary>
    /// <remarks>
    /// This is false until a service worker is registered, even in a browser that implements the
    /// API - the whole surface hangs off a registration.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.backgroundFetch.isSupported");

    /// <summary>
    /// Invoked from JS on each progress event. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(ProgressMethodName)]
    public void InvokeBackgroundFetchProgress(Guid id, BackgroundFetchRegistrationInfo registration)
    {
        if (_progressHandlers.TryGetValue(id, out var handler)) handler.Invoke(registration);
    }

    /// <summary>
    /// Starts a background fetch.
    /// </summary>
    /// <param name="id">
    /// Your name for this fetch, used to find it again in a later session. It has to be unique among
    /// the fetches currently running for this service worker registration - reusing a live id fails.
    /// </param>
    /// <param name="urls">The requests to make. Same-origin, or cross-origin with CORS.</param>
    /// <param name="title">What the browser shows the user while it runs.</param>
    /// <param name="downloadTotal">
    /// Total bytes you expect to download. The browser shows progress against it and aborts the
    /// fetch if the real total exceeds it, so an estimate that is too low is worse than 0 (which
    /// means "unknown" and shows an indeterminate UI).
    /// </param>
    /// <param name="icons">Icons for the browser's UI. Optional.</param>
    /// <returns>The registration, or null when the API is unavailable or the browser refused the fetch.</returns>
    /// <remarks>
    /// The browser may prompt or delay - it treats this as a download the user is aware of, which
    /// is what it is. Nothing here reaches your code again except through the service worker's
    /// <c>backgroundfetch*</c> events and <see cref="SubscribeProgress"/>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BackgroundFetchIcon))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BackgroundFetchRegistrationInfo))]
    public ValueTask<BackgroundFetchRegistrationInfo?> Fetch(string id,
                                                             string[] urls,
                                                             string title = "",
                                                             long downloadTotal = 0,
                                                             BackgroundFetchIcon[]? icons = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(urls);
        return js.Invoke<BackgroundFetchRegistrationInfo?>("BitButil.backgroundFetch.fetch",
            id, urls, title, downloadTotal, icons ?? []);
    }

    /// <summary>Reads a running fetch's progress, or null when nothing is running under that id.</summary>
    /// <param name="id">The id the fetch was started with.</param>
    /// <remarks>
    /// A fetch disappears from here soon after it finishes - a null is "not running now", not
    /// "never existed".
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BackgroundFetchRegistrationInfo))]
    public ValueTask<BackgroundFetchRegistrationInfo?> Get(string id)
        => js.Invoke<BackgroundFetchRegistrationInfo?>("BitButil.backgroundFetch.get", id);

    /// <summary>Lists the ids of the fetches currently running for this service worker registration.</summary>
    public ValueTask<string[]> GetIds() => js.Invoke<string[]>("BitButil.backgroundFetch.getIds");

    /// <summary>Cancels a running fetch. Returns false when nothing was running under that id.</summary>
    /// <param name="id">The id the fetch was started with.</param>
    /// <remarks>
    /// The service worker gets a <c>backgroundfetchabort</c> event, whether the abort came from here
    /// or from the user cancelling it in the browser's own UI.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Abort(string id) => js.Invoke<bool>("BitButil.backgroundFetch.abort", id);

    /// <summary>
    /// The request URLs a running fetch is made of, in the order they were given.
    /// </summary>
    /// <param name="id">The id the fetch was started with.</param>
    /// <returns>
    /// The URLs, or an empty array once
    /// <see cref="BackgroundFetchRegistrationInfo.RecordsAvailable"/> has gone false.
    /// </returns>
    public ValueTask<string[]> GetRecordUrls(string id) => js.Invoke<string[]>("BitButil.backgroundFetch.getRecordUrls", id);

    /// <summary>
    /// Reads one of a fetch's responses as text, once that request has finished.
    /// </summary>
    /// <param name="id">The id the fetch was started with.</param>
    /// <param name="url">One of the URLs from <see cref="GetRecordUrls"/>.</param>
    /// <param name="timeoutMs">How long to wait for that response before giving up.</param>
    /// <returns>The body, or null when the record is gone, the request hasn't finished, or the wait timed out.</returns>
    /// <remarks>
    /// A record's response resolves only when its request completes, which for the API's own use
    /// case is minutes away - the timeout is what keeps this from awaiting a whole download.
    /// </remarks>
    public ValueTask<string?> ReadRecordText(string id, string url, int timeoutMs = 10_000)
        => js.Invoke<string?>("BitButil.backgroundFetch.readRecordText", id, url, timeoutMs);

    /// <summary>
    /// Subscribes to a running fetch's progress events, which fire as bytes move.
    /// </summary>
    /// <param name="id">The id the fetch was started with.</param>
    /// <param name="handler">Called with the registration's current counters on every event.</param>
    /// <returns>
    /// A subscription to dispose, or null when nothing is running under that id.
    /// </returns>
    /// <remarks>
    /// This is a convenience for a page that happens to be open, not the way to observe a fetch:
    /// the events stop when the page goes away and the fetch does not. The service worker's
    /// <c>backgroundfetchsuccess</c> / <c>backgroundfetchfail</c> handlers are what always run.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BackgroundFetchRegistrationInfo))]
    [DynamicDependency(nameof(InvokeBackgroundFetchProgress), typeof(BackgroundFetch))]
    public async Task<ButilSubscription?> SubscribeProgress(string id, Action<BackgroundFetchRegistrationInfo> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var listenerId = Guid.NewGuid();
        _progressHandlers.TryAdd(listenerId, handler);

        var subscribed = await js.Invoke<bool>("BitButil.backgroundFetch.subscribeProgress", DotNetRef, listenerId, id);
        if (subscribed is false)
        {
            _progressHandlers.TryRemove(listenerId, out _);
            return null;
        }

        return new ButilSubscription(listenerId, async () =>
        {
            _progressHandlers.TryRemove(listenerId, out _);
            await js.InvokeVoid("BitButil.backgroundFetch.unsubscribeProgress", listenerId);
        });
    }

    /// <summary>
    /// On scope/circuit teardown, detaches every progress listener this instance registered. The
    /// fetches themselves keep running - that is what they are for.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            var listenerIds = _progressHandlers.Keys.ToArray();
            _progressHandlers.Clear();
            foreach (var listenerId in listenerIds)
                await js.InvokeVoid("BitButil.backgroundFetch.unsubscribeProgress", listenerId);
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}
