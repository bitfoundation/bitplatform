namespace Bit.Bheadless;

/// <summary>Where a step of a <see cref="BitBhStepper"/> stands.</summary>
public enum BitBhStepState
{
    /// <summary>Not reached yet.</summary>
    Inactive,

    /// <summary>The current step.</summary>
    Active,

    /// <summary>Done (before the current one, or marked complete).</summary>
    Completed,
}
