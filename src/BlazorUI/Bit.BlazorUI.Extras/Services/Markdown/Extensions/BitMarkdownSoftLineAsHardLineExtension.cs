namespace Bit.BlazorUI;

/// <summary>
/// Promotes every soft line break to a hard one, so a single newline inside a paragraph
/// renders as <c>&lt;br /&gt;</c>. This is the behavior chat, comment and issue UIs expect,
/// where authors do not think in terms of Markdown's paragraph reflow - the equivalent of
/// the <c>breaks</c> option in marked and markdown-it.
/// </summary>
public sealed class BitMarkdownSoftLineAsHardLineExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
        => builder.AstProcessors.Add(new BitMarkdownSoftLineAsHardLineAstProcessor());
}

/// <summary>Marks every <see cref="BitMarkdownLineBreakNode"/> in the tree as a hard break.</summary>
public sealed class BitMarkdownSoftLineAsHardLineAstProcessor : BitMarkdownAstProcessor
{
    public override void Process(BitMarkdownDocumentNode document, BitMarkdownPipeline pipeline)
    {
        foreach (var lineBreak in BitMarkdownAstHelper.Descendants(document).OfType<BitMarkdownLineBreakNode>())
        {
            lineBreak.Hard = true;
        }
    }
}
