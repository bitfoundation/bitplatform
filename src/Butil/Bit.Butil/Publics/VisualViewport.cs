using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The VisualViewport interface represents the visual viewport for a given window. 
/// For a page containing iframes, each iframe, as well as the containing page, will have a unique window object. 
/// Each window on a page will have a unique VisualViewport representing the properties associated with that window.
/// <br/>
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport</see>
/// </summary>
/// <param name="js"></param>
public class VisualViewport(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeVisualViewport);

    private readonly ConcurrentDictionary<Guid, Action> _handlers = new();

    // Per-instance callback reference (see Keyboard): resize/scroll listeners are isolated per
    // circuit / WASM app and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<VisualViewport>? _dotNetRef;
    private DotNetObjectReference<VisualViewport> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>
    /// Invoked from JS on a resize/scroll event. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeVisualViewport(Guid id)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke();
    }

    /// <summary>
    /// Returns the offset of the left edge of the visual viewport from the left edge of 
    /// the layout viewport in CSS pixels, or 0 if current document is not fully active.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/offsetLeft">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/offsetLeft</see>
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<double> GetOffsetLeft()
        => await js.Invoke<double>("BitButil.visualViewport.offsetLeft");

    /// <summary>
    /// Returns the offset of the top edge of the visual viewport from the top edge of 
    /// the layout viewport in CSS pixels, or 0 if current document is not fully active.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/offsetTop">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/offsetTop</see>
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<double> GetOffsetTop()
        => await js.Invoke<double>("BitButil.visualViewport.offsetTop");

    /// <summary>
    /// Returns the x coordinate of the left edge of the visual viewport relative to the 
    /// initial containing block origin, in CSS pixels, or 0 if current document is not fully active.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/pageLeft">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/pageLeft</see>
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<double> GetPageLeft()
        => await js.Invoke<double>("BitButil.visualViewport.pageLeft");

    /// <summary>
    /// Returns the y coordinate of the top edge of the visual viewport relative to the 
    /// initial containing block origin, in CSS pixels, or 0 if current document is not fully active.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/pageTop">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/pageTop</see>
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<double> GetPageTop()
        => await js.Invoke<double>("BitButil.visualViewport.pageTop");

    /// <summary>
    /// Returns the width of the visual viewport, in CSS pixels, or 0 if current document is not fully active.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/pageTop">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/pageTop</see>
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<double> GetWidth()
        => await js.Invoke<double>("BitButil.visualViewport.width");

    /// <summary>
    /// Returns the height of the visual viewport, in CSS pixels, or 0 if current document is not fully active.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/height">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/height</see>
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<double> GetHeight()
        => await js.Invoke<double>("BitButil.visualViewport.height");

    /// <summary>
    /// Returns the pinch-zoom scaling factor applied to the visual viewport, or 0 if current 
    /// document is not fully active, or 1 if there is no output device.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/scale">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/scale</see>
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<double> GetScale()
        => await js.Invoke<double>("BitButil.visualViewport.scale");

    /// <summary>
    /// Fired when the visual viewport is resized.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/resize_event">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/resize_event</see>
    /// </summary>
    [DynamicDependency(nameof(InvokeVisualViewport), typeof(VisualViewport))]
    public async ValueTask<Guid> AddResize(Action handler)
    {
        var listenerId = Guid.NewGuid();
        _handlers.TryAdd(listenerId, handler);

        await js.InvokeVoid("BitButil.visualViewport.addResize", DotNetRef, listenerId);

        return listenerId;
    }

    /// <summary>
    /// Subscribe variant of <see cref="AddResize"/> returning an <see cref="IAsyncDisposable"/> handle.
    /// </summary>
    public async ValueTask<ButilSubscription> SubscribeResize(Action handler)
    {
        var id = await AddResize(handler);
        return new ButilSubscription(id, () => RemoveResize(id));
    }

    /// <summary>
    /// Fired when the visual viewport is resized.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/resize_event">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/resize_event</see>
    /// </summary>
    /// <remarks>
    /// Listeners are matched by delegate identity, so you must pass the very same
    /// <paramref name="handler"/> instance that was registered. A newly-created lambda will not
    /// match and the returned array will be empty. To avoid this, keep the <see cref="Guid"/>
    /// returned by <see cref="AddResize"/> and remove by id, or use <see cref="SubscribeResize"/>
    /// which returns a disposable <see cref="ButilSubscription"/>.
    /// </remarks>
    public async ValueTask<Guid[]> RemoveResize(Action handler)
    {
        var ids = _handlers.Where(h => h.Value == handler).Select(h => h.Key).ToArray();

        await RemoveResize(ids);

        return ids;
    }

    /// <summary>
    /// Fired when the visual viewport is resized.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/resize_event">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/resize_event</see>
    /// </summary>
    public async ValueTask RemoveResize(Guid id)
    {
        await RemoveResize([id]);
    }

    private async ValueTask RemoveResize(Guid[] ids)
    {
        if (ids.Length == 0) return;

        foreach (var id in ids)
        {
            _handlers.TryRemove(id, out _);
        }

        await RemoveResizeFromJs(ids);
    }

    private async ValueTask RemoveResizeFromJs(Guid[] ids)
    {
        await js.InvokeVoid("BitButil.visualViewport.removeResize", ids);
    }

    /// <summary>
    /// Fired when the visual viewport is scrolled.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/scroll_event">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/scroll_event</see>
    /// </summary>
    [DynamicDependency(nameof(InvokeVisualViewport), typeof(VisualViewport))]
    public async ValueTask<Guid> AddScroll(Action handler)
    {
        var listenerId = Guid.NewGuid();
        _handlers.TryAdd(listenerId, handler);

        await js.InvokeVoid("BitButil.visualViewport.addScroll", DotNetRef, listenerId);

        return listenerId;
    }

    /// <summary>
    /// Subscribe variant of <see cref="AddScroll"/> returning an <see cref="IAsyncDisposable"/> handle.
    /// </summary>
    public async ValueTask<ButilSubscription> SubscribeScroll(Action handler)
    {
        var id = await AddScroll(handler);
        return new ButilSubscription(id, () => RemoveScroll(id));
    }

    /// <summary>
    /// Fired when the visual viewport is scrolled.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/scroll_event">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/scroll_event</see>
    /// </summary>
    /// <remarks>
    /// Listeners are matched by delegate identity, so you must pass the very same
    /// <paramref name="handler"/> instance that was registered. A newly-created lambda will not
    /// match and the returned array will be empty. To avoid this, keep the <see cref="Guid"/>
    /// returned by <see cref="AddScroll"/> and remove by id, or use <see cref="SubscribeScroll"/>
    /// which returns a disposable <see cref="ButilSubscription"/>.
    /// </remarks>
    public async ValueTask<Guid[]> RemoveScroll(Action handler)
    {
        var ids = _handlers.Where(h => h.Value == handler).Select(h => h.Key).ToArray();

        await RemoveScroll(ids);

        return ids;
    }

    /// <summary>
    /// Fired when the visual viewport is scrolled.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/scroll_event">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/scroll_event</see>
    /// </summary>
    public async ValueTask RemoveScroll(Guid id)
    {
        await RemoveScroll([id]);
    }

    private async ValueTask RemoveScroll(Guid[] ids)
    {
        foreach (var id in ids)
        {
            _handlers.TryRemove(id, out _);
        }

        await RemoveScrollFromJs(ids);
    }

    private async ValueTask RemoveScrollFromJs(Guid[] ids)
    {
        await js.InvokeVoid("BitButil.visualViewport.removeScroll", ids);
    }


    public async ValueTask RemoveAllEventHandlers()
    {
        if (_handlers.Count == 0) return;

        var ids = _handlers.Select(h => h.Key).ToArray();

        _handlers.Clear();

        var toAwait = new List<Task>();

        var resizeValueTask = RemoveResizeFromJs(ids);
        var scrollValueTask = RemoveScrollFromJs(ids);

        if (resizeValueTask.IsCompleted is false)
        {
            toAwait.Add(resizeValueTask.AsTask());
        }

        if (scrollValueTask.IsCompleted is false)
        {
            toAwait.Add(scrollValueTask.AsTask());
        }

        await Task.WhenAll(toAwait);
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
            await RemoveAllEventHandlers();
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }
}
