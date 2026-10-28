using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Bdnd;

/// <summary>
/// Wires its host element into the DnD system as a drop target (React DnD's <c>useDrop</c>).
/// Besides in-app drags it can also accept native OS drags (files, text, urls, html) by
/// listing one of the <see cref="BdndNativeTypes"/> in <see cref="Accept"/>/<see cref="AcceptTypes"/>.
/// </summary>
/// <typeparam name="TItem">The expected dragged item type.</typeparam>
public sealed class BdndDropTarget<TItem> : ComponentBase, IHandleEvent, IAsyncDisposable
{
    [CascadingParameter] private BdndDragDropManager? Manager { get; set; }
    [CascadingParameter] private BdndDropTargetScope? ParentScope { get; set; }
    [Inject] private IServiceProvider Services { get; set; } = default!;

    // Resolved rather than injected, so the drop target also works when the app never called AddBitBdndServices.
    private BdndInterop Interop => field ??= Services.GetBdndInterop();

    /// <summary>A single accepted item type.</summary>
    [Parameter] public string? Accept { get; set; }

    /// <summary>Multiple accepted item types (combined with <see cref="Accept"/>).</summary>
    [Parameter] public IEnumerable<string>? AcceptTypes { get; set; }

    /// <summary>
    /// Called when a compatible in-app item is dropped. Set <c>args.Result</c> to publish a result.
    /// Nested targets are called innermost first; <c>args.Monitor.DidDrop()</c> tells whether an inner
    /// target already handled the drop.
    /// </summary>
    [Parameter] public EventCallback<BdndDropEventArgs<TItem>> OnDrop { get; set; }

    /// <summary>
    /// Called repeatedly while an in-app item of an accepted type is hovered - like React DnD's <c>hover</c>,
    /// also when <see cref="CanDropPredicate"/> refuses the item.
    /// </summary>
    [Parameter] public EventCallback<BdndDropEventArgs<TItem>> OnHover { get; set; }

    /// <summary>
    /// Called when an in-app drag of an accepted type moves over the target (or a target nested in it): its
    /// <c>IsOver</c> turns true. For native drags, follow <c>IsOver</c> in the state.
    /// </summary>
    [Parameter] public EventCallback<BdndDropEventArgs<TItem>> OnEnter { get; set; }

    /// <summary>
    /// Called when an in-app drag that was over the target moves off it while the drag goes on. A drag that ends over
    /// the target - dropped or cancelled - does not leave it: <c>OnDrop</c> or the source's <c>OnEnd</c> tells the end.
    /// </summary>
    [Parameter] public EventCallback<BdndDropEventArgs<TItem>> OnLeave { get; set; }

    /// <summary>
    /// Takes the target out of every drag: nothing is accepted - no hover, no drop, no native drop - and the keyboard
    /// skips it. Defaults to false.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Called when a native OS drag (file/text/url/html) is dropped.</summary>
    [Parameter] public EventCallback<BdndNativeDropEventArgs> OnNativeDrop { get; set; }

    /// <summary>
    /// The file types a native file drop is limited to, as an <c>&lt;input type="file" accept&gt;</c> lists them: MIME
    /// types (<c>application/pdf</c>), MIME groups (<c>image/*</c>) and extensions (<c>.png</c>). While files are dragged
    /// over the target the browser reports their MIME types (most browsers do), and a drag carrying none of them is
    /// refused there; on drop, the files of other types are left out of <see cref="BdndNativeDropEventArgs.Files"/>.
    /// </summary>
    [Parameter] public IEnumerable<string>? AcceptFileTypes { get; set; }

    /// <summary>
    /// Whether a dropped folder is expanded into the files inside it, recursively, each with its
    /// <see cref="BdndNativeFile.RelativePath"/>. Off, a folder arrives as one <see cref="BdndNativeFile"/> with
    /// <see cref="BdndNativeFile.IsDirectory"/> set. Defaults to false.
    /// </summary>
    [Parameter] public bool ExpandDirectories { get; set; }

    /// <summary>
    /// The most files a native drop hands over (the first ones, in the order the browser lists them);
    /// <see cref="BdndNativeDropEventArgs.TotalFileCount"/> tells how many there were. 0, the default, for no limit.
    /// </summary>
    [Parameter] public int MaxFiles { get; set; }

    /// <summary>Predicate gating whether this target can accept the current in-app item.</summary>
    [Parameter] public Func<TItem?, IBdndMonitor, bool>? CanDropPredicate { get; set; }

    /// <summary>
    /// The edges of the target a drag is placed against: while a drag is over the target, the edge it is closest to
    /// is reported as <see cref="BdndDropState{TItem}.ClosestEdge"/> (and <c>data-bit-bdnd-edge</c> on the host), and a
    /// drop carries it as <see cref="BdndDropEventArgs{TItem}.ClosestEdge"/> - before or after the item the target
    /// shows. <see cref="BdndEdges.Vertical"/> for the items of a vertical list, <see cref="BdndEdges.Horizontal"/> for
    /// a horizontal one. A keyboard drag visits both places, before and after. Defaults to none.
    /// </summary>
    [Parameter] public BdndEdges Edges { get; set; }

    /// <summary>
    /// The target's name for screen readers, read out as a keyboard drag moves onto it or drops on it ("Card is over
    /// Done"). Without it the target's <c>aria-label</c> or <c>title</c> is used, else its place among the targets
    /// ("drop target 2 of 3").
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Host element tag name. Defaults to <c>div</c>.</summary>
    [Parameter] public string Tag { get; set; } = "div";

    /// <summary>The drop target's content; it receives the collected <see cref="BdndDropState{TItem}"/>.</summary>
    [Parameter] public RenderFragment<BdndDropState<TItem>>? ChildContent { get; set; }

    /// <summary>Any other attribute (class, style, id...), applied to the host element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private readonly string _handlerId;
    private BdndDropTargetScope _scope = default!;
    private string[] _accept = [];
    private (string Token, string NativeType)[] _nativeAccepts = [];
    private string[] _nativeTokens = [];
    private string[] _fileTypes = [];
    private BdndNativeDropFiles? _lastDrop;
    private int _nativeDropSerial; // counts the native drops, so that one still reading its files knows it is outdated
    private BdndEdge? _nativeLeaveEdge; // the edge a native drop would have carried when the last native hover ended
    private ElementReference _element;
    private BdndJsRegistration<BdndDropTargetOptions>? _registration;
    private DotNetObjectReference<BdndDropTarget<TItem>>? _selfRef;
    private readonly Handler _handler;
    private Collected _collected;
    private bool _canDropInApp;
    private bool _disposed;

    public BdndDropTarget()
    {
        _handlerId = Guid.NewGuid().ToString("N");
        _handler = new Handler(this);
    }

    protected override void OnInitialized()
    {
        if (Manager is null)
            throw new InvalidOperationException(
                $"{nameof(BdndDropTarget<TItem>)} must be placed inside a {nameof(BdndProvider)}.");
        _scope = new BdndDropTargetScope(Manager, _handlerId, ParentScope);
        _registration = new(RegisterAsync);
        Manager.StateChanged += OnStateChanged;
    }

    protected override void OnParametersSet()
    {
        _accept = Disabled ? [] : BdndAcceptTypes.Of(Accept, AcceptTypes);
        _nativeAccepts = _accept
            .Where(BdndNativeTypes.IsNative)
            .Select(t => (BdndNativeTypes.ToDataTransferType(t), t))
            .ToArray();
        var tokens = _nativeAccepts.Select(na => na.Token).ToArray();
        if (tokens.SequenceEqual(_nativeTokens) is false) _nativeTokens = tokens;
        var fileTypes = AcceptFileTypes?.Where(t => string.IsNullOrWhiteSpace(t) is false).Select(t => t.Trim()).ToArray() ?? [];
        if (fileTypes.SequenceEqual(_fileTypes) is false) _fileTypes = fileTypes;

        var predicate = CanDropPredicate;
        Manager!.RegisterTarget(new BdndDropTargetEntry
        {
            Id = _handlerId,
            ParentId = ParentScope?.FindTargetId(Manager),
            Accept = _accept,
            CanDrop = predicate is null
                ? null
                : (item, monitor) => predicate(Cast(item), monitor),
            Label = Label,
            Handler = _handler,
            Edges = Edges,
            Changed = OnStateChanged,
        });
    }

    protected override Task OnAfterRenderAsync(bool firstRender) =>
        _registration!.SyncAsync(_element, new BdndDropTargetOptions
        {
            Id = _handlerId,
            CanDrop = _canDropInApp,
            Hover = OnHover.HasDelegate && Disabled is false,
            Edges = (int)Edges,
            NativeTypes = _nativeTokens,
            FileTypes = _fileTypes,
            ExpandDirectories = ExpandDirectories,
            MaxFiles = Math.Max(0, MaxFiles),
        });

    private ValueTask<IJSObjectReference?> RegisterAsync(ElementReference element, BdndDropTargetOptions options)
    {
        _selfRef ??= DotNetObjectReference.Create(this);
        return Interop.RegisterDropTargetAsync(element, _selfRef, options);
    }

    // Like React DnD's collect: a hover change anywhere in the provider re-renders only the targets
    // whose collected state it changed.
    private void OnStateChanged()
    {
        // a callback run inline in the manager's notification may have removed this target already
        if (_disposed) return;
        var collected = Collect();
        TrackEnterAndLeave(collected.IsDragging && collected.IsOver);
        if (collected != _collected) InvokeAsync(StateHasChanged);
    }

    // Whether an in-app drag was over the target when its state last changed: OnEnter and OnLeave run on the change.
    private bool _over;

    private void TrackEnterAndLeave(bool over)
    {
        if (over == _over) return;
        _over = over;
        var manager = Manager!;
        // the end of a drag over the target is no leave
        if (over is false && manager.IsDragging() is false) return;
        var callback = over ? OnEnter : OnLeave;
        if (callback.HasDelegate is false) return;
        var args = new BdndDropEventArgs<TItem> { Monitor = manager.Monitor, Item = Cast(manager.GetItem()) };
        _ = InvokeAsync(() => GuardAsync(_ => callback.InvokeAsync(args), null));
    }

    // Drag events must not trigger the automatic re-render ComponentBase performs after every
    // event (dragover fires continuously); the manager's state changes drive renders.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);

    private static TItem? Cast(object? item) => item is TItem t ? t : default;

    private bool Accepts() =>
        Manager!.Monitor.GetItemType() is { } type && _accept.Contains(type);

    private Collected Collect()
    {
        var manager = Manager!;
        if (manager.IsDragging() is false && manager.GetNativeItemType(_handlerId) is { } nativeType)
        {
            // a native drag only reaches a target that accepts it, and only while it hovers it
            var shallowNative = manager.IsNativeOver(_handlerId, shallow: true);
            return new(false, true, shallowNative, true, nativeType, default,
                shallowNative ? manager.GetTargetEdge(_handlerId) : null);
        }
        var isOverShallow = manager.IsOver(_handlerId, shallow: true);
        var canDrop = manager.CanDrop(_handlerId);
        return new(
            manager.IsDragging(),
            manager.IsOver(_handlerId),
            isOverShallow,
            canDrop,
            manager.GetItemType(),
            Cast(manager.GetItem()),
            ClosestEdgeOf(manager, isOverShallow && canDrop));
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var monitor = Manager!.Monitor;
        _collected = Collect();
        _canDropInApp = _collected.IsDragging && _collected.CanDrop;

        var state = new BdndDropState<TItem>
        {
            Monitor = monitor,
            IsOver = _collected.IsOver,
            IsOverShallow = _collected.IsOverShallow,
            CanDrop = _collected.CanDrop,
            ItemType = _collected.ItemType,
            Item = _collected.Item,
            ClosestEdge = _collected.ClosestEdge,
        };

        // Hover tracking, drop-effect feedback (preventDefault/dropEffect) and native drops are
        // handled by the JS registration; the managed handlers cover in-app hover and drops.
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        // stable styling hooks: a stylesheet can follow the drag without the content rendering classes for it
        builder.AddAttribute(2, "data-bit-bdnd-can-drop", _collected.CanDrop);
        builder.AddAttribute(3, "data-bit-bdnd-over", _collected.IsOver);
        builder.AddAttribute(4, "data-bit-bdnd-over-shallow", _collected.IsOverShallow);
        builder.AddAttribute(5, "data-bit-bdnd-edge", BdndEdgeNames.Name(_collected.ClosestEdge));
        if (OnHover.HasDelegate && Disabled is false)
        {
            builder.AddAttribute(6, "ondragover",
                EventCallback.Factory.Create<DragEventArgs>(this, HandleDragOverAsync));
        }
        builder.AddAttribute(7, "ondrop",
            EventCallback.Factory.Create<DragEventArgs>(this, HandleDropAsync));
        builder.AddElementReferenceCapture(8, el => _element = el);
        // lets nested drop targets find their parent target (shallow IsOver)
        builder.OpenComponent<CascadingValue<BdndDropTargetScope>>(9);
        builder.AddAttribute(10, "Value", _scope);
        builder.AddAttribute(11, "IsFixed", true);
        builder.AddAttribute(12, "ChildContent", (RenderFragment)(b =>
        {
            b.AddContent(0, ChildContent?.Invoke(state));
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }

    private string? MatchNative(IReadOnlyCollection<string>? types)
    {
        if (types is null) return null;
        foreach (var (token, nativeType) in _nativeAccepts)
            if (types.Contains(token))
                return nativeType;
        return null;
    }

    // Drags started by a BdndDraggable of any provider (or document) are never native.
    private static bool IsInAppDrag(IEnumerable<string>? types) =>
        types?.Any(t => t.StartsWith(BdndInterop.SourceTypePrefix, StringComparison.Ordinal)) == true;

    // native (OS) drags are no drags of the manager: it only keeps their hover per target
    private void SetNativeItemType(string? nativeType) => Manager!.SetNativeOver(_handlerId, nativeType);

    // The edge is the innermost hovered target's: a target around it (a tree node around a nested one) shows no
    // second indicator and gets no edge in its OnDrop. And it is not shown where the drop would be refused.
    private BdndEdge? ClosestEdgeOf(BdndDragDropManager manager, bool shown) => shown ? manager.GetTargetEdge(_handlerId) : null;

    private BdndEdge? DropEdge(BdndDragDropManager manager) => ClosestEdgeOf(manager, manager.IsOver(_handlerId, shallow: true));

    private Task HandleDragOverAsync(DragEventArgs e) => HoverCoreAsync(new BdndXYCoord(e.ClientX, e.ClientY));

    // the drop event reports the exact final pointer position
    private Task HandleDropAsync(DragEventArgs e) => DropCoreAsync(new BdndXYCoord(e.ClientX, e.ClientY));

    // A managed dragover, or a keyboard move onto this target (or onto one nested in it).
    private async Task HoverCoreAsync(BdndXYCoord? pointer)
    {
        var manager = Manager!;
        if (manager.IsDragging() is false || Accepts() is false) return;
        await manager.FlushBeginAsync();
        if (pointer is { } offset) manager.SetClientOffset(offset);
        if (OnHover.HasDelegate is false) return;
        await OnHover.InvokeAsync(new BdndDropEventArgs<TItem>
        {
            Monitor = manager.Monitor,
            Item = Cast(manager.GetItem()),
            ClosestEdge = DropEdge(manager),
        });
    }

    // A managed drop event, or a keyboard drop, on this target (or on one nested in it).
    private async Task DropCoreAsync(BdndXYCoord? pointer)
    {
        var manager = Manager!;
        if (manager.IsDragging() is false) return; // native drops are handled in JS
        await manager.FlushBeginAsync();
        if (Accepts() is false || manager.CanDrop(_handlerId) is false) return;

        if (pointer is { } offset) manager.SetClientOffset(offset);
        var dragId = manager.DragId;
        var args = new BdndDropEventArgs<TItem>
        {
            Monitor = manager.Monitor,
            Item = Cast(manager.GetItem()),
            ClosestEdge = DropEdge(manager),
        };
        var onDrop = OnDrop.InvokeAsync(args);
        if (onDrop.IsCompleted is false)
        {
            // An async handler may still run when the source's dragend arrives: the drop counts right
            // away (outer targets see DidDrop), and OnEnd waits for the result the handler publishes.
            manager.RecordPendingDrop(dragId, ResultAsync(onDrop, args));
            await onDrop;
            return;
        }
        await onDrop;
        manager.RecordDrop(dragId, args.Result);
    }

    private static async Task<object?> ResultAsync(Task onDrop, BdndDropEventArgs<TItem> args)
    {
        await onDrop;
        return args.Result;
    }

    /// <summary>Invoked from JS when the pointer enters or leaves this target during any drag.</summary>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task HandleHoverChanged(bool isOver, string[]? types)
    {
        if (_disposed) return;
        var manager = Manager!;
        // a keyboard drag decides its hover itself; a native drag passing over meanwhile must not change it
        if (manager.GetDragInput() == BdndDragInput.Keyboard) return;
        if (manager.IsDragging())
        {
            await manager.FlushBeginAsync();
            SetNativeItemType(null);
            manager.SetTargetOver(_handlerId, isOver && Accepts());
            return;
        }
        // External/native drag. The module reports the end of the hover before the drop when it walks dropped folders
        // first: the edge the drop carries is the one the leave forgets.
        _nativeLeaveEdge = isOver ? null : NativeDropEdge(manager);
        SetNativeItemType(isOver && IsInAppDrag(types) is false ? MatchNative(types) : null);
    }

    // the edge a native drop on the target carries: the innermost hovered target's, as in-app drops do
    private BdndEdge? NativeDropEdge(BdndDragDropManager manager) =>
        manager.IsNativeOver(_handlerId, shallow: true) ? manager.GetTargetEdge(_handlerId) : null;

    /// <summary>
    /// Invoked from JS when the edge a drag over this target is closest to changes (only when the target has
    /// <see cref="Edges"/>): <c>top</c>, <c>bottom</c>, <c>left</c>, <c>right</c>, or null.
    /// </summary>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public void HandleEdgeChanged(string? edge)
    {
        if (_disposed) return;
        var manager = Manager!;
        // a keyboard drag places itself
        if (manager.GetDragInput() == BdndDragInput.Keyboard) return;
        manager.SetTargetEdge(_handlerId, BdndEdgeNames.Parse(edge));
    }

    /// <summary>Invoked from JS while a pointer drag moves over this target (only when it has an <c>OnHover</c>).</summary>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task HandlePointerHover(double clientX, double clientY)
    {
        if (_disposed || Manager!.GetDragInput() != BdndDragInput.Pointer) return Task.CompletedTask;
        return GuardAsync(HoverCoreAsync, new BdndXYCoord(clientX, clientY));
    }

    /// <summary>
    /// Invoked from JS when a pointer drag is released over this target - the innermost hovered target first, then
    /// the targets around it until one answers true.
    /// </summary>
    /// <returns>
    /// Whether the drag is this target's provider's: then the drop ran on this target and the targets of the same
    /// provider around it (those that can take the item), innermost first, and no other target is asked.
    /// </returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task<bool> HandlePointerDrop(double clientX, double clientY)
    {
        var manager = Manager!;
        if (_disposed || manager.GetDragInput() != BdndDragInput.Pointer) return false;
        await manager.DropChainAsync(_handlerId, new BdndXYCoord(clientX, clientY));
        return true;
    }

    /// <summary>Invoked from JS when a native OS drop occurs on this target.</summary>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BdndNativeDropPayload))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BdndNativeFile))]
    public async Task HandleNativeDrop(BdndNativeDropPayload payload)
    {
        if (_disposed) return;
        var manager = Manager!;
        var edge = manager.IsNativeOver(_handlerId) ? NativeDropEdge(manager) : _nativeLeaveEdge;
        _nativeLeaveEdge = null;
        SetNativeItemType(null);
        if (manager.IsDragging() || IsInAppDrag(payload.Types)) return;
        var nativeType = MatchNative(payload.Types);
        if (nativeType is null) return;

        // the files of the previous drop are no longer kept
        _lastDrop?.Release();
        _lastDrop = null;
        var serial = ++_nativeDropSerial;
        var files = payload.Files ?? [];
        if (payload.DropId is { } dropId)
        {
            files = await ReadAllFilesAsync(dropId, files, payload.FileCount);
            // A newer drop came while the descriptions were being read (the module keeps the newer one's files in
            // place of these), or the target went away: this drop is outdated, as the module drops a folder walk that
            // ends after a newer drop.
            if (_disposed || serial != _nativeDropSerial) return;
            _lastDrop = new BdndNativeDropFiles(Interop, dropId);
            for (var i = 0; i < files.Length; i++) files[i].Source = new BdndNativeFileSource(_lastDrop, i);
        }

        // Called from JS, where an exception would only reject the module's promise: hand it to the renderer (and an
        // error boundary around the target) as a managed event handler's would be.
        try
        {
            await OnNativeDrop.InvokeAsync(new BdndNativeDropEventArgs
            {
                ItemType = nativeType,
                ClosestEdge = edge,
                Files = files,
                TotalFileCount = Math.Max(payload.TotalFiles, files.Length),
                FileListIncomplete = payload.Incomplete,
                Data = payload.Data ?? new Dictionary<string, string>(),
            });
        }
        catch (Exception exception)
        {
            await ReportAsync(exception);
        }
    }

    // The descriptions of a drop's files come a page at a time (a Blazor Server message has a size limit): the first
    // with the drop, the rest read here. A page that cannot be read ends the list there.
    private async Task<BdndNativeFile[]> ReadAllFilesAsync(string dropId, BdndNativeFile[] first, int count)
    {
        // the count is the browser's: no more than the target hands over, and no capacity taken on its word
        if (MaxFiles > 0) count = Math.Min(count, MaxFiles);
        if (count <= first.Length) return first;
        var files = new List<BdndNativeFile>(first);
        while (files.Count < count)
        {
            BdndNativeFile[] page;
            try
            {
                page = await Interop.GetNativeDropFilesAsync(dropId, files.Count, count - files.Count);
            }
            catch (Exception exception) when (exception is JSException or JSDisconnectedException or TaskCanceledException)
            {
                break;
            }
            if (page.Length == 0) break;
            files.AddRange(page);
        }
        return [.. files];
    }

    // Keyboard drags run the target's callbacks from JS-invoked code, where an exception would be lost: hand it to
    // the renderer as a managed event handler's would be.
    private async Task GuardAsync(Func<BdndXYCoord?, Task> core, BdndXYCoord? pointer)
    {
        try
        {
            await core(pointer);
        }
        catch (Exception exception)
        {
            await ReportAsync(exception);
        }
    }

    // Through this component, or once it is disposed (a drop re-rendered it away), through its provider.
    private Task ReportAsync(Exception exception) =>
        Manager!.ReportExceptionAsync(exception, _disposed ? null : DispatchExceptionAsync);

    private sealed class Handler(BdndDropTarget<TItem> owner) : IBdndDropTargetHandler
    {
        public Task HoverAsync(BdndXYCoord? pointer) => owner.GuardAsync(owner.HoverCoreAsync, pointer);

        public Task DropAsync(BdndXYCoord? pointer) => owner.GuardAsync(owner.DropCoreAsync, pointer);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        // the module releases the files of the last drop with the registration
        _lastDrop?.Release();
        if (Manager is not null)
        {
            Manager.StateChanged -= OnStateChanged;
            Manager.UnregisterTarget(_handlerId);
        }
        if (_registration is not null) await _registration.DisposeAsync();
        _selfRef?.Dispose();
    }

    /// <summary>The state a render hands the content, besides the monitor.</summary>
    private readonly record struct Collected(
        bool IsDragging, bool IsOver, bool IsOverShallow, bool CanDrop, string? ItemType, TItem? Item, BdndEdge? ClosestEdge);
}
