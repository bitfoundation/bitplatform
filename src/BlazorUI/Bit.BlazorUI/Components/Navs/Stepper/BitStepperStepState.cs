namespace Bit.BlazorUI;

/// <summary>
/// Where a step of a <see cref="BitStepper{TItem}"/> stands in the process.
/// </summary>
internal enum BitStepperStepState
{
    Completed,
    Current,
    Upcoming,
    Error
}
