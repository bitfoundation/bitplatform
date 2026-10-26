using Microsoft.JSInterop;

namespace Bit.BlazorHeadlessUI;

/// <summary>Why a floating layer asked to be dismissed.</summary>
public enum BitBhDismissReason
{
    /// <summary>The Escape key was pressed.</summary>
    EscapeKey,

    /// <summary>A pointer was pressed outside the layer (and its trigger / nested layers).</summary>
    PointerDownOutside,

    /// <summary>Focus moved outside the layer.</summary>
    FocusOutside,

    /// <summary>The layer was closed from code (selection, close button, …).</summary>
    Programmatic,

    /// <summary>
    /// The surface it was opened from closed (a submenu of a closing menu, a popover inside a closing
    /// dialog). The runtime has already closed it, so cancelling the request only reopens it.
    /// </summary>
    ParentClosed,
}

/// <summary>Layer (dismissal) behaviour of a floating surface.</summary>
public sealed class BitBhLayerOptions
{
    /// <summary>Escape dismisses the layer when focus is outside every layer. Default: true.</summary>
    public bool Escape { get; set; } = true;

    /// <summary>Pointer-down outside dismisses the layer. Default: true.</summary>
    public bool PointerDownOutside { get; set; } = true;

    /// <summary>Focus moving outside dismisses the layer. Default: false.</summary>
    public bool FocusOutside { get; set; }

    /// <summary>A modal layer shields the layers below it from outside interaction.</summary>
    public bool Modal { get; set; }

    /// <summary>Hover-driven layer (tooltip, hover card): Escape always dismisses it first.</summary>
    public bool Hover { get; set; }

    /// <summary>Handle of the parent surface (nested menus, popovers opened from popovers).</summary>
    public int ParentHandle { get; set; }

    /// <summary>Extra element ids that belong to the layer (trigger, input, …).</summary>
    public List<string?> BranchIds { get; } = new();

    /// <summary>
    /// Element ids where a secondary-button press (the one that opens a context menu) is not "outside": the
    /// context menu it opens again moves the open one instead of closing and reopening it.
    /// </summary>
    public List<string?> SecondaryPressIds { get; } = new();

    internal Dictionary<string, object?> ToJs() => new()
    {
        ["escape"] = Escape,
        ["outside"] = PointerDownOutside,
        ["focusOutside"] = FocusOutside,
        ["modal"] = Modal,
        ["hover"] = Hover,
        ["parentFloating"] = ParentHandle,
        ["ids"] = BranchIds.Where(id => !string.IsNullOrEmpty(id)).ToArray(),
        ["secondaryIds"] = SecondaryPressIds.Where(id => !string.IsNullOrEmpty(id)).ToArray(),
    };
}

/// <summary>Everything the JS runtime needs to open a floating surface.</summary>
public sealed class BitBhFloatingRequest
{
    /// <summary>The id of the content element (layer, focus scope, positioning target).</summary>
    public required string ContentId { get; init; }

    /// <summary>
    /// Elements moved by the portal (in order) and kept visible by modal isolation.
    /// Defaults to <see cref="ContentId"/>. Use it to move a wrapper (dialog root)
    /// or several siblings (overlay + content) together.
    /// </summary>
    public IReadOnlyList<string?>? SurfaceIds { get; init; }

    /// <summary>Teleport the surface to the portal root (or <see cref="PortalTarget"/>) while open.</summary>
    public bool Portal { get; init; }

    /// <summary>Optional id / CSS selector of the portal container.</summary>
    public string? PortalTarget { get; init; }

    /// <summary>Id of the reference element used for anchor positioning.</summary>
    public string? ReferenceId { get; init; }

    /// <summary>Virtual reference point (viewport coordinates) used instead of <see cref="ReferenceId"/>.</summary>
    public (double X, double Y)? Point { get; init; }

    /// <summary>Anchor positioning options; null disables positioning.</summary>
    public BitBhAnchorOptions? Anchor { get; init; }

    /// <summary>Id of an arrow element to position along the reference.</summary>
    public string? ArrowId { get; init; }

    /// <summary>Dismissal behaviour; null disables the layer.</summary>
    public BitBhLayerOptions? Layer { get; init; }

    /// <summary>Hide the rest of the page from assistive tech and lock scrolling.</summary>
    public bool Modal { get; init; }

    /// <summary>Lock document scroll when <see cref="Modal"/>. Default: true.</summary>
    public bool LockScroll { get; init; } = true;

    /// <summary>Focus management; null leaves focus alone.</summary>
    public BitBhFocusScopeOptions? Focus { get; init; }

    /// <summary>Whether the content is only revealed by the runtime (anchored or portalled).</summary>
    public bool RevealedByScript => Anchor is not null || Portal;

    internal Dictionary<string, object?> ToJs()
    {
        object? reference = Point is { } p
            ? new Dictionary<string, object?> { ["x"] = p.X, ["y"] = p.Y }
            : ReferenceId;

        return new Dictionary<string, object?>
        {
            ["portalIds"] = SurfaceIds?.Where(id => !string.IsNullOrEmpty(id)).ToArray(),
            ["portal"] = Portal ? (object?)PortalTarget ?? true : null,
            ["reference"] = reference,
            ["anchor"] = Anchor?.ToJs(ArrowId),
            ["layer"] = Layer?.ToJs(),
            ["modal"] = Modal ? new Dictionary<string, object?> { ["hideOthers"] = true, ["scrollLock"] = LockScroll } : null,
            ["focus"] = Focus?.ToJs(),
        };
    }
}

/// <summary>
/// Drives the lifecycle of one floating surface (popover panel, menu, listbox
/// options, tooltip, dialog …) against the JS runtime, including presence:
/// the surface stays visible with <c>data-state="closed"</c> until its CSS exit
/// animation has finished, so consumers can animate both directions with plain
/// CSS keyframes or transitions.
///
/// <para>
/// Owners call <see cref="SyncAsync"/> from <c>OnAfterRenderAsync</c> and use
/// <see cref="ShouldHide"/> / <see cref="IsPresent"/> while rendering.
/// </para>
/// </summary>
public sealed class BitBhFloatingController : IAsyncDisposable
{
    private enum Phase { Closed, Opening, Open, Closing }

    private readonly BitBhInterop _interop;
    private readonly Func<BitBhDismissReason, Task> _onDismiss;
    private readonly Action _stateHasChanged;
    private readonly Func<Exception, Task>? _onError;
    private DotNetObjectReference<BitBhFloatingController>? _selfRef;
    private Phase _phase;
    private bool _syncing;
    private bool _disposed;
    private Func<bool>? _isOpen;
    private string? _sentAnchor;

    /// <summary>Creates a controller.</summary>
    /// <param name="interop">The JS runtime wrapper.</param>
    /// <param name="onDismiss">Invoked when the layer requests dismissal (Escape, outside click, focus outside).</param>
    /// <param name="stateHasChanged">The owner's <c>StateHasChanged</c>.</param>
    /// <param name="onError">
    /// Receives what <paramref name="onDismiss"/> throws when the runtime asked for the dismissal (the owner's
    /// <c>DispatchExceptionAsync</c>, so it reaches an <c>ErrorBoundary</c>). Without it the exception goes back
    /// to the runtime, which cannot report it.
    /// </param>
    public BitBhFloatingController(BitBhInterop interop, Func<BitBhDismissReason, Task> onDismiss, Action stateHasChanged, Func<Exception, Task>? onError = null)
    {
        _interop = interop;
        _onDismiss = onDismiss;
        _stateHasChanged = stateHasChanged;
        _onError = onError;
    }

    /// <summary>The JS handle of the open surface, or 0. Pass to child layers as their parent.</summary>
    public int Handle { get; private set; }

    /// <summary>Whether the surface is mounted in JS (open or animating out).</summary>
    public bool IsActive => _phase is Phase.Open or Phase.Closing;

    /// <summary>Whether the content should be rendered at all (open, or still animating out).</summary>
    public bool IsPresent(bool isOpen) => isOpen || _phase is Phase.Open or Phase.Closing;

    /// <summary>
    /// Whether the content element should carry the <c>hidden</c> attribute.
    /// Closed content is hidden right away — elements marked <c>data-bit-bh-presence</c>
    /// are kept visible by the runtime while a CSS exit animation runs, so closing
    /// is instant when nothing animates. Script-revealed surfaces (anchored /
    /// portalled) stay hidden until the runtime has positioned them, which
    /// prevents a flash at the wrong spot.
    /// </summary>
    public bool ShouldHide(bool isOpen, bool revealedByScript)
        => !isOpen || (revealedByScript && _phase is Phase.Closed or Phase.Opening);

    /// <summary>
    /// Reconciles the JS surface with <paramref name="isOpen"/>. Safe to call on
    /// every render; transitions are serialized and re-evaluated after each await.
    /// Anchor options that changed while open (placement, gap, arrow …) are re-applied.
    /// </summary>
    public async Task SyncAsync(Func<bool> isOpen, Func<BitBhFloatingRequest> request)
    {
        _isOpen = isOpen;
        if (_syncing || _disposed) return;
        _syncing = true;
        try
        {
            while (!_disposed)
            {
                var open = isOpen();
                if (open && _phase == Phase.Closed)
                {
                    _phase = Phase.Opening;
                    var req = request();
                    _selfRef ??= DotNetObjectReference.Create(this);
                    _sentAnchor = AnchorKey(req);
                    var handle = await _interop.FloatingOpenAsync(_selfRef, req.ContentId, req.ToJs());
                    if (_disposed)
                    {
                        // Disposed while the open was in flight: DisposeAsync saw no handle to close, and a
                        // modal surface left open keeps the rest of the page inert and its scroll locked.
                        await _interop.FloatingCloseAsync(handle, restoreFocus: true, waitForExit: false);
                        return;
                    }
                    Handle = handle;
                    _phase = Phase.Open;
                    _stateHasChanged();
                    continue;
                }

                if (!open && _phase == Phase.Open)
                {
                    _phase = Phase.Closing;
                    var handle = Handle;
                    Handle = 0;
                    await _interop.FloatingCloseAsync(handle, restoreFocus: true, waitForExit: true);
                    _phase = Phase.Closed;
                    _stateHasChanged();
                    continue;
                }

                if (open && _phase == Phase.Open)
                {
                    // The runtime keeps the options it was opened with: a placement changed while open (a
                    // panel's Anchor, a tooltip's side) is sent along.
                    var req = request();
                    var anchor = AnchorKey(req);
                    if (req.Anchor is not null && anchor != _sentAnchor)
                    {
                        _sentAnchor = anchor;
                        await UpdateAsync(req.Anchor, req.ArrowId);
                        continue;
                    }
                }

                break;
            }
        }
        finally
        {
            _syncing = false;
        }
    }

    private static string? AnchorKey(BitBhFloatingRequest request)
        => request.Anchor is null ? null : string.Join('|', request.Anchor.ToJs(request.ArrowId).Values);

    /// <summary>Re-applies positioning (e.g. after the anchor options changed).</summary>
    public ValueTask UpdateAsync(BitBhAnchorOptions? anchor = null, string? arrowId = null)
    {
        if (Handle <= 0) return ValueTask.CompletedTask;
        var options = anchor is null ? null : new Dictionary<string, object?> { ["anchor"] = anchor.ToJs(arrowId) };
        return _interop.FloatingUpdateAsync(Handle, options);
    }

    /// <summary>Moves the virtual anchor point (context menus re-opened at a new position).</summary>
    public ValueTask SetPointAsync(double x, double y) => _interop.FloatingSetPointAsync(Handle, x, y);

    /// <summary>
    /// Invoked by the JS runtime when the layer should be dismissed. Answers whether the surface is now
    /// closing: a request the owner declined (<c>OnDismissRequest</c> cancelled it, Escape is off ...)
    /// leaves the layer dismissable by the next outside press or Escape.
    /// </summary>
    [JSInvokable]
    public async Task<bool> OnLayerDismiss(string reason)
    {
        if (_disposed || _phase != Phase.Open) return false;
        var parsed = reason switch
        {
            "escape" => BitBhDismissReason.EscapeKey,
            "focus-outside" => BitBhDismissReason.FocusOutside,
            _ => BitBhDismissReason.PointerDownOutside,
        };
        await DismissAsync(parsed);
        return _disposed || _phase != Phase.Open || _isOpen?.Invoke() is not true;
    }

    /// <summary>
    /// Invoked by the JS runtime when it closed this surface along with the surface it was opened from
    /// (a submenu of a closing menu). Brings the owner's state in line with what the page shows.
    /// </summary>
    [JSInvokable]
    public Task OnClosedByParent()
    {
        if (_disposed || _phase != Phase.Open) return Task.CompletedTask;
        Handle = 0;
        _phase = Phase.Closed;
        _stateHasChanged();
        return DismissAsync(BitBhDismissReason.ParentClosed);
    }

    private async Task DismissAsync(BitBhDismissReason reason)
    {
        try
        {
            await _onDismiss(reason);
        }
        catch (Exception ex) when (_onError is not null)
        {
            await _onError(ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (Handle > 0)
        {
            var handle = Handle;
            Handle = 0;
            await _interop.FloatingCloseAsync(handle, restoreFocus: true, waitForExit: false);
        }
        _selfRef?.Dispose();
        _selfRef = null;
    }
}
