using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>The ordered list of a <see cref="BitBhStepper"/>'s steps (<c>&lt;ol&gt;</c>).</summary>
public class BitBhStepperList : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhStepperContext? Context { get; set; }

    /// <summary>The <see cref="BitBhStepperItem"/>s.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "ol";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-label"] = Strings.Steps;
        if (Context?.Owner is { } stepper) SetDataValue(attrs, "orientation", stepper.Orientation.ToAttribute());
        return attrs;
    }
}
