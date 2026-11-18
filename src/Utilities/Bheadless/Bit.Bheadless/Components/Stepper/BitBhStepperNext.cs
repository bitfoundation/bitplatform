using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>Goes to the next step of a <see cref="BitBhStepper"/>; on the last step it completes the stepper (<c>OnComplete</c>).</summary>
public class BitBhStepperNext : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhStepperContext? Context { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", On<MouseEventArgs>("onclick", () => Context?.NextAsync() ?? Task.CompletedTask));
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "button";
        if (Context is { } context) SetDataFlag(attrs, "last", context.IsLast);
        return attrs;
    }
}
