namespace Bit.BlazorUI;

/// <summary>Enables GitHub-style task lists (<c>- [ ]</c> / <c>- [x]</c>).</summary>
public sealed class BitMarkdownViewerTaskListExtension : IBitMarkdownViewerExtension
{
    public void Setup(BitMarkdownViewerPipelineBuilder builder)
    {
        builder.AstProcessors.Add(new BitMarkdownViewerTaskListAstProcessor());
        builder.Renderers.Add(new BitMarkdownViewerTaskCheckboxRenderer());
    }
}
