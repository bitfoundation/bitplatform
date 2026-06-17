using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The Screen interface represents a screen, usually the one on which the current window is being rendered, 
/// and is obtained using window.screen.
/// <br />
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen">https://developer.mozilla.org/en-US/docs/Web/API/Screen</see>
/// </summary>
public class Screen(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeScreenChange);

    private readonly ConcurrentDictionary<Guid, Action> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<Screen>? _dotNetRef;
    private DotNetObjectReference<Screen> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>
    /// Invoked from JS on the screen <c>change</c> event. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeScreenChange(Guid id)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke();
    }

    /// <summary>
    /// Specifies the height of the screen, in pixels, minus permanent or semipermanent user interface 
    /// features displayed by the operating system, such as the Taskbar on Windows.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/availHeight">https://developer.mozilla.org/en-US/docs/Web/API/Screen/availHeight</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<float> GetAvailableHeight()
        => await js.Invoke<float>("BitButil.screen.availHeight");

    /// <summary>
    /// Returns the amount of horizontal space in pixels available to the window.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/availWidth">https://developer.mozilla.org/en-US/docs/Web/API/Screen/availWidth</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<float> GetAvailableWidth()
        => await js.Invoke<float>("BitButil.screen.availWidth");

    /// <summary>
    /// Returns the color depth of the screen.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/colorDepth">https://developer.mozilla.org/en-US/docs/Web/API/Screen/colorDepth</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<byte> GetColorDepth()
        => await js.Invoke<byte>("BitButil.screen.colorDepth");

    /// <summary>
    /// Returns the height of the screen in pixels.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/height">https://developer.mozilla.org/en-US/docs/Web/API/Screen/height</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<float> GetHeight()
        => await js.Invoke<float>("BitButil.screen.height");

    /// <summary>
    /// Returns true if the user's device has multiple screens, and false if not.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/isExtended">https://developer.mozilla.org/en-US/docs/Web/API/Screen/isExtended</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<bool> IsExtended()
        => await js.Invoke<bool>("BitButil.screen.isExtended");

    /// <summary>
    /// Gets the bit depth of the screen.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/pixelDepth">https://developer.mozilla.org/en-US/docs/Web/API/Screen/pixelDepth</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<byte> GetPixelDepth()
        => await js.Invoke<byte>("BitButil.screen.pixelDepth");

    /// <summary>
    /// Returns the width of the screen.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/width">https://developer.mozilla.org/en-US/docs/Web/API/Screen/width</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<float> GetWidth()
        => await js.Invoke<float>("BitButil.screen.width");

    /// <summary>
    /// Fired on a specific screen when it changes in some way - width or height, 
    /// available width or height, color depth, or orientation.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/change_event">https://developer.mozilla.org/en-US/docs/Web/API/Screen/change_event</see>
    /// </summary>
    [DynamicDependency(nameof(InvokeScreenChange), typeof(Screen))]
    public async ValueTask<Guid> AddChange(Action handler)
    {
        var listenerId = Guid.NewGuid();
        _handlers.TryAdd(listenerId, handler);

        await js.InvokeVoid("BitButil.screen.addChange", DotNetRef, listenerId);

        return listenerId;
    }

    /// <summary>
    /// Subscribe variant returning an <see cref="IAsyncDisposable"/> handle.
    /// </summary>
    public async ValueTask<ButilSubscription> SubscribeChange(Action handler)
    {
        var id = await AddChange(handler);
        return new ButilSubscription(id, () => RemoveChange(id));
    }

    /// <summary>
    /// Fired on a specific screen when it changes in some way - width or height, 
    /// available width or height, color depth, or orientation.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/change_event">https://developer.mozilla.org/en-US/docs/Web/API/Screen/change_event</see>
    /// </summary>
    /// <remarks>
    /// Listeners are matched by delegate identity, so you must pass the very same
    /// <paramref name="handler"/> instance that was registered. A newly-created lambda will not
    /// match and the returned array will be empty. To avoid this, keep the <see cref="Guid"/>
    /// returned by <c>AddChange</c> and remove by id, or use <c>SubscribeChange</c> which returns a
    /// disposable <see cref="ButilSubscription"/>.
    /// </remarks>
    public async ValueTask<Guid[]> RemoveChange(Action handler)
    {
        var ids = _handlers.Where(h => h.Value == handler).Select(h => h.Key).ToArray();

        await RemoveChange(ids);

        return ids;
    }

    /// <summary>
    /// Fired on a specific screen when it changes in some way - width or height, 
    /// available width or height, color depth, or orientation.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/change_event">https://developer.mozilla.org/en-US/docs/Web/API/Screen/change_event</see>
    /// </summary>
    public async ValueTask RemoveChange(Guid id)
    {
        await RemoveChange([id]);
    }

    private async ValueTask RemoveChange(Guid[] ids)
    {
        if (ids.Length == 0) return;

        foreach (var id in ids)
        {
            _handlers.TryRemove(id, out _);
        }

        await RemoveFromJs(ids);
    }

    public async ValueTask RemoveAllChanges()
    {
        if (_handlers.Count == 0) return;

        var ids = _handlers.Select(h => h.Key).ToArray();

        _handlers.Clear();

        await RemoveFromJs(ids);
    }

    private async ValueTask RemoveFromJs(Guid[] ids)
    {
        await js.InvokeVoid("BitButil.screen.removeChange", ids);
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
            await RemoveAllChanges();
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }
}
