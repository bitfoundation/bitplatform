using System.ComponentModel;
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
    [Inject] private BdndInterop Interop { get; set; } = default!;

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

    /// <summary>Called when a native OS drag (file/text/url/html) is dropped.</summary>
    [Parameter] public EventCallback<BdndNativeDropEventArgs> OnNativeDrop { get; set; }

    /// <summary>Predicate gating whether this target can accept the current in-app item.</summary>
    [Parameter] public Func<TItem?, IBdndMonitor, bool>? CanDropPredicate { get; set; }

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
    private ElementReference _element;
    private BdndJsRegistration<BdndDropTargetOptions>? _registration;
    private DotNetObjectReference<BdndDropTarget<TItem>>? _selfRef;
    private bool _canDropInApp;
    private bool _disposed;

    // native (OS) drag local state - the manager only tracks in-app drags
    private string? _nativeItemType;

    public BdndDropTarget()
    {
        _handlerId = Guid.NewGuid().ToString("N");
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
        var list = new List<string>();
        if (string.IsNullOrEmpty(Accept) is false) list.Add(Accept);
        if (AcceptTypes is not null) list.AddRange(AcceptTypes.Where(t => string.IsNullOrEmpty(t) is false));
        _accept = list.Distinct().ToArray();
        _nativeAccepts = _accept
            .Where(BdndNativeTypes.IsNative)
            .Select(t => (BdndNativeTypes.ToDataTransferType(t), t))
            .ToArray();
        var tokens = _nativeAccepts.Select(na => na.Token).ToArray();
        if (tokens.SequenceEqual(_nativeTokens) is false) _nativeTokens = tokens;

        var predicate = CanDropPredicate;
        Manager!.RegisterTarget(new BdndDropTargetEntry
        {
            Id = _handlerId,
            ParentId = ParentScope?.FindTargetId(Manager),
            Accept = _accept,
            CanDrop = predicate is null
                ? null
                : (item, monitor) => predicate(Cast(item), monitor),
        });
    }

    protected override Task OnAfterRenderAsync(bool firstRender) =>
        _registration!.SyncAsync(_element, new BdndDropTargetOptions(_canDropInApp, _nativeTokens));

    private ValueTask<IJSObjectReference?> RegisterAsync(ElementReference element, BdndDropTargetOptions options)
    {
        _selfRef ??= DotNetObjectReference.Create(this);
        return Interop.RegisterDropTargetAsync(element, _selfRef, options);
    }

    private void OnStateChanged() => InvokeAsync(StateHasChanged);

    // Drag events must not trigger the automatic re-render ComponentBase performs after every
    // event (dragover fires continuously); the manager and the native hover state drive renders.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);

    private static TItem? Cast(object? item) => item is TItem t ? t : default;

    private bool Accepts() =>
        Manager!.Monitor.GetItemType() is { } type && _accept.Contains(type);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var monitor = Manager!.Monitor;
        var native = monitor.IsDragging() is false && _nativeItemType is not null;
        _canDropInApp = monitor.CanDrop(_handlerId);

        var state = new BdndDropState<TItem>
        {
            Monitor = monitor,
            IsOver = native || monitor.IsOver(_handlerId),
            IsOverShallow = native || monitor.IsOver(_handlerId, shallow: true),
            CanDrop = native || _canDropInApp,
            ItemType = native ? _nativeItemType : monitor.GetItemType(),
            Item = Cast(monitor.GetItem()),
        };

        // Hover tracking, drop-effect feedback (preventDefault/dropEffect) and native drops are
        // handled by the JS registration; the managed handlers cover in-app hover and drops.
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        if (OnHover.HasDelegate)
        {
            builder.AddAttribute(2, "ondragover",
                EventCallback.Factory.Create<DragEventArgs>(this, HandleDragOverAsync));
        }
        builder.AddAttribute(3, "ondrop",
            EventCallback.Factory.Create<DragEventArgs>(this, HandleDropAsync));
        builder.AddElementReferenceCapture(4, el => _element = el);
        // lets nested drop targets find their parent target (shallow IsOver)
        builder.OpenComponent<CascadingValue<BdndDropTargetScope>>(5);
        builder.AddAttribute(6, "Value", _scope);
        builder.AddAttribute(7, "IsFixed", true);
        builder.AddAttribute(8, "ChildContent", (RenderFragment)(b =>
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

    private void SetNativeItemType(string? nativeType)
    {
        if (_nativeItemType == nativeType) return;
        _nativeItemType = nativeType;
        StateHasChanged();
    }

    private async Task HandleDragOverAsync(DragEventArgs e)
    {
        var manager = Manager!;
        if (manager.IsDragging() is false || Accepts() is false) return;
        await manager.FlushBeginAsync();
        manager.SetClientOffset(new BdndXYCoord(e.ClientX, e.ClientY));
        await OnHover.InvokeAsync(new BdndDropEventArgs<TItem>
        {
            Monitor = manager.Monitor,
            Item = Cast(manager.GetItem()),
        });
    }

    private async Task HandleDropAsync(DragEventArgs e)
    {
        var manager = Manager!;
        if (manager.IsDragging() is false) return; // native drops are handled in JS
        await manager.FlushBeginAsync();
        if (Accepts() is false || manager.CanDrop(_handlerId) is false) return;

        // the drop event reports the exact final pointer position
        manager.SetClientOffset(new BdndXYCoord(e.ClientX, e.ClientY));
        var dragId = manager.DragId;
        var args = new BdndDropEventArgs<TItem>
        {
            Monitor = manager.Monitor,
            Item = Cast(manager.GetItem()),
        };
        var onDrop = OnDrop.InvokeAsync(args);
        // An async handler may still run when the source's dragend arrives: mark the drop as
        // handled right away so OnEnd sees DidDrop; the result is recorded once it completes.
        if (onDrop.IsCompleted is false) manager.RecordDrop(dragId, null);
        await onDrop;
        manager.RecordDrop(dragId, args.Result);
    }

    /// <summary>Invoked from JS when the pointer enters or leaves this target during any drag.</summary>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task HandleHoverChanged(bool isOver, string[]? types)
    {
        if (_disposed) return;
        var manager = Manager!;
        if (manager.IsDragging())
        {
            await manager.FlushBeginAsync();
            SetNativeItemType(null);
            manager.SetTargetOver(_handlerId, isOver && Accepts());
            return;
        }
        // external/native drag
        SetNativeItemType(isOver && IsInAppDrag(types) is false ? MatchNative(types) : null);
    }

    /// <summary>Invoked from JS when a native OS drop occurs on this target.</summary>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task HandleNativeDrop(BdndNativeDropPayload payload)
    {
        if (_disposed) return;
        SetNativeItemType(null);
        if (Manager!.IsDragging() || IsInAppDrag(payload.Types)) return;
        var nativeType = MatchNative(payload.Types);
        if (nativeType is null) return;

        await OnNativeDrop.InvokeAsync(new BdndNativeDropEventArgs
        {
            ItemType = nativeType,
            Files = payload.Files ?? [],
            Data = payload.Data ?? new Dictionary<string, string>(),
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (Manager is not null)
        {
            Manager.StateChanged -= OnStateChanged;
            Manager.UnregisterTarget(_handlerId);
        }
        if (_registration is not null) await _registration.DisposeAsync();
        _selfRef?.Dispose();
    }
}
