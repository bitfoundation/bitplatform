using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The ScreenOrientation interface of the Screen Orientation API provides information about the current orientation of the document.
/// <br/>
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation</see>
/// </summary>
public class ScreenOrientation(IJSRuntime js) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, Action<OrientationState>> _handlers = new();

    /// <summary>
    /// Returns the document's current orientation type.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/type">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/type</see>
    /// </summary>
    public async Task<ScreenOrientationType> GetOrientationType()
    {
        var type = await js.FastInvokeAsync<string>("BitButil.screenOrientation.type");

        return type switch
        {
            "portrait-primary" => ScreenOrientationType.PortraitPrimary,
            "portrait-secondary" => ScreenOrientationType.PortraitSecondary,
            "landscape-primary" => ScreenOrientationType.LandscapePrimary,
            "landscape-secondary" => ScreenOrientationType.LandscapeSecondary,
            _ => ScreenOrientationType.LandscapePrimary
        };
    }

    /// <summary>
    /// Returns the document's current orientation angle.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/angle">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/angle</see>
    /// </summary>
    public async Task<ushort> GetAngle()
        => await js.FastInvokeAsync<ushort>("BitButil.screenOrientation.angle");

    /// <summary>
    /// Locks the orientation of the containing document to the specified orientation.
    /// Typically orientation locking is only enabled on mobile devices, and when the browser context is full screen.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/lock">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/lock</see>
    /// </summary>
    public async Task Lock(OrientationLockType lockType)
    {
        var type = lockType switch
        {
            OrientationLockType.Any => "any",
            OrientationLockType.Natural => "natural",
            OrientationLockType.Landscape => "landscape",
            OrientationLockType.Portrait => "portrait",
            OrientationLockType.PortraitPrimary => "portrait-primary",
            OrientationLockType.PortraitSecondary => "portrait-secondary",
            OrientationLockType.LandscapePrimary => "landscape-primary",
            OrientationLockType.LandscapeSecondary => "landscape-secondary",
            _ => "any"
        };

        await js.FastInvokeVoidAsync("BitButil.screenOrientation.lock", type);
    }

    /// <summary>
    /// Unlocks the orientation of the containing document from its default orientation.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/unlock">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/unlock</see>
    /// </summary>
    public async Task Unlock()
        => await js.FastInvokeVoidAsync("BitButil.screenOrientation.unlock");

    /// <summary>
    /// The change event of the ScreenOrientation interface fires when the orientation of the 
    /// screen has changed, for example when a user rotates their mobile phone.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/change_event">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/change_event</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ScreenOrientationListenersManager))]
    public async ValueTask<Guid> AddChange(Action<OrientationState> handler)
    {
        var listenerId = ScreenOrientationListenersManager.AddListener(handler);
        _handlers.TryAdd(listenerId, handler);

        await js.FastInvokeVoidAsync("BitButil.screenOrientation.addChange", ScreenOrientationListenersManager.InvokeMethodName, listenerId);

        return listenerId;
    }

    /// <summary>
    /// The change event of the ScreenOrientation interface fires when the orientation of the 
    /// screen has changed, for example when a user rotates their mobile phone.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/change_event">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/change_event</see>
    /// </summary>
    public async ValueTask<Guid[]> RemoveChange(Action<OrientationState> handler)
    {
        var ids = ScreenOrientationListenersManager.RemoveListener(handler);

        await RemoveChange(ids);

        return ids;
    }

    /// <summary>
    /// The change event of the ScreenOrientation interface fires when the orientation of the 
    /// screen has changed, for example when a user rotates their mobile phone.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/change_event">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/change_event</see>
    /// </summary>
    public async ValueTask RemoveChange(Guid id)
    {
        ScreenOrientationListenersManager.RemoveListeners([id]);

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

        ScreenOrientationListenersManager.RemoveListeners(ids);

        await RemoveFromJs(ids);
    }

    private async ValueTask RemoveFromJs(Guid[] ids)
    {
        if (OperatingSystem.IsBrowser() is false) return;

        await js.FastInvokeVoidAsync("BitButil.screenOrientation.removeChange", ids);
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
