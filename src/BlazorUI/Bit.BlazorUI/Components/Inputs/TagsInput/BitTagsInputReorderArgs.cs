namespace Bit.BlazorUI;

/// <summary>
/// Arguments of the OnReorder callback of <see cref="BitTagsInput"/>, describing a tag that was moved
/// within the list, either by dragging it or with the keyboard.
/// </summary>
public class BitTagsInputReorderArgs
{
    /// <summary>
    /// The tag that was moved.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// The zero based position the tag was moved from.
    /// </summary>
    public int OldIndex { get; set; }

    /// <summary>
    /// The zero based position the tag was moved to.
    /// </summary>
    public int NewIndex { get; set; }
}
