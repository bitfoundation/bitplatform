using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The calendar popover of a <see cref="BitBhDatePicker"/>: a non-modal <c>role="dialog"</c> named by the field's label
/// (else "Choose date"), anchored below the <see cref="BitBhDatePickerGroup"/> (<c>"bottom start"</c> unless
/// <see cref="BitBhFloatingContentBase.Anchor"/> says otherwise). Put a <see cref="BitBhCalendar"/> in it.
///
/// <para><b>Behaviour:</b> on open the focus moves to the calendar's focused date; Escape, a press outside and the focus
/// leaving close it, and the focus returns to the <see cref="BitBhDatePickerTrigger"/>. Unlike other floating parts it is
/// removed from the page while closed (<see cref="BitBhFloatingContentBase.Unmount"/> defaults to true), so the calendar
/// opens on the picker's date each time. A <see cref="BitBhCloseButton"/> inside closes it.</para>
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c> (kept <c>closed</c> during exit animations), <c>data-side</c>,
/// <c>data-align</c> and the anchor CSS variables.</para>
/// </summary>
public class BitBhDatePickerContent : BitBhFloatingContentBase
{
    private static readonly BitBhAnchorOptions DefaultAnchor = "bottom start";

    [CascadingParameter]
    private BitBhDatePickerContext? Picker { get; set; }

    /// <summary>Creates the component.</summary>
    public BitBhDatePickerContent() => Unmount = true;

    /// <summary>The content: a <see cref="BitBhCalendar"/>, and anything around it.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override bool IsOpen => Picker?.IsOpen ?? false;

    protected override BitBhAnchorOptions? EffectiveAnchor => Anchor ?? DefaultAnchor;

    private string ContentId => Picker?.Owner.ContentId ?? ComponentId;

    protected override BitBhFloatingRequest CreateFloatingRequest()
    {
        var owner = Picker?.Owner;
        var layer = new BitBhLayerOptions
        {
            FocusOutside = true,
            ParentHandle = ParentLayerHandle,
        };
        layer.BranchIds.Add(owner?.TriggerId);

        return new BitBhFloatingRequest
        {
            ContentId = ContentId,
            ReferenceId = owner is null ? null : owner.HasGroup ? owner.GroupId : owner.TriggerId,
            Anchor = EffectiveAnchor,
            ArrowId = ArrowId,
            Portal = EffectivePortal,
            PortalTarget = PortalTarget,
            Layer = layer,
            Focus = new BitBhFocusScopeOptions
            {
                // The calendar marks its focused date data-bit-bh-autofocus: "first" lands on it.
                AutoFocus = "first",
                RestoreFocus = true,
                ReturnFocusId = owner?.TriggerId,
            },
        };
    }

    protected override Task OnDismissAsync(BitBhDismissReason reason) => Picker?.CloseAsync() ?? Task.CompletedTask;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!ShouldRenderContent) return;

        Scope.Owner = ContentId;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ContentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (IsHidden) builder.AddAttribute(30, "hidden", true);
        builder.AddAttribute(35, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        // While open, keep Escape from also closing an enclosing popover / dialog.
        builder.AddEventStopPropagationAttribute(36, "onkeydown", IsOpen);
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.OpenComponent<CascadingValue<IBitBhCloseContext>>(50);
        builder.AddComponentParameter(51, "Value", (IBitBhCloseContext?)Picker);
        builder.AddComponentParameter(52, "ChildContent", (RenderFragment)(inner => BitBhFloatingScope.Render(inner, 0, Scope, ChildContent)));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "dialog";
        attrs["tabindex"] = "-1";
        var named = GetAdditionalAttribute("aria-label") is not null || GetAdditionalAttribute("aria-labelledby") is not null;
        if (!named)
        {
            if (Picker?.Owner.LabelledBy is { } label) attrs["aria-labelledby"] = label;
            else attrs["aria-label"] = Strings.ChooseDate;
        }
        SetDataState(attrs, IsOpen);
        return attrs;
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
        => args.Key == "Escape" && IsOpen && Picker is not null ? Picker.CloseAsync() : Task.CompletedTask;
}
