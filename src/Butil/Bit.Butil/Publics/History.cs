using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The History interface allows manipulation of the browser session history, that is the pages visited in the tab or frame that the current page is loaded in.
/// <br/>
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/History">https://developer.mozilla.org/en-US/docs/Web/API/History</see>
/// </summary>
// DotNetObjectReference.Create demands every public method of this type be preserved for trimming, and
// this type's public surface includes [RequiresUnreferencedCode] JSON APIs (GetState<T>), so holding a
// DotNetObjectReference<History> field/property raises IL2026. The interop ref only ever dispatches the
// [JSInvokable] callbacks, never the JSON generics, and those generics keep their own RUC/RDC attributes
// so a trimming/AOT consumer is still warned at the real call site. Scoped to this type (not assembly-wide).
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "DotNetObjectReference.Create preserves all public methods; the RUC JSON APIs it pulls in are never invoked through this ref and stay annotated for consumers.")]
public class History(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeHistoryPopState);

    private readonly ConcurrentDictionary<Guid, Action<object>> _handlers = new();

    // Per-instance callback reference (see Keyboard/Geolocation): listeners are isolated per
    // circuit / WASM app and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<History>? _dotNetRef;
    private DotNetObjectReference<History> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>
    /// Invoked from JS on <c>popstate</c>. Public + <see cref="JSInvokableAttribute"/> so it can be
    /// dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeHistoryPopState(Guid id, object state)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(state);
    }

    /// <summary>
    /// Returns an Integer representing the number of elements in the session history, including the currently loaded page.
    /// For example, for a page loaded in a new tab this property returns 1.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/length">https://developer.mozilla.org/en-US/docs/Web/API/History/length</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<int> GetLength()
        => await js.InvokeFast<int>("BitButil.history.length");

    /// <summary>
    /// Gets default scroll restoration behavior on history navigation. This property can be either auto or manual.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/scrollRestoration">https://developer.mozilla.org/en-US/docs/Web/API/History/scrollRestoration</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<ScrollRestoration> GetScrollRestoration()
    {
        var value = await js.InvokeFast<string>("BitButil.history.scrollRestoration");
        return value == "auto" ? ScrollRestoration.Auto : ScrollRestoration.Manual;
    }

    /// <summary>
    /// Allows web applications to explicitly set default scroll restoration behavior on history navigation. 
    /// This property can be either auto or manual.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/scrollRestoration">https://developer.mozilla.org/en-US/docs/Web/API/History/scrollRestoration</see>
    /// </summary>
    public async Task SetScrollRestoration(ScrollRestoration value)
        => await js.InvokeVoid("BitButil.history.setScrollRestoration", value.ToString().ToLowerInvariant());

    /// <summary>
    /// Returns an any value representing the state at the top of the history stack.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/state">https://developer.mozilla.org/en-US/docs/Web/API/History/state</see>
    /// </summary>
    public async Task<object> GetState()
        => await js.InvokeFast<object>("BitButil.history.state");

    /// <summary>
    /// Strongly-typed accessor for <see cref="GetState"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public async Task<T?> GetState<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] T>()
        => await js.InvokeFast<T?>("BitButil.history.state");

    /// <summary>
    /// This asynchronous method goes to the previous page in session history, the same action as 
    /// when the user clicks the browser's Back button. Calling this method to go back beyond the 
    /// first page in the session history has no effect and doesn't raise an exception.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/back">https://developer.mozilla.org/en-US/docs/Web/API/History/back</see>
    /// </summary>
    public async Task GoBack()
        => await js.InvokeVoid("BitButil.history.back");

    /// <summary>
    /// This asynchronous method goes to the next page in session history, the same action as 
    /// when the user clicks the browser's Forward button. Calling this method to go forward 
    /// beyond the most recent page in the session history has no effect and doesn't raise an exception.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/forward">https://developer.mozilla.org/en-US/docs/Web/API/History/forward</see>
    /// </summary>
    public async Task GoForward()
        => await js.InvokeVoid("BitButil.history.forward");

    /// <summary>
    /// Asynchronously loads a page from the session history, identified by its relative location 
    /// to the current page, for example -1 for the previous page or 1 for the next page. Calling 
    /// this method without parameters or a value of 0 reloads the current page.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/go">https://developer.mozilla.org/en-US/docs/Web/API/History/go</see>
    /// </summary>
    public async Task Go(int? delta = null)
        => await js.InvokeVoid("BitButil.history.go", delta);

    /// <summary>
    /// Pushes the given data onto the session history stack with the specified title (and, if provided, URL).
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/pushState">https://developer.mozilla.org/en-US/docs/Web/API/History/pushState</see>
    /// </summary>
    /// <param name="state">The state object can be anything that can be serialized.</param>
    /// <param name="url">The new history entry's URL. The new URL must be of the same origin as the current URL; 
    /// otherwise PushState throws an exception.</param>
    public async Task PushState(object? state = null, string? url = null)
        => await js.InvokeVoid("BitButil.history.pushState", state, string.Empty, url);

    /// <summary>
    /// Updates the most recent entry on the history stack to have the specified data, title, and, if provided, URL.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/replaceState">https://developer.mozilla.org/en-US/docs/Web/API/History/replaceState</see>
    /// </summary>
    /// <param name="state">An object which is associated with the history entry passed to the ReplaceState() method. 
    /// The state object can be null.</param>
    /// <param name="url">The URL of the history entry. The new URL must be of the same origin as the current URL; 
    /// otherwise ReplaceState throws an exception.</param>
    public async Task ReplaceState(object? state = null, string? url = null)
        => await js.InvokeVoid("BitButil.history.replaceState", state, string.Empty, url);

    /// <summary>
    /// The popstate event of the Window interface is fired when the active history entry changes while the user 
    /// navigates the session history.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/popstate_event">https://developer.mozilla.org/en-US/docs/Web/API/Window/popstate_event</see>
    /// </summary>
    public async ValueTask<Guid> AddPopState(Action<object> handler)
    {
        var listenerId = Guid.NewGuid();
        _handlers.TryAdd(listenerId, handler);

        await js.InvokeVoid("BitButil.history.addPopState", DotNetRef, listenerId);

        return listenerId;
    }

    /// <summary>
    /// Subscribes to <c>popstate</c> and returns an <see cref="IAsyncDisposable"/> handle that
    /// detaches the listener when disposed. Pair with <c>await using</c>.
    /// </summary>
    public async ValueTask<ButilSubscription> SubscribePopState(Action<object> handler)
    {
        var id = await AddPopState(handler);
        return new ButilSubscription(id, () => RemovePopState(id));
    }

    /// <summary>
    /// The popstate event of the Window interface is fired when the active history entry changes while the user 
    /// navigates the session history.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/popstate_event">https://developer.mozilla.org/en-US/docs/Web/API/Window/popstate_event</see>
    /// </summary>
    /// <remarks>
    /// Listeners are matched by delegate identity, so you must pass the very same
    /// <paramref name="handler"/> instance that was registered. A newly-created lambda will not
    /// match and the returned array will be empty. To avoid this, keep the <see cref="Guid"/>
    /// returned by <see cref="AddPopState"/> and remove by id, or use <see cref="SubscribePopState"/>
    /// which returns a disposable <see cref="ButilSubscription"/>.
    /// </remarks>
    public async ValueTask<Guid[]> RemovePopState(Action<object> handler)
    {
        var ids = _handlers.Where(h => Equals(h.Value, handler)).Select(h => h.Key).ToArray();

        await RemovePopState(ids);

        return ids;
    }

    /// <summary>
    /// The popstate event of the Window interface is fired when the active history entry changes while the user 
    /// navigates the session history.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/popstate_event">https://developer.mozilla.org/en-US/docs/Web/API/Window/popstate_event</see>
    /// </summary>
    public async ValueTask RemovePopState(Guid id)
    {
        await RemovePopState([id]);
    }

    private async ValueTask RemovePopState(Guid[] ids)
    {
        if (ids.Length == 0) return;

        foreach (var id in ids)
        {
            _handlers.TryRemove(id, out _);
        }

        await RemoveFromJs(ids);
    }

    public async ValueTask RemoveAllPopStates()
    {
        if (_handlers.Count == 0) return;

        var ids = _handlers.Select(h => h.Key).ToArray();

        _handlers.Clear();

        await RemoveFromJs(ids);
    }

    private async ValueTask RemoveFromJs(Guid[] ids)
    {
        await js.InvokeVoid("BitButil.history.removePopState", ids);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);

        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing is false) return;

        try
        {
            await RemoveAllPopStates();
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }
}
