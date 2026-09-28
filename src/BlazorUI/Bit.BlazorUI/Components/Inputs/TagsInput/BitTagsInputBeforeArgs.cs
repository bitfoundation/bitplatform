namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnBeforeAdd and OnBeforeRemove callbacks of BitTagsInput.
/// Set <see cref="Cancel"/> to true to prevent the operation.
/// </summary>
public class BitTagsInputBeforeArgs
{
    /// <summary>
    /// The tag text being added or removed, after the trimming and the transformation were applied to it.
    /// <br />
    /// On an add it is also what the handler hands back: writing to it is what canonicalizes a value that
    /// only the server knows the right spelling of - the address an alias resolves to, the id a code stands
    /// for - and what is written is what the list is given, exactly as an OnEdit handler corrects a tag on
    /// its way in. Leaving it empty is not a way to call the add off, which is what <see cref="Cancel"/> is
    /// for: the tag is then added as it stood. On a remove there is nothing to rewrite, the tag having been
    /// named by the list it is being taken out of.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Set to true to cancel the add or remove operation.
    /// </summary>
    public bool Cancel { get; set; }
}
