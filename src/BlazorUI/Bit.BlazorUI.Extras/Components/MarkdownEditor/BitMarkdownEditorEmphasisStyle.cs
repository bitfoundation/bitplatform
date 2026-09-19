namespace Bit.BlazorUI;

/// <summary>
/// Which characters the <see cref="BitMarkdownEditor"/> emphasis commands wrap a selection in.
/// Both spellings mean the same thing to a markdown parser, so this is purely a house style.
/// </summary>
public enum BitMarkdownEditorEmphasisStyle
{
    /// <summary>
    /// <c>**bold**</c> and <c>*italic*</c>. The default.
    /// </summary>
    Asterisk,

    /// <summary>
    /// <c>__bold__</c> and <c>_italic_</c>. Note that a parser following CommonMark does not
    /// read underscores inside a word as emphasis (<c>snake_case_name</c> stays intact).
    /// </summary>
    Underscore
}
