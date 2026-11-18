using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The button that goes to its step (<c>aria-current="step"</c> on the current one; disabled in a linear stepper until the
/// step can be reached). Its text, or the step's <see cref="BitBhStepperTitle"/>, names the step.
/// </summary>
public class BitBhStepperTrigger : BitBhStepperStepPartBase
{
    /// <summary>The content (number, title, indicator).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Stepper?.TriggerId(StepIndex) ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", () => Stepper?.GoToAsync(StepIndex) ?? Task.CompletedTask));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Stepper is not { } stepper) return attrs;
        var disabled = !stepper.CanGoTo(StepIndex);
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = disabled ? "-1" : "0";
            attrs[BitBhKeyboard.KeysAttribute] = "Enter Space";
            if (disabled) attrs["aria-disabled"] = "true";
        }
        if (stepper.StateOf(StepIndex) == BitBhStepState.Active) attrs["aria-current"] = "step";
        attrs["data-bit-bh-interactive"] = "";
        AddStepState(attrs);
        SetDataFlag(attrs, "disabled", disabled);
        return attrs;
    }
}
