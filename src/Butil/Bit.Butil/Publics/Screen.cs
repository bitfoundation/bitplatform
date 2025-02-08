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
    private readonly ConcurrentDictionary<Guid, Action> _handlers = new();

    /// <summary>
    /// Specifies the height of the screen, in pixels, minus permanent or semipermanent user interface 
    /// features displayed by the operating system, such as the Taskbar on Windows.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/availHeight">https://developer.mozilla.org/en-US/docs/Web/API/Screen/availHeight</see>
    /// </summary>
    public async Task<float> GetAvailableHeight()
        => await js.FastInvokeAsync<float>("BitButil.screen.availHeight");

    /// <summary>
    /// Returns the amount of horizontal space in pixels available to the window.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/availWidth">https://developer.mozilla.org/en-US/docs/Web/API/Screen/availWidth</see>
    /// </summary>
    public async Task<float> GetAvailableWidth()
        => await js.FastInvokeAsync<float>("BitButil.screen.availWidth");

    /// <summary>
    /// Returns the color depth of the screen.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/colorDepth">https://developer.mozilla.org/en-US/docs/Web/API/Screen/colorDepth</see>
    /// </summary>
    public async Task<byte> GetColorDepth()
        => await js.FastInvokeAsync<byte>("BitButil.screen.colorDepth");

    /// <summary>
    /// Returns the height of the screen in pixels.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/height">https://developer.mozilla.org/en-US/docs/Web/API/Screen/height</see>
    /// </summary>
    public async Task<float> GetHeight()
        => await js.FastInvokeAsync<float>("BitButil.screen.height");

    /// <summary>
    /// Returns true if the user's device has multiple screens, and false if not.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/isExtended">https://developer.mozilla.org/en-US/docs/Web/API/Screen/isExtended</see>
    /// </summary>
    public async Task<bool> IsExtended()
        => await js.FastInvokeAsync<bool>("BitButil.screen.isExtended");

    /// <summary>
    /// Gets the bit depth of the screen.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/pixelDepth">https://developer.mozilla.org/en-US/docs/Web/API/Screen/pixelDepth</see>
    /// </summary>
    public async Task<byte> GetPixelDepth()
        => await js.FastInvokeAsync<byte>("BitButil.screen.pixelDepth");

    /// <summary>
    /// Returns the width of the screen.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/width">https://developer.mozilla.org/en-US/docs/Web/API/Screen/width</see>
    /// </summary>
    public async Task<float> GetWidth()
        => await js.FastInvokeAsync<float>("BitButil.screen.width");

    /// <summary>
    /// Fired on a specific screen when it changes in some way — width or height, 
    /// available width or height, color depth, or orientation.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/change_event">https://developer.mozilla.org/en-US/docs/Web/API/Screen/change_event</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ScreenListenersManager))]
    public async ValueTask<Guid> AddChange(Action handler)
    {
        var listenerId = ScreenListenersManager.AddListener(handler);
        _handlers.TryAdd(listenerId, handler);

        await js.FastInvokeVoidAsync("BitButil.screen.addChange", ScreenListenersManager.InvokeMethodName, listenerId);

        return listenerId;
    }

    /// <summary>
    /// Fired on a specific screen when it changes in some way — width or height, 
    /// available width or height, color depth, or orientation.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/change_event">https://developer.mozilla.org/en-US/docs/Web/API/Screen/change_event</see>
    /// </summary>
    public async ValueTask<Guid[]> RemoveChange(Action handler)
    {
        var ids = ScreenListenersManager.RemoveListener(handler);

        await RemoveChange(ids);

        return ids;
    }

    /// <summary>
    /// Fired on a specific screen when it changes in some way — width or height, 
    /// available width or height, color depth, or orientation.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Screen/change_event">https://developer.mozilla.org/en-US/docs/Web/API/Screen/change_event</see>
    /// </summary>
    public async ValueTask RemoveChange(Guid id)
    {
        ScreenListenersManager.RemoveListeners([id]);

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

        ScreenListenersManager.RemoveListeners(ids);

        await RemoveFromJs(ids);
    }

    private async ValueTask RemoveFromJs(Guid[] ids)
    {
        if (OperatingSystem.IsBrowser() is false) return;

        await js.FastInvokeVoidAsync("BitButil.screen.removeChange", ids);
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
            await RemoveAllChanges();
        }
    }
}
