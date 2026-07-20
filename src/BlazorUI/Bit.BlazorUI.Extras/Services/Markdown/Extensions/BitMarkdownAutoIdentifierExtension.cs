namespace Bit.BlazorUI;

/// <summary>Enables automatic heading <c>id</c> slugs.</summary>
public sealed class BitMarkdownAutoIdentifierExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
        => builder.AstProcessors.Add(new BitMarkdownAutoIdentifierAstProcessor());
}
