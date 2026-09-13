namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnChanging callback of BitTag.
/// Set <see cref="Cancel"/> to true to keep the tag in its current selection state.
/// </summary>
public class BitTagChangeArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="BitTagChangeArgs"/>.
    /// </summary>
    /// <param name="value">
    /// The selection state the tag is about to move to.
    /// </param>
    public BitTagChangeArgs(bool value)
    {
        Value = value;
    }

    /// <summary>
    /// The selection state the tag is about to move to.
    /// </summary>
    public bool Value { get; }

    /// <summary>
    /// Set to true to cancel the change and keep the current selection state.
    /// </summary>
    public bool Cancel { get; set; }
}
