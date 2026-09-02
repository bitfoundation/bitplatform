using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/PointerEvent/getCoalescedEvents">PointerEvent.getCoalescedEvents()</see>
/// and <see href="https://developer.mozilla.org/en-US/docs/Web/API/PointerEvent/getPredictedEvents">getPredictedEvents()</see>:
/// the input samples between two animation frames, and the browser's guess at the next few.
/// </summary>
/// <remarks>
/// <b>Why this exists:</b> the browser fires at most one <c>pointermove</c> per frame, but a pen
/// samples several times faster than that. A drawing surface built on ordinary pointer events
/// therefore loses most of its input, and a quick stroke comes out as a polygon. The coalesced list
/// is the samples that were merged away.
/// <br/>
/// Blazor's own <c>@onpointermove</c> gives a <c>PointerEventArgs</c> with no way back to the DOM
/// event, so the coalesced list cannot be reached from it - this service attaches its own listener
/// instead.
/// <br/>
/// The listener is registered as passive, so a handler cannot cancel scrolling. Call
/// <c>ElementReferenceExtensions.SetPointerCapture</c> and set <c>touch-action: none</c> on the
/// surface if the gesture has to belong to it.
/// </remarks>
[ButilService(typeof(PointerTracker))]
public class PointerTracker(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokePointerFrame);

    private readonly ConcurrentDictionary<Guid, Action<PointerFrame>> _handlers = new();

    // Per-instance callback reference (see Keyboard): trackers are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<PointerTracker>? _dotNetRef;
    private DotNetObjectReference<PointerTracker> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime implements <c>getCoalescedEvents</c>.</summary>
    /// <remarks>
    /// Where it is false, tracking still works - each frame simply carries a single sample - so this
    /// is a fidelity check, not a gate.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.pointerTracker.isSupported");

    /// <summary>True when the runtime implements <c>getPredictedEvents</c> - Chromium only.</summary>
    public ValueTask<bool> SupportsPrediction() => js.Invoke<bool>("BitButil.pointerTracker.supportsPrediction");

    /// <summary>
    /// Invoked from JS for each pointer event. Public + <see cref="JSInvokableAttribute"/> so it can
    /// be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokePointerFrame(Guid id, PointerFrame frame)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke(frame ?? new PointerFrame());
    }

    /// <summary>
    /// Watches an element's pointer input at full sample rate.
    /// </summary>
    /// <param name="element">The surface to track - a canvas, an annotation layer.</param>
    /// <param name="handler">
    /// Called once per delivered event, with every sample merged into it. Called on the interop
    /// dispatch, so a Blazor component has to <c>StateHasChanged</c> itself.
    /// </param>
    /// <param name="includePredicted">
    /// Also report the browser's predicted positions. Off by default - they are guesses, and only
    /// worth taking on when the UI can draw them provisionally. See <see cref="PointerFrame.Predicted"/>.
    /// </param>
    /// <param name="events">
    /// Which DOM events to track. Defaults to <c>pointermove</c> alone; pass
    /// <c>"pointerdown"</c>/<c>"pointerup"</c> too when the stroke's ends matter.
    /// </param>
    /// <returns>A subscription that detaches the listeners on dispose.</returns>
    /// <remarks>
    /// <b>Chatty by design.</b> Every frame crosses interop, which under Blazor Server means a
    /// message per frame over the circuit - fine for a signature pad, not for tracking the whole
    /// document. Track the smallest element you can, and detach as soon as the gesture is over.
    /// </remarks>
    [DynamicDependency(nameof(InvokePointerFrame), typeof(PointerTracker))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PointerFrame))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PointerSample))]
    public async Task<ButilSubscription> Track(
        ElementReference element,
        Action<PointerFrame> handler,
        bool includePredicted = false,
        params string[] events)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        _handlers.TryAdd(id, handler);

        await js.Invoke<bool>("BitButil.pointerTracker.track", element, id, events, includePredicted, DotNetRef, InvokeMethodName);

        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.pointerTracker.untrack", id);
        });
    }

    /// <summary>Detaches every tracker registered through this instance and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = _handlers.Keys.ToArray();
            _handlers.Clear();
            foreach (var id in ids) await js.InvokeVoid("BitButil.pointerTracker.untrack", id);
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
