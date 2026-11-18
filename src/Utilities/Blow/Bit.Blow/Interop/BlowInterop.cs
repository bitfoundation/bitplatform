using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Bit.Blow;

/// <summary>
/// Thin wrapper over the <c>bit-blow.js</c> module.
/// </summary>
internal sealed class BlowInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public BlowInterop(IJSRuntime js)
    {
        _moduleTask = new(() => js.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Bit.Blow/bit-blow.js").AsTask());
    }

    public async ValueTask<BlowDomRect?> GetRectAsync(BlowElementReferenceLike el)
        => await (await _moduleTask.Value).InvokeAsync<BlowDomRect?>("getRect", el.Reference);

    /// <summary>
    /// The pane's rectangle as pointer <paramref name="pointerId"/> last went down on it, read in that event, and its
    /// rectangle now. Needs <see cref="ObserveResizeAsync"/> running to have seen the press.
    /// </summary>
    // BlowDomRect is only nested in the result, which InvokeAsync<T> does not preserve.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BlowPaneRectsAtPointerDown))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BlowDomRect))]
    public async ValueTask<BlowPaneRectsAtPointerDown?> PaneRectsAtPointerDownAsync(BlowElementReferenceLike pane, long pointerId)
        => await (await _moduleTask.Value).InvokeAsync<BlowPaneRectsAtPointerDown?>(
            "paneRectsAtPointerDown", pane.Reference, pointerId);

    // InvokeAsync<T> preserves what System.Text.Json needs of T, but T is an array here and the element types are
    // only ever built by System.Text.Json: without this the trimmer drops their constructors and every measurement
    // fails to deserialize in trimmed (release) builds.
    /// <param name="viewport">The flow's viewport element.</param>
    /// <param name="zoom">The zoom, should the scale the viewport is drawn at not be measurable.</param>
    /// <param name="changedOnly">
    /// Only the nodes whose size or handles changed since the last report for this viewport, those not reported
    /// before, and <paramref name="include"/>; every node when false.
    /// </param>
    /// <param name="include">Nodes to report with <paramref name="changedOnly"/> whether they changed or not.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BlowMeasuredNode))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BlowMeasuredHandle))]
    public async ValueTask<BlowMeasuredNode[]> MeasureNodesAsync(BlowElementReferenceLike viewport, double zoom,
        bool changedOnly = false, IReadOnlyCollection<string>? include = null)
        => await (await _moduleTask.Value).InvokeAsync<BlowMeasuredNode[]>("measureNodes", viewport.Reference, zoom,
               changedOnly, include ?? [])
           ?? [];

    /// <summary>
    /// The handle under a screen point, looking only inside <paramref name="pane"/> - another
    /// flow on the same page may well use the same node ids.
    /// </summary>
    public async ValueTask<BlowHandleHit?> ElementDataAtPointAsync(object pane, double x, double y, string selector)
        => await (await _moduleTask.Value).InvokeAsync<BlowHandleHit?>("elementDataAtPoint", x, y, selector, pane);

    /// <summary>
    /// The handle of <paramref name="pane"/> the pointer is over, from the browser's hover state rather than an event -
    /// another flow nested inside it keeps its own.
    /// </summary>
    public async ValueTask<BlowHandleHit?> HoveredHandleAsync(object pane)
        => await (await _moduleTask.Value).InvokeAsync<BlowHandleHit?>("hoveredHandle", pane);

    /// <summary>
    /// Starts watching the pane and its nodes for size changes. The returned reference must be
    /// disposed to stop observing.
    /// </summary>
    public async ValueTask<IJSObjectReference?> ObserveResizeAsync(
        object pane, object viewport, object dotNetRef)
        => await (await _moduleTask.Value).InvokeAsync<IJSObjectReference?>(
            "observeResize", pane, viewport, dotNetRef);

    /// <summary>
    /// Installs the nodrag/nopan/nowheel guards and the input policy on the pane. The returned
    /// reference must be disposed to remove them.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BlowInputPolicy))]
    public async ValueTask<IJSObjectReference?> InstallInteractionGuardsAsync(
        object pane, string? noDragClass, string? noPanClass, string? noWheelClass, BlowInputPolicy policy)
        => await (await _moduleTask.Value).InvokeAsync<IJSObjectReference?>(
            "installInteractionGuards", pane, noDragClass, noPanClass, noWheelClass, policy);

    public async ValueTask SetPointerCaptureAsync(object element, long pointerId)
        => await (await _moduleTask.Value).InvokeVoidAsync("setPointerCapture", element, pointerId);

    public async ValueTask ReleasePointerCaptureAsync(object element, long pointerId)
        => await (await _moduleTask.Value).InvokeVoidAsync("releasePointerCapture", element, pointerId);

    /// <summary>True when the operating system asks applications to reduce motion.</summary>
    public async ValueTask<bool> PrefersReducedMotionAsync()
        => await (await _moduleTask.Value).InvokeAsync<bool>("prefersReducedMotion");

    /// <summary>
    /// Moves keyboard focus to one of the pane's own nodes or edges (<paramref name="kind"/> is <c>node</c> or
    /// <c>edge</c>) - not one of a flow nested inside it. False when it is not in the page or would not take focus.
    /// </summary>
    public async ValueTask<bool> FocusElementAsync(object pane, string kind, string id)
        => await (await _moduleTask.Value).InvokeAsync<bool>("focusElement", pane, kind, id);

    /// <summary>
    /// True when the pane's node <paramref name="nodeId"/> holds focus a pointer gave it rather than the keyboard
    /// (it is not <c>:focus-visible</c>), or no longer holds focus at all.
    /// </summary>
    public async ValueTask<bool> IsPointerFocusAsync(object pane, string nodeId)
        => await (await _moduleTask.Value).InvokeAsync<bool>("isPointerFocus", pane, nodeId);

    /// <summary>Focuses the pane when focus has fallen to the page, as it does when the focused element is removed.</summary>
    public async ValueTask KeepFocusAsync(object pane)
        => await (await _moduleTask.Value).InvokeVoidAsync("keepFocus", pane);

    /// <summary>True when focus currently sits in a text input, textarea, select or contenteditable.</summary>
    public async ValueTask<bool> IsEditableFocusedAsync()
        => await (await _moduleTask.Value).InvokeAsync<bool>("isEditableFocused");

    public async ValueTask DisposeAsync()
    {
        if (!_moduleTask.IsValueCreated) return;

        // Teardown ordering is not guaranteed: the circuit/renderer may already be gone.
        try
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (OperationCanceledException) { }
        catch (JSException) { }
    }
}
