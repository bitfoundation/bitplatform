namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnChanging callback of BitToggleButton.
/// Set <see cref="Cancel"/> to true to keep the toggle button in its current state.
/// </summary>
public class BitToggleButtonChangeArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="BitToggleButtonChangeArgs"/>.
    /// </summary>
    /// <param name="value">
    /// The checked state the toggle button is about to move to.
    /// </param>
    public BitToggleButtonChangeArgs(bool value)
    {
        Value = value;
    }

    /// <summary>
    /// The checked state the toggle button is about to move to.
    /// </summary>
    public bool Value { get; }

    /// <summary>
    /// Set to true to cancel the change and keep the current checked state.
    /// </summary>
    public bool Cancel { get; set; }
}
