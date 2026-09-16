namespace Bit.BlazorUI;

/// <summary>
/// Enables YAML (<c>---</c>) and TOML (<c>+++</c>) front matter: a metadata block at the very top
/// of the document is parsed into a <see cref="BitMarkdownFrontMatterNode"/> and rendered as
/// nothing, instead of showing up as a thematic break followed by a heading.
/// </summary>
public sealed class BitMarkdownFrontMatterExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.BlockParsers.Add(new BitMarkdownFrontMatterBlockParser());
        builder.Renderers.Add(new BitMarkdownFrontMatterRenderer());
    }
}
