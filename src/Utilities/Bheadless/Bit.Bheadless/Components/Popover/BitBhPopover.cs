using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A headless, accessible Popover — a floating panel that can contain any
/// content (navigation links, forms, rich overlays), opened by a button.
///
/// <para><b>Key differences from <see cref="BitBhDialog"/> (when not <see cref="Modal"/>):</b></para>
/// <list type="bullet">
///   <item><b>No focus trap</b> — Tab navigates freely; moving focus out closes the popover.</item>
///   <item><b>No scroll lock</b> and <b>no page isolation</b> — the rest of the page stays interactive.</item>
///   <item><b>Focus moves into the panel</b> on open and returns to the button when focus would otherwise be lost.</item>
/// </list>
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Dismissal</b> — Escape, pointer-down outside and focus leaving close it (cancellable via <see cref="OnDismissRequest"/>).</item>
///   <item><b>Anchor positioning</b> and <b>portal</b> on <see cref="BitBhPopoverPanel"/>; optional <see cref="BitBhPopoverArrow"/>.</item>
///   <item><b>Group coordination</b> — inside an <see cref="BitBhPopoverGroup"/>, opening one popover closes the others.</item>
///   <item><b>Binding</b> — <c>@bind-Open</c>, or uncontrolled with <see cref="DefaultOpen"/>.</item>
///   <item><b>Data attributes</b> — <c>data-state="open|closed"</c> on root, button, and panel.</item>
/// </list>
/// </summary>
public class BitBhPopover : BitBhComponentBase, IDisposable
{
    private bool _open;

    [CascadingParameter]
    private BitBhPopoverGroupContext? GroupContext { get; set; }

    /// <summary>Whether the popover is open. Supplying it makes the popover controlled (use <c>@bind-Open</c>).</summary>
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

    /// <summary>Raised before the popover is dismissed by Escape, an outside click or focus leaving it. Set <see cref="BitBhDismissRequest.Cancel"/> to keep it open.</summary>
    [Parameter]
    public EventCallback<BitBhDismissRequest> OnDismissRequest { get; set; }

    /// <summary>
    /// Modal popovers trap focus, lock scrolling and hide the rest of the page from
    /// assistive tech (like a dialog anchored to a button). Default: false.
    /// </summary>
    [Parameter]
    public bool Modal { get; set; }

    /// <summary>Child content. Should contain an <see cref="BitBhPopoverButton"/> and an <see cref="BitBhPopoverPanel"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => IsProvided(nameof(Open)) ? Open : _open;

    protected override void OnInitialized()
    {
        _open = DefaultOpen;
        GroupContext?.Register(this);
    }

    public void Dispose() => GroupContext?.Unregister(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = CreateContext();

        builder.OpenComponent<CascadingValue<IBitBhCloseContext>>(0);
        builder.AddComponentParameter(1, "Value", (IBitBhCloseContext)context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(closeOuter =>
        {
            closeOuter.OpenComponent<CascadingValue<BitBhPopoverContext>>(0);
            closeOuter.AddComponentParameter(1, "Value", context);
            closeOuter.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenElement(0, Tag);
                inner.AddAttribute(10, "id", ComponentId);
                inner.AddMultipleAttributes(20, GetFinalAttributes());
                inner.AddElementReferenceCapture(30, CaptureElement);
                inner.AddContent(40, ChildContent);
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

    private BitBhPopoverContext CreateContext() => new(
        isOpen: IsOpen,
        modal: Modal,
        baseId: ComponentId,
        hasButton: _hasButton,
        openAsync: OpenAsync,
        closeAsync: CloseAsync,
        toggleAsync: ToggleAsync,
        dismissAsync: DismissAsync,
        registerButton: RegisterButton);

    private bool _hasButton;

    private void RegisterButton(bool present)
    {
        if (_hasButton == present) return;
        _hasButton = present;
        StateHasChanged();
    }

    /// <summary>Opens the popover.</summary>
    public async Task OpenAsync()
    {
        if (IsOpen) return;
        await SetOpenAsync(true);
        if (GroupContext is not null)
            await GroupContext.CloseOthersAsync(this);
    }

    /// <summary>Closes the popover.</summary>
    public Task CloseAsync() => IsOpen ? SetOpenAsync(false) : Task.CompletedTask;

    private Task ToggleAsync() => IsOpen ? CloseAsync() : OpenAsync();

    internal Task CloseFromGroupAsync() => CloseAsync();

    private async Task DismissAsync(BitBhDismissReason reason)
    {
        if (!IsOpen) return;
        // The runtime already closed a surface whose parent closed: a cancel would only reopen it.
        if (reason != BitBhDismissReason.ParentClosed && OnDismissRequest.HasDelegate)
        {
            var request = new BitBhDismissRequest(reason);
            await OnDismissRequest.InvokeAsync(request);
            if (request.Cancel) return;
        }
        await CloseAsync();
    }

    private async Task SetOpenAsync(bool value)
    {
        if (!IsProvided(nameof(Open)))
            _open = value;
        await OpenChanged.InvokeAsync(value);
        await OnOpenChange.InvokeAsync(value);
        StateHasChanged();
    }
}
