using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The text input of a <see cref="BitBhTagsInput"/>. Enter or a delimiter adds what was typed; ← or Backspace
/// while it is empty moves to the last tag. Inside a <see cref="BitBhField"/> it takes the field's id, so the
/// field's label names it.
/// </summary>
public class BitBhTagsInputInput : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhTagsInputContext? Context { get; set; }

    protected override string DefaultTag => "input";

    protected override void OnParametersSet() => Context?.Owner.SetInputId(Id);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var owner = Context?.Owner;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", owner?.InputId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (owner is not null)
        {
            builder.AddAttribute(30, "value", owner.Text);
            builder.AddAttribute(31, "oninput", On<ChangeEventArgs>("oninput", e => owner.OnTextInputAsync(e.Value?.ToString())));
            builder.SetUpdatesAttributeName("value");
            builder.AddAttribute(32, "onkeydown", On<KeyboardEventArgs>("onkeydown", e => e.CtrlKey || e.MetaKey || e.AltKey || e.IsComposing ? Task.CompletedTask : owner.HandleInputKeyDownAsync(e.Key)));
            builder.AddAttribute(33, "onblur", On<FocusEventArgs>("onblur", owner.HandleInputBlurAsync));
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Context?.Owner is not { } owner) return attrs;
        if (Tag.Equals("input", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "text";
        attrs["autocomplete"] = "off";
        if (owner.IsDisabled) attrs["disabled"] = true;
        if (owner.ReadOnly) attrs["readonly"] = true;
        if (owner.IsInvalid) attrs["aria-invalid"] = "true";
        if (owner.IsRequired && owner.CurrentValues.Count == 0) attrs["aria-required"] = "true";
        if (owner.Field?.DescribedBy is { } describedBy) attrs["aria-describedby"] = describedBy;
        // Enter adds the typed text instead of submitting the form - only while there is text to add.
        if (owner.Text.Trim().Length > 0) attrs[BitBhKeyboard.KeysAttribute] = "Enter";
        SetDataFlag(attrs, "disabled", owner.IsDisabled);
        SetDataFlag(attrs, "readonly", owner.ReadOnly);
        SetDataFlag(attrs, "invalid", owner.IsInvalid);
        SetDataFlag(attrs, "max-reached", owner.IsMaxReached);
        return attrs;
    }
}
