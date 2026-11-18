using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>The title of a step: names the step's content.</summary>
public class BitBhStepperTitle : BitBhStepperStepPartBase, IDisposable
{
    private int _registered = -1;

    protected override void OnParametersSet()
    {
        if (_registered == StepIndex || Stepper is null) return;
        if (_registered >= 0) Stepper.RegisterTitle(_registered, false);
        _registered = StepIndex;
        if (_registered >= 0) Stepper.RegisterTitle(_registered, true);
    }

    public void Dispose()
    {
        if (_registered >= 0) Stepper?.RegisterTitle(_registered, false);
    }

    /// <summary>The title.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Stepper?.TitleId(StepIndex) ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        AddStepState(attrs);
        return attrs;
    }
}
