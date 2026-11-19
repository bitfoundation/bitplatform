using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>One step of a <see cref="BitBhStepper"/> (<c>&lt;li&gt;</c>): holds its trigger, indicator, title, description and separator.</summary>
public class BitBhStepperItem : BitBhStepperStepPartBase
{
    /// <summary>The step (0-based).</summary>
    [Parameter, EditorRequired]
    public int Index { get; set; }

    /// <summary>The step's parts.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The step's parts, rendered with its state.</summary>
    [Parameter]
    public RenderFragment<BitBhStepState>? Template { get; set; }

    protected override string DefaultTag => "li";

    protected override int StepIndex => Index;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhStepperItem>>(30);
        builder.AddComponentParameter(31, "Value", this);
        builder.AddComponentParameter(32, "IsFixed", true);
        builder.AddComponentParameter(33, "ChildContent", BitBhContent.Pick(Template, ChildContent, Stepper?.StateOf(Index) ?? BitBhStepState.Inactive));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        AddStepState(attrs);
        return attrs;
    }
}
