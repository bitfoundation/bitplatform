using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/VirtualKeyboard_API">VirtualKeyboard API</see>:
/// show and hide the on-screen keyboard, and know exactly where it is.
/// </summary>
/// <remarks>
/// By default the browser handles the keyboard by shrinking the viewport, and the page never learns
/// anything about it. Turning on <see cref="SetOverlaysContent(bool)"/> switches that off: the
/// keyboard is drawn <b>over</b> the page, the viewport keeps its size, and the app becomes
/// responsible for keeping its content out from under it - using
/// <see cref="OnGeometryChange"/> here, or the <c>keyboard-inset-*</c> CSS environment variables.
/// That is what makes a chat composer stay pinned above the keyboard instead of jumping.
/// <br/>
/// The <c>virtualkeyboardpolicy</c> attribute is the other half of this API, and is set per element
/// through <c>ElementReferenceStateExtensions.SetVirtualKeyboardPolicy</c>: set it to
/// <see cref="VirtualKeyboardPolicy.Manual"/> on a <c>contenteditable</c> element and focus no longer
/// shows the keyboard on its own - <see cref="Show"/> and <see cref="Hide"/> do.
/// <br/>
/// Chromium on touch devices. Elsewhere <see cref="IsSupported"/> is false and every call is a no-op.
/// </remarks>
[ButilService(typeof(VirtualKeyboard))]
public class VirtualKeyboard(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeGeometryChange);

    private readonly ConcurrentDictionary<Guid, Action<VirtualKeyboardGeometry>> _handlers = new();

    // Per-instance callback reference (see Keyboard): listeners are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<VirtualKeyboard>? _dotNetRef;
    private DotNetObjectReference<VirtualKeyboard> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>navigator.virtualKeyboard</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.virtualKeyboard.isSupported");

    /// <summary>
    /// Asks for the on-screen keyboard.
    /// </summary>
    /// <remarks>
    /// Only works from a user-gesture handler while a focused editable element has its
    /// <c>virtualkeyboardpolicy</c> set to <c>manual</c>. Anywhere else it is ignored.
    /// </remarks>
    public ValueTask Show() => js.InvokeVoid("BitButil.virtualKeyboard.show");

    /// <summary>Dismisses the on-screen keyboard, leaving focus where it is.</summary>
    public ValueTask Hide() => js.InvokeVoid("BitButil.virtualKeyboard.hide");

    /// <summary>
    /// True when the keyboard is drawn over the page rather than resizing the viewport - i.e. when
    /// the app has taken responsibility for laying out around it.
    /// </summary>
    public ValueTask<bool> GetOverlaysContent() => js.Invoke<bool>("BitButil.virtualKeyboard.getOverlaysContent");

    /// <summary>
    /// Takes over (or hands back) responsibility for laying out around the keyboard.
    /// </summary>
    /// <param name="value">
    /// True to have the keyboard overlay the page - the viewport stops resizing and
    /// <see cref="GetBoundingRect"/> starts reporting. False to go back to the browser's own
    /// handling.
    /// </param>
    /// <remarks>
    /// Set this once at start-up, before anything can be focused; flipping it while the keyboard is
    /// up produces a visible jump.
    /// </remarks>
    public ValueTask SetOverlaysContent(bool value) => js.InvokeVoid("BitButil.virtualKeyboard.setOverlaysContent", value);

    /// <summary>
    /// Where the keyboard is right now. All zeros when it isn't showing - or when
    /// <see cref="SetOverlaysContent(bool)"/> was never turned on, since the browser then resizes the
    /// viewport and has nothing to report.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VirtualKeyboardGeometry))]
    public async ValueTask<VirtualKeyboardGeometry> GetBoundingRect()
        => await js.Invoke<VirtualKeyboardGeometry?>("BitButil.virtualKeyboard.getBoundingRect")
           ?? new VirtualKeyboardGeometry();

    /// <summary>
    /// Invoked from JS on each geometry change. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeGeometryChange(Guid id, VirtualKeyboardGeometry geometry)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(geometry ?? new VirtualKeyboardGeometry());
    }

    /// <summary>
    /// Watches the keyboard: fires when it appears, disappears or resizes, and once immediately with
    /// the current geometry.
    /// </summary>
    /// <returns>
    /// A subscription that detaches the listener on dispose. On a runtime without the API the handler
    /// is never called.
    /// </returns>
    [DynamicDependency(nameof(InvokeGeometryChange), typeof(VirtualKeyboard))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VirtualKeyboardGeometry))]
    public async Task<ButilSubscription> OnGeometryChange(Action<VirtualKeyboardGeometry> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers.TryAdd(id, handler);

        await js.Invoke<bool>("BitButil.virtualKeyboard.onGeometryChange", DotNetRef, id, InvokeMethodName);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.virtualKeyboard.offGeometryChange", id);
        });
    }

    /// <summary>Detaches every listener registered through this instance and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = _handlers.Keys.ToArray();
            _handlers.Clear();
            foreach (var id in ids) await js.InvokeVoid("BitButil.virtualKeyboard.offGeometryChange", id);
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
