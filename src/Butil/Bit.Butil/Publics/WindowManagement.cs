using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window_Management_API">Window Management API</see>
/// (<c>getScreenDetails</c>): enumerate every attached screen and place windows or fullscreen
/// content on a chosen one.
/// </summary>
/// <remarks>
/// This is the multi-screen counterpart to the <see cref="Screen"/> service, which only ever
/// describes the screen the window happens to be on and has no coordinate space to compare screens in.
/// <br/>
/// Chromium only, and only over HTTPS. The first <see cref="GetScreenDetails"/> prompts for the
/// <c>window-management</c> permission and so must run inside a user gesture; the resolved details
/// are cached for the page, so later calls are cheap and do not re-prompt.
/// </remarks>
[ButilService(typeof(WindowManagement))]
public class WindowManagement(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeScreensChange);

    private readonly ConcurrentDictionary<Guid, Action<ScreenDetails?>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<WindowManagement>? _dotNetRef;
    private DotNetObjectReference<WindowManagement> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>window.getScreenDetails</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.windowManagement.isSupported");

    /// <summary>
    /// True when more than one screen is attached. Readable without any permission, which makes it
    /// the right gate for showing a "move to second screen" affordance at all.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsExtended() => js.Invoke<bool>("BitButil.windowManagement.isExtended");

    /// <summary>
    /// The <c>window-management</c> permission state: <c>"granted"</c>, <c>"denied"</c> or
    /// <c>"prompt"</c>. A query only - the prompt happens on the first <see cref="GetScreenDetails"/>.
    /// </summary>
    public ValueTask<string> QueryPermission() => js.Invoke<string>("BitButil.windowManagement.queryPermission");

    /// <summary>
    /// Every attached screen, and which one this window is on. Null when the API is missing or the
    /// user dismissed the permission prompt. Call it from a user gesture the first time.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ScreenDetails))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ScreenDetailInfo))]
    public ValueTask<ScreenDetails?> GetScreenDetails()
        => js.Invoke<ScreenDetails?>("BitButil.windowManagement.getScreenDetails");

    /// <summary>
    /// Opens a window positioned on the screen at <paramref name="screenIndex"/> - the index into
    /// <see cref="ScreenDetails.Screens"/>.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    /// <param name="screenIndex">Which screen to place the window on.</param>
    /// <param name="fullSize">
    /// True fills the target screen's available area; false opens a window of an ordinary size in
    /// its top-left corner.
    /// </param>
    /// <param name="features">
    /// Extra <c>window.open</c> features, appended to the computed position and size.
    /// </param>
    /// <remarks>
    /// Must be called from a user gesture - it is still a popup, and the popup blocker still
    /// applies. False means the window was blocked or the screen index does not exist.
    /// </remarks>
    public ValueTask<bool> OpenOnScreen(string url, int screenIndex, bool fullSize = false, string? features = null)
        => js.Invoke<bool>("BitButil.windowManagement.openOnScreen", url, screenIndex, features, fullSize);

    /// <summary>
    /// Takes an element fullscreen on the screen at <paramref name="screenIndex"/> rather than on
    /// the one the window is currently on - the presenter's second-screen slide view.
    /// </summary>
    /// <param name="element">The element to show. Pass <c>default</c> for the whole document.</param>
    /// <param name="screenIndex">Which screen to go fullscreen on.</param>
    /// <remarks>Must be called from a user gesture, like any fullscreen request.</remarks>
    public ValueTask<bool> RequestFullscreenOnScreen(ElementReference element, int screenIndex)
        => js.Invoke<bool>("BitButil.windowManagement.requestFullscreenOnScreen", element, screenIndex);

    /// <summary>
    /// Invoked from JS when the set of screens changes or the window moves to another one. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeScreensChange(Guid id, ScreenDetails? details)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(details);
    }

    /// <summary>
    /// Runs <paramref name="handler"/> when a screen is attached or removed, or when this window is
    /// dragged onto a different screen. The handler gets the whole snapshot, because either event
    /// can invalidate every index the caller is holding.
    /// </summary>
    /// <returns>A subscription - dispose it to detach the listener.</returns>
    /// <remarks>Requires the window-management permission, so it only attaches once
    /// <see cref="GetScreenDetails"/> has succeeded.</remarks>
    [DynamicDependency(nameof(InvokeScreensChange), typeof(WindowManagement))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ScreenDetails))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ScreenDetailInfo))]
    public async ValueTask<ButilSubscription> SubscribeChange(Action<ScreenDetails?> handler)
    {
        var id = Guid.NewGuid();
        _handlers[id] = handler;
        await js.InvokeVoid("BitButil.windowManagement.addChange", DotNetRef, id);

        return new ButilSubscription(id, () => RemoveChange(id));
    }

    /// <summary>Detaches one screens-change listener by the id its subscription carries.</summary>
    public async ValueTask RemoveChange(Guid id)
    {
        if (_handlers.TryRemove(id, out _) is false) return;

        await js.InvokeVoid("BitButil.windowManagement.removeChange", new[] { id });
    }

    /// <summary>Detaches every screens-change listener registered through this instance.</summary>
    public async ValueTask RemoveAllChanges()
    {
        if (_handlers.IsEmpty) return;

        var ids = _handlers.Keys.ToArray();
        _handlers.Clear();

        await js.InvokeVoid("BitButil.windowManagement.removeChange", ids);
    }

    /// <summary>Detaches every listener this instance registered and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
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

        GC.SuppressFinalize(this);
    }
}
