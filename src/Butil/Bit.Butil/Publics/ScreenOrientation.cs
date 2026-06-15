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
    internal const string InvokeMethodName = nameof(InvokeScreenOrientationChange);

    private readonly ConcurrentDictionary<Guid, Action<OrientationState>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<ScreenOrientation>? _dotNetRef;
    private DotNetObjectReference<ScreenOrientation> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>
    /// Invoked from JS on the orientation <c>change</c> event. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeScreenOrientationChange(Guid id, OrientationState state)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(state);
    }

    /// <summary>
    /// Returns the document's current orientation type.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/type">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/type</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<ScreenOrientationType> GetOrientationType()
    {
        var type = await js.Invoke<string>("BitButil.screenOrientation.type");

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
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<ushort> GetAngle()
        => await js.Invoke<ushort>("BitButil.screenOrientation.angle");

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

        await js.InvokeVoid("BitButil.screenOrientation.lock", type);
    }

    /// <summary>
    /// Unlocks the orientation of the containing document from its default orientation.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/unlock">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/unlock</see>
    /// </summary>
    public async Task Unlock()
        => await js.InvokeVoid("BitButil.screenOrientation.unlock");

    /// <summary>
    /// The change event of the ScreenOrientation interface fires when the orientation of the 
    /// screen has changed, for example when a user rotates their mobile phone.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/change_event">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/change_event</see>
    /// </summary>
    [DynamicDependency(nameof(InvokeScreenOrientationChange), typeof(ScreenOrientation))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OrientationState))]
    public async ValueTask<Guid> AddChange(Action<OrientationState> handler)
    {
        var listenerId = Guid.NewGuid();
        _handlers.TryAdd(listenerId, handler);

        await js.InvokeVoid("BitButil.screenOrientation.addChange", DotNetRef, listenerId);

        return listenerId;
    }

    /// <summary>
    /// Subscribe variant returning an <see cref="IAsyncDisposable"/> handle.
    /// </summary>
    public async ValueTask<ButilSubscription> SubscribeChange(Action<OrientationState> handler)
    {
        var id = await AddChange(handler);
        return new ButilSubscription(id, () => RemoveChange(id));
    }

    /// <summary>
    /// The change event of the ScreenOrientation interface fires when the orientation of the 
    /// screen has changed, for example when a user rotates their mobile phone.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/change_event">https://developer.mozilla.org/en-US/docs/Web/API/ScreenOrientation/change_event</see>
    /// </summary>
    /// <remarks>
    /// Listeners are matched by delegate identity, so you must pass the very same
    /// <paramref name="handler"/> instance that was registered. A newly-created lambda will not
    /// match and the returned array will be empty. To avoid this, keep the <see cref="Guid"/>
    /// returned by <c>AddChange</c> and remove by id, or use <c>SubscribeChange</c> which returns a
    /// disposable <see cref="ButilSubscription"/>.
    /// </remarks>
    public async ValueTask<Guid[]> RemoveChange(Action<OrientationState> handler)
    {
        var ids = _handlers.Where(h => h.Value == handler).Select(h => h.Key).ToArray();

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
        await js.InvokeVoid("BitButil.screenOrientation.removeChange", ids);
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
