using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Bdnd;

/// <summary>
/// Wires its host element into the DnD system as a drag source.
/// This is the component equivalent of React DnD's <c>useDrag</c> hook: you declare a
/// <see cref="Type"/> and an <see cref="Item"/> (the data), and the child content receives
/// a <see cref="BdndDragState"/> with the collected props (e.g. <c>IsDragging</c>).
/// </summary>
/// <typeparam name="TItem">The type of the dragged item payload.</typeparam>
public sealed class BdndDraggable<TItem> : ComponentBase, IHandleEvent, IAsyncDisposable
{
    [CascadingParameter] private BdndDragDropManager? Manager { get; set; }
    [Inject] private BdndInterop Interop { get; set; } = default!;

    /// <summary>Required. The item type used to match against drop-target <c>Accept</c> lists.</summary>
    [Parameter, EditorRequired] public string Type { get; set; } = default!;

    /// <summary>The item payload. Ignored when <see cref="ItemFactory"/> is supplied.</summary>
    [Parameter] public TItem? Item { get; set; }

    /// <summary>
    /// Produces the item lazily at drag start. Return <c>null</c> to cancel the drag (use a nullable
    /// <typeparamref name="TItem"/> for value types, where <c>default</c> is a valid item).
    /// </summary>
    [Parameter] public Func<TItem?>? ItemFactory { get; set; }

    /// <summary>Whether dragging is allowed. Defaults to true.</summary>
    [Parameter] public bool CanDrag { get; set; } = true;

    /// <summary>Optional predicate overriding <see cref="CanDrag"/>, evaluated at render time and at drag start.</summary>
    [Parameter] public Func<bool>? CanDragPredicate { get; set; }

    /// <summary>Custom <c>isDragging</c> implementation (e.g. id-based for re-parented items).</summary>
    [Parameter] public Func<IBdndMonitor, bool>? IsDraggingOverride { get; set; }

    /// <summary>Hide the native drag image (use together with a <see cref="BdndDragLayer{TItem}"/>).</summary>
    [Parameter] public bool UseCustomDragLayer { get; set; }

    /// <summary>Require a <see cref="BdndDragHandle"/> child to initiate the drag.</summary>
    [Parameter] public bool UseHandle { get; set; }

    /// <summary>
    /// The drop effects the source allows, assigned to the native <c>dataTransfer.effectAllowed</c>
    /// (drives the cursor): <c>move</c> (default), <c>copy</c>, <c>link</c>, <c>copyMove</c>,
    /// <c>copyLink</c>, <c>linkMove</c> or <c>all</c>.
    /// </summary>
    [Parameter] public string DropEffect { get; set; } = "move";

    /// <summary>
    /// Fired when the drag begins, right after the browser started the drag and before any hover,
    /// drop or end callback of that drag.
    /// </summary>
    [Parameter] public EventCallback<TItem?> OnBegin { get; set; }

    /// <summary>Fired when the drag ends (drop or cancel), even if this component was removed meanwhile.</summary>
    [Parameter] public EventCallback<BdndDragEndContext<TItem>> OnEnd { get; set; }

    /// <summary>Host element tag name. Defaults to <c>div</c>.</summary>
    [Parameter] public string Tag { get; set; } = "div";

    /// <summary>The draggable's content; it receives the collected <see cref="BdndDragState"/>.</summary>
    [Parameter] public RenderFragment<BdndDragState>? ChildContent { get; set; }

    /// <summary>Any other attribute (class, style, id...), applied to the host element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private readonly string _handlerId;
    private readonly BdndHandleController _handleController;
    private BdndJsRegistration<BdndDragSourceOptions>? _registration;
    private DotNetObjectReference<BdndDraggable<TItem>>? _selfRef;
    private ElementReference _element;
    private bool _canDrag;
    private TItem? _draggingItem;
    private bool _disposed;

    public BdndDraggable()
    {
        _handlerId = Guid.NewGuid().ToString("N");
        _handleController = new BdndHandleController(this);
    }

    protected override void OnInitialized()
    {
        if (Manager is null)
            throw new InvalidOperationException(
                $"{nameof(BdndDraggable<TItem>)} must be placed inside a {nameof(BdndProvider)}.");
        Manager.StateChanged += OnStateChanged;
        _registration = new(RegisterAsync);
    }

    private bool EffectiveCanDrag => CanDragPredicate?.Invoke() ?? CanDrag;

    private void OnStateChanged() => InvokeAsync(StateHasChanged);

    // Drag events must not trigger the automatic re-render ComponentBase performs after every
    // event (e.g. while dragstart is still being dispatched); the manager drives re-renders.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var monitor = Manager!.Monitor;
        _canDrag = EffectiveCanDrag;
        var state = new BdndDragState
        {
            Monitor = monitor,
            IsDragging = IsDraggingOverride?.Invoke(monitor) ?? monitor.IsDragging(_handlerId),
            CanDrag = _canDrag,
        };

        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        // with UseHandle the BdndDragHandle element is the (native) draggable one
        builder.AddAttribute(2, "draggable", _canDrag && UseHandle is false ? "true" : "false");
        builder.AddAttribute(3, "ondragstart",
            EventCallback.Factory.Create<DragEventArgs>(this, HandleDragStartAsync));
        // stop nested (ancestor) draggables from also starting a drag
        builder.AddEventStopPropagationAttribute(4, "ondragstart", true);
        builder.AddElementReferenceCapture(5, el => _element = el);
        // expose handle wiring to descendant BdndDragHandle components; not fixed, so
        // handles re-render when CanDrag/UseHandle change
        builder.OpenComponent<CascadingValue<IBdndDragHandleController>>(6);
        builder.AddAttribute(7, "Value", _handleController);
        builder.AddAttribute(8, "ChildContent", (RenderFragment)(b =>
        {
            b.AddContent(0, ChildContent?.Invoke(state));
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Task OnAfterRenderAsync(bool firstRender) =>
        _registration!.SyncAsync(_element, new BdndDragSourceOptions(
            _handlerId, _canDrag, UseHandle, UseCustomDragLayer,
            string.IsNullOrWhiteSpace(DropEffect) ? "move" : DropEffect));

    private ValueTask<IJSObjectReference?> RegisterAsync(ElementReference element, BdndDragSourceOptions options)
    {
        _selfRef ??= DotNetObjectReference.Create(this);
        return Interop.RegisterDragSourceAsync(element, _selfRef, options);
    }

    private async Task HandleDragStartAsync(DragEventArgs e)
    {
        var manager = Manager!;
        // The JS registration decides which source a dragstart belongs to (the innermost one,
        // and only from a handle when UseHandle is set) and tags the drag with this type.
        if (e.DataTransfer?.Types is not { } types || types.Contains(BdndInterop.SourceType(_handlerId)) is false)
            return;

        TItem? item = default;
        var canDrag = EffectiveCanDrag;
        if (canDrag)
        {
            item = ItemFactory is not null ? ItemFactory() : Item;
            canDrag = ItemFactory is null || item is not null;
        }
        if (canDrag is false)
        {
            CancelNativeDrag();
            return;
        }

        // In Blazor WebAssembly this handler runs while dragstart is being dispatched, so the
        // source rectangle JS measured for this drag can be read synchronously. Otherwise start
        // from an estimate and correct it once the rectangle has been fetched.
        var rect = ReadDragStartRect();
        var sourceOffset = rect is not null
            ? new BdndXYCoord(rect.Left, rect.Top)
            : new BdndXYCoord(e.ClientX - e.OffsetX, e.ClientY - e.OffsetY);

        _draggingItem = item;
        manager.BeginDrag(_handlerId, item, Type, new BdndXYCoord(e.ClientX, e.ClientY), sourceOffset,
            () => OnBegin.InvokeAsync(item));

        // The browser snapshots the drag image after dragstart has been dispatched, and some
        // browsers abort a drag when the DOM changes during dragstart. Blazor WebAssembly would
        // apply re-renders synchronously, so publish the drag only after dispatch has finished.
        if (OperatingSystem.IsBrowser()) await Task.Delay(1);
        await manager.FlushBeginAsync();

        if (rect is null && _registration!.Reference is { } registration)
        {
            rect = await registration.InvokeAsync<BdndDomRect?>("getDragStartRect");
            if (rect is not null && manager.IsDragging(_handlerId))
                manager.SetInitialSourceClientOffset(new BdndXYCoord(rect.Left, rect.Top));
        }
    }

    // The synchronous read works only in-process (Blazor WebAssembly); elsewhere it answers null.
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "BdndDomRect's public properties and constructor are rooted by BitBdnd.PreserveInteropContracts.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "BdndDomRect is a plain DTO that System.Text.Json reads without runtime code generation.")]
    private BdndDomRect? ReadDragStartRect() =>
        (_registration!.Reference as IJSInProcessObjectReference)?.Invoke<BdndDomRect?>("getDragStartRect");

    // Possible only while dragstart is still being dispatched (Blazor WebAssembly). Elsewhere
    // the native drag goes on as an inert drag: no drop target accepts it.
    private void CancelNativeDrag()
    {
        if (_registration?.Reference is IJSInProcessObjectReference registration)
            registration.InvokeVoid("cancelDrag");
    }

    /// <summary>Invoked from JS with the pointer position while this source is being dragged.</summary>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task HandleDragMove(double clientX, double clientY)
    {
        var manager = Manager!;
        if (manager.IsDragging(_handlerId) is false) return;
        await manager.FlushBeginAsync();
        manager.SetClientOffset(new BdndXYCoord(clientX, clientY));
    }

    /// <summary>Invoked from JS when the drag of this source ends (drop or cancel).</summary>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task HandleDragEnd()
    {
        var manager = Manager!;
        try
        {
            if (manager.IsDragging(_handlerId) is false) return;
            await manager.FlushBeginAsync();
            if (manager.IsDragging(_handlerId) is false) return;

            var context = new BdndDragEndContext<TItem>
            {
                Monitor = manager.Monitor,
                Item = _draggingItem,
                DidDrop = manager.DidDrop(),
                DropResult = manager.GetDropResult(),
            };
            _draggingItem = default;
            var onEnd = Task.CompletedTask;
            try
            {
                if (OnEnd.HasDelegate) onEnd = OnEnd.InvokeAsync(context);
            }
            finally
            {
                // end right after OnEnd's synchronous part, so an async handler does not keep
                // the drag state (and every target's highlight) alive
                manager.EndDrag();
            }
            await onEnd;
        }
        finally
        {
            if (_disposed) await ReleaseInteropAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (Manager is null) return;
        Manager.StateChanged -= OnStateChanged;
        // While this source is being dragged, keep its JS registration and this instance alive:
        // the element-level 'dragend' listener ends the drag even after the element was removed
        // (e.g. by the re-render of a drop), and releases them through HandleDragEnd.
        if (Manager.IsDragging(_handlerId)) return;
        await ReleaseInteropAsync();
    }

    private async ValueTask ReleaseInteropAsync()
    {
        if (_registration is not null) await _registration.DisposeAsync();
        _selfRef?.Dispose();
        _selfRef = null;
    }

    private sealed class BdndHandleController : IBdndDragHandleController
    {
        private readonly BdndDraggable<TItem> _owner;
        public BdndHandleController(BdndDraggable<TItem> owner) => _owner = owner;
        public bool RequiresHandle => _owner.UseHandle;
        public bool CanDrag => _owner._canDrag;
    }
}
