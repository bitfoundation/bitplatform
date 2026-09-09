namespace Bit.BlazorUI;

/// <summary>
/// The GitHub Flavored Markdown bundle: the four extensions of the GFM spec - pipe tables,
/// strikethrough, task lists and autolink literals - plus the two things GitHub's own
/// renderer adds on top of it, footnotes and alerts.
/// </summary>
/// <remarks>
/// Footnotes are bundled rather than left separate because their definition syntax
/// (<c>[^1]: ...</c>) is also a valid CommonMark link reference definition. Writing a
/// footnote under a pipeline that has tables and task lists but not footnotes would quietly
/// turn the note into a link destination instead of failing visibly.
/// </remarks>
public sealed class BitMarkdownGitHubFlavoredExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.Use(new BitMarkdownPipeTableExtension())
               .Use(new BitMarkdownStrikethroughExtension())
               .Use(new BitMarkdownTaskListExtension())
               .Use(new BitMarkdownAutoLinkExtension())
               .Use(new BitMarkdownFootnoteExtension())
               .Use(new BitMarkdownAlertExtension());
    }
}
