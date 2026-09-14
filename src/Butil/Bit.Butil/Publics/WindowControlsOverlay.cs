using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window_Controls_Overlay_API">Window Controls Overlay API</see>:
/// a desktop PWA that draws its own title bar needs to know where the OS window controls are, so its
/// toolbar doesn't end up underneath them.
/// </summary>
/// <remarks>
/// The overlay only exists for an <b>installed</b> desktop app whose manifest sets
/// <c>"display_override": ["window-controls-overlay"]</c>, and the user can switch it off from the
/// app's own menu. In a browser tab <see cref="IsVisible"/> is false and the geometry is all zeros.
/// <br/>
/// The CSS half of this API is what you usually want alongside it: the
/// <c>titlebar-area-x/y/width/height</c> environment variables carry the same rectangle, and
/// <c>app-region: drag</c> marks the part of your header that moves the window.
/// </remarks>
[ButilService(typeof(WindowControlsOverlay))]
public class WindowControlsOverlay(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeGeometryChange);

    private readonly ConcurrentDictionary<Guid, Action<WindowControlsOverlayGeometry>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<WindowControlsOverlay>? _dotNetRef;
    private DotNetObjectReference<WindowControlsOverlay> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.windowControlsOverlay</c> (Chromium desktop).</summary>
    /// <remarks>
    /// Supported is not the same as visible - a supporting browser still reports an invisible overlay
    /// for a page running in a tab.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.windowControlsOverlay.isSupported");

    /// <summary>True while the app is actually drawing its own title bar.</summary>
    public ValueTask<bool> IsVisible() => js.Invoke<bool>("BitButil.windowControlsOverlay.isVisible");

    /// <summary>
    /// The rectangle your content may draw in - see <see cref="WindowControlsOverlayGeometry"/>.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WindowControlsOverlayGeometry))]
    public async ValueTask<WindowControlsOverlayGeometry> GetTitlebarAreaRect()
        => await js.Invoke<WindowControlsOverlayGeometry?>("BitButil.windowControlsOverlay.getTitlebarAreaRect")
           ?? new WindowControlsOverlayGeometry();

    /// <summary>
    /// Invoked from JS on each geometry change. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeGeometryChange(Guid id, WindowControlsOverlayGeometry geometry)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(geometry ?? new WindowControlsOverlayGeometry());
    }

    /// <summary>
    /// Watches the overlay: fires when the window is resized, when the overlay is toggled, and once
    /// immediately with the current geometry so a subscriber can lay out before anything changes.
    /// </summary>
    /// <returns>
    /// A subscription that detaches the listener on dispose. On a runtime without the overlay the
    /// handler is never called.
    /// </returns>
    [DynamicDependency(nameof(InvokeGeometryChange), typeof(WindowControlsOverlay))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WindowControlsOverlayGeometry))]
    public async Task<ButilSubscription> OnGeometryChange(Action<WindowControlsOverlayGeometry> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers.TryAdd(id, handler);

        try
        {
            await js.Invoke<bool>("BitButil.windowControlsOverlay.onGeometryChange", DotNetRef, id, InvokeMethodName);
        }
        catch
        {
            // No subscription reaches the caller, so nothing would ever drop the handler again.
            _handlers.TryRemove(id, out _);
            try { await js.InvokeVoid("BitButil.windowControlsOverlay.offGeometryChange", id); } catch { /* the registration is what failed */ }
            throw;
        }

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.windowControlsOverlay.offGeometryChange", id);
        });
    }

    /// <summary>Detaches every listener registered through this instance and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = _handlers.Keys.ToArray();
            _handlers.Clear();
            foreach (var id in ids) await js.InvokeVoid("BitButil.windowControlsOverlay.offGeometryChange", id);
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
