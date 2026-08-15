using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigation_API">Navigation API</see>:
/// the modern successor to <see cref="History"/>, which can finally answer the question the old API
/// never could - <see cref="CanGoBack"/>.
/// </summary>
/// <remarks>
/// <b>What this is for.</b> <see cref="History"/> can move through the session history but cannot
/// see it: there is no way to ask whether there is anywhere to go back to, so an in-app back button
/// either renders enabled and sometimes does nothing, or navigates and traps the user in a loop.
/// This API exposes the list itself - <see cref="GetEntries"/>, <see cref="GetCurrentEntry"/>,
/// <see cref="CanGoBack"/> - and lets you jump straight to a remembered entry with
/// <see cref="TraverseTo"/>.
/// <br/>
/// <b>What this is not for.</b> Routing. Blazor's own router owns navigation inside the app; use
/// <c>NavigationManager.NavigateTo</c> for that. <see cref="Navigate"/> here is the raw browser
/// call, which - with nothing intercepting it - loads the document afresh. The API's
/// <c>intercept()</c> half is deliberately not wrapped: it exists so that a router can take over
/// navigations, and a second router fighting Blazor's is not something this library should make
/// easy.
/// <br/>
/// Reads are cheap and safe to call often. Everything is scoped to the current document, so a
/// cross-document navigation resets what <see cref="GetEntries"/> returns.
/// </remarks>
// Same trimming situation as History, for the same reason: DotNetObjectReference.Create demands every
// public method of this type be preserved, and this type's public surface includes a
// [RequiresUnreferencedCode] JSON generic (GetCurrentState<T>), so holding the reference raises IL2026.
// The interop ref only ever dispatches the [JSInvokable] callback, never the JSON generic, and that
// generic keeps its own RUC/RDC attributes so a trimming/AOT consumer is still warned at the real call
// site. Scoped to this type (not assembly-wide).
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "DotNetObjectReference.Create preserves all public methods; the RUC JSON API it pulls in is never invoked through this ref and stays annotated for consumers.")]
public class Navigation(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeNavigation);

    private readonly ConcurrentDictionary<Guid, Action<NavigationEventInfo>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Navigation>? _dotNetRef;
    private DotNetObjectReference<Navigation> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>
    /// Invoked from JS when a subscribed navigation event fires. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeNavigation(Guid id, string _, NavigationEventInfo info)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(info);
    }

    /// <summary>True when the runtime exposes <c>window.navigation</c>.</summary>
    /// <remarks>
    /// Baseline since early 2026, so a current Chrome, Edge, Firefox or Safari all answer true -
    /// but this is the one API on this page worth gating on, because the fallback (hiding a back
    /// button rather than showing a broken one) is a real difference to the user.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.navigation.isSupported");

    /// <summary>
    /// True when there is an entry behind the current one that this document may traverse to.
    /// </summary>
    /// <remarks>
    /// The reason this class exists. <c>history.length</c> counts the whole session including
    /// entries from other sites, and gives no way to tell a fresh tab from one with a page behind
    /// it; this is the direct answer, and it is what an in-app back button should be enabled by.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> CanGoBack() => js.Invoke<bool>("BitButil.navigation.canGoBack");

    /// <summary>True when there is an entry ahead of the current one to traverse to.</summary>
    /// <remarks>
    /// Going forward is only possible after going back, so this is false on a freshly loaded page
    /// and becomes true once the user has moved backwards through the list.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> CanGoForward() => js.Invoke<bool>("BitButil.navigation.canGoForward");

    /// <summary>The entry the document is currently showing, or null when the API is unavailable.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NavigationEntry))]
    public ValueTask<NavigationEntry?> GetCurrentEntry()
        => js.Invoke<NavigationEntry?>("BitButil.navigation.currentEntry");

    /// <summary>
    /// Every entry in this document's session history, oldest first - the list the browser's own
    /// back and forward buttons walk.
    /// </summary>
    /// <remarks>
    /// Only entries belonging to the current document are listed. Entries from other origins are
    /// not exposed at all, which is why this can be read without a permission prompt.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NavigationEntry))]
    public ValueTask<NavigationEntry[]> GetEntries()
        => js.Invoke<NavigationEntry[]>("BitButil.navigation.entries");

    /// <summary>
    /// The state object stored on the current entry, deserialized to
    /// <typeparamref name="T"/> - or <c>default</c> when there is none.
    /// </summary>
    /// <remarks>
    /// State set by <see cref="UpdateCurrentEntry"/> or by <see cref="Navigate"/> survives a
    /// reload and a traversal, which makes it the right place for "where was the user in this
    /// view" - a scroll offset, an open panel, a filter - as opposed to application data.
    /// </remarks>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<T?> GetCurrentState<[DynamicallyAccessedMembers(JsonSerialized)] T>()
        => js.Invoke<T?>("BitButil.navigation.currentState");

    /// <summary>
    /// Traverses one entry backwards, the same as the browser's back button.
    /// </summary>
    /// <returns>
    /// False when there was nothing behind the current entry, or the traversal was refused -
    /// check <see cref="CanGoBack"/> first if you want to know before asking. Nothing is thrown:
    /// a back button pressed at the start of the list is a normal outcome.
    /// </returns>
    public ValueTask<bool> Back() => js.Invoke<bool>("BitButil.navigation.back");

    /// <summary>Traverses one entry forwards, the same as the browser's forward button.</summary>
    /// <returns>False when there was nothing ahead, or the traversal was refused.</returns>
    public ValueTask<bool> Forward() => js.Invoke<bool>("BitButil.navigation.forward");

    /// <summary>
    /// Jumps straight to the entry with the given <see cref="NavigationEntry.Key"/>, however far
    /// away it is.
    /// </summary>
    /// <param name="key">A key taken from <see cref="GetEntries"/> or <see cref="GetCurrentEntry"/>.</param>
    /// <returns>False when the key names an entry that is no longer in the list.</returns>
    /// <remarks>
    /// This is what makes a "back to results" button work correctly: store the key when the user
    /// leaves the list and traverse to it later, rather than calling <see cref="Back"/> a guessed
    /// number of times or pushing a duplicate entry on top of the stack.
    /// </remarks>
    public ValueTask<bool> TraverseTo(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return js.Invoke<bool>("BitButil.navigation.traverseTo", key);
    }

    /// <summary>
    /// Navigates to <paramref name="url"/>.
    /// </summary>
    /// <param name="url">Where to go. Relative URLs resolve against the current document.</param>
    /// <param name="state">Optional state to store on the new entry - see <see cref="GetCurrentState"/>.</param>
    /// <param name="history">Whether to push a new entry, replace the current one, or let the browser choose.</param>
    /// <returns>False when the navigation was refused or aborted.</returns>
    /// <remarks>
    /// <b>This loads the document.</b> Nothing intercepts the navigation, so in a Blazor app this
    /// is a full page load, not a route change - use <c>NavigationManager.NavigateTo</c> for
    /// in-app routing. What this is good for is the case the router does not cover: leaving the app
    /// while controlling whether the current page stays on the back stack.
    /// </remarks>
    public ValueTask<bool> Navigate(string url, object? state = null, NavigationHistoryBehavior history = NavigationHistoryBehavior.Auto)
    {
        ArgumentNullException.ThrowIfNull(url);

        return js.Invoke<bool>("BitButil.navigation.navigate", url, state, history switch
        {
            NavigationHistoryBehavior.Push => "push",
            NavigationHistoryBehavior.Replace => "replace",
            _ => "auto",
        });
    }

    /// <summary>Reloads the current entry, optionally replacing its state.</summary>
    /// <returns>False when the reload was refused.</returns>
    public ValueTask<bool> Reload(object? state = null)
        => js.Invoke<bool>("BitButil.navigation.reload", state);

    /// <summary>
    /// Replaces the state on the current entry without navigating and without touching the history
    /// list.
    /// </summary>
    /// <returns>False when there is no current entry to update.</returns>
    /// <remarks>
    /// The right call for "remember where the user was" - it does not create an entry, so it can
    /// run on every scroll or filter change without filling the back stack, and unlike
    /// <see cref="History.ReplaceState"/> it does not require restating the URL.
    /// </remarks>
    public ValueTask<bool> UpdateCurrentEntry(object? state)
        => js.Invoke<bool>("BitButil.navigation.updateCurrentEntry", state);

    /// <summary>
    /// Calls <paramref name="handler"/> when the current entry changes - a traversal, a push, a
    /// replace, or an <see cref="UpdateCurrentEntry"/>.
    /// </summary>
    /// <returns>A subscription - dispose it to stop listening.</returns>
    /// <remarks>
    /// This is the one to use instead of <c>popstate</c>: it fires for every kind of change rather
    /// than only for traversals, and it reports which kind through
    /// <see cref="NavigationEventInfo.NavigationType"/>.
    /// </remarks>
    public ValueTask<ButilSubscription> SubscribeCurrentEntryChange(Action<NavigationEventInfo> handler)
        => Subscribe("currententrychange", handler);

    /// <summary>Calls <paramref name="handler"/> when a navigation completes successfully.</summary>
    /// <returns>A subscription - dispose it to stop listening.</returns>
    public ValueTask<ButilSubscription> SubscribeNavigateSuccess(Action<NavigationEventInfo> handler)
        => Subscribe("navigatesuccess", handler);

    /// <summary>
    /// Calls <paramref name="handler"/> when a navigation fails, with the reason in
    /// <see cref="NavigationEventInfo.Message"/>.
    /// </summary>
    /// <returns>A subscription - dispose it to stop listening.</returns>
    public ValueTask<ButilSubscription> SubscribeNavigateError(Action<NavigationEventInfo> handler)
        => Subscribe("navigateerror", handler);

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NavigationEventInfo))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NavigationEntry))]
    private async ValueTask<ButilSubscription> Subscribe(string eventName, Action<NavigationEventInfo> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers[id] = handler;
        await js.InvokeVoid("BitButil.navigation.subscribe", DotNetRef, id, eventName);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.navigation.unsubscribe", new[] { id });
        });
    }

    /// <summary>
    /// On scope/circuit teardown, detaches every listener this instance registered - the safety net
    /// under a subscription whose owner forgot to dispose it.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = _handlers.Keys.ToArray();
            _handlers.Clear();

            if (ids.Length > 0)
            {
                await js.InvokeVoid("BitButil.navigation.unsubscribe", ids);
            }
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
