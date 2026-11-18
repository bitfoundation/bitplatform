using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>The content of one step: shown while its step is current (hidden otherwise, or removed with <see cref="Unmount"/>).</summary>
public class BitBhStepperContent : BitBhStepperStepPartBase
{
    /// <summary>The step (0-based).</summary>
    [Parameter, EditorRequired]
    public int Index { get; set; }

    /// <summary>Removes the content while its step isn't current (default: kept, hidden, so its state survives).</summary>
    [Parameter]
    public bool Unmount { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override int StepIndex => Index;

    private bool IsActive => Stepper?.CurrentStep == Index;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Unmount && !IsActive) return;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Stepper?.ContentId(Index) ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        // A region needs a name: the step's title, when there is one.
        if (Stepper is { } stepper && stepper.HasTitle(Index))
        {
            attrs["role"] = "region";
            attrs["aria-labelledby"] = stepper.TitleId(Index);
        }
        if (!IsActive) attrs["hidden"] = true;
        AddStepState(attrs);
        return attrs;
    }
}
