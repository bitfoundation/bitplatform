using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The button of a <see cref="BitBhDatePicker"/>, <see cref="BitBhDateTimePicker"/> or <see cref="BitBhDateRangePicker"/>
/// that opens (and closes) its calendar popover: named "Choose date" (<see cref="BitBhStrings.ChooseDate"/>; "Choose date
/// range" in a range picker, <see cref="BitBhStrings.ChooseDateRange"/>; your <c>aria-label</c> wins), with <c>aria-haspopup="dialog"</c>,
/// <c>aria-expanded</c> and, while open, <c>aria-controls</c>. The focus comes back to it when the popover closes.
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c>, <c>data-disabled</c>.</para>
/// </summary>
public class BitBhDatePickerTrigger : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhDatePickerContext? Picker { get; set; }

    /// <summary>The content (an icon or text).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Content rendered from the picker's state: <c>&lt;Template Context="picker"&gt;…&lt;/Template&gt;</c>.</summary>
    [Parameter]
    public RenderFragment<BitBhDatePickerContext>? Template { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Picker?.Owner.TriggerId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", HandleClickAsync));
        builder.AddAttribute(31, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDownAsync));
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Picker is { } picker && Template is not null) builder.AddContent(50, Template(picker));
        else builder.AddContent(51, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var isButton = Tag.Equals("button", StringComparison.OrdinalIgnoreCase);
        var disabled = Picker?.IsDisabled ?? false;
        var open = Picker?.IsOpen ?? false;
        if (isButton) attrs["type"] = "button";
        else
        {
            attrs["role"] = "button";
            if (!disabled) attrs["tabindex"] = "0";
        }
        attrs["aria-label"] = Picker?.ChooseLabel(Strings) ?? Strings.ChooseDate;
        attrs["aria-haspopup"] = "dialog";
        SetAriaBool(attrs, "aria-expanded", open);
        if (Picker is { } picker)
        {
            if (open) attrs["aria-controls"] = picker.Owner.ContentId;
            // With a label, the button says which field it chooses for: "Choose date, Departure".
            if (picker.Owner.LabelledBy is { } label && GetAdditionalAttribute("aria-label") is null && GetAdditionalAttribute("aria-labelledby") is null)
                attrs["aria-labelledby"] = $"{picker.Owner.TriggerId} {label}";
        }
        // The trigger belongs to the open popover's layer: an Escape pressed on it closes the popover.
        if (open) attrs[BitBhKeyboard.KeysAttribute] = "Escape";
        if (disabled)
        {
            if (isButton) attrs["disabled"] = true;
            else attrs["aria-disabled"] = "true";
        }
        SetDataState(attrs, open);
        SetDataFlag(attrs, "disabled", disabled);
        return attrs;
    }

    private Task HandleClickAsync() => Picker is { IsDisabled: false } picker ? picker.ToggleAsync() : Task.CompletedTask;

    private Task HandleKeyDownAsync(KeyboardEventArgs e)
        => e.Key == "Escape" && Picker is { IsOpen: true } picker ? picker.CloseAsync() : Task.CompletedTask;
}
