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

    public async ValueTask<BlowMeasuredNode[]> MeasureNodesAsync(BlowElementReferenceLike viewport, double zoom)
        => await (await _moduleTask.Value).InvokeAsync<BlowMeasuredNode[]>("measureNodes", viewport.Reference, zoom)
           ?? [];

    /// <summary>
    /// The handle under a screen point, looking only inside <paramref name="pane"/> - another
    /// flow on the same page may well use the same node ids.
    /// </summary>
    public async ValueTask<BlowHandleHit?> ElementDataAtPointAsync(object pane, double x, double y, string selector)
        => await (await _moduleTask.Value).InvokeAsync<BlowHandleHit?>("elementDataAtPoint", x, y, selector, pane);

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
    public async ValueTask<IJSObjectReference?> InstallInteractionGuardsAsync(
        object pane, string? noDragClass, string? noPanClass, string? noWheelClass, BlowInputPolicy policy)
        => await (await _moduleTask.Value).InvokeAsync<IJSObjectReference?>(
            "installInteractionGuards", pane, noDragClass, noPanClass, noWheelClass, policy);

    public async ValueTask SetPointerCaptureAsync(object element, long pointerId)
        => await (await _moduleTask.Value).InvokeVoidAsync("setPointerCapture", element, pointerId);

    public async ValueTask ReleasePointerCaptureAsync(object element, long pointerId)
        => await (await _moduleTask.Value).InvokeVoidAsync("releasePointerCapture", element, pointerId);

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

/// <summary>
/// Decides, per DOM event, whether the browser's own handling of it (scrolling the page, mostly)
/// should be cancelled. Only the DOM can cancel an event, and only synchronously, so the rules
/// have to be handed to JS up front.
/// </summary>
/// <param name="PreventScrolling">Whether wheel events the flow acts on are kept from scrolling the page.</param>
/// <param name="PanOnScroll">Whether a plain wheel pans the flow (in which case the flow always acts on it).</param>
/// <param name="ZoomKey">
/// The <c>KeyboardEvent</c> flag (<c>"ctrlKey"</c>, <c>"metaKey"</c>, ...) that must be set for a plain
/// wheel to zoom, or null when the wheel zooms unconditionally. Without it the wheel passes to the page.
/// </param>
/// <param name="PanKey">The key that, held, turns a drag into a pan; it must not also scroll the page.</param>
/// <param name="KeyboardA11y">Whether the arrow keys move the selection, and so must not also scroll the page.</param>
internal sealed record BlowInputPolicy(
    bool PreventScrolling, bool PanOnScroll, string? ZoomKey, string? PanKey, bool KeyboardA11y);

/// <summary>Wrapper so we can pass an ElementReference without leaking the type around.</summary>
internal readonly record struct BlowElementReferenceLike(object Reference);

/// <summary>An element's bounding rectangle as read from the DOM, in client (screen) pixels.</summary>
/// <param name="Left">Distance from the viewport's left edge.</param>
/// <param name="Top">Distance from the viewport's top edge.</param>
/// <param name="Width">Rendered width.</param>
/// <param name="Height">Rendered height.</param>
public sealed record BlowDomRect(double Left, double Top, double Width, double Height);

/// <summary>One node as measured in the DOM, with sizes already divided back out of the zoom.</summary>
/// <param name="Id">The node's id, read from its <c>data-id</c> attribute.</param>
/// <param name="Width">Rendered width in flow units.</param>
/// <param name="Height">Rendered height in flow units.</param>
/// <param name="Handles">The handles found inside the node.</param>
public sealed record BlowMeasuredNode(string Id, double Width, double Height, BlowMeasuredHandle[] Handles);

/// <summary>One handle as measured in the DOM, positioned relative to its node's top-left.</summary>
/// <param name="HandleId">The handle's own id, or an empty string when it has none.</param>
/// <param name="Type">Either <c>"source"</c> or <c>"target"</c>.</param>
/// <param name="Position">The side the handle is anchored to: <c>"top"</c>, <c>"right"</c>, <c>"bottom"</c> or <c>"left"</c>.</param>
/// <param name="OffsetX">Horizontal offset of the handle's centre from the node's left edge, in flow units.</param>
/// <param name="OffsetY">Vertical offset of the handle's centre from the node's top edge, in flow units.</param>
/// <param name="ConnectableEnd">False when the handle refuses dropped connections; null (not reported) means it accepts them.</param>
public sealed record BlowMeasuredHandle(
    string HandleId, string Type, string Position, double OffsetX, double OffsetY, bool? ConnectableEnd = null);

/// <summary>The handle found under a screen point, used to resolve where a connection was dropped.</summary>
/// <param name="NodeId">The id of the node the handle belongs to, or null when nothing was hit.</param>
/// <param name="HandleId">The handle's own id, or an empty string when it has none.</param>
/// <param name="HandleType">Either <c>"source"</c> or <c>"target"</c>.</param>
/// <param name="Position">The side the handle is anchored to.</param>
/// <param name="ConnectableEnd">False when the handle refuses dropped connections; null (not reported) means it accepts them.</param>
public sealed record BlowHandleHit(
    string? NodeId, string? HandleId, string? HandleType, string? Position, bool? ConnectableEnd = null);
