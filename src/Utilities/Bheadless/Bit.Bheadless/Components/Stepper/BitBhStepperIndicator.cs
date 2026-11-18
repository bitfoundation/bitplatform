using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>A decorative part of a step (its number or tick, its description, the line to the next step), with the step's state hooks.</summary>
public class BitBhStepperIndicator : BitBhStepperStepPartBase
{
    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The content, rendered with the step's state (a tick once completed).</summary>
    [Parameter]
    public RenderFragment<BitBhStepState>? Template { get; set; }

    /// <summary>Hides the part from assistive technology (a number or a line the trigger already says). Default: true.</summary>
    [Parameter]
    public bool Decorative { get; set; } = true;

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, BitBhContent.Pick(Template, ChildContent, Stepper?.StateOf(StepIndex) ?? BitBhStepState.Inactive));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Decorative) attrs["aria-hidden"] = "true";
        AddStepState(attrs);
        return attrs;
    }
}
