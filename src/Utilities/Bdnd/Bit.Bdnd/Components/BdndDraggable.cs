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
public sealed class BdndDraggable<TItem> : ComponentBase, IHandleEvent, IAsyncDisposable, IBdndDragSourceHandler
{
    [CascadingParameter] private BdndDragDropManager? Manager { get; set; }
    [CascadingParameter] private BdndDropTargetScope? TargetScope { get; set; }
    [Inject] private IServiceProvider Services { get; set; } = default!;

    // Resolved rather than injected, so the draggable also works when the app never called AddBitBdndServices.
    private BdndInterop Interop => field ??= Services.GetBdndInterop();

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
    /// Whether the keyboard can drag the source: Space or Enter on it (or on its <see cref="BdndDragHandle"/>) picks
    /// it up, the arrow keys move it between the drop targets that can take it, Space or Enter drops it and Escape
    /// cancels. Defaults to true, which makes the source (or its handle) focusable while it can drag. A host that is
    /// interactive by itself (<see cref="Tag"/> <c>a</c>, <c>button</c>, <c>input</c>, <c>select</c>,
    /// <c>textarea</c>, <c>summary</c>) keeps its own keys: give it a <see cref="BdndDragHandle"/> to drag it with
    /// the keyboard. <see cref="BdndProvider.AllowKeyboard"/> turns keyboard dragging off for a whole provider.
    /// </summary>
    [Parameter] public bool AllowKeyboard { get; set; } = true;

    /// <summary>
    /// Which pointers drag the source through Bdnd's pointer-driven drag instead of the browser's native drag and
    /// drop: <see cref="BdndPointerDrag.Touch"/> (the default) for touch and pen, which most touch browsers never
    /// turn into native drags; <see cref="BdndPointerDrag.All"/> for the mouse too; <see cref="BdndPointerDrag.None"/>
    /// for the native drag and drop alone. A pointer drag shows a copy of the source following the pointer (unless
    /// <see cref="UseCustomDragLayer"/> is set, when a <see cref="BdndDragLayer{TItem}"/> draws the preview), and
    /// scrolls the containers it nears the edge of (<see cref="AutoScroll"/>).
    /// </summary>
    [Parameter] public BdndPointerDrag PointerDrag { get; set; } = BdndPointerDrag.Touch;

    /// <summary>
    /// How long, in milliseconds, a touch (or pen) has to rest on the source before a pointer drag starts, so that a
    /// swipe across it still scrolls the page; moving more than <see cref="DragThreshold"/> before then gives the
    /// press up. A <see cref="BdndDragHandle"/> starts at once, without the delay: it does not scroll
    /// (<c>touch-action: none</c>). Defaults to 250.
    /// </summary>
    [Parameter] public int TouchDelay { get; set; } = 250;

    /// <summary>
    /// How far, in CSS pixels, a pointer has to move with the button down before a pointer drag starts, so that a
    /// click is not taken for a drag. Defaults to 5.
    /// </summary>
    [Parameter] public double DragThreshold { get; set; } = 5;

    /// <summary>
    /// Whether a pointer drag scrolls the scroll container (or the page) under the pointer while the pointer is near
    /// its edge. Native drags scroll as the browser does. Defaults to true.
    /// </summary>
    [Parameter] public bool AutoScroll { get; set; } = true;

    /// <summary>
    /// Keeps the copy that follows the pointer in a pointer drag on one axis: <see cref="BdndAxis.Vertical"/> moves it
    /// only up and down (a vertical list), <see cref="BdndAxis.Horizontal"/> only sideways. Where the drop lands is
    /// still decided by the pointer. A native mouse drag shows the browser's image, which no page can hold to an
    /// axis; a <see cref="BdndDragLayer{TItem}"/> keeps its own preview with <c>SourceClientOffsetOn</c>. Defaults to
    /// <see cref="BdndAxis.None"/>.
    /// </summary>
    [Parameter] public BdndAxis PreviewAxis { get; set; }

    /// <summary>
    /// Data a native drag of the source carries for destinations outside the page, by <c>DataTransfer</c> type - say
    /// <c>text/plain</c> for a text field or another application, <c>text/uri-list</c> for a browser tab, or a type of
    /// your own for another window of your app. It is set at <c>dragstart</c>; pointer and keyboard drags never leave
    /// the page and carry none. Drop targets of this page still treat the drag as the in-app drag it is.
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, string>? NativeData { get; set; }

    /// <summary>
    /// The source's name for screen readers, read out as a keyboard drag picks it up, moves and drops it ("Picked
    /// up Write the docs"); a <see cref="BdndDragHandle"/> without an <c>aria-label</c> of its own is labelled with
    /// it too. Without it the source's <c>aria-label</c> or text is read.
    /// </summary>
    [Parameter] public string? Label { get; set; }

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
    private Collected _collected;
    private bool _canDrag;
    private string _keyboardLabel = "";
    private string? _keyboardTargetLabel;
    private BdndEdge? _keyboardTargetEdge;
    private bool? _keyboardTargetBefore;
    private Dictionary<string, string>? _nativeData;
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

    // whether the mouse drags through the browser's native drag and drop
    private bool NativeDrag => PointerDrag != BdndPointerDrag.All;

    // the keyboard may drag the source: the source and its provider allow it
    private bool KeyboardAllowed => AllowKeyboard && Manager!.AllowKeyboard;

    // the keyboard can pick the source up (from the host, or from a handle)
    private bool KeyboardEnabled => KeyboardAllowed && _canDrag && (UseHandle || IsInteractiveTag(Tag) is false);

    // hosts whose Space and Enter already do something (follow a link, click, type, open)
    private static bool IsInteractiveTag(string? tag) => tag?.ToLowerInvariant() is "a" or "button" or "input" or "select" or "textarea" or "summary";

    // Like React DnD's collect: a drag state change re-renders only the draggables whose collected
    // state it changed - and every draggable when a drag begins or ends - not each one on every hover.
    private void OnStateChanged()
    {
        // a callback run inline in the manager's notification may have removed this draggable already
        if (_disposed) return;
        if (Collect() != _collected) InvokeAsync(StateHasChanged);
    }

    // Drag events must not trigger the automatic re-render ComponentBase performs after every
    // event (e.g. while dragstart is still being dispatched); the manager drives re-renders.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);

    private Collected Collect()
    {
        var monitor = Manager!.Monitor;
        return new(
            monitor.IsDragging(),
            IsDraggingOverride?.Invoke(monitor) ?? monitor.IsDragging(_handlerId),
            EffectiveCanDrag,
            Manager.InstructionsId,
            Manager.AllowKeyboard);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var manager = Manager!;
        var monitor = manager.Monitor;
        _collected = Collect();
        _canDrag = _collected.CanDrag;
        var state = new BdndDragState
        {
            Monitor = monitor,
            IsDragging = _collected.IsDragging,
            CanDrag = _canDrag,
        };

        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        // with UseHandle the BdndDragHandle element is the (native) draggable one; with every pointer dragging
        // through pointer events, nothing is natively draggable
        builder.AddAttribute(2, "draggable", _canDrag && UseHandle is false && NativeDrag ? "true" : "false");
        if (KeyboardEnabled && UseHandle is false)
        {
            // the host is what the keyboard picks up: focusable, and described by the instructions
            if (BdndAttributes.Has(AdditionalAttributes, "tabindex") is false) builder.AddAttribute(3, "tabindex", "0");
            builder.AddAttribute(4, "aria-describedby", BdndAttributes.DescribedBy(AdditionalAttributes, manager.InstructionsId));
        }
        // a stable styling hook: a stylesheet can follow the drag without the content rendering classes for it
        builder.AddAttribute(5, "data-bit-bdnd-dragging", _collected.IsDragging);
        builder.AddAttribute(6, "ondragstart",
            EventCallback.Factory.Create<DragEventArgs>(this, HandleDragStartAsync));
        // stop nested (ancestor) draggables from also starting a drag
        builder.AddEventStopPropagationAttribute(7, "ondragstart", true);
        builder.AddElementReferenceCapture(8, el => _element = el);
        // expose handle wiring to descendant BdndDragHandle components; not fixed, so
        // handles re-render when CanDrag/UseHandle change
        builder.OpenComponent<CascadingValue<IBdndDragHandleController>>(9);
        builder.AddAttribute(10, "Value", _handleController);
        builder.AddAttribute(11, "ChildContent", (RenderFragment)(b =>
        {
            b.AddContent(0, ChildContent?.Invoke(state));
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await SyncRegistrationAsync();
        await AdoptFocusAsync();
    }

    // Pointer moves are reported only while something follows them (a drag layer drawing this source's type): on
    // Blazor Server each one is a message over the circuit.
    private Task SyncRegistrationAsync()
    {
        var manager = Manager!;
        var keyboard = KeyboardEnabled;
        return _registration!.SyncAsync(_element, new BdndDragSourceOptions
        {
            Id = _handlerId,
            CanDrag = _canDrag,
            UseHandle = UseHandle,
            UseEmptyPreview = UseCustomDragLayer,
            EffectAllowed = string.IsNullOrWhiteSpace(DropEffect) ? "move" : DropEffect,
            TrackPointer = manager.FollowsPointer(Type),
            Keyboard = keyboard,
            InstructionsId = keyboard ? manager.InstructionsId : null,
            Instructions = keyboard ? manager.Announcements.Instructions : null,
            PointerDrag = PointerDrag switch
            {
                BdndPointerDrag.All => "all",
                BdndPointerDrag.None => "none",
                _ => "touch",
            },
            TouchDelay = Math.Max(0, TouchDelay),
            DragThreshold = Math.Max(0, DragThreshold),
            AutoScroll = AutoScroll,
            PreviewAxis = PreviewAxis switch
            {
                BdndAxis.Horizontal => "x",
                BdndAxis.Vertical => "y",
                _ => null,
            },
            NativeData = NativeDataOption(),
        });
    }

    // the same instance while the data is the same, so unchanged options compare equal and are not pushed again
    private Dictionary<string, string>? NativeDataOption()
    {
        if (NativeData is not { Count: > 0 } data)
        {
            _nativeData = null;
            return null;
        }
        if (_nativeData is null || _nativeData.Count != data.Count
            || data.Any(pair => _nativeData.TryGetValue(pair.Key, out var value) is false || value != pair.Value))
        {
            _nativeData = new Dictionary<string, string>(data);
        }
        return _nativeData;
    }

    private ValueTask<IJSObjectReference?> RegisterAsync(ElementReference element, BdndDragSourceOptions options)
    {
        _selfRef ??= DotNetObjectReference.Create(this);
        return Interop.RegisterDragSourceAsync(element, _selfRef, options);
    }

    // Produces the item a drag starting now carries; false when the drag may not start.
    private bool TryProduceItem(out TItem? item)
    {
        item = default;
        if (EffectiveCanDrag is false) return false;
        item = ItemFactory is not null ? ItemFactory() : Item;
        return ItemFactory is null || item is not null;
    }

    // TryProduceItem for a drag the module starts from JS: an ItemFactory or CanDragPredicate that throws refuses the
    // drag and is reported, as it would be from a managed dragstart.
    private async Task<(bool Ok, TItem? Item)> TryProduceItemGuardedAsync()
    {
        try
        {
            return TryProduceItem(out var item) ? (true, item) : (false, default);
        }
        catch (Exception exception)
        {
            await ReportAsync(exception);
            return (false, default);
        }
    }

    private async Task HandleDragStartAsync(DragEventArgs e)
    {
        var manager = Manager!;
        // The JS registration decides which source a dragstart belongs to (the innermost one,
        // and only from a handle when UseHandle is set) and tags the drag with this type.
        if (e.DataTransfer?.Types is not { } types || types.Contains(BdndInterop.SourceType(_handlerId)) is false)
            return;

        if (TryProduceItem(out var item) is false)
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

        manager.BeginDrag(_handlerId, item, Type, new BdndXYCoord(e.ClientX, e.ClientY), sourceOffset,
            () => OnBegin.InvokeAsync(item), source: this);

        // The browser snapshots the drag image after dragstart has been dispatched, and some
        // browsers abort a drag when the DOM changes during dragstart. Blazor WebAssembly would
        // apply re-renders synchronously, so publish the drag only after dispatch has finished.
        if (OperatingSystem.IsBrowser()) await Task.Delay(1);
        await manager.FlushBeginAsync();
        // a drag layer added since the last render needs the pointer moves of this drag
        var sync = SyncRegistrationAsync();

        if (rect is null && _registration!.Reference is { } registration)
        {
            rect = await registration.InvokeAsync<BdndDomRect?>("getDragStartRect");
            if (rect is not null && manager.IsDragging(_handlerId))
                manager.SetInitialSourceClientOffset(new BdndXYCoord(rect.Left, rect.Top));
        }
        await sync;
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
        await GuardAsync(manager.FlushBeginAsync());
        manager.SetClientOffset(new BdndXYCoord(clientX, clientY));
    }

    /// <summary>Invoked from JS when the drag of this source ends (drop or cancel).</summary>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task HandleDragEnd()
    {
        try
        {
            var manager = Manager!;
            if (manager.IsDragging(_handlerId) is false) return;
            await EndAsync(manager.DragId);
        }
        finally
        {
            if (_disposed) await ReleaseInteropAsync();
        }
    }

    // Ends drag dragId of this source: OnEnd runs with the drop's outcome - once the async OnDrop handlers of the
    // drop are done - and the drag state is cleared right after OnEnd's synchronous part, so an async handler does
    // not keep the drag (and every target's highlight) alive. Runs from JS-invoked code, so the application's
    // exceptions are handed to the renderer instead of being lost with the module's promise. The task completes once
    // the drag is over, not once an async OnEnd is: the module waits for it before it starts the next drag, which an
    // OnEnd saving something (or asking the user) must not hold up; the rest of OnEnd runs on, guarded.
    private async Task EndAsync(long dragId)
    {
        var manager = Manager!;
        await GuardAsync(manager.FlushBeginAsync());
        // OnEnd gets the results async OnDrop handlers publish after an await
        await manager.WhenDropsCompletedAsync();
        if (manager.IsDragging(_handlerId) is false || manager.DragId != dragId) return;

        var onEnd = StartOnEnd();
        if (onEnd.IsCompleted) await GuardAsync(onEnd);
        else _ = GuardAsync(onEnd);
    }

    // Ends the drag in progress, which the caller made sure is this source's: OnEnd runs with what the drag has - its
    // item, and the drops recorded so far - and the drag state is cleared right after OnEnd's synchronous part. The
    // item is the manager's, not one this component keeps: the next drag of this source, beginning, ends this one
    // through AbortAsync, by when such a field would hold that drag's item already. The task is OnEnd's.
    private Task StartOnEnd()
    {
        var manager = Manager!;
        var context = new BdndDragEndContext<TItem>
        {
            Monitor = manager.Monitor,
            Item = manager.GetItem<TItem>(),
            DidDrop = manager.DidDrop(),
            DropResult = manager.GetDropResult(),
        };
        try
        {
            return OnEnd.HasDelegate ? OnEnd.InvokeAsync(context) : Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
        finally
        {
            manager.EndDrag();
        }
    }

    private async Task GuardAsync(Task callback)
    {
        try
        {
            await callback;
        }
        catch (Exception exception)
        {
            await ReportAsync(exception);
        }
    }

    // Hands an application callback's exception to the renderer - through this component, or, once it is disposed
    // (a drop re-rendered it away while its OnEnd ran), through its provider.
    private Task ReportAsync(Exception exception) =>
        Manager!.ReportExceptionAsync(exception, _disposed ? null : DispatchExceptionAsync);

    // Ends drag dragId at once, for the manager (a new drag began while this one was in progress), without waiting for
    // the async OnDrop handlers of its drop.
    Task IBdndDragSourceHandler.AbortAsync(long dragId)
    {
        var manager = Manager!;
        if (manager.IsDragging(_handlerId) is false || manager.DragId != dragId) return Task.CompletedTask;
        // what the drag has so far: a drop already recorded counts, a result an async OnDrop has yet to publish does
        // not - the same answer the manager's DragEnded gives
        return CompleteAbortAsync(StartOnEnd());
    }

    private async Task CompleteAbortAsync(Task onEnd)
    {
        await GuardAsync(onEnd);
        if (_disposed) await ReleaseInteropAsync();
    }

    // ---- pointer drags -----------------------------------------------------------------------
    // Touch and pen (and with PointerDrag="All" the mouse) raise no native drag events on most browsers, so the module
    // follows them through pointer events: it starts the drag once the press qualifies, moves a preview, hit-tests
    // the targets under the pointer and reports hover changes, OnHover moves and the drop to them, and the end here.

    /// <summary>Invoked from JS when a pointer drag of this source starts, with the press point and the source's rectangle.</summary>
    /// <returns>Whether the drag began.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BdndDomRect))]
    public async Task<bool> HandlePointerDragStart(double clientX, double clientY, BdndDomRect rect)
    {
        var manager = Manager!;
        if (_disposed || _canDrag is false || PointerDrag == BdndPointerDrag.None || await TryProduceItemGuardedAsync() is not (true, var item))
            return false;

        manager.BeginDrag(_handlerId, item, Type, new BdndXYCoord(clientX, clientY), new BdndXYCoord(rect.Left, rect.Top),
            () => OnBegin.InvokeAsync(item), BdndDragInput.Pointer, this);
        var dragId = manager.DragId;
        try
        {
            await GuardAsync(manager.FlushBeginAsync());
            // a drag layer added since the last render needs the pointer moves of this drag
            await SyncRegistrationAsync();
            return manager.IsDragging(_handlerId) && manager.DragId == dragId;
        }
        catch (Exception exception)
        {
            if (manager.IsDragging(_handlerId) && manager.DragId == dragId) await manager.AbortDragAsync();
            await ReportAsync(exception);
            return false;
        }
    }

    // ---- keyboard drags ----------------------------------------------------------------------
    // The module listens for Space/Enter on the source (or its handle) and, once the drag has begun, for the keys
    // that move, drop and cancel it - sent here one at a time, in order. The drag is the manager's like any other:
    // the same hover state, the same OnHover/OnDrop chain innermost first, the same OnEnd.

    /// <summary>Invoked from JS when Space or Enter is pressed on the source (or its handle).</summary>
    /// <returns>Whether the keyboard drag began.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BdndDomRect))]
    public async Task<bool> HandleKeyboardDragStart(BdndDomRect rect, string? text)
    {
        var manager = Manager!;
        if (_disposed || KeyboardEnabled is false || await TryProduceItemGuardedAsync() is not (true, var item)) return false;

        _keyboardTargetLabel = null;
        _keyboardTargetEdge = null;
        _keyboardTargetBefore = null;
        _keyboardLabel = Label is { Length: > 0 } label ? label
            : string.IsNullOrWhiteSpace(text) ? manager.Announcements.UnnamedSource
            : text.Trim();
        // the "pointer" of a keyboard drag is the middle of what it is over: the source to begin with
        manager.BeginDrag(_handlerId, item, Type,
            new BdndXYCoord(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2), new BdndXYCoord(rect.Left, rect.Top),
            () => OnBegin.InvokeAsync(item), BdndDragInput.Keyboard, this);
        var dragId = manager.DragId;
        manager.RequestFocus(item, Type, _handlerId, ended: false);
        try
        {
            await GuardAsync(manager.FlushBeginAsync());
            if (IsKeyboardDrag(dragId) is false) return false;

            // no place at all: nothing the item can go to, not every target there is
            var place = await NavigateAsync("stay") ?? new BdndKeyboardTarget();
            await AnnounceAsync(manager.Announcements.DragStarted, null, place);
            return IsKeyboardDrag(dragId);
        }
        catch (Exception exception)
        {
            await FailKeyboardDragAsync(dragId, exception);
            return false;
        }
    }

    // Whether keyboard drag dragId of this source is still the drag in progress: a pointer may have started another
    // one while a key was being handled.
    private bool IsKeyboardDrag(long dragId)
    {
        var manager = Manager!;
        return manager.IsDragging(_handlerId) && manager.DragId == dragId && manager.GetDragInput() == BdndDragInput.Keyboard;
    }

    // A keyboard call failed (an application predicate or callback threw on a path with no event dispatch): the
    // drag must not stay in progress in .NET while the module has ended it, so it ends as a cancel.
    private async Task FailKeyboardDragAsync(long dragId, Exception exception)
    {
        var manager = Manager!;
        if (IsKeyboardDrag(dragId)) await manager.AbortDragAsync();
        await ReportAsync(exception);
    }

    /// <summary>
    /// Invoked from JS with a key of a keyboard drag of this source: <c>next</c>, <c>previous</c>, <c>down</c>,
    /// <c>up</c> (by row in a layout that wraps), <c>first</c>, <c>last</c>, <c>drop</c> or <c>cancel</c>.
    /// </summary>
    /// <returns>Whether the keyboard drag goes on.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task<bool> HandleKeyboardCommand(string command)
    {
        var manager = Manager!;
        var dragId = manager.DragId;
        try
        {
            if (IsKeyboardDrag(dragId) is false) return false;
            await GuardAsync(manager.FlushBeginAsync());

            switch (command)
            {
                case "next" or "previous" or "down" or "up" or "first" or "last":
                    await MoveKeyboardDragAsync(dragId, command);
                    break;
                case "drop":
                    await DropKeyboardDragAsync(dragId);
                    break;
                case "cancel":
                    if (IsKeyboardDrag(dragId)) await EndKeyboardDragAsync(dragId, manager.Announcements.Cancelled, null);
                    break;
            }
            return IsKeyboardDrag(dragId);
        }
        catch (Exception exception)
        {
            await FailKeyboardDragAsync(dragId, exception);
            return false;
        }
        finally
        {
            if (_disposed && manager.IsDragging(_handlerId) is false) await ReleaseInteropAsync();
        }
    }

    // How often, and how long apart, a keyboard move waits for the rows a virtualized list is rendering that way.
    private const int KeyboardRowAttempts = 20;
    private static readonly TimeSpan KeyboardRowWait = TimeSpan.FromMilliseconds(100);

    private async Task MoveKeyboardDragAsync(long dragId, string command)
    {
        var manager = Manager!;
        var place = await NavigateAsync(command);
        if (IsKeyboardDrag(dragId) is false) return;
        // The list goes on that way with rows it has not rendered, which the module scrolled into view: they take a
        // render and a registration to become targets (a virtualized list renders them as it scrolls).
        for (var attempt = 0; place is { Moved: false, Pending: true } && attempt < KeyboardRowAttempts; attempt++)
        {
            await Task.Delay(KeyboardRowWait);
            if (IsKeyboardDrag(dragId) is false) return;
            place = await NavigateAsync(command, waiting: true);
            if (IsKeyboardDrag(dragId) is false) return;
        }

        if (place is null)
        {
            manager.SetKeyboardTarget(null);
            _keyboardTargetLabel = null;
            _keyboardTargetEdge = null;
            _keyboardTargetBefore = null;
            await AnnounceAsync(manager.Announcements.DraggedOver, null, null);
            return;
        }
        if (place.Id.Length == 0)
        {
            // over no target yet, and nothing that way
            await AnnounceAsync(manager.Announcements.NoFurtherTarget, null, place);
            return;
        }
        var label = TargetLabel(place);
        var edge = BdndEdgeNames.Parse(place.Edge);
        var before = edge is null ? null : place.Before;
        if (place.Moved is false)
        {
            await AnnounceAsync(manager.Announcements.NoFurtherTarget, label, place, edge: edge, before: before);
            return;
        }

        // the "pointer" of a keyboard drag: the middle of the target - or of its half next to the edge
        var rect = place.Rect;
        var pointer = edge switch
        {
            BdndEdge.Top => new BdndXYCoord(rect.Left + rect.Width / 2, rect.Top + rect.Height / 4),
            BdndEdge.Bottom => new BdndXYCoord(rect.Left + rect.Width / 2, rect.Top + rect.Height * 3 / 4),
            BdndEdge.Left => new BdndXYCoord(rect.Left + rect.Width / 4, rect.Top + rect.Height / 2),
            BdndEdge.Right => new BdndXYCoord(rect.Left + rect.Width * 3 / 4, rect.Top + rect.Height / 2),
            _ => new BdndXYCoord(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2),
        };
        _keyboardTargetLabel = label;
        _keyboardTargetEdge = edge;
        _keyboardTargetBefore = before;
        manager.SetKeyboardTarget(place.Id, edge);
        manager.SetClientOffset(pointer);
        await manager.HoverChainAsync(place.Id, pointer);
        if (IsKeyboardDrag(dragId)) await AnnounceAsync(manager.Announcements.DraggedOver, label, place, edge: edge, before: before);
    }

    private async Task DropKeyboardDragAsync(long dragId)
    {
        var manager = Manager!;
        if (IsKeyboardDrag(dragId) is false) return;
        if (manager.KeyboardTargetId is not { } targetId || manager.GetTarget(targetId) is null)
        {
            await EndKeyboardDragAsync(dragId, manager.Announcements.Cancelled, null);
            return;
        }

        var label = _keyboardTargetLabel;
        var edge = _keyboardTargetEdge;
        var before = _keyboardTargetBefore;
        await manager.DropChainAsync(targetId, manager.GetClientOffset());
        if (IsKeyboardDrag(dragId) is false) return;
        var dropped = manager.DidDrop();
        await EndKeyboardDragAsync(dragId, dropped ? manager.Announcements.Dropped : manager.Announcements.Cancelled,
            dropped ? label : null, dropped ? edge : null, dropped ? before : null);
    }

    private async Task EndKeyboardDragAsync(long dragId, Func<BdndAnnouncement, string?> announcement, string? targetLabel,
        BdndEdge? edge = null, bool? before = null)
    {
        var manager = Manager!;
        var item = manager.GetItem();
        var type = Type;
        await EndAsync(dragId);
        // a drop that re-rendered the item as another draggable hands that one the focus
        manager.RequestFocus(item, type, _handlerId, ended: true);
        await AnnounceAsync(announcement, targetLabel, null, item, type, edge, before);
    }

    // Asks the module for the target a keyboard command lands on, among the targets the item can be dropped on.
    private async Task<BdndKeyboardTarget?> NavigateAsync(string command, bool waiting = false)
    {
        var manager = Manager!;
        if (_registration?.Reference is not { } registration) return null;
        var candidates = manager.GetDroppableTargets()
            .Select(t => new BdndKeyboardCandidate { Id = t.Id, ParentId = t.ParentId, Edges = (int)t.Edges })
            .ToArray();
        // the targets around this source, by which the module tells the place the item already has
        var sourceTargets = TargetScope?.FindTargetId(manager) is { } enclosing ? manager.ChainOf(enclosing).ToArray() : [];
        var currentId = manager.KeyboardTargetId;
        var currentEdge = currentId is null ? null : BdndEdgeNames.Name(manager.GetTargetEdge(currentId));
        try
        {
            return await Interop.KeyboardNavigateAsync(registration, candidates, sourceTargets, currentId, currentEdge, command, waiting);
        }
        catch (Exception exception) when (exception is JSException or JSDisconnectedException or TaskCanceledException)
        {
            return null;
        }
    }

    private string TargetLabel(BdndKeyboardTarget place)
    {
        var manager = Manager!;
        return manager.GetTarget(place.Id)?.Label is { Length: > 0 } label ? label
            : string.IsNullOrWhiteSpace(place.Label) is false ? place.Label.Trim()
            : manager.Announcements.UnnamedTarget(place.Position, place.Count);
    }

    private async Task AnnounceAsync(Func<BdndAnnouncement, string?> announcement, string? targetLabel, BdndKeyboardTarget? place,
        object? item = null, string? type = null, BdndEdge? edge = null, bool? before = null)
    {
        var manager = Manager!;
        string? text;
        try
        {
            text = announcement(new BdndAnnouncement
            {
                SourceLabel = _keyboardLabel,
                TargetLabel = targetLabel,
                Edge = targetLabel is null ? null : edge,
                Before = targetLabel is null ? null : before ?? edge switch
                {
                    BdndEdge.Top or BdndEdge.Left => true,
                    BdndEdge.Bottom or BdndEdge.Right => false,
                    _ => null,
                },
                TargetPosition = place is { Id.Length: > 0 } ? place.Position : 0,
                TargetCount = place?.Count ?? manager.GetDroppableTargets().Count,
                ItemType = type ?? manager.GetItemType() ?? Type,
                Item = item ?? manager.GetItem(),
            });
        }
        catch (Exception exception)
        {
            await ReportAsync(exception);
            return;
        }
        if (string.IsNullOrEmpty(text)) return;
        try
        {
            await Interop.AnnounceAsync(text);
        }
        catch (Exception exception) when (exception is JSException or JSDisconnectedException or TaskCanceledException) { }
    }

    // After a keyboard drag re-rendered its item as this draggable (moved it to another list, for one), this one
    // takes the focus over from the source whose element is gone.
    private async Task AdoptFocusAsync()
    {
        var manager = Manager!;
        if (KeyboardAllowed is false || _registration?.Reference is not { } registration || manager.WantsFocus(Item, Type, _handlerId) is false)
            return;
        try
        {
            if (await registration.InvokeAsync<bool>("adoptFocus")) manager.FocusTaken(_handlerId);
        }
        catch (Exception exception) when (exception is JSException or JSDisconnectedException or TaskCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (Manager is null) return;
        Manager.StateChanged -= OnStateChanged;
        // While this source is being dragged, keep its JS registration and this instance alive:
        // the element-level 'dragend' listener ends the drag even after the element was removed
        // (e.g. by the re-render of a drop), and releases them through HandleDragEnd; a keyboard
        // drag goes on through HandleKeyboardCommand, which releases them once it ends.
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
        public bool KeyboardEnabled => _owner.KeyboardEnabled;
        public string? InstructionsId => _owner.Manager?.InstructionsId;
        public string? Label => _owner.Label;
        public bool IsDragging => _owner._collected.IsDragging;
        public bool NativeDrag => _owner.NativeDrag;
        public bool PointerDragEnabled => _owner.PointerDrag != BdndPointerDrag.None;
    }

    /// <summary>
    /// The state a render hands the content, besides the monitor - and the instructions it is described by, and whether
    /// its provider lets the keyboard drag.
    /// </summary>
    private readonly record struct Collected(bool AnyDragging, bool IsDragging, bool CanDrag, string InstructionsId, bool KeyboardAllowed);
}
