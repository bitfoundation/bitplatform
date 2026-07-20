namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Available commands to run by a BitMarkdownEditorLegacy on its current value.
/// </summary>
public enum BitMarkdownEditorLegacyCommand
{
    /// <summary>
    /// Makes the current line a heading.
    /// </summary>
    Heading,

    /// <summary>
    /// Makes the current selection text bold.
    /// </summary>
    Bold,

    /// <summary>
    /// Makes the current selection text italic.
    /// </summary>
    Italic,

    /// <summary>
    /// Makes the current selection text a link.
    /// </summary>
    Link,

    /// <summary>
    /// Makes the current selection text an image.
    /// </summary>
    Picture,

    /// <summary>
    /// Makes the current selection text a quote message.
    /// </summary>
    Quote,

    /// <summary>
    /// Makes the current selection text a code phrase.
    /// </summary>
    Code,

    /// <summary>
    /// Makes the current selection text a code block.
    /// </summary>
    CodeBlock,
}
