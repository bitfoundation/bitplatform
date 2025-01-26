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
public class History(IJSRuntime js) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, Action<object>> _handlers = new();

    /// <summary>
    /// Returns an Integer representing the number of elements in the session history, including the currently loaded page.
    /// For example, for a page loaded in a new tab this property returns 1.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/length">https://developer.mozilla.org/en-US/docs/Web/API/History/length</see>
    /// </summary>
    public async Task<int> GetLength()
        => await js.FastInvokeAsync<int>("BitButil.history.length");

    /// <summary>
    /// Gets default scroll restoration behavior on history navigation. This property can be either auto or manual.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/scrollRestoration">https://developer.mozilla.org/en-US/docs/Web/API/History/scrollRestoration</see>
    /// </summary>
    public async Task<ScrollRestoration> GetScrollRestoration()
    {
        var value = await js.FastInvokeAsync<string>("BitButil.history.scrollRestoration");
        return value == "auto" ? ScrollRestoration.Auto : ScrollRestoration.Manual;
    }

    /// <summary>
    /// Allows web applications to explicitly set default scroll restoration behavior on history navigation. 
    /// This property can be either auto or manual.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/scrollRestoration">https://developer.mozilla.org/en-US/docs/Web/API/History/scrollRestoration</see>
    /// </summary>
    public async Task SetScrollRestoration(ScrollRestoration value)
        => await js.FastInvokeVoidAsync("BitButil.history.setScrollRestoration", value.ToString().ToLowerInvariant());

    /// <summary>
    /// Returns an any value representing the state at the top of the history stack.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/state">https://developer.mozilla.org/en-US/docs/Web/API/History/state</see>
    /// </summary>
    public async Task<object> GetState()
        => await js.FastInvokeAsync<object>("BitButil.history.state");

    /// <summary>
    /// This asynchronous method goes to the previous page in session history, the same action as 
    /// when the user clicks the browser's Back button. Calling this method to go back beyond the 
    /// first page in the session history has no effect and doesn't raise an exception.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/back">https://developer.mozilla.org/en-US/docs/Web/API/History/back</see>
    /// </summary>
    public async Task GoBack()
        => await js.FastInvokeVoidAsync("BitButil.history.back");

    /// <summary>
    /// This asynchronous method goes to the next page in session history, the same action as 
    /// when the user clicks the browser's Forward button. Calling this method to go forward 
    /// beyond the most recent page in the session history has no effect and doesn't raise an exception.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/forward">https://developer.mozilla.org/en-US/docs/Web/API/History/forward</see>
    /// </summary>
    public async Task GoForward()
        => await js.FastInvokeVoidAsync("BitButil.history.forward");

    /// <summary>
    /// Asynchronously loads a page from the session history, identified by its relative location 
    /// to the current page, for example -1 for the previous page or 1 for the next page. Calling 
    /// this method without parameters or a value of 0 reloads the current page.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/go">https://developer.mozilla.org/en-US/docs/Web/API/History/go</see>
    /// </summary>
    public async Task Go(int? delta = null)
        => await js.FastInvokeVoidAsync("BitButil.history.go", delta);

    /// <summary>
    /// Pushes the given data onto the session history stack with the specified title (and, if provided, URL).
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/History/pushState">https://developer.mozilla.org/en-US/docs/Web/API/History/pushState</see>
    /// </summary>
    /// <param name="state">The state object can be anything that can be serialized.</param>
    /// <param name="url">The new history entry's URL. The new URL must be of the same origin as the current URL; 
    /// otherwise PushState throws an exception.</param>
    public async Task PushState(object? state = null, string? url = null)
        => await js.FastInvokeVoidAsync("BitButil.history.pushState", state, string.Empty, url);

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
        => await js.FastInvokeVoidAsync("BitButil.history.replaceState", state, string.Empty, url);

    /// <summary>
    /// The popstate event of the Window interface is fired when the active history entry changes while the user 
    /// navigates the session history.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/popstate_event">https://developer.mozilla.org/en-US/docs/Web/API/Window/popstate_event</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HistoryListenersManager))]
    public async ValueTask<Guid> AddPopState(Action<object> handler)
    {
        var listenerId = HistoryListenersManager.AddListener(handler);
        _handlers.TryAdd(listenerId, handler);

        await js.FastInvokeVoidAsync("BitButil.history.addPopState", HistoryListenersManager.InvokeMethodName, listenerId);

        return listenerId;
    }

    /// <summary>
    /// The popstate event of the Window interface is fired when the active history entry changes while the user 
    /// navigates the session history.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/popstate_event">https://developer.mozilla.org/en-US/docs/Web/API/Window/popstate_event</see>
    /// </summary>
    public async ValueTask<Guid[]> RemovePopState(Action<object> handler)
    {
        var ids = HistoryListenersManager.RemoveListener(handler);

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
        HistoryListenersManager.RemoveListeners([id]);

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

        HistoryListenersManager.RemoveListeners(ids);

        await RemoveFromJs(ids);
    }

    private async ValueTask RemoveFromJs(Guid[] ids)
    {
        if (OperatingSystem.IsBrowser() is false) return;

        await js.FastInvokeVoidAsync("BitButil.history.removePopState", ids);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);

        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            await RemoveAllPopStates();
        }
    }
}
