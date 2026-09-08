namespace Bit.BlazorUI;

/// <summary>
/// Defines the capitalization of a run of text in the bit BlazorUI.
/// </summary>
/// <remarks>
/// The values are the CSS "text-transform" keywords. The transform is a purely visual one: the characters in the
/// DOM are the ones that were written, so what a screen reader announces and what a copy puts on the clipboard is
/// the original text rather than the transformed one.
/// </remarks>
public enum BitTextTransform
{
    /// <summary>
    /// The text is rendered with the capitalization it was written in.
    /// </summary>
    None,

    /// <summary>
    /// Every character is rendered in upper case.
    /// </summary>
    Uppercase,

    /// <summary>
    /// Every character is rendered in lower case.
    /// </summary>
    Lowercase,

    /// <summary>
    /// The first character of every word is rendered in upper case.
    /// </summary>
    Capitalize
}
