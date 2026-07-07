namespace Bit.BlazorUI;

/// <summary>Enables GitHub-style task lists (<c>- [ ]</c> / <c>- [x]</c>).</summary>
public sealed class BitMarkdownTaskListExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.AstProcessors.Add(new BitMarkdownTaskListAstProcessor());
        builder.Renderers.Add(new BitMarkdownTaskCheckboxRenderer());
    }
}
