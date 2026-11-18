namespace Bit.Bheadless;

/// <summary>The state of a <see cref="BitBhStepper"/>, cascaded to its parts and given to its <c>Template</c>.</summary>
public sealed class BitBhStepperContext
{
    internal BitBhStepperContext(BitBhStepper owner) => Owner = owner;

    internal BitBhStepper Owner { get; }

    /// <summary>The current step (0-based).</summary>
    public int Step => Owner.CurrentStep;

    /// <summary>The number of steps.</summary>
    public int Count => Owner.Count;

    /// <summary>Whether the current step is the first.</summary>
    public bool IsFirst => Owner.CurrentStep == 0;

    /// <summary>Whether the current step is the last.</summary>
    public bool IsLast => Owner.CurrentStep >= Owner.Count - 1;

    /// <summary>The state of a step.</summary>
    public BitBhStepState StateOf(int index) => Owner.StateOf(index);

    /// <summary>Goes to the next step (or completes on the last).</summary>
    public Task NextAsync() => Owner.NextAsync();

    /// <summary>Goes to the previous step.</summary>
    public Task PreviousAsync() => Owner.PreviousAsync();

    /// <summary>Goes to a step.</summary>
    public Task GoToAsync(int index) => Owner.GoToAsync(index);
}
