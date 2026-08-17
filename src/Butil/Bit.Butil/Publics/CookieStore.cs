using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the modern async <see href="https://developer.mozilla.org/en-US/docs/Web/API/CookieStore">CookieStore</see> API.
/// </summary>
/// <remarks>
/// The legacy <see cref="Cookie"/> service still works on every browser, but it can only see Name/Value
/// because <c>document.cookie</c> doesn't expose other attributes. Use this service when you need the full
/// metadata (Domain/Path/Expires/SameSite). Browser support is Chromium-only at the time of writing.
/// </remarks>
[ButilService(typeof(CookieStore))]
public class CookieStore(IJSRuntime js) : IAsyncDisposable
{
    internal const string ChangeMethodName = nameof(InvokeCookieStoreChange);

    private readonly ConcurrentDictionary<Guid, Action<CookieStoreChange>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<CookieStore>? _dotNetRef;
    private DotNetObjectReference<CookieStore> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>cookieStore</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.cookieStore.isSupported");

    /// <summary>Returns every cookie visible to the current document.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CookieStoreItem))]
    public ValueTask<CookieStoreItem[]> GetAll() => js.Invoke<CookieStoreItem[]>("BitButil.cookieStore.getAll");

    /// <summary>Returns the cookie with the given name, or null when absent.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CookieStoreItem))]
    public ValueTask<CookieStoreItem?> Get(string name) => js.Invoke<CookieStoreItem?>("BitButil.cookieStore.get", name);

    /// <summary>Sets a cookie. Use <see cref="Delete"/> to remove one (don't pass MaxAge=0 - that's the legacy trick).</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CookieStoreItem))]
    public ValueTask Set(CookieStoreItem cookie) => js.InvokeVoid("BitButil.cookieStore.set", cookie);

    /// <summary>Deletes the named cookie.</summary>
    public ValueTask Delete(string name) => js.InvokeVoid("BitButil.cookieStore.delete", name);

    /// <summary>Invoked from JS on each cookie change. See <see cref="SubscribeChange"/>.</summary>
    [JSInvokable(ChangeMethodName)]
    public void InvokeCookieStoreChange(Guid id, CookieStoreChange change)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(change);
    }

    /// <summary>
    /// Watches cookies and calls <paramref name="handler"/> whenever any are written or removed.
    /// </summary>
    /// <param name="handler">Called with the changed and deleted cookies of each batch.</param>
    /// <returns>
    /// A subscription to dispose, or null on a browser without CookieStore - <c>document.cookie</c>
    /// has no change event, so there is no fallback to offer.
    /// </returns>
    /// <remarks>
    /// Fires for cookies set by script, by a server's <c>Set-Cookie</c>, and on expiry - which is
    /// what makes this useful for noticing a session cookie dying without polling for it.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CookieStoreChange))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CookieStoreItem))]
    [DynamicDependency(nameof(InvokeCookieStoreChange), typeof(CookieStore))]
    public async ValueTask<ButilSubscription?> SubscribeChange(Action<CookieStoreChange> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers[id] = handler;

        if (await js.Invoke<bool>("BitButil.cookieStore.subscribe", DotNetRef, id) is false)
        {
            _handlers.TryRemove(id, out _);
            return null;
        }

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.cookieStore.unsubscribe", id);
        });
    }

    /// <summary>
    /// On scope/circuit teardown, detaches any listener whose <see cref="ButilSubscription"/> was
    /// never disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _handlers.Clear();
            await js.InvokeVoid("BitButil.cookieStore.disposeAll");
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
