using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Bit.Bheadless;

/// <summary>
/// A headless Drawer - a modal dialog that slides in from an edge of the screen (a bottom sheet, a side panel)
/// and can be swiped away (Base UI / Vaul-style API, WAI-ARIA Dialog pattern).
///
/// <para><b>Anatomy:</b></para>
/// <code>
/// &lt;BitBhDrawer @bind-Open="open" Side="BitBhDrawerSide.Bottom"&gt;
///     &lt;BitBhDrawerTrigger&gt;Filters&lt;/BitBhDrawerTrigger&gt;
///     &lt;BitBhDrawerBackdrop class="backdrop" /&gt;
///     &lt;BitBhDrawerPanel class="sheet"&gt;
///         &lt;BitBhDrawerHandle class="handle" /&gt;          @* optional grab bar *@
///         &lt;BitBhDrawerTitle&gt;Filters&lt;/BitBhDrawerTitle&gt;
///         &lt;BitBhDrawerDescription&gt;Narrow the results.&lt;/BitBhDrawerDescription&gt;
///         &lt;BitBhDrawerClose&gt;Done&lt;/BitBhDrawerClose&gt;
///     &lt;/BitBhDrawerPanel&gt;
/// &lt;/BitBhDrawer&gt;
/// </code>
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>A dialog</b> - shares <see cref="BitBhDialog"/>'s machinery: focus trap, initial focus and focus return to the trigger, scroll lock, an <c>inert</c> / <c>aria-hidden</c> page behind it, portal, exit animations, <c>role="dialog"</c> named by its title and described by its description.</item>
///   <item><b>Sides</b> - <see cref="Side"/>: bottom (default), top, left, right, or start / end that follow the reading direction. Every part gets <c>data-side</c>.</item>
///   <item><b>Swipe to dismiss</b> - dragging the panel towards its edge moves it (<c>--bit-bh-drawer-swipe-movement-x</c> / <c>-y</c>, <c>--bit-bh-drawer-swipe-progress</c> and <c>data-swiping</c> on the panel and backdrop); released past <see cref="SwipeThreshold"/>, or flicked, it closes, else it settles back. Drags in text fields, in <c>[data-bit-bh-swipe-ignore]</c> or in content that can still scroll that way are left alone.</item>
///   <item><b>Dismissal</b> - Escape, a backdrop / outside press and a swipe close it; each can be turned off or cancelled through <see cref="OnDismissRequest"/>.</item>
///   <item><b>Nesting</b> - a drawer opened from another one gets <c>data-nested</c>; the one behind it gets <c>data-nested-drawer-open</c> and <c>--bit-bh-nested-drawers</c> (how many) to scale it back.</item>
///   <item><b>Binding</b> - <c>@bind-Open</c>, or uncontrolled with <see cref="DefaultOpen"/>.</item>
/// </list>
/// </summary>
public class BitBhDrawer : BitBhComponentBase, IAsyncDisposable
{
    private BitBhFloatingController? _floating;
    private BitBhFloatingScope? _scope;
    private bool _open;
    private bool _hasTrigger;
    private bool _hasBackdrop;
    private bool _hasTitle;
    private bool _hasDescription;
    private Func<ElementReference>? _initialFocus;
    private bool _panelDismissOnEscape = true;
    private readonly HashSet<BitBhDrawer> _openChildren = new();
    private bool _reportedOpen;
    private DotNetObjectReference<SwipeCallbacks>? _swipeRef;
    private int _swipeHandle;
    private bool _swipeTried;
    private bool _swipeSyncing;
    private string? _sentSwipe;
    private bool _disposed;

    [CascadingParameter]
    private BitBhFloatingScope? ParentScope { get; set; }

    // The drawer this one is nested in (it renders inside that drawer's panel).
    [CascadingParameter]
    private BitBhDrawerContext? ParentDrawer { get; set; }

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>Whether the drawer is open. Supplying it makes the drawer controlled (use <c>@bind-Open</c>).</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Initial open state when uncontrolled.</summary>
    [Parameter]
    public bool DefaultOpen { get; set; }

    /// <summary>Two-way binding callback for <see cref="Open"/>.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Fires whenever the open state changes.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>
    /// Raised before the drawer is dismissed by Escape, an outside press or a swipe. Set
    /// <see cref="BitBhDismissRequest.Cancel"/> to keep it open (a swiped panel then settles back).
    /// </summary>
    [Parameter]
    public EventCallback<BitBhDismissRequest> OnDismissRequest { get; set; }

    /// <summary>The screen edge the drawer is attached to. Default: <see cref="BitBhDrawerSide.Bottom"/>.</summary>
    [Parameter]
    public BitBhDrawerSide Side { get; set; }

    /// <summary>Reading direction (resolves <see cref="BitBhDrawerSide.Start"/> / <see cref="BitBhDrawerSide.End"/>). Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>
    /// Modal drawers trap focus, lock scrolling and hide the rest of the page from assistive tech. A non-modal one
    /// (a side panel next to the page) does none of that. Default: true.
    /// </summary>
    [Parameter]
    public bool Modal { get; set; } = true;

    /// <summary>Whether Escape closes the drawer. Default: true.</summary>
    [Parameter]
    public bool DismissOnEscape { get; set; } = true;

    /// <summary>Whether pressing outside the panel (the backdrop) closes the drawer. Default: true.</summary>
    [Parameter]
    public bool DismissOnOutsideClick { get; set; } = true;

    /// <summary>Whether dragging the panel towards its edge closes the drawer. Default: true.</summary>
    [Parameter]
    public bool DismissOnSwipe { get; set; } = true;

    /// <summary>
    /// How far (a fraction of the panel's size, 0-1) a swipe has to move the panel for the drawer to close when
    /// released; a quick flick closes it regardless. Default: 0.25.
    /// </summary>
    [Parameter]
    public double SwipeThreshold { get; set; } = 0.25;

    /// <summary>Render the backdrop and panel at the end of <c>&lt;body&gt;</c> while open. Default: true.</summary>
    [Parameter]
    public bool Portal { get; set; } = true;

    /// <summary>Optional id or CSS selector of the portal container.</summary>
    [Parameter]
    public string? PortalTarget { get; set; }

    /// <summary>
    /// Child content: a <see cref="BitBhDrawerPanel"/> and optionally a <see cref="BitBhDrawerTrigger"/> and a
    /// <see cref="BitBhDrawerBackdrop"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    internal bool IsOpen => IsProvided(nameof(Open)) ? Open : _open;

    private BitBhFloatingController Floating => _floating ??= new BitBhFloatingController(Interop, DismissAsync, StateHasChanged, DispatchExceptionAsync);

    private BitBhFloatingScope Scope => _scope ??= new BitBhFloatingScope(() => _floating?.Handle ?? 0);

    private string TriggerId => $"{ComponentId}-trigger";

    private string PanelId => $"{ComponentId}-panel";

    private string BackdropId => $"{ComponentId}-backdrop";

    /// <summary>The physical edge: Start / End resolved by the reading direction.</summary>
    private string PhysicalSide => (Side, Dir ?? CascadedDir) switch
    {
        (BitBhDrawerSide.Top, _) => "top",
        (BitBhDrawerSide.Left, _) => "left",
        (BitBhDrawerSide.Right, _) => "right",
        (BitBhDrawerSide.Start, BitBhDirection.Rtl) or (BitBhDrawerSide.End, BitBhDirection.Ltr) => "right",
        (BitBhDrawerSide.Start, _) or (BitBhDrawerSide.End, _) => "left",
        _ => "bottom",
    };

    /// <summary>How many drawers nested in this one are open.</summary>
    internal int NestedDrawers => _openChildren.Count;

    protected override void OnInitialized() => _open = DefaultOpen;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The drawer this one is nested in learns when it opens and closes: it is then "behind" (data-nested-drawer-open).
        if (_reportedOpen != IsOpen)
        {
            _reportedOpen = IsOpen;
            ParentDrawer?.Owner.ReportNested(this, IsOpen);
        }
        await Floating.SyncAsync(() => IsOpen, CreateRequest);
        await SyncSwipeAsync();
    }

    private BitBhFloatingRequest CreateRequest()
    {
        var ids = new List<string?>();
        if (_hasBackdrop) ids.Add(BackdropId);
        ids.Add(PanelId);
        var layer = new BitBhLayerOptions
        {
            Escape = DismissOnEscape && _panelDismissOnEscape,
            PointerDownOutside = DismissOnOutsideClick,
            Modal = Modal,
            ParentHandle = ParentScope?.Handle ?? 0,
        };
        // A non-modal drawer's trigger toggles it: pressing it is not "outside".
        if (_hasTrigger) layer.BranchIds.Add(TriggerId);
        return new BitBhFloatingRequest
        {
            ContentId = PanelId,
            SurfaceIds = ids,
            Portal = Portal,
            PortalTarget = PortalTarget,
            Modal = Modal,
            Layer = layer,
            Focus = new BitBhFocusScopeOptions
            {
                Trap = Modal,
                AutoFocus = "first",
                // Read now, after the render: an element inside the panel captures its reference while the panel renders.
                AutoFocusElement = _initialFocus?.Invoke(),
                RestoreFocus = true,
                ReturnFocusId = _hasTrigger ? TriggerId : null,
            },
        };
    }

    // ── Swipe ────────────────────────────────────────────────────────────────

    private Dictionary<string, object?> SwipeOptions() => new()
    {
        ["side"] = PhysicalSide,
        ["threshold"] = Math.Clamp(SwipeThreshold, 0, 1),
        ["backdropId"] = _hasBackdrop ? BackdropId : null,
        ["enabled"] = DismissOnSwipe,
    };

    private string SwipeKey() => string.Create(CultureInfo.InvariantCulture, $"{PhysicalSide}|{SwipeThreshold}|{_hasBackdrop}|{DismissOnSwipe}");

    // The runtime follows drags on the panel while it is open. Renders can arrive while the attach is in flight: one
    // loop applies them in order, and a drawer closed or disposed meanwhile detaches what the call attached.
    private async Task SyncSwipeAsync()
    {
        if (_swipeSyncing) return;
        _swipeSyncing = true;
        try
        {
            while (true)
            {
                var wanted = !_disposed && IsOpen && Floating.IsActive;
                if (!wanted)
                {
                    _swipeTried = false;
                    if (_swipeHandle <= 0) break;
                    var handle = _swipeHandle;
                    _swipeHandle = 0;
                    await Interop.DrawerDetachAsync(handle);
                    continue;
                }
                var key = SwipeKey();
                if (_swipeHandle > 0)
                {
                    if (key == _sentSwipe) break;
                    _sentSwipe = key;
                    await Interop.DrawerUpdateAsync(_swipeHandle, SwipeOptions());
                    continue;
                }
                // One attempt per open: a 0 handle (no runtime, no panel) isn't retried on every render.
                if (_swipeTried) break;
                _swipeTried = true;
                _sentSwipe = key;
                _swipeRef ??= DotNetObjectReference.Create(new SwipeCallbacks(this));
                _swipeHandle = await Interop.DrawerAttachAsync(_swipeRef, PanelId, SwipeOptions());
            }
        }
        finally
        {
            _swipeSyncing = false;
        }
    }

    private sealed class SwipeCallbacks(BitBhDrawer drawer)
    {
        /// <summary>A swipe released past the threshold. Answers whether the drawer is closing (else the panel settles back).</summary>
        [JSInvokable]
        public async Task<bool> OnSwipeDismiss()
        {
            await drawer.RunFromRuntimeAsync(() => drawer.DismissAsync(BitBhDismissReason.Swipe));
            return !drawer.IsOpen;
        }
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhDrawerContext(this)
        {
            IsOpen = IsOpen,
            IsPresent = Floating.IsPresent(IsOpen),
            IsHidden = Floating.ShouldHide(IsOpen, Portal),
            BaseId = ComponentId,
            Side = PhysicalSide,
            Modal = Modal,
            HasTitle = _hasTitle,
            HasDescription = _hasDescription,
            IsNested = ParentDrawer is not null,
            NestedDrawers = NestedDrawers,
        };

        builder.OpenComponent<CascadingValue<IBitBhCloseContext>>(0);
        builder.AddComponentParameter(1, "Value", (IBitBhCloseContext)context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(closeOuter =>
        {
            closeOuter.OpenComponent<CascadingValue<BitBhDrawerContext>>(0);
            closeOuter.AddComponentParameter(1, "Value", context);
            closeOuter.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenElement(0, Tag);
                inner.AddAttribute(10, "id", ComponentId);
                inner.AddMultipleAttributes(20, GetFinalAttributes());
                inner.AddElementReferenceCapture(30, CaptureElement);
                BitBhFloatingScope.Render(inner, 40, Scope, ChildContent);
                inner.CloseElement();
            }));
            closeOuter.CloseComponent();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        SetDataValue(attrs, "side", PhysicalSide);
        // A wrapper only: it holds the trigger in place and the portalled parts while closed.
        if (GetAdditionalAttribute("style") is null && GetAdditionalAttribute("class") is null)
            attrs["style"] = "display:contents";
        return attrs;
    }

    /// <summary>
    /// <c>data-nested</c> on a drawer opened from another one; <c>data-nested-drawer-open</c> and
    /// <c>--bit-bh-nested-drawers</c> (how many) on one that has drawers open in front of it.
    /// </summary>
    internal static void AddNestingAttributes(Dictionary<string, object> attrs, BitBhDrawerContext? drawer)
    {
        if (drawer is null) return;
        if (drawer.IsNested) attrs["data-nested"] = "";
        if (drawer.NestedDrawers <= 0) return;
        // Only while there are any: read it as var(--bit-bh-nested-drawers, 0).
        attrs["data-nested-drawer-open"] = "";
        var variable = $"--bit-bh-nested-drawers:{drawer.NestedDrawers.ToString(CultureInfo.InvariantCulture)}";
        attrs["style"] = attrs.TryGetValue("style", out var style) ? BitBhAttributeUtilities.JoinStyles(style?.ToString(), variable) : variable;
    }

    // ── Plumbing ─────────────────────────────────────────────────────────────

    internal void Register(BitBhDrawerPart part, bool present)
    {
        ref var flag = ref _hasTrigger;
        switch (part)
        {
            case BitBhDrawerPart.Backdrop: flag = ref _hasBackdrop; break;
            case BitBhDrawerPart.Title: flag = ref _hasTitle; break;
            case BitBhDrawerPart.Description: flag = ref _hasDescription; break;
        }
        if (flag == present) return;
        flag = present;
        StateHasChanged();
    }

    internal void RegisterPanel(Func<ElementReference>? initialFocus, bool dismissOnEscape)
    {
        _initialFocus = initialFocus;
        _panelDismissOnEscape = dismissOnEscape;
    }

    private void ReportNested(BitBhDrawer child, bool open)
    {
        if (open ? _openChildren.Add(child) : _openChildren.Remove(child)) StateHasChanged();
    }

    internal async Task DismissAsync(BitBhDismissReason reason)
    {
        if (!IsOpen) return;
        if (reason == BitBhDismissReason.EscapeKey && (!DismissOnEscape || !_panelDismissOnEscape)) return;
        if (reason == BitBhDismissReason.PointerDownOutside && !DismissOnOutsideClick) return;
        if (reason == BitBhDismissReason.Swipe && !DismissOnSwipe) return;
        // The runtime already closed a surface whose parent closed: a cancel would only reopen it.
        if (reason != BitBhDismissReason.ParentClosed && OnDismissRequest.HasDelegate)
        {
            var request = new BitBhDismissRequest(reason);
            await OnDismissRequest.InvokeAsync(request);
            if (request.Cancel) return;
        }
        await CloseAsync();
    }

    /// <summary>Opens the drawer.</summary>
    public Task OpenAsync() => SetOpenAsync(true);

    /// <summary>Closes the drawer.</summary>
    public Task CloseAsync() => SetOpenAsync(false);

    private async Task SetOpenAsync(bool value)
    {
        if (IsOpen == value) return;
        if (!IsProvided(nameof(Open)))
            _open = value;
        await OpenChanged.InvokeAsync(value);
        await OnOpenChange.InvokeAsync(value);
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_reportedOpen) ParentDrawer?.Owner.ReportNested(this, false);
        if (!_swipeSyncing && _swipeHandle > 0)
        {
            var handle = _swipeHandle;
            _swipeHandle = 0;
            await Interop.DrawerDetachAsync(handle);
        }
        if (_floating is not null)
            await _floating.DisposeAsync();
        _swipeRef?.Dispose();
        GC.SuppressFinalize(this);
    }
}
