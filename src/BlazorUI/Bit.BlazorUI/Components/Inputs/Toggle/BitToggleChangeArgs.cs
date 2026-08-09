namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnChanging callback of BitToggle.
/// Set <see cref="Cancel"/> to true to keep the toggle in its current state.
/// </summary>
public class BitToggleChangeArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="BitToggleChangeArgs"/>.
    /// </summary>
    /// <param name="value">
    /// The state the toggle is about to move to.
    /// </param>
    public BitToggleChangeArgs(bool value)
    {
        Value = value;
    }

    /// <summary>
    /// The state the toggle is about to move to.
    /// </summary>
    public bool Value { get; }

    /// <summary>
    /// Set to true to cancel the change and keep the current state of the toggle.
    /// </summary>
    public bool Cancel { get; set; }
}
