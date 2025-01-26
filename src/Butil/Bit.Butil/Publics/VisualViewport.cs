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
    private readonly ConcurrentDictionary<Guid, Action> _handlers = new();

    /// <summary>
    /// Returns the offset of the left edge of the visual viewport from the left edge of 
    /// the layout viewport in CSS pixels, or 0 if current document is not fully active.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/offsetLeft">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/offsetLeft</see>
    /// </summary>
    /// <returns></returns>
    public async Task<double> GetOffsetLeft()
        => await js.FastInvokeAsync<double>("BitButil.visualViewport.offsetLeft");

    /// <summary>
    /// Returns the offset of the top edge of the visual viewport from the top edge of 
    /// the layout viewport in CSS pixels, or 0 if current document is not fully active.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/offsetTop">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/offsetTop</see>
    /// </summary>
    /// <returns></returns>
    public async Task<double> GetOffsetTop()
        => await js.FastInvokeAsync<double>("BitButil.visualViewport.offsetTop");

    /// <summary>
    /// Returns the x coordinate of the left edge of the visual viewport relative to the 
    /// initial containing block origin, in CSS pixels, or 0 if current document is not fully active.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/pageLeft">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/pageLeft</see>
    /// </summary>
    /// <returns></returns>
    public async Task<double> GetPageLeft()
        => await js.FastInvokeAsync<double>("BitButil.visualViewport.pageLeft");

    /// <summary>
    /// Returns the y coordinate of the top edge of the visual viewport relative to the 
    /// initial containing block origin, in CSS pixels, or 0 if current document is not fully active.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/pageTop">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/pageTop</see>
    /// </summary>
    /// <returns></returns>
    public async Task<double> GetPageTop()
        => await js.FastInvokeAsync<double>("BitButil.visualViewport.pageTop");

    /// <summary>
    /// Returns the width of the visual viewport, in CSS pixels, or 0 if current document is not fully active.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/pageTop">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/pageTop</see>
    /// </summary>
    /// <returns></returns>
    public async Task<double> GetWidth()
        => await js.FastInvokeAsync<double>("BitButil.visualViewport.width");

    /// <summary>
    /// Returns the height of the visual viewport, in CSS pixels, or 0 if current document is not fully active.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/height">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/height</see>
    /// </summary>
    /// <returns></returns>
    public async Task<double> GetHeight()
        => await js.FastInvokeAsync<double>("BitButil.visualViewport.height");

    /// <summary>
    /// Returns the pinch-zoom scaling factor applied to the visual viewport, or 0 if current 
    /// document is not fully active, or 1 if there is no output device.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/scale">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/scale</see>
    /// </summary>
    /// <returns></returns>
    public async Task<double> GetScale()
        => await js.FastInvokeAsync<double>("BitButil.visualViewport.scale");

    /// <summary>
    /// Fired when the visual viewport is resized.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/resize_event">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/resize_event</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VisualViewportListenersManager))]
    public async ValueTask<Guid> AddResize(Action handler)
    {
        var listenerId = VisualViewportListenersManager.AddListener(handler);
        _handlers.TryAdd(listenerId, handler);

        await js.FastInvokeVoidAsync("BitButil.visualViewport.addResize", VisualViewportListenersManager.InvokeMethodName, listenerId);

        return listenerId;
    }

    /// <summary>
    /// Fired when the visual viewport is resized.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/resize_event">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/resize_event</see>
    /// </summary>
    public async ValueTask<Guid[]> RemoveResize(Action handler)
    {
        var ids = VisualViewportListenersManager.RemoveListener(handler);

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
        VisualViewportListenersManager.RemoveListeners([id]);

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
        if (OperatingSystem.IsBrowser() is false) return;

        await js.FastInvokeVoidAsync("BitButil.visualViewport.removeResize", ids);
    }

    /// <summary>
    /// Fired when the visual viewport is scrolled.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/scroll_event">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/scroll_event</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VisualViewportListenersManager))]
    public async ValueTask<Guid> AddScroll(Action handler)
    {
        var listenerId = VisualViewportListenersManager.AddListener(handler);
        _handlers.TryAdd(listenerId, handler);

        await js.FastInvokeVoidAsync("BitButil.visualViewport.addScroll", VisualViewportListenersManager.InvokeMethodName, listenerId);

        return listenerId;
    }

    /// <summary>
    /// Fired when the visual viewport is scrolled.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/scroll_event">https://developer.mozilla.org/en-US/docs/Web/API/VisualViewport/scroll_event</see>
    /// </summary>
    public async ValueTask<Guid[]> RemoveScroll(Action handler)
    {
        var ids = VisualViewportListenersManager.RemoveListener(handler);

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
        VisualViewportListenersManager.RemoveListeners([id]);

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
        if (OperatingSystem.IsBrowser() is false) return;

        await js.FastInvokeVoidAsync("BitButil.visualViewport.removeScroll", ids);
    }


    public async ValueTask RemoveAllEventHandlers()
    {
        if (_handlers.Count == 0) return;

        var ids = _handlers.Select(h => h.Key).ToArray();

        _handlers.Clear();

        VisualViewportListenersManager.RemoveListeners(ids);

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
        if (disposing)
        {
            await RemoveAllEventHandlers();
        }
    }
}
