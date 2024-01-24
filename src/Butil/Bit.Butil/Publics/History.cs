﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The History interface allows manipulation of the browser session history, that is the pages visited in the tab or frame that the current page is loaded in.
/// <br/>
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/History">https://developer.mozilla.org/en-US/docs/Web/API/History</see>
/// </summary>
public class History(IJSRuntime js) : IDisposable
{
    private readonly ConcurrentDictionary<Guid, Action<object>> _handlers = new();

    /// <summary>
    /// Returns an Integer representing the number of elements in the session history, including the currently loaded page.
    /// For example, for a page loaded in a new tab this property returns 1.
    /// </summary>
    public async Task<int> GetLength()
        => await js.InvokeAsync<int>("BitButil.history.length");

    /// <summary>
    /// Gets default scroll restoration behavior on history navigation. This property can be either auto or manual.
    /// </summary>
    public async Task<ScrollRestoration> GetScrollRestoration()
    {
        var value = await js.InvokeAsync<string>("BitButil.history.scrollRestoration");
        return value == "auto" ? ScrollRestoration.Auto : ScrollRestoration.Manual;
    }

    /// <summary>
    /// Allows web applications to explicitly set default scroll restoration behavior on history navigation. This property can be either auto or manual.
    /// </summary>
    public async Task SetScrollRestoration(ScrollRestoration value)
        => await js.InvokeVoidAsync("BitButil.history.setScrollRestoration", value.ToString().ToLowerInvariant());

    /// <summary>
    /// Returns an any value representing the state at the top of the history stack.
    /// </summary>
    public async Task<object> GetState()
        => await js.InvokeAsync<object>("BitButil.history.state");

    /// <summary>
    /// This asynchronous method goes to the previous page in session history, the same action as when the user clicks the browser's Back button.
    /// Calling this method to go back beyond the first page in the session history has no effect and doesn't raise an exception.
    /// </summary>
    public async Task GoBack()
        => await js.InvokeVoidAsync("BitButil.history.back");

    /// <summary>
    /// This asynchronous method goes to the next page in session history, the same action as when the user clicks the browser's Forward button.
    /// Calling this method to go forward beyond the most recent page in the session history has no effect and doesn't raise an exception.
    /// </summary>
    /// <returns></returns>
    public async Task GoForward()
        => await js.InvokeVoidAsync("BitButil.history.forward");

    /// <summary>
    /// Asynchronously loads a page from the session history, identified by its relative location to the current page, for example -1 for the previous page or 1 for the next page.
    /// Calling this method without parameters or a value of 0 reloads the current page.
    /// </summary>
    public async Task Go(int? delta = null)
        => await js.InvokeVoidAsync("BitButil.history.go", delta);

    /// <summary>
    /// Pushes the given data onto the session history stack with the specified title (and, if provided, URL).
    /// </summary>
    /// <param name="state">The state object can be anything that can be serialized.</param>
    /// <param name="url">The new history entry's URL. The new URL must be of the same origin as the current URL; otherwise PushState throws an exception.</param>
    public async Task PushState(object? state = null, string? url = null)
        => await js.InvokeVoidAsync("BitButil.history.pushState", state, string.Empty, url);

    /// <summary>
    /// Updates the most recent entry on the history stack to have the specified data, title, and, if provided, URL.
    /// </summary>
    /// <param name="state">An object which is associated with the history entry passed to the ReplaceState() method. The state object can be null.</param>
    /// <param name="url">The URL of the history entry. The new URL must be of the same origin as the current URL; otherwise ReplaceState throws an exception.</param>
    public async Task ReplaceState(object? state = null, string? url = null)
        => await js.InvokeVoidAsync("BitButil.history.replaceState", state, string.Empty, url);

    /// <summary>
    /// The popstate event of the Window interface is fired when the active history entry changes while the user navigates the session history.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HistoryListenersManager))]
    public async Task<Guid> AddPopState(Action<object> handler)
    {
        var listenerId = HistoryListenersManager.AddListener(handler);
        _handlers.TryAdd(listenerId, handler);

        await js.InvokeVoidAsync("BitButil.history.addPopState", HistoryListenersManager.InvokeMethodName, listenerId);

        return listenerId;
    }

    /// <summary>
    /// The popstate event of the Window interface is fired when the active history entry changes while the user navigates the session history.
    /// </summary>
    public Guid[] RemovePopState(Action<object> handler)
    {
        var ids = HistoryListenersManager.RemoveListener(handler);

        RemovePopState(ids);

        return ids;
    }

    /// <summary>
    /// The popstate event of the Window interface is fired when the active history entry changes while the user navigates the session history.
    /// </summary>
    public void RemovePopState(Guid id)
    {
        HistoryListenersManager.RemoveListeners([id]);

        RemovePopState([id]);
    }

    private void RemovePopState(Guid[] ids)
    {
        foreach (var id in ids)
        {
            _handlers.TryRemove(id, out _);
        }

        _ = js.InvokeVoidAsync("BitButil.history.removePopState", ids);
    }

    public async Task RemoveAllPopStates()
    {
        var ids = _handlers.Select(h => h.Key).ToArray();

        _handlers.Clear();

        HistoryListenersManager.RemoveListeners(ids);

        _ = js.InvokeVoidAsync("BitButil.history.removePopState", ids);
    }

    public void Dispose()
    {
        _ = RemoveAllPopStates();
    }
}
