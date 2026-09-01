namespace Bit.BlazorUI;

/// <summary>
/// Where the content of the <see cref="BitSeparator"/> sits along its line.
/// </summary>
public enum BitSeparatorAlignContent
{
    /// <summary>
    /// The content sits at the start of the line - the top of a vertical separator.
    /// </summary>
    Start,

    /// <summary>
    /// The content sits at the middle of the line, which is the default.
    /// </summary>
    Center,

    /// <summary>
    /// The content sits at the end of the line - the bottom of a vertical separator.
    /// </summary>
    End
}
