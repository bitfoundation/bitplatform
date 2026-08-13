using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/View_Transition_API">View Transition API</see>:
/// the browser snapshots the page, lets you change it, and animates between the two states - so a
/// list reordering or a detail panel opening cross-fades instead of snapping.
/// </summary>
/// <remarks>
/// The animation is CSS. Butil starts and controls the transition; which elements morph into which
/// is decided by giving them a matching <c>view-transition-name</c>, and the default cross-fade
/// needs no CSS at all.
/// <br/>
/// <b>The timing that matters:</b> the browser takes the "before" snapshot when the transition
/// starts, calls your update callback, and takes the "after" snapshot when that callback's task
/// completes. In Blazor the DOM changes on a render, not on an assignment - so a callback that
/// merely sets a field and returns has not changed anything yet, and both snapshots come out the
/// same. Await the render before returning; the demo page shows the
/// <see cref="TaskCompletionSource"/>-in-<c>OnAfterRenderAsync</c> pattern that does this reliably.
/// </remarks>
public class ViewTransition(IJSRuntime js) : IAsyncDisposable
{
    internal const string UpdateMethodName = nameof(InvokeViewTransitionUpdate);
    internal const string PhaseMethodName = nameof(InvokeViewTransitionPhase);

    private readonly ConcurrentDictionary<Guid, Func<Task>> _updates = new();
    private readonly ConcurrentDictionary<Guid, ViewTransitionHandle> _handles = new();

    // Per-instance callback reference (see Keyboard): transitions are isolated per circuit / WASM
    // app and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<ViewTransition>? _dotNetRef;
    private DotNetObjectReference<ViewTransition> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>document.startViewTransition</c>.</summary>
    /// <remarks>
    /// Treat view transitions as a progressive enhancement: when this is false, do the same DOM
    /// change without one and the page still works, just without the animation.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.viewTransition.isSupported");

    /// <summary>
    /// Invoked from JS to run the caller's DOM update between the two snapshots. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    /// <remarks>
    /// The returned task is what the browser awaits before snapshotting the new state, so this
    /// must not complete before the DOM has actually changed.
    /// </remarks>
    [JSInvokable(UpdateMethodName)]
    public async Task InvokeViewTransitionUpdate(Guid id)
    {
        if (_updates.TryGetValue(id, out var update)) await update();
    }

    /// <summary>
    /// Invoked from JS as the transition moves through its phases. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(PhaseMethodName)]
    public void InvokeViewTransitionPhase(Guid id, string phase, string message)
    {
        if (_handles.TryGetValue(id, out var handle) is false) return;

        handle.Advance(phase, message);

        // 'ready' and 'skipped' are mid-flight; only the terminal phases retire the handle.
        if (phase is "finished" or "failed")
        {
            _handles.TryRemove(id, out _);
            _updates.TryRemove(id, out _);
        }
    }

    /// <summary>
    /// Runs <paramref name="updateDom"/> inside a view transition, animating from the page as it
    /// looks now to the page as it looks afterwards.
    /// </summary>
    /// <param name="updateDom">
    /// Makes the change. It must not return until the DOM has actually updated - in Blazor that
    /// means awaiting a render, not just assigning a field.
    /// </param>
    /// <param name="types">
    /// Optional <see href="https://developer.mozilla.org/en-US/docs/Web/API/ViewTransition/types">transition types</see>,
    /// which CSS can select on to animate a "forward" navigation differently from a "back" one.
    /// Ignored by engines that only implement level 1.
    /// </param>
    /// <returns>
    /// A handle for awaiting the animation and skipping it, or null when the engine has no view
    /// transitions. On null, <paramref name="updateDom"/> is <b>not</b> run - the caller does the
    /// same change unanimated, which keeps the "did the update happen" question in one place.
    /// </returns>
    public async ValueTask<ViewTransitionHandle?> Start(Func<Task> updateDom, string[]? types = null)
    {
        ArgumentNullException.ThrowIfNull(updateDom);

        var id = Guid.NewGuid();
        var handle = new ViewTransitionHandle(js, id);

        _updates[id] = updateDom;
        _handles[id] = handle;

        var started = await js.Invoke<bool>("BitButil.viewTransition.start", DotNetRef, id, types);
        if (started) return handle;

        _updates.TryRemove(id, out _);
        _handles.TryRemove(id, out _);
        return null;
    }

    /// <summary>
    /// The common case: change some state and re-render inside a transition.
    /// </summary>
    /// <param name="updateState">Applies the state change. Runs before the re-render is requested.</param>
    /// <param name="render">
    /// Renders and completes once the DOM has caught up - pass a delegate that awaits your
    /// component's next render pass.
    /// </param>
    /// <param name="types">Optional transition types - see <see cref="Start(Func{Task}, string[])"/>.</param>
    /// <returns>The handle, or null when view transitions are unavailable. On null, the caller
    /// should apply the change itself without one.</returns>
    /// <remarks>
    /// This is <see cref="Start(Func{Task}, string[])"/> with the two halves named, so it is
    /// obvious that the render is the part that has to be awaited.
    /// </remarks>
    public ValueTask<ViewTransitionHandle?> Start(Action updateState, Func<Task> render, string[]? types = null)
    {
        ArgumentNullException.ThrowIfNull(updateState);
        ArgumentNullException.ThrowIfNull(render);

        return Start(async () =>
        {
            updateState();
            await render();
        }, types);
    }

    /// <summary>
    /// On scope/circuit teardown, skips any transition still running so a half-finished animation
    /// can't outlive the page that started it.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _updates.Clear();
            _handles.Clear();
            await js.InvokeVoid("BitButil.viewTransition.disposeAll");
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
