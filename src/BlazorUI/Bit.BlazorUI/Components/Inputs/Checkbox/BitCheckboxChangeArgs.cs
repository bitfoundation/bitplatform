namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnChanging callback of BitCheckbox.
/// Set <see cref="Cancel"/> to true to keep the checkbox in its current state.
/// </summary>
public class BitCheckboxChangeArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="BitCheckboxChangeArgs"/>.
    /// </summary>
    /// <param name="value">
    /// The checked state the checkbox is about to move to.
    /// </param>
    /// <param name="indeterminate">
    /// The indeterminate state the checkbox is about to move to.
    /// </param>
    public BitCheckboxChangeArgs(bool value, bool indeterminate)
    {
        Value = value;
        Indeterminate = indeterminate;
    }

    /// <summary>
    /// The checked state the checkbox is about to move to.
    /// </summary>
    public bool Value { get; }

    /// <summary>
    /// The indeterminate state the checkbox is about to move to.
    /// </summary>
    public bool Indeterminate { get; }

    /// <summary>
    /// Set to true to cancel the change and keep the current state of the checkbox.
    /// </summary>
    public bool Cancel { get; set; }
}
