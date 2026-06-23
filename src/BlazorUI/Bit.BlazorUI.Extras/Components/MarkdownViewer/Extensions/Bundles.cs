namespace Bit.BlazorUI;

/// <summary>
/// The GitHub Flavored Markdown bundle: pipe tables, strikethrough, task lists and
/// autolink literals.
/// </summary>
public sealed class GitHubFlavoredExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.Use(new PipeTableExtension())
               .Use(new StrikethroughExtension())
               .Use(new TaskListExtension())
               .Use(new AutoLinkExtension());
    }
}
