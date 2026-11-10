using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The text input of a <see cref="BitBhNumberField{TValue}"/> (<c>role="spinbutton"</c>, with
/// <c>aria-valuenow</c> / <c>aria-valuetext</c> / <c>aria-valuemin</c> / <c>aria-valuemax</c>). What is typed is
/// kept as typed until the input is left or Enter is pressed, then parsed, clamped and shown formatted;
/// text that isn't a number reverts. Characters that can't be part of a number are refused as they are typed.
/// Inside a <see cref="BitBhField"/> it takes the field's id, so the field's label names it.
/// </summary>
public class BitBhNumberFieldInput : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhNumberFieldContext? FieldContext { get; set; }

    protected override string DefaultTag => "input";

    // An id given to the input is the field's input id: the buttons' aria-controls and a <label for> follow it.
    protected override void OnParametersSet() => FieldContext?.SetInputId(Id);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", FieldContext?.InputId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (FieldContext is { } field)
        {
            builder.AddAttribute(30, "value", field.DisplayText);
            builder.AddAttribute(31, "oninput", On<ChangeEventArgs>("oninput", e => field.OnTextInputAsync(e.Value?.ToString())));
            // The renderer takes the typed text as already shown, so a slow round trip never puts back older text.
            builder.SetUpdatesAttributeName("value");
            builder.AddAttribute(32, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDownAsync));
            builder.AddAttribute(33, "onfocus", On<FocusEventArgs>("onfocus", () => field.SetFocused(true)));
            builder.AddAttribute(34, "onblur", On<FocusEventArgs>("onblur", async () =>
            {
                field.SetFocused(false);
                await field.CommitTextAsync();
            }));
            builder.AddAttribute(35, "onwheel", On<WheelEventArgs>("onwheel", HandleWheelAsync));
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (FieldContext is not { } field) return attrs;

        if (Tag.Equals("input", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "text";
        attrs["role"] = "spinbutton";
        attrs["inputmode"] = field.InputMode;
        attrs["autocomplete"] = "off";
        attrs["autocorrect"] = "off";
        attrs["spellcheck"] = "false";
        if (field.NumericValue is { } value)
        {
            attrs["aria-valuenow"] = value.ToString(CultureInfo.InvariantCulture);
            if (field.ValueText is { } text) attrs["aria-valuetext"] = text;
        }
        if (field.MinValue is { } min) attrs["aria-valuemin"] = min.ToString(CultureInfo.InvariantCulture);
        if (field.MaxValue is { } max) attrs["aria-valuemax"] = max.ToString(CultureInfo.InvariantCulture);
        if (field.IsDisabled) attrs["disabled"] = true;
        if (field.IsReadOnly) attrs["readonly"] = true;
        if (field.IsRequired) attrs["required"] = true;
        if (field.IsInvalid) attrs["aria-invalid"] = "true";
        if (field.Field?.DescribedBy is { } describedBy) attrs["aria-describedby"] = describedBy;

        attrs[BitBhKeyboard.KeysAttribute] = field.HasHomeEnd
            ? "ArrowUp ArrowDown Alt+ArrowUp Alt+ArrowDown PageUp PageDown Home End"
            : "ArrowUp ArrowDown Alt+ArrowUp Alt+ArrowDown PageUp PageDown";
        // The runtime refuses typed characters that can't be part of a number, and keeps the page from
        // scrolling under a wheel that steps the focused input.
        attrs["data-bit-bh-chars"] = field.AllowedCharacters;
        if (field.AllowWheelScrub && !field.IsDisabled && !field.IsReadOnly) attrs["data-bit-bh-wheel"] = "";

        SetDataFlag(attrs, "disabled", field.IsDisabled);
        SetDataFlag(attrs, "readonly", field.IsReadOnly);
        SetDataFlag(attrs, "invalid", field.IsInvalid);
        SetDataFlag(attrs, "required", field.IsRequired);
        SetDataFlag(attrs, "scrubbing", field.IsScrubbing);
        return attrs;
    }

    private Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (FieldContext is not { } field || e.CtrlKey || e.MetaKey) return Task.CompletedTask;
        var size = e.ShiftKey ? BitBhStepSize.Large : e.AltKey ? BitBhStepSize.Small : BitBhStepSize.Normal;
        return e.Key switch
        {
            "ArrowUp" => field.StepAsync(1, size, commit: true),
            "ArrowDown" => field.StepAsync(-1, size, commit: true),
            "PageUp" => field.StepAsync(1, BitBhStepSize.Large, commit: true),
            "PageDown" => field.StepAsync(-1, BitBhStepSize.Large, commit: true),
            "Home" when field.MinValue is not null => field.SetToBoundAsync(max: false),
            "End" when field.MaxValue is not null => field.SetToBoundAsync(max: true),
            "Enter" => field.CommitTextAsync(),
            _ => Task.CompletedTask,
        };
    }

    private Task HandleWheelAsync(WheelEventArgs e)
    {
        if (FieldContext is not { AllowWheelScrub: true } field || e.CtrlKey || e.DeltaY == 0) return Task.CompletedTask;
        // Only while focused: the runtime stops the page scrolling under a focused input only.
        return field.IsFocused ? field.StepAsync(e.DeltaY < 0 ? 1 : -1, e.ShiftKey ? BitBhStepSize.Large : BitBhStepSize.Normal, commit: true) : Task.CompletedTask;
    }
}
