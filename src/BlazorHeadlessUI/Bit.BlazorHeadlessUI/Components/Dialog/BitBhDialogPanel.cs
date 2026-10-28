using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The dialog's surface — the actual <c>role="dialog"</c> element that holds the
/// title, description, and content. It is the focus-trap boundary and the area
/// outside of which a click dismisses the dialog.
/// </summary>
public class BitBhDialogPanel : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhDialogContext? DialogContext { get; set; }

    /// <summary>
    /// Optional element to receive focus when the dialog opens, e.g.
    /// <c>InitialFocus="() =&gt; confirmButton"</c>. When omitted the first element
    /// marked <c>data-bit-bh-autofocus</c>, else the first focusable element, else
    /// the panel itself is focused.
    /// <para>
    /// A function rather than an <see cref="ElementReference"/>: an element inside the
    /// panel only gets its reference while the panel renders - after the panel's
    /// parameters were set - and gets a new one on every open, since the content is
    /// removed while the dialog is closed. The function is called once the open
    /// dialog has rendered.
    /// </para>
    /// </summary>
    [Parameter]
    public Func<ElementReference>? InitialFocus { get; set; }

    /// <summary>
    /// When true (the default), Escape closes the dialog. Set to <c>false</c>
    /// to require explicit dismissal.
    /// </summary>
    [Parameter]
    public bool DismissOnEscape { get; set; } = true;

    /// <summary>Child content. Title, description, and any other dialog markup go here.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnParametersSet()
    {
        DialogContext?.RegisterPanel(InitialFocus, DismissOnEscape);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", DialogContext?.PanelId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onkeydown",
            On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        // Keep Escape from reaching an enclosing popover / dialog in the same DOM tree.
        builder.AddEventStopPropagationAttribute(31, "onkeydown", true);

        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["role"] = DialogContext?.Role ?? "dialog";
        if (DialogContext?.Modal != false)
            attrs["aria-modal"] = "true";
        if (DialogContext?.HasTitle == true)
            attrs["aria-labelledby"] = DialogContext.TitleId;
        if (DialogContext?.HasDescription == true)
            attrs["aria-describedby"] = DialogContext.DescriptionId;

        // tabindex=-1 so the panel itself is programmatically focusable when no
        // descendants are focusable, but isn't part of the tab order.
        attrs["tabindex"] = "-1";

        SetDataState(attrs, DialogContext?.IsOpen ?? false);

        // Default stacking above the backdrop. Consumers override via class=/style=.
        if (GetAdditionalAttribute("style") is null && GetAdditionalAttribute("class") is null)
            attrs["style"] = "position:relative;z-index:1;";

        return attrs;
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape" && DismissOnEscape && DialogContext is not null)
            return DialogContext.DismissAsync(BitBhDismissReason.EscapeKey);
        return Task.CompletedTask;
    }
}
