namespace Bit.BlazorUI;

/// <summary>
/// The context passed to the CompactTemplate of the <see cref="BitStepper{TItem}"/>: where the process is, for the compact
/// layout to show.
/// </summary>
public class BitStepperCompactContext<TItem> where TItem : class
{
    /// <summary>
    /// Creates a new instance of <see cref="BitStepperCompactContext{TItem}"/>.
    /// </summary>
    /// <param name="index">
    /// The index of the current step, as the CurrentIndex of the stepper is.
    /// </param>
    /// <param name="count">
    /// The number of steps.
    /// </param>
    /// <param name="item">
    /// The current step, or null when the index is outside the steps.
    /// </param>
    public BitStepperCompactContext(int index, int count, TItem? item)
    {
        Index = index;
        Count = count;
        Item = item;
    }

    /// <summary>
    /// The zero-based index of the current step, as the CurrentIndex of the stepper is: it is past the last step once
    /// every step is done, and negative while none is started.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The number of steps.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// The current step, or null while <see cref="Index"/> is outside the steps.
    /// </summary>
    public TItem? Item { get; }
}
