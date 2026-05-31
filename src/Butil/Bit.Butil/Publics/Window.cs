using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The Window interface represents a window containing a DOM document; 
/// the document property points to the DOM document loaded in that window.
/// <br />
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window">https://developer.mozilla.org/en-US/docs/Web/API/Window</see>
/// </summary>
public class Window(IJSRuntime js) : IAsyncDisposable
{
    private const string ElementName = "window";

    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Action<MediaQueryList>> _matchMediaHandlers = new();

    private readonly System.Collections.Concurrent.ConcurrentDictionary<(Guid Id, string Event, bool UseCapture), byte> _listenerIds = new();

    public async Task AddEventListener<T>(string domEvent, Action<T> listener, bool useCapture = false)
    {
        var id = await DomEventDispatcher.AddEventListener(js, ElementName, domEvent, listener, useCapture);
        _listenerIds.TryAdd((id, domEvent, useCapture), 0);
    }

    public async Task RemoveEventListener<T>(string domEvent, Action<T> listener, bool useCapture = false)
    {
        var ids = await DomEventDispatcher.RemoveEventListener(js, ElementName, domEvent, listener, useCapture);
        foreach (var id in ids) _listenerIds.TryRemove((id, domEvent, useCapture), out _);
    }

    /// <summary>
    /// Subscribe variant of <see cref="AddEventListener{T}"/> returning an <see cref="IAsyncDisposable"/> handle.
    /// </summary>
    public async Task<ButilSubscription> SubscribeEvent<T>(string domEvent, Action<T> listener, bool useCapture = false)
    {
        var id = await DomEventDispatcher.AddEventListener(js, ElementName, domEvent, listener, useCapture);
        var key = (id, domEvent, useCapture);
        _listenerIds.TryAdd(key, 0);

        return new ButilSubscription(id, async () =>
        {
            _listenerIds.TryRemove(key, out _);
            if (OperatingSystem.IsBrowser() is false) return;
            await DomEventDispatcher.RemoveEventListenerById(js, ElementName, domEvent, id, useCapture);
        });
    }

    /// <summary>
    /// The beforeunload event is fired when the current window, contained document, and associated resources are about to be unloaded. 
    /// The document is still visible and the event is still cancelable at this point.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event">https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event</see>
    /// </summary>
    public async Task AddBeforeUnload()
        => await js.InvokeVoid("BitButil.window.addBeforeUnload");

    /// <summary>
    /// Same as <see cref="AddBeforeUnload()"/> but stores a confirmation message. Modern browsers
    /// ignore the message text and show their own generic warning, but supplying a message
    /// guarantees the prompt fires consistently across user-gesture vs auto-navigation cases.
    /// </summary>
    public Task AddBeforeUnload(string message)
        => js.InvokeVoid("BitButil.window.addBeforeUnload", message).AsTask();

    /// <summary>
    /// The beforeunload event is fired when the current window, contained document, and associated resources are about to be unloaded. 
    /// The document is still visible and the event is still cancelable at this point.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event">https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event</see>
    /// </summary>
    public async Task RemoveBeforeUnload()
        => await js.InvokeVoid("BitButil.window.removeBeforeUnload");

    // ─── Page Lifecycle ─────────────────────────────────────────────────────────

    /// <summary>
    /// Fires when the page is frozen by the browser (typically because it has been moved
    /// to the back/forward cache). Use this to release expensive resources.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/freeze_event">freeze</see>
    /// </summary>
    public Task<ButilSubscription> SubscribeFreeze(Action handler)
    {
        Action<object> bridge = _ => handler();
        return SubscribeEvent("freeze", bridge);
    }

    /// <summary>
    /// Fires when the page resumes from the back/forward cache.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/resume_event">resume</see>
    /// </summary>
    public Task<ButilSubscription> SubscribeResume(Action handler)
    {
        Action<object> bridge = _ => handler();
        return SubscribeEvent("resume", bridge);
    }

    /// <summary>
    /// Gets the height of the content area of the browser window in px including, if rendered, the horizontal scrollbar.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/innerHeight">https://developer.mozilla.org/en-US/docs/Web/API/Window/innerHeight</see>
    /// </summary>
    public async Task<float> GetInnerHeight()
        => await js.Invoke<float>("BitButil.window.innerHeight");

    /// <summary>
    /// Gets the width of the content area of the browser window in px including, if rendered, the vertical scrollbar.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/innerWidth">https://developer.mozilla.org/en-US/docs/Web/API/Window/innerWidth</see>
    /// </summary>
    public async Task<float> GetInnerWidth()
        => await js.Invoke<float>("BitButil.window.innerWidth");

    /// <summary>
    /// Returns a boolean indicating whether the current context is secure (true) or not (false).
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/isSecureContext">https://developer.mozilla.org/en-US/docs/Web/API/isSecureContext</see>
    /// </summary>
    public async Task<bool> IsSecureContext()
        => await js.Invoke<bool>("BitButil.window.isSecureContext");

    /// <summary>
    /// Returns the locationbar object. For privacy and interoperability reasons, 
    /// the value of the visible property is now false if this Window is a popup, and true otherwise.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/locationbar">https://developer.mozilla.org/en-US/docs/Web/API/Window/locationbar</see>
    /// </summary>
    public async Task<BarProp> GetLocationBar()
        => await js.Invoke<BarProp>("BitButil.window.locationbar");

    /// <summary>
    /// Gets the name of the window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/name">https://developer.mozilla.org/en-US/docs/Web/API/Window/name</see>
    /// </summary>
    public async Task<string> GetName()
        => await js.Invoke<string>("BitButil.window.getName");
    /// <summary>
    /// Sets the name of the window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/name">https://developer.mozilla.org/en-US/docs/Web/API/Window/name</see>
    /// </summary>
    public async Task SetName(string value)
        => await js.InvokeVoid("BitButil.window.setName", value);

    /// <summary>
    /// Returns the global object's origin, serialized as a string.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/origin">https://developer.mozilla.org/en-US/docs/Web/API/origin</see>
    /// </summary>
    public async Task<string> GetOrigin()
        => await js.Invoke<string>("BitButil.window.origin");

    /// <summary>
    /// Gets the height of the outside of the browser window in px.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/outerHeight">https://developer.mozilla.org/en-US/docs/Web/API/Window/outerHeight</see>
    /// </summary>
    public async Task<float> GetOuterHeight()
        => await js.Invoke<float>("BitButil.window.outerHeight");

    /// <summary>
    /// Gets the width of the outside of the browser window in px.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/outerWidth">https://developer.mozilla.org/en-US/docs/Web/API/Window/outerWidth</see>
    /// </summary>
    public async Task<float> GetOuterWidth()
        => await js.Invoke<float>("BitButil.window.outerWidth");

    /// <summary>
    /// Returns the horizontal distance in px from the left border of the user's browser viewport to the left side of the screen.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/screenX">https://developer.mozilla.org/en-US/docs/Web/API/Window/screenX</see>
    /// </summary>
    public async Task<float> GetScreenX()
        => await js.Invoke<float>("BitButil.window.screenX");

    /// <summary>
    /// Returns the vertical distance in px from the top border of the user's browser viewport to the top side of the screen.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/screenY">https://developer.mozilla.org/en-US/docs/Web/API/Window/screenY</see>
    /// </summary>
    public async Task<float> GetScreenY()
        => await js.Invoke<float>("BitButil.window.screenY");

    /// <summary>
    /// Returns the number of pixels that the document has already been scrolled horizontally.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollX">https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollX</see>
    /// </summary>
    public async Task<float> GetScrollX()
        => await js.Invoke<float>("BitButil.window.scrollX");

    /// <summary>
    /// Returns the number of pixels that the document has already been scrolled vertically.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollY">https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollY</see>
    /// </summary>
    public async Task<float> GetScrollY()
        => await js.Invoke<float>("BitButil.window.scrollY");

    /// <summary>
    /// Decodes a string of data which has been encoded using base-64 encoding.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/atob">https://developer.mozilla.org/en-US/docs/Web/API/atob</see>
    /// </summary>
    public async Task<string> Atob(string data)
        => await js.Invoke<string>("BitButil.window.atob", data);

    /// <summary>
    /// Displays an alert dialog.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/alert">https://developer.mozilla.org/en-US/docs/Web/API/Window/alert</see>
    /// </summary>
    public async Task Alert(string? message = null)
        => await js.InvokeVoid("BitButil.window.alert", message);

    /// <summary>
    /// Sets focus away from the window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/blur">https://developer.mozilla.org/en-US/docs/Web/API/Window/blur</see>
    /// </summary>
    public async Task Blur()
        => await js.InvokeVoid("BitButil.window.blur");

    /// <summary>
    /// Creates a base-64 encoded ASCII string from a string of binary data.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/btoa">https://developer.mozilla.org/en-US/docs/Web/API/btoa</see>
    /// </summary>
    public async Task<string> Btoa(string data)
        => await js.Invoke<string>("BitButil.window.btoa", data);

    /// <summary>
    /// Closes the current window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/close">https://developer.mozilla.org/en-US/docs/Web/API/Window/close</see>
    /// </summary>
    public async Task Close(string? id = null)
        => await (id is null ? js.InvokeVoid("BitButil.window.close")
                             : js.InvokeVoid("BitButil.window.close", id));

    /// <summary>
    /// Displays a dialog with a message that the user needs to respond to.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/confirm">https://developer.mozilla.org/en-US/docs/Web/API/Window/confirm</see>
    /// </summary>
    public async Task<bool> Confirm(string? message = null)
        => await js.Invoke<bool>("BitButil.window.confirm", message);

    /// <summary>
    /// Searches for a given string in a window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/find">https://developer.mozilla.org/en-US/docs/Web/API/Window/find</see>
    /// </summary>
    public async Task<bool> Find(string? text = null,
        bool? caseSensitive = null,
        bool? backward = null,
        bool? wrapAround = null,
        bool? wholeWord = null,
        bool? searchInFrame = null)
        => await js.Invoke<bool>("BitButil.window.find", text, caseSensitive, backward, wrapAround, wholeWord, searchInFrame);

    /// <summary>
    /// Sets focus on the current window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/focus">https://developer.mozilla.org/en-US/docs/Web/API/Window/focus</see>
    /// </summary>
    public async Task Focus()
        => await js.InvokeVoid("BitButil.window.focus");

    /// <summary>
    /// Returns a snapshot of the current selection (selected text plus range metadata).
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/getSelection">https://developer.mozilla.org/en-US/docs/Web/API/Window/getSelection</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WindowSelection))]
    public async Task<WindowSelection?> GetSelection()
        => await js.Invoke<WindowSelection?>("BitButil.window.getSelection");

    /// <summary>
    /// Returns just the selected text (equivalent to <c>window.getSelection().toString()</c>).
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/getSelection">https://developer.mozilla.org/en-US/docs/Web/API/Window/getSelection</see>
    /// </summary>
    public async Task<string> GetSelectionText()
        => await js.Invoke<string>("BitButil.window.getSelectionText");

    /// <summary>Removes any current selection.</summary>
    public Task ClearSelection() => js.InvokeVoid("BitButil.window.clearSelection").AsTask();

    /// <summary>
    /// Selects every text node inside <paramref name="element"/>. Works on form-control inputs
    /// too, falling back to <c>HTMLInputElement.select()</c>.
    /// </summary>
    public Task SelectElement(Microsoft.AspNetCore.Components.ElementReference element)
        => js.InvokeVoid("BitButil.window.selectElement", element).AsTask();

    /// <summary>Copies the current selection to the clipboard, returning true on success.</summary>
    public Task<bool> CopySelection() => js.Invoke<bool>("BitButil.window.copySelection").AsTask();

    /// <summary>
    /// Returns a MediaQueryList object representing the specified media query string.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/matchMedia">https://developer.mozilla.org/en-US/docs/Web/API/Window/matchMedia</see>
    /// </summary>
    public async Task<MediaQueryList> MatchMedia(string query)
        => await js.Invoke<MediaQueryList>("BitButil.window.matchMedia", query);

    /// <summary>
    /// Subscribes to the <c>change</c> event of <c>matchMedia(query)</c>. The handler fires whenever
    /// the media query's evaluation flips (e.g. when the user toggles dark mode or rotates the device).
    /// Use <see cref="UnsubscribeMatchMedia(Guid)"/> with the returned id to stop listening.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaQueryList/change_event">https://developer.mozilla.org/en-US/docs/Web/API/MediaQueryList/change_event</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaQueryListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaQueryList))]
    public async Task<Guid> SubscribeMatchMedia(string query, Action<MediaQueryList> handler)
    {
        var listenerId = MediaQueryListenersManager.AddListener(handler);
        _matchMediaHandlers.TryAdd(listenerId, handler);

        await js.InvokeVoid("BitButil.window.subscribeMatchMedia",
            MediaQueryListenersManager.InvokeMethodName,
            listenerId,
            query);

        return listenerId;
    }

    /// <summary>
    /// Subscribe variant of <see cref="SubscribeMatchMedia(string, Action{MediaQueryList})"/>
    /// returning an <see cref="IAsyncDisposable"/> handle.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaQueryListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MediaQueryList))]
    public async Task<ButilSubscription> WatchMatchMedia(string query, Action<MediaQueryList> handler)
    {
        var id = await SubscribeMatchMedia(query, handler);
        return new ButilSubscription(id, () => UnsubscribeMatchMedia(id));
    }

    /// <summary>
    /// Removes a previously registered match-media listener.
    /// </summary>
    public async ValueTask UnsubscribeMatchMedia(Guid id)
    {
        MediaQueryListenersManager.RemoveListeners([id]);
        _matchMediaHandlers.TryRemove(id, out _);
        if (OperatingSystem.IsBrowser() is false) return;
        await js.InvokeVoid("BitButil.window.unsubscribeMatchMedia", new[] { id });
    }

    /// <summary>
    /// Removes a previously registered match-media listener by handler reference.
    /// </summary>
    public async ValueTask<Guid[]> UnsubscribeMatchMedia(Action<MediaQueryList> handler)
    {
        var ids = MediaQueryListenersManager.RemoveListener(handler);
        if (ids.Length == 0) return ids;

        foreach (var id in ids) _matchMediaHandlers.TryRemove(id, out _);

        if (OperatingSystem.IsBrowser())
        {
            await js.InvokeVoid("BitButil.window.unsubscribeMatchMedia", ids);
        }

        return ids;
    }

    /// <summary>
    /// Opens a new window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/open">https://developer.mozilla.org/en-US/docs/Web/API/Window/open</see>
    /// </summary>
    public async Task<string?> Open(string? url = null, string? target = null, string? windowFeatures = null)
        => await js.Invoke<string?>("BitButil.window.open", Guid.NewGuid(), url, target, windowFeatures);
    /// <summary>
    /// Opens a new window.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/open">https://developer.mozilla.org/en-US/docs/Web/API/Window/open</see>
    /// </summary>
    public async Task<string?> Open(string? url = null, string? target = null, WindowFeatures? windowFeatures = null)
        => await js.Invoke<string?>("BitButil.window.open", Guid.NewGuid(), url, target, windowFeatures?.ToString());

    /// <summary>
    /// Opens the Print Dialog to print the current document.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/print">https://developer.mozilla.org/en-US/docs/Web/API/Window/print</see>
    /// </summary>
    public async Task Print()
        => await js.InvokeVoid("BitButil.window.print");

    /// <summary>
    /// Returns the text entered by the user in a prompt dialog.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/prompt">https://developer.mozilla.org/en-US/docs/Web/API/Window/prompt</see>
    /// </summary>
    public async Task<string> Prompt(string? message, string? defaultValue)
        => await js.Invoke<string>("BitButil.window.prompt", message, defaultValue);

    /// <summary>
    /// Scrolls the window to a particular place in the document.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/scroll">https://developer.mozilla.org/en-US/docs/Web/API/Window/scroll</see>
    /// </summary>
    public async Task Scroll(ScrollOptions? options)
        => await js.InvokeVoid("BitButil.window.scroll", options?.ToJsObject(), null, null);
    /// <summary>
    /// Scrolls the window to a particular place in the document.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/scroll">https://developer.mozilla.org/en-US/docs/Web/API/Window/scroll</see>
    /// </summary>
    public async Task Scroll(float? x, float? y)
        => await js.InvokeVoid("BitButil.window.scroll", null, x, y);

    /// <summary>
    /// Scrolls the document in the window by the given amount.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollBy">https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollBy</see>
    /// </summary>
    public async Task ScrollBy(ScrollOptions? options)
        => await js.InvokeVoid("BitButil.window.scrollBy", options?.ToJsObject(), null, null);
    /// <summary>
    /// Scrolls the document in the window by the given amount.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollBy">https://developer.mozilla.org/en-US/docs/Web/API/Window/scrollBy</see>
    /// </summary>
    public async Task ScrollBy(float? x, float? y)
        => await js.InvokeVoid("BitButil.window.scrollBy", null, x, y);

    /// <summary>
    /// This method stops window loading.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/stop">https://developer.mozilla.org/en-US/docs/Web/API/Window/stop</see>
    /// </summary>
    public async Task Stop()
        => await js.InvokeVoid("BitButil.window.stop");

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
            if (_matchMediaHandlers.Count > 0)
            {
                var ids = _matchMediaHandlers.Keys.ToArray();
                _matchMediaHandlers.Clear();
                MediaQueryListenersManager.RemoveListeners(ids);
                if (OperatingSystem.IsBrowser())
                {
                    await js.InvokeVoid("BitButil.window.unsubscribeMatchMedia", ids);
                }
            }

            if (_listenerIds.IsEmpty is false)
            {
                var snapshot = _listenerIds.Keys.ToArray();
                _listenerIds.Clear();
                if (OperatingSystem.IsBrowser())
                {
                    foreach (var (id, evt, useCapture) in snapshot)
                    {
                        await DomEventDispatcher.RemoveEventListenerById(js, ElementName, evt, id, useCapture);
                    }
                }
            }

            await js.InvokeVoid("BitButil.window.dispose");
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }
}
