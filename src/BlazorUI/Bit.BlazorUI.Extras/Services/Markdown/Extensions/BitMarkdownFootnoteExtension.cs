namespace Bit.BlazorUI;

/// <summary>
/// Enables GitHub-style footnotes: a <c>[^label]</c> reference in the text and a
/// <c>[^label]: the note</c> definition anywhere in the document, collected into a numbered
/// footnotes section at the end with back-links to every citation.
/// </summary>
public sealed class BitMarkdownFootnoteExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.BlockParsers.Add(new BitMarkdownFootnoteDefinitionParser());
        builder.InlineParsers.Add(new BitMarkdownFootnoteInlineParser());
        builder.AstProcessors.Add(new BitMarkdownFootnoteAstProcessor());
        builder.Renderers.Add(new BitMarkdownFootnoteRenderer());
    }
}
