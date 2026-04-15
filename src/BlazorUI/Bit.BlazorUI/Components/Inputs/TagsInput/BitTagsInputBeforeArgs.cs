namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnBeforeAdd and OnBeforeRemove callbacks of BitTagsInput.
/// Set <see cref="Cancel"/> to true to prevent the operation.
/// </summary>
public class BitTagsInputBeforeArgs
{
    /// <summary>
    /// The tag text being added or removed.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Set to true to cancel the add or remove operation.
    /// </summary>
    public bool Cancel { get; set; }
}
