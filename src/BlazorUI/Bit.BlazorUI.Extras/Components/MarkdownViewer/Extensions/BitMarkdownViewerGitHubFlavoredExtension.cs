namespace Bit.BlazorUI;

/// <summary>
/// The GitHub Flavored Markdown bundle: pipe tables, strikethrough, task lists and
/// autolink literals.
/// </summary>
public sealed class BitMarkdownViewerGitHubFlavoredExtension : IBitMarkdownViewerExtension
{
    public void Setup(BitMarkdownViewerPipelineBuilder builder)
    {
        builder.Use(new BitMarkdownViewerPipeTableExtension())
               .Use(new BitMarkdownViewerStrikethroughExtension())
               .Use(new BitMarkdownViewerTaskListExtension())
               .Use(new BitMarkdownViewerAutoLinkExtension());
    }
}
