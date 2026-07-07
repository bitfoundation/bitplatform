namespace Bit.BlazorUI;

/// <summary>Enables automatic heading <c>id</c> slugs.</summary>
public sealed class BitMarkdownViewerAutoIdentifierExtension : IBitMarkdownViewerExtension
{
    public void Setup(BitMarkdownViewerPipelineBuilder builder)
        => builder.AstProcessors.Add(new BitMarkdownViewerAutoIdentifierAstProcessor());
}
