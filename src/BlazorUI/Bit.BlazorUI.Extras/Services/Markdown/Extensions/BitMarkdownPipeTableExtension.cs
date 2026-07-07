namespace Bit.BlazorUI;

/// <summary>Enables GitHub-style pipe tables.</summary>
public sealed class BitMarkdownPipeTableExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.BlockParsers.Add(new BitMarkdownPipeTableBlockParser());
        builder.Renderers.Add(new BitMarkdownTableRenderer());
    }
}
