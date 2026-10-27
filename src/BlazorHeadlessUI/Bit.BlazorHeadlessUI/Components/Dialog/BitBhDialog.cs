using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless, accessible Dialog (modal) implementing the WAI-ARIA Dialog pattern.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Focus trap</b> — Tab and Shift+Tab cycle within the panel; focus can't escape with the mouse either.</item>
///   <item><b>Initial focus</b> — first focusable element (or an element marked <c>data-bit-bh-autofocus</c>) by default; override via <see cref="BitBhDialogPanel.InitialFocus"/>.</item>
///   <item><b>Focus return</b> — restores focus to the previously-focused element on close.</item>
///   <item><b>Scroll lock</b> — body scrolling is locked while the dialog is open. Stack-aware.</item>
///   <item><b>Isolation</b> — everything outside the dialog is made <c>inert</c> and <c>aria-hidden</c> (at every ancestor level, so it works inside any layout).</item>
///   <item><b>Portal</b> — rendered at the end of <c>&lt;body&gt;</c> while open (opt out with <see cref="Portal"/>), escaping overflow and z-index traps.</item>
///   <item><b>Dismissal</b> — Escape, backdrop click and outside click close it; each can be disabled or cancelled via <see cref="OnDismissRequest"/>.</item>
///   <item><b>Exit animations</b> — stays mounted with <c>data-state="closed"</c> until CSS animations / transitions finish.</item>
///   <item><b>Auto-wired ARIA</b> — <c>role="dialog"</c>, <c>aria-modal</c>, <c>aria-labelledby</c>, <c>aria-describedby</c> (only when a title / description exists).</item>
///   <item><b>Binding</b> — <c>@bind-Open</c>, or <c>Open</c> + <c>OnClose</c>, or uncontrolled with <see cref="DefaultOpen"/>.</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BitBhDialog @bind-Open="isOpen"&gt;
///     &lt;BitBhDialogBackdrop class="dialog-backdrop" /&gt;
///     &lt;BitBhDialogPanel class="dialog-panel"&gt;
///         &lt;BitBhDialogTitle&gt;Confirm action&lt;/BitBhDialogTitle&gt;
///         &lt;BitBhDialogDescription&gt;This cannot be undone.&lt;/BitBhDialogDescription&gt;
///         &lt;BitBhCloseButton&gt;Cancel&lt;/BitBhCloseButton&gt;
///         &lt;button @onclick="Confirm"&gt;Confirm&lt;/button&gt;
///     &lt;/BitBhDialogPanel&gt;
/// &lt;/BitBhDialog&gt;
/// </code>
/// </summary>
public class BitBhDialog : BitBhComponentBase, IAsyncDisposable
{
    private BitBhFloatingController? _floating;
    private bool _open;
    private bool _hasTitle;
    private bool _hasDescription;
    private Func<ElementReference>? _initialFocus;
    private bool _panelDismissOnEscape = true;
    private BitBhFloatingScope? _scope;

    [CascadingParameter]
    private BitBhFloatingScope? ParentScope { get; set; }

    private BitBhFloatingScope Scope => _scope ??= new BitBhFloatingScope(() => _floating?.Handle ?? 0);

    /// <summary>Whether the dialog is open. Supplying it makes the dialog controlled (use <c>@bind-Open</c>).</summary>
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

    /// <summary>Fires when the user dismisses the dialog (Escape, backdrop / outside click, close button).</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>
    /// Raised before the dialog is dismissed by Escape or an outside click. Set
    /// <see cref="BitBhDismissRequest.Cancel"/> to keep it open.
    /// </summary>
    [Parameter]
    public EventCallback<BitBhDismissRequest> OnDismissRequest { get; set; }

    /// <summary>
    /// Modal dialogs trap focus, lock scrolling and hide the rest of the page from
    /// assistive tech. Non-modal dialogs do none of that. Default: true.
    /// </summary>
    [Parameter]
    public bool Modal { get; set; } = true;

    /// <summary>The ARIA role of the panel: <c>"dialog"</c> (default) or <c>"alertdialog"</c>.</summary>
    [Parameter]
    public string Role { get; set; } = "dialog";

    /// <summary>Whether Escape closes the dialog. Default: true.</summary>
    [Parameter]
    public bool DismissOnEscape { get; set; } = true;

    /// <summary>Whether pressing outside the panel closes the dialog. Default: true.</summary>
    [Parameter]
    public bool DismissOnOutsideClick { get; set; } = true;

    /// <summary>Render the dialog at the end of <c>&lt;body&gt;</c> while open. Default: true.</summary>
    [Parameter]
    public bool Portal { get; set; } = true;

    /// <summary>Optional id or CSS selector of the portal container.</summary>
    [Parameter]
    public string? PortalTarget { get; set; }

    /// <summary>Optional element that receives focus when the dialog closes (defaults to the previously focused element).</summary>
    [Parameter]
    public ElementReference? ReturnFocus { get; set; }

    /// <summary>
    /// Child content. Should contain an <see cref="BitBhDialogPanel"/> and optionally an
    /// <see cref="BitBhDialogBackdrop"/>, <see cref="BitBhDialogTitle"/>, and
    /// <see cref="BitBhDialogDescription"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => IsProvided(nameof(Open)) ? Open : _open;

    private BitBhFloatingController Floating => _floating ??= new BitBhFloatingController(Interop, DismissAsync, StateHasChanged, DispatchExceptionAsync);

    private string PanelId => $"{ComponentId}-panel";

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        _open = DefaultOpen;
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
        => Floating.SyncAsync(() => IsOpen, CreateRequest);

    private BitBhFloatingRequest CreateRequest() => new()
    {
        ContentId = PanelId,
        SurfaceIds = [ComponentId],
        Portal = Portal,
        PortalTarget = PortalTarget,
        Modal = Modal,
        Layer = new BitBhLayerOptions
        {
            Escape = DismissOnEscape && _panelDismissOnEscape,
            PointerDownOutside = DismissOnOutsideClick,
            Modal = Modal,
            ParentHandle = ParentScope?.Handle ?? 0,
        },
        Focus = new BitBhFocusScopeOptions
        {
            Trap = Modal,
            AutoFocus = "first",
            // Read now, after the render: an element inside the panel captures its reference while the panel
            // renders, after the panel registered.
            AutoFocusElement = _initialFocus?.Invoke(),
            RestoreFocus = true,
            ReturnFocusElement = ReturnFocus,
        },
    };

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!Floating.IsPresent(IsOpen)) return;

        var context = CreateContext();

        builder.OpenComponent<CascadingValue<IBitBhCloseContext>>(0);
        builder.AddComponentParameter(1, "Value", (IBitBhCloseContext)context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(closeOuter =>
        {
            closeOuter.OpenComponent<CascadingValue<BitBhDialogContext>>(0);
            closeOuter.AddComponentParameter(1, "Value", context);
            closeOuter.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenElement(0, Tag);
                inner.AddAttribute(10, "id", ComponentId);
                inner.AddMultipleAttributes(20, GetFinalAttributes());

                if (Floating.ShouldHide(IsOpen, Portal))
                    inner.AddAttribute(25, "hidden", true);

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
        // Stays visible while the panel / backdrop play their exit animations.
        attrs["data-bit-bh-presence"] = "subtree";
        // A modal dialog's root is a positioning container that fills the viewport so
        // the panel and backdrop can be positioned within it. Consumers can
        // override these styles entirely via class= or style=. A non-modal one leaves
        // the page usable, so it gets none: a transparent layer over the whole viewport
        // would take every press on the page (and turn each into an outside dismissal).
        if (Modal && GetAdditionalAttribute("style") is null && GetAdditionalAttribute("class") is null)
            attrs["style"] = "position:fixed;inset:0;z-index:50;";
        return attrs;
    }

    // ── Context plumbing ─────────────────────────────────────────────────────

    private BitBhDialogContext CreateContext() => new(
        isOpen: IsOpen,
        baseId: ComponentId,
        role: Role,
        modal: Modal,
        hasTitle: _hasTitle,
        hasDescription: _hasDescription,
        closeAsync: CloseAsync,
        dismissAsync: DismissAsync,
        registerTitle: present => SetFlag(ref _hasTitle, present),
        registerDescription: present => SetFlag(ref _hasDescription, present),
        registerPanel: RegisterPanel);

    private void SetFlag(ref bool field, bool value)
    {
        if (field == value) return;
        field = value;
        StateHasChanged();
    }

    private void RegisterPanel(Func<ElementReference>? initialFocus, bool dismissOnEscape)
    {
        _initialFocus = initialFocus;
        _panelDismissOnEscape = dismissOnEscape;
    }

    private async Task DismissAsync(BitBhDismissReason reason)
    {
        if (!IsOpen) return;
        if (reason == BitBhDismissReason.EscapeKey && (!DismissOnEscape || !_panelDismissOnEscape)) return;
        if (reason == BitBhDismissReason.PointerDownOutside && DismissOnOutsideClick is false) return;
        // The runtime already closed a surface whose parent closed: a cancel would only reopen it.
        if (reason != BitBhDismissReason.ParentClosed && OnDismissRequest.HasDelegate)
        {
            var request = new BitBhDismissRequest(reason);
            await OnDismissRequest.InvokeAsync(request);
            if (request.Cancel) return;
        }
        await CloseAsync();
    }

    /// <summary>Closes the dialog.</summary>
    public async Task CloseAsync()
    {
        if (!IsOpen) return;
        if (!IsProvided(nameof(Open)))
            _open = false;
        await OpenChanged.InvokeAsync(false);
        await OnOpenChange.InvokeAsync(false);
        await OnClose.InvokeAsync();
        StateHasChanged();
    }

    /// <summary>Opens the dialog (uncontrolled mode, or notifies the binding in controlled mode).</summary>
    public async Task ShowAsync()
    {
        if (IsOpen) return;
        if (!IsProvided(nameof(Open)))
            _open = true;
        await OpenChanged.InvokeAsync(true);
        await OnOpenChange.InvokeAsync(true);
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (_floating is not null)
            await _floating.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
