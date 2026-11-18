using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The input of a <see cref="BitBhEditable"/>, shown while editing. Enter commits and Escape cancels (as the
/// editable's <c>SubmitMode</c> says); give it a name with <c>aria-label</c>.
/// </summary>
public class BitBhEditableInput : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhEditableContext? Context { get; set; }

    protected override string DefaultTag => "input";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var owner = Context?.Owner;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", owner?.InputId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (owner is not null)
        {
            builder.AddAttribute(30, "value", owner.Draft);
            builder.AddAttribute(31, "oninput", On<ChangeEventArgs>("oninput", e => owner.SetDraft(e.Value?.ToString())));
            builder.SetUpdatesAttributeName("value");
            builder.AddAttribute(32, "onkeydown", On<KeyboardEventArgs>("onkeydown", owner.HandleInputKeyDownAsync));
            builder.AddAttribute(33, "onblur", On<FocusEventArgs>("onblur", owner.HandleInputBlurAsync));
            // Escape ends the edit only: a dialog around it stays open.
            builder.AddEventStopPropagationAttribute(34, "onkeydown", owner.IsEditing);
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Context?.Owner is not { } owner) return attrs;
        if (Tag.Equals("input", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "text";
        if (!owner.IsEditing) attrs["hidden"] = true;
        if (!string.IsNullOrEmpty(owner.Placeholder)) attrs["placeholder"] = owner.Placeholder;
        if (owner.IsDisabled) attrs["disabled"] = true;
        // Enter and Escape belong to the edit, not to a form or a dialog around it.
        attrs[BitBhKeyboard.KeysAttribute] = owner.SubmitMode is BitBhEditableSubmitMode.Both or BitBhEditableSubmitMode.Enter ? "Enter Escape" : "Escape";
        owner.AddState(attrs);
        return attrs;
    }
}
