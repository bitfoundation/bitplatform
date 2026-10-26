using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless, accessible Alert Dialog implementing the WAI-ARIA
/// <c>alertdialog</c> pattern. An alert dialog is a modal dialog that interrupts
/// the user with an important message and expects a deliberate response — for
/// example, confirming a destructive action.
///
/// <para><b>Key differences from <see cref="BitBhDialog"/>:</b></para>
/// <list type="bullet">
///   <item><b>Built-in trigger</b> — opens via an <see cref="BitBhAlertDialogTrigger"/>; supports <c>@bind-Open</c> and uncontrolled use.</item>
///   <item><b><c>role="alertdialog"</c></b> instead of <c>role="dialog"</c>.</item>
///   <item><b>No dismiss on overlay click</b> — the user must explicitly choose <see cref="BitBhAlertDialogCancel"/> or <see cref="BitBhAlertDialogAction"/>.</item>
///   <item><b>Default focus on Cancel</b> — focus lands on the <see cref="BitBhAlertDialogCancel"/> button when present, the safest choice.</item>
/// </list>
///
/// <para><b>Shared with <see cref="BitBhDialog"/>:</b> focus trap, focus return to the trigger,
/// scroll lock, page isolation (inert + aria-hidden), portal, exit animations and
/// auto-wired <c>aria-labelledby</c> / <c>aria-describedby</c>.</para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BitBhAlertDialog&gt;
///     &lt;BitBhAlertDialogTrigger&gt;Delete account&lt;/BitBhAlertDialogTrigger&gt;
///     &lt;BitBhAlertDialogOverlay class="overlay" /&gt;
///     &lt;BitBhAlertDialogContent class="content"&gt;
///         &lt;BitBhAlertDialogTitle&gt;Are you absolutely sure?&lt;/BitBhAlertDialogTitle&gt;
///         &lt;BitBhAlertDialogDescription&gt;This action cannot be undone.&lt;/BitBhAlertDialogDescription&gt;
///         &lt;BitBhAlertDialogCancel&gt;Cancel&lt;/BitBhAlertDialogCancel&gt;
///         &lt;BitBhAlertDialogAction OnClick="DeleteAccount"&gt;Yes, delete account&lt;/BitBhAlertDialogAction&gt;
///     &lt;/BitBhAlertDialogContent&gt;
/// &lt;/BitBhAlertDialog&gt;
/// </code>
/// </summary>
public class BitBhAlertDialog : BitBhComponentBase, IAsyncDisposable
{
    private BitBhFloatingController? _floating;
    private bool _open;
    private bool _hasTitle;
    private bool _hasDescription;
    private bool _hasOverlay;
    private bool _hasCancel;
    private BitBhFloatingScope? _scope;

    [CascadingParameter]
    private BitBhFloatingScope? ParentScope { get; set; }

    private BitBhFloatingScope Scope => _scope ??= new BitBhFloatingScope(() => _floating?.Handle ?? 0);

    /// <summary>Whether the alert dialog is open. Supplying it makes the component controlled (use <c>@bind-Open</c>).</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Initial open state when uncontrolled.</summary>
    [Parameter]
    public bool DefaultOpen { get; set; }

    /// <summary>Two-way binding callback for <see cref="Open"/>.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Fires whenever the open state changes (trigger, Escape, Cancel, or Action).</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>Raised before Escape dismisses the dialog. Set <see cref="BitBhDismissRequest.Cancel"/> to keep it open.</summary>
    [Parameter]
    public EventCallback<BitBhDismissRequest> OnDismissRequest { get; set; }

    /// <summary>Whether Escape closes the alert dialog. Default: true.</summary>
    [Parameter]
    public bool DismissOnEscape { get; set; } = true;

    /// <summary>Render the overlay and content at the end of <c>&lt;body&gt;</c> while open. Default: true.</summary>
    [Parameter]
    public bool Portal { get; set; } = true;

    /// <summary>Optional id or CSS selector of the portal container.</summary>
    [Parameter]
    public string? PortalTarget { get; set; }

    /// <summary>
    /// Child content. Should contain an <see cref="BitBhAlertDialogTrigger"/> and an
    /// <see cref="BitBhAlertDialogContent"/>, optionally an <see cref="BitBhAlertDialogOverlay"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => IsProvided(nameof(Open)) ? Open : _open;

    private BitBhFloatingController Floating => _floating ??= new BitBhFloatingController(Interop, DismissAsync, StateHasChanged);

    protected override void OnInitialized() => _open = DefaultOpen;

    protected override Task OnAfterRenderAsync(bool firstRender)
        => Floating.SyncAsync(() => IsOpen, CreateRequest);

    private BitBhFloatingRequest CreateRequest()
    {
        var ids = new List<string?>();
        if (_hasOverlay) ids.Add(OverlayId);
        ids.Add(ContentId);
        return new BitBhFloatingRequest
        {
            ContentId = ContentId,
            SurfaceIds = ids,
            Portal = Portal,
            PortalTarget = PortalTarget,
            Modal = true,
            Layer = new BitBhLayerOptions { Escape = DismissOnEscape, PointerDownOutside = false, Modal = true, ParentHandle = ParentScope?.Handle ?? 0 },
            Focus = new BitBhFocusScopeOptions
            {
                Trap = true,
                AutoFocus = _hasCancel ? CancelId : "first",
                RestoreFocus = true,
                ReturnFocusId = TriggerId,
            },
        };
    }

    private string TriggerId => $"{ComponentId}-trigger";
    private string ContentId => $"{ComponentId}-content";
    private string OverlayId => $"{ComponentId}-overlay";
    private string CancelId => $"{ComponentId}-cancel";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = CreateContext();

        builder.OpenComponent<CascadingValue<IBitBhCloseContext>>(0);
        builder.AddComponentParameter(1, "Value", (IBitBhCloseContext)context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(closeOuter =>
        {
            closeOuter.OpenComponent<CascadingValue<BitBhAlertDialogContext>>(0);
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
        return attrs;
    }

    private BitBhAlertDialogContext CreateContext() => new(
        isOpen: IsOpen,
        isPresent: Floating.IsPresent(IsOpen),
        isHidden: Floating.ShouldHide(IsOpen, Portal),
        baseId: ComponentId,
        hasTitle: _hasTitle,
        hasDescription: _hasDescription,
        openAsync: OpenAsync,
        closeAsync: CloseAsync,
        dismissAsync: DismissAsync,
        register: Register);

    private void Register(BitBhAlertDialogPart part, bool present)
    {
        ref var field = ref _hasTitle;
        switch (part)
        {
            case BitBhAlertDialogPart.Description: field = ref _hasDescription; break;
            case BitBhAlertDialogPart.Overlay: field = ref _hasOverlay; break;
            case BitBhAlertDialogPart.Cancel: field = ref _hasCancel; break;
        }
        if (field == present) return;
        field = present;
        StateHasChanged();
    }

    private async Task DismissAsync(BitBhDismissReason reason)
    {
        if (!IsOpen) return;
        if (reason == BitBhDismissReason.ParentClosed)
        {
            await CloseAsync();
            return;
        }
        if (reason != BitBhDismissReason.EscapeKey || !DismissOnEscape) return;
        if (OnDismissRequest.HasDelegate)
        {
            var request = new BitBhDismissRequest(reason);
            await OnDismissRequest.InvokeAsync(request);
            if (request.Cancel) return;
        }
        await CloseAsync();
    }

    /// <summary>Opens the alert dialog.</summary>
    public Task OpenAsync() => SetOpenAsync(true);

    /// <summary>Closes the alert dialog.</summary>
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
        if (_floating is not null)
            await _floating.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

internal enum BitBhAlertDialogPart { Title, Description, Overlay, Cancel }
