using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

public class Document(IJSRuntime js) : IAsyncDisposable
{
    private const string ElementName = "document";

    // Track listener ids registered through this *instance* so dispose actually drains them.
    private readonly ConcurrentDictionary<(Guid Id, string Event, bool UseCapture), byte> _listenerIds = new();

    // Per-instance DOM event dispatcher: listeners are isolated per circuit / WASM app and released
    // on disposal - no static state, no cross-circuit leak.
    private readonly DomEventsInterop _events = new();

    public async Task AddEventListener<T>(
        string domEvent,
        Action<T> listener,
        bool useCapture = false,
        bool preventDefault = false,
        bool stopPropagation = false)
    {
        var id = await _events.AddEventListener(js, ElementName, domEvent, listener, useCapture, preventDefault, stopPropagation);
        _listenerIds.TryAdd((id, domEvent, useCapture), 0);
    }

    /// <summary>
    /// Removes a listener previously added with <see cref="AddEventListener{T}"/>.
    /// </summary>
    /// <remarks>
    /// Listeners are matched by delegate identity, so you must pass the very same
    /// <paramref name="listener"/> instance that was registered. A newly-created lambda will not
    /// match and nothing will be removed. For lambdas, prefer <see cref="SubscribeEvent{T}(string, Action{T}, bool, bool, bool)"/>,
    /// which returns a disposable <see cref="ButilSubscription"/> you can dispose to detach.
    /// </remarks>
    public async Task RemoveEventListener<T>(string domEvent, Action<T> listener, bool useCapture = false)
    {
        var ids = await _events.RemoveEventListener(js, ElementName, domEvent, listener, useCapture);
        foreach (var id in ids) _listenerIds.TryRemove((id, domEvent, useCapture), out _);
    }

    /// <summary>
    /// Subscribe variant of <see cref="AddEventListener{T}"/> returning an <see cref="IAsyncDisposable"/> handle.
    /// Pair with <c>await using</c> to guarantee detachment.
    /// </summary>
    public async Task<ButilSubscription> SubscribeEvent<T>(
        string domEvent,
        Action<T> listener,
        bool useCapture = false,
        bool preventDefault = false,
        bool stopPropagation = false)
    {
        var id = await _events.AddEventListener(js, ElementName, domEvent, listener, useCapture, preventDefault, stopPropagation);
        var key = (id, domEvent, useCapture);
        _listenerIds.TryAdd(key, 0);

        return new ButilSubscription(id, async () =>
        {
            _listenerIds.TryRemove(key, out _);
            await _events.RemoveEventListenerById(js, ElementName, domEvent, id, useCapture);
        });
    }

    /// <summary>
    /// <see cref="ButilEventListenerOptions"/> variant of <see cref="SubscribeEvent{T}(string, Action{T}, bool, bool, bool)"/>,
    /// adding <c>passive</c> and <c>once</c> control on top of <c>capture</c>.
    /// </summary>
    public async Task<ButilSubscription> SubscribeEvent<T>(
        string domEvent,
        Action<T> listener,
        ButilEventListenerOptions options,
        bool preventDefault = false,
        bool stopPropagation = false)
    {
        var useCapture = options.Capture;
        var id = await _events.AddEventListener(js, ElementName, domEvent, listener,
            useCapture, preventDefault, stopPropagation, options.Passive, options.Once);
        var key = (id, domEvent, useCapture);
        _listenerIds.TryAdd(key, 0);

        return new ButilSubscription(id, async () =>
        {
            _listenerIds.TryRemove(key, out _);
            await _events.RemoveEventListenerById(js, ElementName, domEvent, id, useCapture);
        });
    }

    /// <summary>
    /// Returns the character set being used by the document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/characterSet">https://developer.mozilla.org/en-US/docs/Web/API/Document/characterSet</see>
    /// </summary>
    public async Task<string> GetCharacterSet()
        => await js.Invoke<string>("BitButil.document.characterSet");

    /// <summary>
    /// Indicates whether the document is rendered in quirks or strict mode.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/compatMode">https://developer.mozilla.org/en-US/docs/Web/API/Document/compatMode</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<CompatMode> GetCompatMode()
    {
        var mode = await js.Invoke<string>("BitButil.document.compatMode");
        return mode switch
        {
            "BackCompat" => CompatMode.BackCompat,
            _ => CompatMode.CSS1Compat
        };
    }

    /// <summary>
    /// Returns the Content-Type from the MIME Header of the current document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/contentType">https://developer.mozilla.org/en-US/docs/Web/API/Document/contentType</see>
    /// </summary>
    public async Task<string> GetContentType()
        => await js.Invoke<string>("BitButil.document.contentType");

    /// <summary>
    /// Returns the document location as a string.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/documentURI">https://developer.mozilla.org/en-US/docs/Web/API/Document/documentURI</see>
    /// </summary>
    public async Task<string> GetDocumentURI()
        => await js.Invoke<string>("BitButil.document.documentURI");

    /// <summary>
    /// Gets ability to edit the whole document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/designMode">https://developer.mozilla.org/en-US/docs/Web/API/Document/designMode</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<DesignMode> GetDesignMode()
    {
        var mode = await js.Invoke<string>("BitButil.document.getDesignMode");
        return mode switch
        {
            "on" => DesignMode.On,
            _ => DesignMode.Off
        };
    }
    /// <summary>
    /// Sets ability to edit the whole document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/designMode">https://developer.mozilla.org/en-US/docs/Web/API/Document/designMode</see>
    /// </summary>
    public async Task SetDesignMode(DesignMode mode)
        => await js.InvokeVoid("BitButil.document.setDesignMode", mode.ToString());

    /// <summary>
    /// Gets directionality (rtl/ltr) of the document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/dir">https://developer.mozilla.org/en-US/docs/Web/API/Document/dir</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<DocumentDir> GetDir()
    {
        var mode = await js.Invoke<string>("BitButil.document.getDir");
        return mode switch
        {
            "rtl" => DocumentDir.Rtl,
            _ => DocumentDir.Ltr
        };
    }
    /// <summary>
    /// Sets directionality (rtl/ltr) of the document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/dir">https://developer.mozilla.org/en-US/docs/Web/API/Document/dir</see>
    /// </summary>
    public async Task SetDir(DocumentDir dir)
        => await js.InvokeVoid("BitButil.document.setDir", dir.ToString());

    /// <summary>
    /// Returns the URI of the page that linked to this page.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/referrer">https://developer.mozilla.org/en-US/docs/Web/API/Document/referrer</see>
    /// </summary>
    public async Task<string> GetReferrer()
        => await js.Invoke<string>("BitButil.document.referrer");

    /// <summary>
    /// Gets the title of the current document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/title">https://developer.mozilla.org/en-US/docs/Web/API/Document/title</see>
    /// </summary>
    public async Task<string> GetTitle()
        => await js.Invoke<string>("BitButil.document.getTitle");
    /// <summary>
    /// Sets the title of the current document.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/title">https://developer.mozilla.org/en-US/docs/Web/API/Document/title</see>
    /// </summary>
    public async Task SetTitle(string title)
        => await js.InvokeVoid("BitButil.document.setTitle", title);

    /// <summary>
    /// Returns the document location as a string.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/URL">https://developer.mozilla.org/en-US/docs/Web/API/Document/URL</see>
    /// </summary>
    public async Task<string> GetUrl()
        => await js.Invoke<string>("BitButil.document.URL");

    /// <summary>
    /// Stops document's fullscreen element from being displayed fullscreen.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/exitFullscreen">https://developer.mozilla.org/en-US/docs/Web/API/Document/exitFullscreen</see>
    /// </summary>
    public async Task ExitFullscreen()
        => await js.InvokeVoid("BitButil.document.exitFullscreen");

    /// <summary>
    /// Release the pointer lock.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/exitPointerLock">https://developer.mozilla.org/en-US/docs/Web/API/Document/exitPointerLock</see>
    /// </summary>
    public async Task ExitPointerLock()
        => await js.InvokeVoid("BitButil.document.exitPointerLock");

    /// <summary>
    /// Indicates whether the document is currently visible.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/visibilityState">https://developer.mozilla.org/en-US/docs/Web/API/Document/visibilityState</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<VisibilityState> GetVisibilityState()
    {
        var raw = await js.Invoke<string>("BitButil.document.visibilityState");
        return raw == "hidden" ? VisibilityState.Hidden : VisibilityState.Visible;
    }

    /// <summary>
    /// True when the document is currently hidden.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/hidden">https://developer.mozilla.org/en-US/docs/Web/API/Document/hidden</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<bool> IsHidden()
        => await js.Invoke<bool>("BitButil.document.hidden");

    /// <summary>
    /// True when the document or any element inside it has focus.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/hasFocus">https://developer.mozilla.org/en-US/docs/Web/API/Document/hasFocus</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<bool> HasFocus()
        => await js.Invoke<bool>("BitButil.document.hasFocus");

    /// <summary>
    /// True when the page is restored from a discarded state by the browser
    /// (e.g. tab was reclaimed under memory pressure and is now being reactivated).
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/wasDiscarded">Document.wasDiscarded</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> WasDiscarded() => js.Invoke<bool>("BitButil.document.wasDiscarded");

    // ─── Convenience subscription helpers built on SubscribeEvent ───────────────

    /// <summary>
    /// Fires when <see cref="GetVisibilityState"/> flips. The handler receives the new state.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/visibilitychange_event">visibilitychange</see>
    /// </summary>
    /// <remarks>
    /// The new state isn't carried on the event, so it's read back via a separate (cheap, synchronous)
    /// interop call after the event fires. Under very rapid toggles the reported state can lag or
    /// arrive slightly out of order relative to the raw events; treat the value as "latest known"
    /// rather than a strictly-ordered log.
    /// </remarks>
    public async Task<ButilSubscription> SubscribeVisibilityChange(Action<VisibilityState> handler)
    {
        Action<object> bridge = _ =>
        {
            // We don't get the state on the event itself - fetch it on the fly.
            // It's cheap (sync property) so the extra interop is acceptable.
            _ = ReportVisibilityAsync(handler);
        };
        return await SubscribeEvent(ButilEvents.VisibilityChange, bridge);
    }

    private async Task ReportVisibilityAsync(Action<VisibilityState> handler)
    {
        try { handler(await GetVisibilityState()); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }

    /// <summary>
    /// Fires when an element enters or leaves fullscreen. The handler receives true when
    /// the document currently has a fullscreen element.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Document/fullscreenchange_event">fullscreenchange</see>
    /// </summary>
    /// <remarks>
    /// The fullscreen state is read back via a separate interop call after the event fires (the event
    /// itself carries no payload), so under rapid toggles the reported value can lag or arrive
    /// slightly out of order. Treat it as "latest known" rather than a strictly-ordered log.
    /// </remarks>
    public async Task<ButilSubscription> SubscribeFullscreenChange(Action<bool> handler)
    {
        Action<object> bridge = _ => _ = ReportFullscreenAsync(handler);
        return await SubscribeEvent(ButilEvents.FullscreenChange, bridge);
    }

    private async Task ReportFullscreenAsync(Action<bool> handler)
    {
        try
        {
            var hasFs = await js.Invoke<bool>("BitButil.document.hasFullscreenElement");
            handler(hasFs);
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }

    /// <summary>
    /// Fires when entering fullscreen fails. The handler receives no payload - the spec
    /// doesn't expose a structured reason.
    /// </summary>
    public Task<ButilSubscription> SubscribeFullscreenError(Action handler)
    {
        Action<object> bridge = _ => handler();
        return SubscribeEvent(ButilEvents.FullscreenError, bridge);
    }

    /// <summary>
    /// Fires when pointer lock is entered or exited. The handler receives true when an
    /// element currently has pointer lock.
    /// </summary>
    /// <remarks>
    /// The lock state is read back via a separate interop call after the event fires, so under rapid
    /// toggles the reported value can lag or arrive slightly out of order. Treat it as "latest known"
    /// rather than a strictly-ordered log.
    /// </remarks>
    public async Task<ButilSubscription> SubscribePointerLockChange(Action<bool> handler)
    {
        Action<object> bridge = _ => _ = ReportPointerLockAsync(handler);
        return await SubscribeEvent(ButilEvents.PointerLockChange, bridge);
    }

    private async Task ReportPointerLockAsync(Action<bool> handler)
    {
        try
        {
            var hasLock = await js.Invoke<bool>("BitButil.document.hasPointerLockElement");
            handler(hasLock);
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }

    /// <summary>Fires when entering pointer lock fails.</summary>
    public Task<ButilSubscription> SubscribePointerLockError(Action handler)
    {
        Action<object> bridge = _ => handler();
        return SubscribeEvent(ButilEvents.PointerLockError, bridge);
    }

    /// <summary>
    /// Fires when the DOMContentLoaded event has just been raised. Useful when bootstrapping
    /// post-render work after circuit reconnect.
    /// </summary>
    public Task<ButilSubscription> SubscribeDomContentLoaded(Action handler)
    {
        Action<object> bridge = _ => handler();
        return SubscribeEvent(ButilEvents.DomContentLoaded, bridge);
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
            if (_listenerIds.IsEmpty is false)
            {
                var snapshot = _listenerIds.Keys.ToArray();
                _listenerIds.Clear();
                foreach (var (id, evt, useCapture) in snapshot)
                {
                    await _events.RemoveEventListenerById(js, ElementName, evt, id, useCapture);
                }
            }
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _events.Dispose();
        }
    }
}
