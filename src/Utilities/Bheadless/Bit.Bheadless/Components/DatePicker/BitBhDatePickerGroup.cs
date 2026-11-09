using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The group of a <see cref="BitBhDatePicker"/> around its <see cref="BitBhDateFieldSegments"/> and
/// <see cref="BitBhDatePickerTrigger"/>: <c>role="group"</c>, labelled by the field's label (or your <c>aria-label</c>)
/// and described by its descriptions. The calendar popover is anchored to it, and Alt+↓ inside it opens the popover.
///
/// <para><b>Styling hooks:</b> as the picker's root: <c>data-state</c>, <c>data-disabled</c>, <c>data-readonly</c>,
/// <c>data-invalid</c>, <c>data-required</c>, <c>data-focused</c> (focus within), <c>data-empty</c>.</para>
/// </summary>
public class BitBhDatePickerGroup : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhDatePickerContext? Picker { get; set; }

    /// <summary>The segments, the trigger and anything else.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnInitialized() => Picker?.Owner.RegisterGroup(true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Picker?.Owner.GroupId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDownAsync));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Picker is { } picker) picker.Owner.AddGroupState(attrs, AdditionalAttributes);
        else attrs["role"] = "group";
        return attrs;
    }

    private Task HandleKeyDownAsync(KeyboardEventArgs e)
        => e.AltKey && e.Key == "ArrowDown" && Picker is { } picker ? picker.OpenAsync() : Task.CompletedTask;

    public void Dispose() => Picker?.Owner.RegisterGroup(false);
}
