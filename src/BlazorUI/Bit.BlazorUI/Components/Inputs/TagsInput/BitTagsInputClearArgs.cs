namespace Bit.BlazorUI;

/// <summary>
/// Arguments of the OnBeforeClear callback of <see cref="BitTagsInput"/>, describing the whole list that
/// is about to be emptied. Set <see cref="Cancel"/> to true to leave it as it is.
/// </summary>
public class BitTagsInputClearArgs
{
    /// <summary>
    /// The tags that are about to be removed.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>
    /// Set to true to cancel the clear, leaving every tag in the list.
    /// </summary>
    public bool Cancel { get; set; }
}
