namespace Bit.BlazorUI;

/// <summary>
/// The GitHub Flavored Markdown bundle: pipe tables, strikethrough, task lists and
/// autolink literals.
/// </summary>
public sealed class BitMarkdownGitHubFlavoredExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.Use(new BitMarkdownPipeTableExtension())
               .Use(new BitMarkdownStrikethroughExtension())
               .Use(new BitMarkdownTaskListExtension())
               .Use(new BitMarkdownAutoLinkExtension());
    }
}
