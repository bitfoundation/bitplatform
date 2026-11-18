using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>Shared behaviour of the stepper parts tied to one step.</summary>
public abstract class BitBhStepperStepPartBase : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhStepperContext? Context { get; set; }

    [CascadingParameter]
    private BitBhStepperItem? Item { get; set; }

    /// <summary>The owning stepper.</summary>
    protected BitBhStepper? Stepper => Context?.Owner;

    /// <summary>The step (from the enclosing <see cref="BitBhStepperItem"/>, unless set).</summary>
    protected virtual int StepIndex => Item?.Index ?? -1;

    /// <summary>Adds the step's state hooks.</summary>
    protected void AddStepState(Dictionary<string, object> attrs)
    {
        if (Stepper is not { } stepper) return;
        SetDataValue(attrs, "state", stepper.StateOf(StepIndex) switch
        {
            BitBhStepState.Active => "active",
            BitBhStepState.Completed => "completed",
            _ => "inactive",
        });
        SetDataValue(attrs, "orientation", stepper.Orientation.ToAttribute());
        SetDataValue(attrs, "index", StepIndex.ToString(CultureInfo.InvariantCulture));
    }
}
